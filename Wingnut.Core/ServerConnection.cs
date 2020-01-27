﻿namespace Wingnut.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Wingnut.Data;
    using Wingnut.Data.Models;
    using Wingnut.Tracing;

    public class ServerConnection
    {
        private readonly char[] tokenSplitChar = { ' ' };
        private readonly char[] tokenTrimChar = { '\"' };

        private readonly ServerContext serverContext;
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private SslStream sslStream;

        private Stream ActiveStream => this.sslStream ?? (Stream)this.networkStream;

        public ServerConnection(ServerContext serverContext)
        {
            this.serverContext = serverContext;
        }

        public async Task ConnectAsync(
            CancellationToken cancellationToken)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(this.serverContext.ServerState.Address);

            var addresses = hostEntry.AddressList.ToList();

            if (this.serverContext.ServerState.PreferredAddressFamily.HasValue)
            {
                addresses = hostEntry.AddressList
                    .Where(a => a.AddressFamily == this.serverContext.ServerState.PreferredAddressFamily)
                    .ToList();
            }

            Exception lastException = null;

            foreach (IPAddress address in addresses)
            {
                try
                {
                    await this.ConnectWithAddress(address, cancellationToken)
                        .ConfigureAwait(false);

                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    Logger.Error($"Failed to connect to server. The exception was: {ex}");

                    try
                    {
                        if (this.tcpClient != null && this.tcpClient.Connected)
                        {
                            this.tcpClient.Close();
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.Debug(
                            "Caught and suppressed exception will closing TcpClient connection: {0}",
                            exception);
                    }

                    this.sslStream = null;
                    this.tcpClient = null;
                }
            }

            if (this.tcpClient == null)
            {
                throw new WingnutException(
                    $"Failed to connect to server. The list of addresses attempted servers is {string.Join(",", addresses)}",
                    lastException);
            }

            this.serverContext.ServerState.LastConnectionTime = DateTime.UtcNow;
        }

        private async Task ConnectWithAddress(
            IPAddress address,
            CancellationToken cancellationToken)
        {
            this.tcpClient = new TcpClient();

            Logger.Info(
                "Connecting to server at {0}", 
                new IPEndPoint(address, this.serverContext.ServerState.Port));

            await this.tcpClient.ConnectAsync(address, this.serverContext.ServerState.Port).ConfigureAwait(false);

            this.tcpClient.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.KeepAlive,
                true);

            //this.tcpClient.Client.IOControl(IOControlCode.KeepAliveValues)

            Logger.Info("Connection succceded");

            this.networkStream = this.tcpClient.GetStream();
            string response;

            if (this.serverContext.ServerState.UseSSL == SSLUsage.Optional ||
                this.serverContext.ServerState.UseSSL == SSLUsage.Required)
            {
                Logger.Info("Attempting to upgrade to SSL");

                response = await this.StartTLSAsync(cancellationToken).ConfigureAwait(false);

                if (response == null)
                {
                    // We can upgrade to TLS
                    this.sslStream = new SslStream(
                        this.networkStream,
                        false,
                        SslCertificateValidationCallback);

                    await this.sslStream
                        .AuthenticateAsClientAsync(this.serverContext.configuration.SSLTargetName)
                        .ConfigureAwait(false);

                    Logger.Info("Connection successfully upgraded to SSL");
                }
                else
                {
                    if (this.serverContext.ServerState.UseSSL == SSLUsage.Required)
                    {
                        // SSL is required per the configuration, but the server did 
                        // not support it, so throw an exception.
                        throw new WingnutException(
                            "SSL was required, but the server did not support it");
                    }

                    Logger.Info(
                        "SSL was requested, but the server did not support it. Falling back to non-SSL communication.");
                }
            }

            response = await this.UsernameAsync(
                    this.serverContext.ServerState.Username,
                    cancellationToken)
                .ConfigureAwait(false);

            if (response != null)
            {
                throw new WingnutException(
                    $"Failed to set username on server to '{this.serverContext.ServerState.Username}'. The response was: {response}");
            }

            response = await this.PasswordAsync(
                    this.serverContext.ServerState.Password,
                    cancellationToken)
                .ConfigureAwait(false);

            if (response != null)
            {
                throw new WingnutException($"Failed to set password on server. The response was: {response}");
            }

            Logger.Info("Successfully authenticated to server");
        }

        private static bool SslCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            // TODO
            return true;
        }

        public async Task<string> GetAsync(
            string request, 
            CancellationToken cancellationToken)
        {
            await this.WriteToStreamAsync(
                    $"GET {request}",
                    cancellationToken)
                .ConfigureAwait(false);

            string responseLine = 
                await this.ReadFromStreamAsync(cancellationToken).ConfigureAwait(false);

            ValidateResponse(responseLine, request);

            if (!responseLine.StartsWith(request))
            {
                throw new Exception("Bad response?");
            }

            return responseLine;
        }

        private void ValidateResponse(string responseLine, string request)
        {
            if (responseLine.StartsWith("ERR"))
            {
                var tokens = responseLine.Split(this.tokenSplitChar, 2);
                throw new WingnutException(
                    $"The server responded with error: {tokens[1]}. The request was '{request}'.");
            }
        }

        public async Task<Dictionary<string, string>> ListVarsAsync(
            string request,
            CancellationToken cancellationToken)
        {
            List<string> listResponse = 
                await this.ListAsync($"VAR {request}", cancellationToken).ConfigureAwait(false);

            Dictionary<string, string> varDictionary = new Dictionary<string, string>();

            foreach (string varLine in listResponse)
            {
                // Each line will have the format: <upsname> <varname> "<value>"
                // For example: su700 ups.mfr "APC"
                // Split the line into 3 tokens
                string[] lineTokens = varLine.Split(this.tokenSplitChar, 3);

                varDictionary.Add(
                    lineTokens[1],
                    lineTokens[2].Trim(this.tokenTrimChar));
            }

            return varDictionary;
        }

        public async Task<Dictionary<string, string>> ListUpsAsync(
            CancellationToken cancellationToken)
        {
            List<string> listResponse = 
                await this.ListAsync("UPS", cancellationToken).ConfigureAwait(false);

            Dictionary<string, string> varDictionary = new Dictionary<string, string>();

            foreach (string varLine in listResponse)
            {
                // Each line will have the format: <upsname> "<description>"
                // For example: su700 "Development box"
                // Split the line into 2 tokens
                string[] lineTokens = varLine.Split(this.tokenSplitChar, 2);

                varDictionary.Add(
                    lineTokens[0],
                    lineTokens[1].Trim(this.tokenTrimChar));
            }

            return varDictionary;
        }

        public async Task<List<string>> ListAsync(
            string request, 
            CancellationToken cancellationToken)
        {
            await this.WriteToStreamAsync($"LIST {request}", cancellationToken).ConfigureAwait(false);

            List<string> list = null;
            string prependNextResponse = null;

            while (true)
            {
                string readResponse =
                    await this.ReadFromStreamAsync(cancellationToken).ConfigureAwait(false);

                if (prependNextResponse != null)
                {
                    readResponse = prependNextResponse + readResponse;
                }

                string[] responseLines = readResponse.Split('\n');
                for (int i = 0; i < responseLines.Length; i++)
                {
                    string responseLine = responseLines[i];
                    ValidateResponse(responseLine, request);

                    if (responseLine.StartsWith($"BEGIN LIST {request}"))
                    {
                        // The list is being started. This should be the first list from the server,
                        // so verify that we haven't start receiving list items yet.
                        if (list != null)
                        {
                            throw new Exception("Bad response?");
                        }

                        list = new List<string>();
                    }
                    else if (responseLine.StartsWith($"END LIST {request}"))
                    {
                        return list;
                    }
                    else
                    {
                        if (list == null)
                        {
                            throw new Exception("Received list item before start of list?");
                        }

                        if (i == responseLines.Length - 1)
                        {
                            // There will be another payload to continue this
                            prependNextResponse = responseLine;
                        }
                        else
                        {
                            string[] lineTokens = responseLine.Split(this.tokenSplitChar, 2);

                            list.Add(lineTokens[1]);
                        }
                    }
                }
            }
        }

        private async Task<string> StartTLSAsync(
            CancellationToken cancellationToken)
        {
            await this.WriteToStreamAsync(
                    "STARTTLS",
                    cancellationToken)
                .ConfigureAwait(false);

            string responseLine =
                await this.ReadFromStreamAsync(cancellationToken).ConfigureAwait(false);

            ValidateResponse(responseLine, "STARTTLS");

            if (!responseLine.StartsWith("OK"))
            {
                return responseLine;
            }

            return null;
        }

        private async Task<string> UsernameAsync(
            string username,
            CancellationToken cancellationToken)
        {
            await this.WriteToStreamAsync(
                    $"USERNAME {username}",
                    cancellationToken)
                .ConfigureAwait(false);

            string responseLine =
                await this.ReadFromStreamAsync(cancellationToken).ConfigureAwait(false);

            ValidateResponse(responseLine, $"USERNAME {username}");

            if (!responseLine.StartsWith("OK"))
            {
                return responseLine;
            }

            return null;
        }

        private async Task<string> PasswordAsync(
            SecureString password,
            CancellationToken cancellationToken)
        {
            await this.WriteToStreamAsync(
                    $"PASSWORD {password.GetDecrypted()}",
                    cancellationToken)
                .ConfigureAwait(false);

            string responseLine =
                await this.ReadFromStreamAsync(cancellationToken).ConfigureAwait(false);

            ValidateResponse(responseLine, "PASSWORD <password>}");

            if (!responseLine.StartsWith("OK"))
            {
                return responseLine;
            }

            return null;
        }

        private async Task WriteToStreamAsync(
            string request, 
            CancellationToken cancellationToken)
        {
            if (!request.EndsWith("\n"))
            {
                request += '\n';
            }

            byte[] reqBytes = Encoding.ASCII.GetBytes(request);

            try
            {
                // See SO post here: https://stackoverflow.com/a/15067180/7852297
                if (this.tcpClient.Client.Poll(1, SelectMode.SelectRead) &&
                    !this.networkStream.DataAvailable)
                {
                    throw new Exception("Server disconnected. Handle this properly.");
                }

                await this.ActiveStream.WriteAsync(reqBytes, 0, reqBytes.Length, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw new NutCommunicationException(
                    "An error occurred while communicating with the server",
                    exception);
            }
        }

        private async Task<string> ReadFromStreamAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesReceived =
                    await this.ActiveStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                        .ConfigureAwait(false);

                var str = Encoding.ASCII.GetString(buffer, 0, bytesReceived).Trim();
                Console.WriteLine("ReadFromStreamAsync => '{0}'", str);
                return str;

            }
            catch (Exception exception)
            {
                throw new NutCommunicationException(
                    "An error occurred while communicating with the server",
                    exception);
            }
        }
    }
}