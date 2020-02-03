﻿namespace Wingnut.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using Newtonsoft.Json;

    using Wingnut.Channels;
    using Wingnut.Data;
    using Wingnut.Data.Configuration;
    using Wingnut.Tracing;

    public class ServiceRuntime
    {
        private ServiceRuntime()
        {
            this.UpsContexts = new List<UpsContext>();
        }

        private static object initLock = new object();
        private static ServiceRuntime instance;

        public static ServiceRuntime Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (initLock)
                    {
                        if (instance == null)
                        {
                            instance = new ServiceRuntime();
                        }
                    }
                }

                return instance;
            }
        }

        private bool isInitialized;
        private string configurationPath;
        private ServiceHost serviceHost;
        private UpsMonitor upsMonitor;

        public WingnutConfiguration Configuration { get; internal set; }

        public List<UpsContext> UpsContexts { get;}

        public void Initialize()
        {
            if (this.isInitialized)
            {
                throw new InvalidOperationException("Runtime is already initialized");
            }

            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Wingnut");

            Logger.Debug(
                "ServiceRuntime: Initializing runtime with app data path: {0}", 
                appDataPath);

            // Create the directory (safe to call even if the directory exists)
            Directory.CreateDirectory(appDataPath);

            configurationPath = Path.Combine(appDataPath, WingnutConfiguration.FileName);
            if (File.Exists(configurationPath))
            {
                LoadConfiguration();
            }
            else
            {
                this.Configuration = new WingnutConfiguration
                {
                    ServiceConfiguration = WingnutServiceConfiguration.CreateDefault(),
                    ShutdownConfiguration = ShutdownConfiguration.CreateDefault()
                };
            }

            string notificationScriptPath =
                this.Configuration.ServiceConfiguration.PowerShellNotificationScriptPath;

            if (string.IsNullOrWhiteSpace(notificationScriptPath))
            {
                Logger.Info("No PowerShell script path found in configuration");
            }
            else
            {
                if (File.Exists(notificationScriptPath))
                {
                    Logger.Info(
                        "Using PowerShell notification script from path {0}",
                        notificationScriptPath);

                    PowerShellNotifier psNotifier = new PowerShellNotifier(notificationScriptPath);
                    this.OnNotify += psNotifier.HandleNotification;
                }
                else
                {
                    Logger.Info("No PowerShell script found at path {0}", notificationScriptPath);
                }
            }

            // Initialization is finished
            Logger.Debug("ServiceRuntime: Initialization finished");
            this.isInitialized = true;
        }

        public void Start()
        {
            // Create the management service
            this.serviceHost = new ServiceHost(
                typeof(ManagementService),
                new Uri("net.pipe://localhost"));

            this.serviceHost.AddServiceEndpoint(
                typeof(IManagementService),
                new NetNamedPipeBinding(),
                "Wingnut");

            ServiceDebugBehavior behavior =
                this.serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>();

            if (behavior == null)
            {
                behavior = new ServiceDebugBehavior();
                this.serviceHost.Description.Behaviors.Add(behavior);
            }

            behavior.IncludeExceptionDetailInFaults = true;

            Logger.Debug("ServiceRuntime: Opening WCF service host");

            this.serviceHost.Open();

            upsMonitor = new UpsMonitor();

            Logger.Debug("ServiceRuntime: Starting UPSMonitor");

            upsMonitor.Start();
        }

        public void Stop()
        {
            foreach (UpsContext upsContext in UpsContexts)
            {
                upsContext.StopMonitoring();
            }

            this.serviceHost.Close();

            this.upsMonitor.Stop();
        }

        private void LoadConfiguration()
        {
            string configJson = File.ReadAllText(configurationPath);
            this.Configuration = JsonConvert.DeserializeObject<WingnutConfiguration>(configJson);
        }

        public void SaveConfiguration()
        {
            string configJson = JsonConvert.SerializeObject(
                this.Configuration,
                Formatting.Indented);

            File.WriteAllText(this.configurationPath, configJson);
        }

        public void Notify(
            UpsContext upsContext,
            NotificationType notification)
        {
            if (this.OnNotify == null)
            {
                return;
            }

            foreach (EventHandler<NotifyEventArgs> notifyDelegate in this.OnNotify.GetInvocationList())
            {
                notifyDelegate.BeginInvoke(
                    this,
                    new NotifyEventArgs(
                        notification,
                        upsContext),
                    ar =>
                    {
                        EventHandler<NotifyEventArgs> thisDelegate = (EventHandler<NotifyEventArgs>) ar.AsyncState;
                        thisDelegate.EndInvoke(ar);

                        //ServiceRuntime runtime = (ServiceRuntime)ar.AsyncState;
                        //runtime.OnNotify.EndInvoke(ar);
                    },
                    notifyDelegate);
            }
        }

        public event EventHandler<NotifyEventArgs> OnNotify;
    }

    public class NotifyEventArgs : EventArgs
    {
        public NotificationType NotificationType { get; }

        public UpsContext UpsContext { get; }

        public NotifyEventArgs(
            NotificationType notificationType,
            UpsContext upsContext)
        {
            this.NotificationType = notificationType;
            this.UpsContext = upsContext;
        }
    }
}