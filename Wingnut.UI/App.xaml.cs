namespace Wingnut.UI
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Threading;

    using Wingnut.UI.ViewModels;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Dispatcher localDispatcher;

        internal new static App Current { get; private set; }

        public MainWindowViewModel MainWindowViewModel { get; private set; }

        internal static void Start()
        {
            App app = new App();
            Current = app;

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception e = args.ExceptionObject as Exception;
                string message = e?.ToString() ?? "(null)";

                File.WriteAllText(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        string.Format("wingnut.exception.{0:yyyyMMddHHmmss}.txt", DateTime.Now)),
                    message);
            };

            app.localDispatcher = Dispatcher.CurrentDispatcher;
            using (app.MainWindowViewModel = new MainWindowViewModel())
            {
                app.MainWindowViewModel.SetHomePage();

                app.MainWindowViewModel.ConnectToService();

                app.ShowMainWindow();
                app.Run();
            }
        }

        private void ShowMainWindow()
        {
            if (this.MainWindow == null)
            {
                this.MainWindow = new MainWindow { DataContext = this.MainWindowViewModel };
                this.MainWindow.Show();
            }
        }

        public static void DispatcherInvoke(Action action)
        {
            App.Current.localDispatcher.Invoke(action);
        }
    }
}
