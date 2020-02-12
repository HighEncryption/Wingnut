namespace Wingnut.UpsdSim
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        private class ClientState
        {
            public string Username { get; set; }
            public string Password { get; set; }

            public Guid Id { get; }

            public ClientState(Guid id)
            {
                this.Id = id;
            }
        }

        private static readonly List<Device> devices = new List<Device>();

        private static readonly Dictionary<Guid, ClientState> clientStates = new Dictionary<Guid, ClientState>();

        static void Main(string[] args)
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string deviceFilesPath = Path.Combine(currentDir, "DeviceFiles");

            var deviceFilePaths = Directory.GetFiles(deviceFilesPath, "*.txt");

            foreach (var deviceFilePath in deviceFilePaths)
            {
                var newDevice = new Device(deviceFilePath);
                devices.Add(newDevice);
                Console.WriteLine(
                    "Loaded device {0} with {1} variables", 
                    newDevice.DeviceName, 
                    newDevice.Variables.Count);
            }

            FileSystemWatcher watcher = new FileSystemWatcher(deviceFilesPath, "*.txt");
            watcher.Changed += WatcherOnChanged;

            ServerSimulator serverSim = new ServerSimulator(HandleClientRequest);

            serverSim.Start();

            Console.WriteLine("READY");
            Console.ReadLine();

            serverSim.Stop();
        }

        private static string[] HandleClientRequest(Guid clientId, string request)
        {
            if (!clientStates.TryGetValue(clientId, out ClientState state))
            {
                state = new ClientState(clientId);
                clientStates.Add(clientId, state);
            }

            if (request.StartsWith("USERNAME testuser", StringComparison.OrdinalIgnoreCase))
            {
                if (state.Username != null)
                {
                    return new[] {"ERR ALREADY-SET-USERNAME"};
                }

                state.Username = "testuser";
                return new[] {"OK"};
            }

            if (request.StartsWith("PASSWORD testpass", StringComparison.OrdinalIgnoreCase))
            {
                if (state.Username != null)
                {
                    return new[] { "ERR ALREADY-SET-PASSWORD" };
                }

                state.Password = "testpass";
                return new[] {"OK"};
            }

            if (request.StartsWith("STARTTLS", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { "ERR FEATURE-NOT-SUPPORTED" };
            }

            string[] tokens = request.Split(new[] {' '}, 2);
            if (tokens[0] == "LIST")
            {
                return HandleListRequest(state, request);
            }

            return new[] {"ERR UNKNOWN-COMMAND"};
        }

        private static string[] HandleListRequest(ClientState state, string request)
        {
            string[] tokens = request.Split(new[] { ' ' }, 3);

            if (tokens[1] == "UPS")
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("BEGIN LIST UPS");

                foreach (Device device in devices)
                {
                    sb.AppendLine(
                        $"LIST UPS {device.DeviceName} \"{device.Description}\"");
                }

                sb.AppendLine("END LIST UPS");
                return new[] {sb.ToString()};
            }

            if (tokens[1] == "VAR")
            {
                string deviceName = tokens[2].Trim();
                Device device = devices.FirstOrDefault(d => d.DeviceName == deviceName);

                if (device == null)
                {
                    return new[] {"ERR UNKNOWN-UPS"};
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"BEGIN LIST VAR {device.DeviceName}");

                foreach (KeyValuePair<string, string> pair in device.Variables)
                {
                    sb.AppendLine($"VAR {device.DeviceName} {pair.Key} \"{pair.Value}\"");
                }

                sb.AppendLine($"END LIST VAR {device.DeviceName}");
                return new[] { sb.ToString() };
            }

            return new[] { "ERR UNKNOWN-COMMAND" };
        }

        private static void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            var changedDevices = devices.FirstOrDefault(d => d.FilePath == e.FullPath);
            if (changedDevices == null)
            {
                // Missing?
                Debugger.Break();
                return;
            }

            changedDevices.ReadVariables();
        }
    }

    public class Device
    {
        public string FilePath { get; }

        public string DeviceName { get; }

        public string Description { get; set; }

        public Dictionary<string, string> Variables { get; }
            = new Dictionary<string, string>();

        public Device(string path)
        {
            this.FilePath = path;
            this.DeviceName = Path.GetFileNameWithoutExtension(path);
            this.ReadVariables();
        }

        public void ReadVariables()
        {
            var lines = File.ReadAllLines(this.FilePath);
            foreach (string line in lines)
            {
                var tokens = line.Split(new[] {':'}, 2);

                string key = tokens[0].Trim();
                if (key == "description")
                {
                    this.Description = tokens[1].Trim();
                }

                this.Variables[key] = tokens[1].Trim();
            }
        }
    }

    public class ServerSimulator
    {
        private const int DefaultPort = 13493;

        private TcpListener tcpListener;

        private CancellationTokenSource cts;
        private Task listenerTask;

        public ServerSimulator(Func<Guid, string, string[]> handleRequestAction)
        {
            this.handleRequestAction = handleRequestAction;
        }

        private readonly Func<Guid, string, string[]> handleRequestAction;

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
            this.listenerTask.Wait(1000);
        }

        private async Task ListenMain()
        {
            // Wait for the client to connect
            TcpClient client = await this.tcpListener.AcceptTcpClientAsync();

            while (!this.cts.IsCancellationRequested)
            {
#pragma warning disable 4014
                Task.Run(async () => await this.ClientListenMain(client).ConfigureAwait(false));
#pragma warning restore 4014
            }
        }

        private async Task ClientListenMain(TcpClient client)
        {
            var stream = client.GetStream();
            Guid clientId = Guid.NewGuid();

            byte[] readBuffer = new byte[1024];
            while (!this.cts.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(
                    readBuffer,
                    0,
                    readBuffer.Length,
                    this.cts.Token);

                var str = Encoding.ASCII.GetString(readBuffer, 0, bytesRead).Trim();

                string[] responses = this.handleRequestAction(clientId, str);

                foreach (var response in responses)
                {
                    byte[] writeBuffer = Encoding.ASCII.GetBytes(response);
                    await stream.WriteAsync(writeBuffer, 0, writeBuffer.Length).ConfigureAwait(false);
                }
            }
        }
    }

}
