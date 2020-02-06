namespace Wingnut.Service
{
    using System.ComponentModel;
    using System.ServiceProcess;

    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        private readonly ServiceProcessInstaller serviceProcessInstaller;
        private readonly ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            InitializeComponent();

            this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller = new System.ServiceProcess.ServiceInstaller();

            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;
            this.serviceProcessInstaller.Account = ServiceAccount.LocalService;

            this.serviceInstaller.ServiceName = "Wingnut Service";

            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
                this.serviceProcessInstaller,
                this.serviceInstaller
            });

        }
    }
}
