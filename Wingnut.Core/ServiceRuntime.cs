namespace Wingnut.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
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
            this.ClientCallbackChannels = new List<IManagementCallback>();
        }

        private static readonly object initLock = new object();
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

        public List<UpsContext> UpsContexts { get; }

        public List<IManagementCallback> ClientCallbackChannels { get; }

        public void Initialize()
        {
            if (this.isInitialized)
            {
                throw new InvalidOperationException("Runtime is already initialized");
            }

            Logger.ServiceStarting(GetAssemblyVersionString());

            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Wingnut");

            Logger.Info(
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

            if (this.Configuration.EnableDetailedTracing)
            {
                Logger.OutputLogLevel = Logger.LogLevel.Debug;
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

                    INotificationHandler psNotifier = new PowerShellNotifier(notificationScriptPath);
                    this.OnNotify += psNotifier.HandleNotification;
                }
                else
                {
                    Logger.Info("No PowerShell script found at path {0}", notificationScriptPath);
                }
            }

            if (this.Configuration.ServiceConfiguration.SmtpConfiguration != null)
            {
                INotificationHandler smtpNotifier = new SmtpNotifier();
                this.OnNotify += smtpNotifier.HandleNotification;
            }

            // Initialization is finished
            Logger.Debug("ServiceRuntime: Initialization finished");
            Logger.ServiceStarted();

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

        private static string GetAssemblyVersionString()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();

            /*
             * Notes on compiled git information
             * By including a build action in the project (shown below), we can include information
             * about the state of the code (such as the last commit ID) from when the when the 
             * project was built. The command is:
             * 
             * git describe --always --long --dirty >$(ProjectDir)\version.txt
             * 
             * Add the command as a pre-build step, then build once to generate the file. Add
             * the file to the root of the project as an embedded resource.
             * 
             * From the code, we can then read the state of the git repository at build time. The
             * output from the command has one of the following two format:
             * 
             * {commidId}[-dirty]
             *   or
             * {latestTag}-{commitDistanceFromTag}-{commitId}[-dirty]
             * 
             * If there is a tag in the history, the second form will be used, which includes
             * the tag and the number of commits from that tag to HEAD (or where the code was
             * built). If there are no tags, the first form is used.
             * 
             * If there were any uncomitted changes (staged or unstaged), then the '-dirty' string
             * will be appended to the commit ID.
             */
            string gitStatus;
            using (Stream stream = executingAssembly.GetManifestResourceStream("Wingnut.Core.version.txt"))
            {
                //Pre.Assert(stream != null, "stream != null");
                using (StreamReader reader = new StreamReader(stream))
                {
                    gitStatus = reader.ReadToEnd().Trim();
                }
            }

            // Check if the output includes '-dirty' at the end to indicate the build state was dirty
            bool isDirty = false;
            if (gitStatus.EndsWith("-dirty"))
            {
                isDirty = true;
                gitStatus = gitStatus.Substring(0, gitStatus.Length - 6);
            }

            // If the remaining output does not contain any dashes, then the non-tag format was used
            // and we know that the remaining string is the commit ID
            string tag = null;
            string distanceFromTag = null;
            string lastCommitId;
            if (!gitStatus.Contains("-"))
            {
                lastCommitId = gitStatus;
            }
            else
            {
                // Split the remaining string and parse out the individual strings. Note that the
                // tag itself may contain a dash, so we need to work backward from the end of the
                // string.
                string[] parts = gitStatus.Split('-');
                lastCommitId = parts[parts.Length - 1];
                distanceFromTag = parts[parts.Length - 2];
                tag = string.Join("-", parts.Take(parts.Length - 2));
            }

            Logger.Info(
                "Build information: Hash={0}, Tag={1}, TagDistance={2}, Dirty={3}",
                lastCommitId,
                tag,
                distanceFromTag,
                isDirty);

            return string.Format(
                "{0}-{1}{2}",
                executingAssembly.GetName().Version,
                lastCommitId,
                isDirty ? "*" : null);
        }
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