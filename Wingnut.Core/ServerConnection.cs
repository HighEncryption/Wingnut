namespace Wingnut.Core
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
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using Wingnut.Data;
    using Wingnut.Data.Models;
    using Wingnut.Tracing;

    public class ServerConnection
    {
        private readonly char[] tokenSplitChar = { ' ' };
        private readonly char[] tokenTrimChar = { '\"' };

        private readonly Server serverState;
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private SslStream sslStream;
        private readonly AsyncLock asyncLock;

        private Stream ActiveStream => this.sslStream ?? (Stream)this.networkStream;

        public ServerConnection(Server serverState)
        {
            this.serverState = serverState;
            this.asyncLock = new AsyncLock();
        }

        public async Task ConnectAsync(
            CancellationToken cancellationToken)
        {
            List<IPAddress> addresses = new List<IPAddress>();

            if (IPAddress.TryParse(this.serverState.Address, out IPAddress ipAddress))
            {
                addresses.Add(ipAddress);
            }
            else
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(this.serverState.Address);
                addresses.AddRange(hostEntry.AddressList);
            }

            if (this.serverState.PreferredAddressFamily.HasValue)
            {
                addresses = addresses
                    .Where(a => a.AddressFamily == this.serverState.PreferredAddressFamily)
                    .ToList();
            }

            Exception lastException = null;

            foreach (IPAddress address in addresses)
            {
                IPEndPoint endpoint = new IPEndPoint(address, this.serverState.Port);
                try
                {
                    await this.ConnectWithAddress(
                            endpoint,
                            this.serverState.Address,
                            cancellationToken)
                        .ConfigureAwait(false);

                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    Logger.Error($"Failed to connect to server with endpoint '{endpoint}'. The error was: {ex.Message}");
                    Logger.Debug($"Failed to connect to server. Exception: {ex}");

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

            this.serverState.LastConnectionTime = DateTime.UtcNow;
        }

        public void Disconnect()
        {
            this.tcpClient.Close();
        }

        private async Task ConnectWithAddress(
            IPEndPoint endpoint,
            string originalAddress,
            CancellationToken cancellationToken)
        {
            this.tcpClient = new TcpClient();

            Logger.Info("Connecting to server at {0}", endpoint);

            await this.tcpClient.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);

            Logger.Info("Connection succceded");

            this.networkStream = this.tcpClient.GetStream();
            string response;

            if (this.serverState.UseSSL == SSLUsage.Optional ||
                this.serverState.UseSSL == SSLUsage.Required)
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

                    string targetHost = this.serverState.ServerSSLName ?? originalAddress;

                    await this.sslStream
                        .AuthenticateAsClientAsync(targetHost)
                        .ConfigureAwait(false);

                    Logger.Info("Connection successfully upgraded to SSL");
                }
                else
                {
                    if (this.serverState.UseSSL == SSLUsage.Required)
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
                    this.serverState.Username,
                    cancellationToken)
                .ConfigureAwait(false);

            if (response != null)
            {
                throw new WingnutException(
                    $"Failed to set username on server to '{this.serverState.Username}'. The response was: {response}");
            }

            response = await this.PasswordAsync(
                    this.serverState.Password,
                    cancellationToken)
                .ConfigureAwait(false);

            if (response != null)
            {
                throw new WingnutException($"Failed to set password on server. The response was: {response}");
            }

            Logger.Info("Successfully authenticated to server");
        }

        private static bool SslCertificateValidationCallback(
            object sender, 
            X509Certificate certificate, 
            X509Chain chain, 
            SslPolicyErrors sslPolicyErrors)
        {
            // TODO
            return true;
        }

        public async Task<string> GetAsync(
            string request, 
            CancellationToken cancellationToken)
        {
            using (this.asyncLock.Lock())
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
                await this.ListWith4TokensAsync($"VAR {request}", cancellationToken).ConfigureAwait(false);

            Dictionary<string, string> varDictionary = new Dictionary<string, string>();

            foreach (string varLine in listResponse)
            {
                // Each line will have the format: <upsname> <varname> "<value>"
                // For example: su700 ups.mfr "APC"
                // Split the line into 3 tokens
                string[] lineTokens = varLine.Split(this.tokenSplitChar, 4);

                varDictionary.Add(
                    lineTokens[2],
                    lineTokens[3].Trim(this.tokenTrimChar));
            }

            return varDictionary;
        }

        public async Task<Dictionary<string, string>> ListRwAsync(
            string request,
            CancellationToken cancellationToken)
        {
            List<string> listResponse = 
                await this.ListWith4TokensAsync($"RW {request}", cancellationToken).ConfigureAwait(false);

            Dictionary<string, string> varDictionary = new Dictionary<string, string>();

            foreach (string varLine in listResponse)
            {
                // Each line will have the format: <upsname> <varname> "<value>"
                // For example: su700 ups.mfr "APC"
                // Split the line into 3 tokens
                string[] lineTokens = varLine.Split(this.tokenSplitChar, 4);

                varDictionary.Add(
                    lineTokens[2],
                    lineTokens[3].Trim(this.tokenTrimChar));
            }

            return varDictionary;
        }

        public async Task<List<string>> ListCmdAsync(
            string request,
            CancellationToken cancellationToken)
        {
            List<string> listResponse = 
                await this.ListWith3TokensAsync($"CMD {request}", cancellationToken).ConfigureAwait(false);

            List<string> cmdList = new List<string>();

            foreach (string varLine in listResponse)
            {
                // Each line will have the format: <upsname> <cmdname>
                // For example: su700 load.on
                // Split the line into 3 tokens
                string[] lineTokens = varLine.Split(this.tokenSplitChar, 3);
                cmdList.Add(lineTokens[2]);
            }

            return cmdList;
        }

        public async Task<Dictionary<string, string>> ListEnumAsync(
            string request,
            CancellationToken cancellationToken)
        {
            List<string> listResponse =
                await this.ListWith4TokensAsync($"ENUM {request}", cancellationToken).ConfigureAwait(false);

            Dictionary<string, string> varDictionary = new Dictionary<string, string>();

            foreach (string varLine in listResponse)
            {
                // Each line will have the format: <upsname> <varname> "<value>"
                // For example: su700 input.transfer.low "103"
                // Split the line into 3 tokens
                string[] lineTokens = varLine.Split(this.tokenSplitChar, 4);

                varDictionary.Add(
                    lineTokens[2],
                    lineTokens[3].Trim(this.tokenTrimChar));
            }

            return varDictionary;
        }

        public async Task<Dictionary<string, string>> ListRangeAsync(
            string request,
            CancellationToken cancellationToken)
        {
            List<string> listResponse =
                await this.ListWith5TokensAsync($"RANGE {request}", cancellationToken).ConfigureAwait(false);

            Dictionary<string, string> varDictionary = new Dictionary<string, string>();

            foreach (string varLine in listResponse)
            {
                // Each line will have the format: <upsname> <varname> "<value>"
                // For example: su700 input.transfer.low "103"
                // Split the line into 3 tokens
                string[] lineTokens = varLine.Split(this.tokenSplitChar, 4);

                varDictionary.Add(
                    lineTokens[2],
                    lineTokens[3].Trim(this.tokenTrimChar));
            }

            return varDictionary;
        }

        public async Task<Dictionary<string, string>> ListUpsAsync(
            CancellationToken cancellationToken)
        {
            List<string> listResponse = 
                await this.ListWith3TokensAsync("UPS", cancellationToken).ConfigureAwait(false);

            Dictionary<string, string> varDictionary = new Dictionary<string, string>();

            foreach (string varLine in listResponse)
            {
                // Each line will have the format: <upsname> "<description>"
                // For example: su700 "Development box"
                // Split the line into 2 tokens
                string[] lineTokens = varLine.Split(this.tokenSplitChar, 3);

                varDictionary.Add(
                    lineTokens[1],
                    lineTokens[2].Trim(this.tokenTrimChar));
            }

            return varDictionary;
        }

        public async Task<List<string>> ListWith3TokensAsync(
            string request,
            CancellationToken cancellationToken)
        {
            {
                await this.WriteToStreamAsync($"LIST {request}", cancellationToken).ConfigureAwait(false);

                StringBuilder stringBuilder = new StringBuilder();

                while (true)
                {
                    string readResponse =
                        await this.ReadFromStreamAsync(cancellationToken).ConfigureAwait(false);

                    ValidateResponse(readResponse, request);

                    stringBuilder.Append(readResponse);

                    if (stringBuilder.ToString().EndsWith("END LIST " + request))
                    {
                        break;
                    }
                }

                string strResponse = stringBuilder.ToString();

                string prefix = $"BEGIN LIST {request}";
                string postfix = $"END LIST {request}";
                string innerResponse =
                    strResponse.Substring(
                        prefix.Length,
                        strResponse.Length - (prefix.Length + postfix.Length));

                Regex regex = new Regex(@"(\w+ \w+ "".*?"")");
                MatchCollection matches = regex.Matches(innerResponse);

                List<string> list = new List<string>();
                foreach (Match match in matches)
                {
                    list.Add(match.Value);
                }

                return list;
            }
        }

        public async Task<List<string>> ListWith4TokensAsync(
            string request,
            CancellationToken cancellationToken)
        {
            {
                await this.WriteToStreamAsync($"LIST {request}", cancellationToken).ConfigureAwait(false);

                StringBuilder stringBuilder = new StringBuilder();

                while (true)
                {
                    string readResponse =
                        await this.ReadFromStreamAsync(cancellationToken).ConfigureAwait(false);

                    ValidateResponse(readResponse, request);

                    stringBuilder.Append(readResponse);

                    if (stringBuilder.ToString().EndsWith("END LIST " + request))
                    {
                        break;
                    }
                }

                string strResponse = stringBuilder.ToString();

                string prefix = $"BEGIN LIST {request}";
                string postfix = $"END LIST {request}";
                string innerResponse =
                    strResponse.Substring(
                        prefix.Length,
                        strResponse.Length - (prefix.Length + postfix.Length));

                Regex regex = new Regex(@"(\w+ \w+ \S+ "".*?"")");
                MatchCollection matches = regex.Matches(innerResponse);

                List<string> list = new List<string>();
                foreach (Match match in matches)
                {
                    list.Add(match.Value);
                }

                return list;
            }
        }

        public async Task<List<string>> ListWith5TokensAsync(
            string request,
            CancellationToken cancellationToken)
        {
            {
                await this.WriteToStreamAsync($"LIST {request}", cancellationToken).ConfigureAwait(false);

                StringBuilder stringBuilder = new StringBuilder();

                while (true)
                {
                    string readResponse =
                        await this.ReadFromStreamAsync(cancellationToken).ConfigureAwait(false);

                    ValidateResponse(readResponse, request);

                    stringBuilder.Append(readResponse);

                    if (stringBuilder.ToString().EndsWith("END LIST " + request))
                    {
                        break;
                    }
                }

                string strResponse = stringBuilder.ToString();

                string prefix = $"BEGIN LIST {request}";
                string postfix = $"END LIST {request}";
                string innerResponse =
                    strResponse.Substring(
                        prefix.Length,
                        strResponse.Length - (prefix.Length + postfix.Length));

                Regex regex = new Regex(@"(\w+ \w+ \S+ "".*?"" "".*?"")");
                MatchCollection matches = regex.Matches(innerResponse);

                List<string> list = new List<string>();
                foreach (Match match in matches)
                {
                    list.Add(match.Value);
                }

                return list;
            }
        }

        private async Task<string> StartTLSAsync(
            CancellationToken cancellationToken)
        {
            using (this.asyncLock.Lock())
            {
                await this.WriteToStreamAsync(
                        "STARTTLS",
                        cancellationToken)
                    .ConfigureAwait(false);

                string responseLine =
                    await this.ReadFromStreamAsync(cancellationToken).ConfigureAwait(false);

                if (responseLine.StartsWith("OK"))
                {
                    return null;
                }

                if (responseLine.Contains("FEATURE-NOT-SUPPORTED"))
                {
                    return responseLine;
                }

                ValidateResponse(responseLine, "STARTTLS");

                return null;
            }
        }

        private async Task<string> UsernameAsync(
            string username,
            CancellationToken cancellationToken)
        {
            using (this.asyncLock.Lock())
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
        }

        private async Task<string> PasswordAsync(
            SecureString password,
            CancellationToken cancellationToken)
        {
            using (this.asyncLock.Lock())
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
                    throw new Exception("Server disconnected.");
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
                byte[] buffer = new byte[10240];
                int bytesReceived =
                    await this.ActiveStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                        .ConfigureAwait(false);

                var str = Encoding.ASCII.GetString(buffer, 0, bytesReceived).Trim();
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