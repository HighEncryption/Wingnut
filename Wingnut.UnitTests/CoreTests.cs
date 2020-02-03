using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Wingnut.UnitTests
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Wingnut.Core;
    using Wingnut.Data;
    using Wingnut.Data.Configuration;

    [TestClass]
    public class CoreTests
    {
        [TestMethod]
        public void TestConnectWithoutSSL()
        {
            ServiceRuntime.Instance.Configuration = 
                ServiceHelper.CreateBasicConfiguration(11001);

            SimpleServer server = SimpleServer.StartNew(11001);

            ManualResetEventSlim waitEvent = new ManualResetEventSlim(false);

            server.HandleRequests(
                () =>
                {
                    server.HandleBasicAuthentication();

                    // All done
                    waitEvent.Set();
                });

            ServiceRuntime.Instance.Start();

            waitEvent.Wait();
        }

        [TestMethod]
        public void TestOnBatteryNotify()
        {
            ServiceRuntime.Instance.Configuration = 
                ServiceHelper.CreateBasicConfiguration(11002);

            // Poll quickly!
            ServiceRuntime.Instance.Configuration.ServiceConfiguration.PollFrequencyInSeconds = 1;

            BlockingCollection<NotifyEventArgs> notifyEventData = new BlockingCollection<NotifyEventArgs>();

            ServiceRuntime.Instance.OnNotify += (sender, args) =>
            {
                Debug.WriteLine("NOTIFY {0} from device {1} on server {2}",
                    args.NotificationType,
                    args.UpsContext.Name,
                    args.UpsContext.UpsConfiguration.ServerConfiguration.DisplayName);

                notifyEventData.Add(args);
            };

            SimpleServer server = SimpleServer.StartNew(11002);

            ManualResetEventSlim waitEvent = new ManualResetEventSlim(false);

            Dictionary<string, string> upsVars = ServiceHelper.CreateTestUpsVars();

            server.HandleRequests(
                () =>
                {
                    server.HandleBasicAuthentication();

                    int reqCount = 0;

                    // Send the first request with status=online
                    server.ExpectAndResponse(
                        "LIST VAR testups",
                        ServiceHelper.GetVarList("testups", upsVars));

                    // Set status to on-battery and re-send
                    upsVars["ups.status"] = "OB";
                    server.ExpectAndResponse(
                        "LIST VAR testups",
                        ServiceHelper.GetVarList("testups", upsVars));

                    // Wait to be notified
                    if (!notifyEventData.TryTake(out NotifyEventArgs eventArgs, 2000))
                    {
                        Assert.Fail("Did not receive notification!");
                    }

                    Assert.AreEqual(
                        NotificationType.OnBattery, 
                        eventArgs.NotificationType);

                    Assert.AreEqual(
                        DeviceStatusType.OnBattery,
                        ServiceRuntime.Instance.UpsContexts[0].State.Status);

                    // All done
                    waitEvent.Set();
                });

            ServiceRuntime.Instance.Start();

            waitEvent.Wait();

            Debug.WriteLine("Stopping instance");

            ServiceRuntime.Instance.Stop();

            Debug.WriteLine("server.dispose");
            server.Dispose();

            Debug.WriteLine("TEST COMPLETE");
        }

        [TestMethod]
        public void TestOnBatteryAndRecover()
        {
            ServiceRuntime.Instance.Configuration = 
                ServiceHelper.CreateBasicConfiguration(11003);

            // Poll quickly!
            ServiceRuntime.Instance.Configuration.ServiceConfiguration.PollFrequencyInSeconds = 1;

            BlockingCollection<NotifyEventArgs> notifyEventData = new BlockingCollection<NotifyEventArgs>();

            ServiceRuntime.Instance.OnNotify += (sender, args) =>
            {
                Debug.WriteLine("NOTIFY {0} from device {1} on server {2}",
                    args.NotificationType,
                    args.UpsContext.Name,
                    args.UpsContext.UpsConfiguration.ServerConfiguration.DisplayName);

                notifyEventData.Add(args);
            };

            SimpleServer server = SimpleServer.StartNew(11003);

            ManualResetEventSlim waitEvent = new ManualResetEventSlim(false);

            Dictionary<string, string> upsVars = ServiceHelper.CreateTestUpsVars();

            server.HandleRequests(
                () =>
                {
                    server.HandleBasicAuthentication();

                    int reqCount = 0;

                    // Send the first request with status=online
                    server.ExpectAndResponse(
                        "LIST VAR testups",
                        ServiceHelper.GetVarList("testups", upsVars));

                    // Set status to on-battery and re-send
                    upsVars["ups.status"] = "OB";
                    server.ExpectAndResponse(
                        "LIST VAR testups",
                        ServiceHelper.GetVarList("testups", upsVars));

                    // Wait to be notified
                    if (!notifyEventData.TryTake(out NotifyEventArgs eventArgs, 2000))
                    {
                        Assert.Fail("Did not receive notification!");
                    }

                    Assert.AreEqual(
                        NotificationType.OnBattery, 
                        eventArgs.NotificationType);

                    Assert.AreEqual(
                        DeviceStatusType.OnBattery,
                        ServiceRuntime.Instance.UpsContexts[0].State.Status);

                    // Set status to on-battery and re-send
                    upsVars["ups.status"] = "OL";
                    server.ExpectAndResponse(
                        "LIST VAR testups",
                        ServiceHelper.GetVarList("testups", upsVars));

                    // Wait to be notified
                    if (!notifyEventData.TryTake(out eventArgs, 2000))
                    {
                        Assert.Fail("Did not receive notification!");
                    }

                    Assert.AreEqual(
                        NotificationType.Online, 
                        eventArgs.NotificationType);

                    Assert.AreEqual(
                        DeviceStatusType.Online,
                        ServiceRuntime.Instance.UpsContexts[0].State.Status);

                    // All done
                    waitEvent.Set();
                });

            ServiceRuntime.Instance.Start();

            waitEvent.Wait();

            Debug.WriteLine("Stopping instance");

            ServiceRuntime.Instance.Stop();

            Debug.WriteLine("server.dispose");
            server.Dispose();

            Debug.WriteLine("TEST COMPLETE");
        }
    }

    public static class ServiceHelper
    {
        public static WingnutConfiguration CreateBasicConfiguration(int port)
        {
            var config = new WingnutConfiguration
            {
                ServiceConfiguration = WingnutServiceConfiguration.CreateDefault(),
                ShutdownConfiguration = ShutdownConfiguration.CreateDefault()
            };

            config.UpsConfigurations.Add(
                new UpsConfiguration
                {
                    ServerConfiguration = new ServerConfiguration
                    {
                        Address = IPAddress.Loopback.ToString(),
                        Port = port,
                        Username = "testuser",
                        Password = SecureStringExtensions.FromString("testpass"),
                        UseSSL = SSLUsage.Optional,
                        PreferredAddressFamily = AddressFamily.InterNetwork,
                    },
                    NumPowerSupplies = 1,
                    DeviceName = "testups"
                });

            return config;
        }

        public static Dictionary<string, string> CreateTestUpsVars()
        {
            return new Dictionary<string, string>
            {
                {"device.type", "ups"},
                {"driver.name", "testups-drv"},
                {"ups.model", "TestUPS 1000"},
                {"ups.id", "testups"},
                {"ups.status", "OL"},
            };
        }

        public static string GetVarList(
            string upsName,
            Dictionary<string, string> upsVars)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("BEGIN LIST VAR " + upsName);
            foreach (KeyValuePair<string, string> upsVar in upsVars)
            {
                stringBuilder.AppendLine(
                    $"VAR {upsName} {upsVar.Key} \"{upsVar.Value}\"");
            }
            stringBuilder.AppendLine("END LIST VAR " + upsName);

            return stringBuilder.ToString();
        }
    }

    public class SimpleServer : IDisposable
    {
        private TcpListener tcpListener;
        public TcpClient TcpClient { get; private set; }

        public NetworkStream NetworkStream { get; private set; }

        public static SimpleServer StartNew(int port)
        {
            SimpleServer server = new SimpleServer();

            server.tcpListener = new TcpListener(
                IPAddress.Loopback,
                port);

            server.tcpListener.Start();

            server.tcpListener.BeginAcceptTcpClient(server.OnClientConnect, server);

            return server;
        }

        private void OnClientConnect(IAsyncResult asyncResult)
        {
            this.TcpClient = this.tcpListener.EndAcceptTcpClient(asyncResult);

            this.NetworkStream = this.TcpClient.GetStream();

            this.requestHandlerMethod.Invoke();
        }

        public void HandleRequests(HandleRequestDelegate handler)
        {
            this.requestHandlerMethod = handler;
        }

        public delegate void HandleRequestDelegate();

        private HandleRequestDelegate requestHandlerMethod;

        public void HandleBasicAuthentication()
        {
            this.ExpectAndResponse("STARTTLS", "ERR FEATURE-NOT-SUPPORTED");
            this.ExpectAndResponse("USERNAME testuser", "OK");
            this.ExpectAndResponse("PASSWORD testpass", "OK");
        }

        public void ExpectAndResponse(string request, string response)
        {
            if (!request.EndsWith("\n"))
            {
                request += '\n';
            }

            byte[] buffer = new byte[1024];
            int bytesReceived = this.NetworkStream.Read(buffer, 0, 1024);

            var str = Encoding.ASCII.GetString(buffer, 0, bytesReceived);

            Debug.WriteLine("RECV: " + str);

            if (str != request)
            {
                throw new Exception(
                    $"Communication test failer. Expected '{request}'. Got '{str}'.");
            }

            byte[] buffer2 = Encoding.ASCII.GetBytes(response);
            this.NetworkStream.Write(buffer2, 0, buffer2.Length);

            Debug.WriteLine("SEND: " + response);
        }

        public void Dispose()
        {
            this.tcpListener?.Stop();
        }
    }

    public class ServerSimulator
    {
        private const int DefaultPort = 13493;

        private TcpListener tcpListener;

        private CancellationTokenSource cts;
        private Task listenerTask;

        public void Start()
        {
            this.tcpListener = new TcpListener(
                IPAddress.Loopback,
                DefaultPort);

            this.cts = new CancellationTokenSource();

            this.tcpListener.Start();

            listenerTask = Task.Run(
                async () => await this.ListenMain().ConfigureAwait(false));
        }

        public void Stop()
        {
            this.cts.Cancel();
            this.listenerTask.Wait();
        }

        private async Task ListenMain()
        {
            // Wait for the client to connect
            TcpClient client = await this.tcpListener.AcceptTcpClientAsync();

            while (!this.cts.IsCancellationRequested)
            {
                Task.Run(async () => await this.ClientListenMain(client).ConfigureAwait(false));
            }
        }

        private async Task ClientListenMain(TcpClient client)
        {
            var stream = client.GetStream();

            byte[] readBuffer = new byte[1024];
            while (!this.cts.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(
                    readBuffer, 
                    0, 
                    readBuffer.Length,
                    this.cts.Token);

                var str = Encoding.ASCII.GetString(readBuffer, 0, bytesRead).Trim();


            }
        }
    }
}
