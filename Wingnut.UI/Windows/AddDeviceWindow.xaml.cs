namespace Wingnut.UI.Windows
{
    using System;
    using System.Windows;
    using System.Windows.Threading;

    using Wingnut.UI.ViewModels;

    /// <summary>
    /// Interaction logic for AddDeviceWindow.xaml
    /// </summary>
    public partial class AddDeviceWindow
    {
        public AddDeviceWindow()
        {
            InitializeComponent();

            this.DataContextChanged += this.OnDataContextChanged;
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is AddDeviceWindowViewModel viewModel)
            {
                viewModel.RequestClose += this.ViewModelRequestClose;
            }
        }


        private void ViewModelRequestClose(object sender, RequestCloseEventArgs e)
        {
            this.Dispatcher?.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(
                    delegate
                    {
                        if (e.DialogResult != null)
                        {
                            this.DialogResult = e.DialogResult;
                        }

                        this.Close();
                    }));
        }
    }
}
