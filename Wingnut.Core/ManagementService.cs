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
        /// <summary>
        /// Register a client for a callback notification when one or more Ups properties change
        /// </summary>
        public void Register()
        {
            IManagementCallback callbackChannel =
                OperationContext.Current.GetCallbackChannel<IManagementCallback>();

            if (!ServiceRuntime.Instance.ClientCallbackChannels.Contains(callbackChannel))
            {
                ServiceRuntime.Instance.ClientCallbackChannels.Add(callbackChannel);
            }
        }

        /// <summary>
        /// Unregister a client for a callback notification when one or more Ups properties change
        /// </summary>
        public void Unregister()
        {
            IManagementCallback callbackChannel =
                OperationContext.Current.GetCallbackChannel<IManagementCallback>();

            if (ServiceRuntime.Instance.ClientCallbackChannels.Contains(callbackChannel))
            {
                ServiceRuntime.Instance.ClientCallbackChannels.Remove(callbackChannel);
            }
        }

        /// <summary>
        /// Get the service configuration
        /// </summary>
        public WingnutServiceConfiguration GetServiceConfiguration()
        {
            return ServiceRuntime.Instance.Configuration.ServiceConfiguration;
        }

        public void UpdateServiceConfiguration(WingnutServiceConfiguration configuration)
        {
            configuration.ValidateProperties();

            ServiceRuntime.Instance.Configuration.ServiceConfiguration = configuration;
            ServiceRuntime.Instance.SaveConfiguration();
        }

        public UpsConfiguration GetUpsConfiguration(string serverName, string upsName)
        {
            var upsContext =
                ServiceRuntime.Instance.UpsContexts.FirstOrDefault(
                    ctx => ctx.Name == upsName &&
                           ctx.UpsConfiguration.ServerConfiguration.Address == serverName);

            if (upsContext == null)
            {
                throw new Exception("Failed to find a UPS with that name");
            }

            return upsContext.UpsConfiguration;
        }

        public void UpdateUpsConfiguration(UpsConfiguration configuration)
        {
            var upsContext =
                ServiceRuntime.Instance.UpsContexts.FirstOrDefault(
                    ctx => ctx.QualifiedName == configuration.GetQualifiedName());

            if (upsContext == null)
            {
                throw new Exception("Failed to find a UPS with that name");
            }

            // Only update properties that support changing like this
            upsContext.UpsConfiguration.EnableEmailNotification = configuration.EnableEmailNotification;
            upsContext.UpsConfiguration.EnablePowerShellNotification = configuration.EnablePowerShellNotification;

            ServiceRuntime.Instance.SaveConfiguration();
        }

        public async Task<List<Ups>> GetUpsFromServer(Server server, string password, string upsName)
        {
            // Update the password on the server object since it can't be passed as a SecureString
            // over the WCF channel
            server.Password = SecureStringExtensions.FromString(password);
            ServerConfiguration serverConfiguration = ServerConfiguration.CreateFromServer(server);

            serverConfiguration.ValidateProperties();

            // Recreate the server object to ensure that it matches what would be created when it
            // is read from configuration at startup.
            server = Server.CreateFromConfiguration(serverConfiguration);

            ServerConnection serverConnection = new ServerConnection(server);

            await serverConnection.ConnectAsync(CancellationToken.None)
                .ConfigureAwait(false);

            try
            {
                Dictionary<string, string> listResponse =
                    await serverConnection
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
                        await serverConnection
                            .ListVarsAsync(thisUpsName, CancellationToken.None)
                            .ConfigureAwait(false);

                    upsList.Add(Ups.Create(thisUpsName, server, upsVars));
                }

                return upsList;
            }
            finally
            {
                serverConnection.Disconnect();
            }
        }

        public async Task<Ups> AddUps(
            Server server, 
            string password, 
            string upsName, 
            int numPowerSupplies, 
            bool monitorOnly, 
            bool force)
        {
            // Update the password on the server object since it can't be passed as a SecureString
            // over the WCF channel
            server.Password = SecureStringExtensions.FromString(password);
            ServerConfiguration serverConfiguration = ServerConfiguration.CreateFromServer(server);

            serverConfiguration.ValidateProperties();

            // Recreate the server object to ensure that it matches what would be created when it
            // is read from configuration at startup.
            server = Server.CreateFromConfiguration(serverConfiguration);

            UpsConfiguration upsConfiguration = new UpsConfiguration()
            {
                DeviceName = upsName,
                MonitorOnly = monitorOnly,
                NumPowerSupplies = numPowerSupplies,
                ServerConfiguration = serverConfiguration
            };

            try
            {
                ServerConnection serverConnection = new ServerConnection(server);

                await serverConnection.ConnectAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Dictionary<string, string> upsVars =
                    await serverConnection
                        .ListVarsAsync(upsName, CancellationToken.None)
                        .ConfigureAwait(false);

                Ups ups = Ups.Create(upsName, server, upsVars);

                // Success. Add the configuration and save
                ServiceRuntime.Instance.Configuration.UpsConfigurations.Add(
                    upsConfiguration);

                ServiceRuntime.Instance.SaveConfiguration();

                // Add to the running instances
                UpsContext upsContext = new UpsContext(upsConfiguration, server)
                {
                    State = ups
                };

                ServiceRuntime.Instance.UpsContexts.Add(upsContext);

                return ups;
            }
            catch (Exception exception)
            {
                Logger.Error("Exception while adding UPS device. {0}", exception.Message);

                if (force)
                {
                    // Add the configuration and save
                    ServiceRuntime.Instance.Configuration.UpsConfigurations.Add(
                        upsConfiguration);

                    ServiceRuntime.Instance.SaveConfiguration();

                    return null;
                }

                throw;
            }
        }

        public Task<List<Ups>> GetUps(string serverName, string upsName)
        {
            List<Ups> upsList = new List<Ups>();

            if (string.IsNullOrEmpty(serverName))
            {
                upsList.AddRange(
                    ServiceRuntime.Instance.UpsContexts
                        .Select(ctx => ctx.State));
            }
            if (string.IsNullOrWhiteSpace(upsName))
            {
                upsList.AddRange(
                    ServiceRuntime.Instance.UpsContexts
                        .Where(ctx => ctx.UpsConfiguration.ServerConfiguration.Address == serverName)
                        .Select(ctx => ctx.State));
            }
            else
            {
                var upsContext =
                    ServiceRuntime.Instance.UpsContexts.FirstOrDefault(
                        ctx => ctx.Name == upsName &&
                               ctx.UpsConfiguration.ServerConfiguration.Address == serverName);

                if (upsContext == null)
                {
                    throw new Exception("Failed to find a UPS with that name");
                }

                upsList.Add(upsContext.State);
            }

            return Task.FromResult(upsList);
        }
    }
}