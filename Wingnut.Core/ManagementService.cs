namespace Wingnut.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel;
    using System.Threading;
    using System.Threading.Tasks;

    using Wingnut.Channels;
    using Wingnut.Data;
    using Wingnut.Data.Configuration;
    using Wingnut.Data.Models;
    using Wingnut.Tracing;

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class ManagementService : IManagementService
    {
        public List<Server> GetServers(string name)
        {
            IEnumerable<Server> allServers = ServiceRuntime.Instance.ServerContexts.Select(s => s.ServerState);

            if (string.IsNullOrWhiteSpace(name))
            {
                return allServers.ToList();
            }

            return allServers.Where(s => s.Name == name).ToList();
        }

        public Server AddServer(Server server, string password, bool ignoreConnectionFailure)
        {
            // Create a configuration object for the data provided
            server.Password = SecureStringExtensions.FromString(password);
            ServerConfiguration serverConfiguration = ServerConfiguration.Create(server);

            serverConfiguration.ValidateProperties();

            if (ServiceRuntime.Instance.Configuration.Servers.Any(
                s => string.Equals(s.Name, serverConfiguration.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new WingnutException(
                    $"A server with the name '{serverConfiguration.Name}' already exists.");
            }

            // Recreate the server object to ensure that it matches what would be created when it
            // is read from configuration at startup.
            server = Server.CreateFromConfiguration(serverConfiguration);
            ServerContext serverContext = new ServerContext(server, serverConfiguration);

            try
            {
                serverContext.Connection.ConnectAsync(CancellationToken.None).Wait();
            }
            catch
            {
                if (ignoreConnectionFailure)
                {
                    Logger.Info("Failed to connect to server. Ignoring.");
                }
                else
                {
                    throw;
                }
            }

            // No exception was thrown, so the server connected successfully
            ServiceRuntime.Instance.ServerContexts.Add(serverContext);
            ServiceRuntime.Instance.Configuration.Servers.Add(serverConfiguration);

            // We have update the configuration, so persist the changes
            ServiceRuntime.Instance.SaveConfiguration();

            return server;
        }

        public void UpdateServer(Server server)
        {
            throw new NotImplementedException();
        }

        public void DeleteServer(string name)
        {
            var server =
                ServiceRuntime.Instance.ServerContexts.FirstOrDefault(
                    s => s.ServerState.Name == name);

            if (server == null)
            {
                throw new Exception($"A server with the name '{name}' was not found.");
            }

            server.StopMonitoring();

            ServiceRuntime.Instance.ServerContexts.Remove(server);
            ServiceRuntime.Instance.Configuration.Servers.RemoveAll(s => s.Name == name);
        }

        public async Task<List<Ups>> GetUpsFromServer(string serverName, string upsName)
        {
            ServerContext serverContext =
                ServiceRuntime.Instance.ServerContexts.FirstOrDefault(
                    s => string.Equals(s.ServerState.Name, serverName));

            if (serverContext == null)
            {
                throw new WingnutException(
                    $"A server with the name '{serverName}' was not found.");
            }

            Dictionary<string, string> listResponse =
                await serverContext.Connection
                    .ListUpsAsync(CancellationToken.None)
                    .ConfigureAwait(false);

            List<Ups> upsList = new List<Ups>();

            foreach (string thisUpsName in listResponse.Keys)
            {
                if (!string.IsNullOrWhiteSpace(upsName) && !string.Equals(thisUpsName, upsName))
                {
                    continue;
                }

                Dictionary<string, string> upsVars =
                    await serverContext.Connection
                        .ListVarsAsync(thisUpsName, CancellationToken.None)
                        .ConfigureAwait(false);

                upsList.Add(new Ups(thisUpsName, upsVars));
            }

            return upsList;
        }

        public async Task<Ups> AddUps(
            string serverName, 
            string upsName,
            bool monitorOnly,
            bool force)
        {
            ServerContext serverContext =
                ServiceRuntime.Instance.ServerContexts.FirstOrDefault(
                    s => string.Equals(s.ServerState.Name, serverName));

            if (serverContext == null)
            {
                throw new WingnutException(
                    $"A server with the name '{serverName}' was not found.");
            }

            if (serverContext.UpsContexts.Any(s => s.Name == upsName))
            {
                throw new Exception("The ups already exist");
            }

            serverContext.Configuration.Upses.Add(
                new UpsConfiguration()
                {
                    Name = upsName,
                    MonitorOnly = monitorOnly
                });

            DateTime pollTime = serverContext.LastPollTime;
            Console.WriteLine("Initial poll time: " + pollTime);
            serverContext.PollNow();
            bool retry = false;

            Console.WriteLine();

            while (true)
            {
                if (serverContext.LastPollTime != pollTime)
                {
                    Console.WriteLine("Time Changed");
                    // The server has completes a poll. Check if the UPS was added
                    var ups = serverContext.UpsContexts.FirstOrDefault(u => u.Name == upsName);

                    if (ups == null)
                    {
                        Console.WriteLine("UPS is null");
                        if (!retry)
                        {
                            retry = true;
                            pollTime = serverContext.LastPollTime;
                            serverContext.PollNow();
                        }
                        else
                        {
                            if (force)
                            {
                                ServiceRuntime.Instance.SaveConfiguration();
                            }

                            throw new Exception("Failed to add ups after 2 polls");
                        }
                    }
                    else
                    {
                        ServiceRuntime.Instance.SaveConfiguration();
                        return ups.State;
                    }
                }

                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        public Task<List<Ups>> GetUps(string serverName, string upsName)
        {
            ServerContext serverContext =
                ServiceRuntime.Instance.ServerContexts.FirstOrDefault(
                    s => string.Equals(s.ServerState.Name, serverName));

            if (serverContext == null)
            {
                throw new WingnutException(
                    $"A server with the name '{serverName}' was not found.");
            }

            List<Ups> upsList = new List<Ups>();

            if (string.IsNullOrWhiteSpace(upsName))
            {
                upsList.AddRange(serverContext.UpsContexts.Select(u => u.State));
            }
            else
            {
                var upsContext = serverContext.UpsContexts.FirstOrDefault(s => s.Name == upsName);

                if (upsContext == null)
                {
                    throw new WingnutException(
                        $"A UPS device with name '{upsName}' was not found on server '{serverName}'");
                }

                upsList.Add(upsContext.State);
            }

            return Task.FromResult(upsList);
        }
    }
}