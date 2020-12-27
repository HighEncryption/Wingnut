using System;
using System.Windows;

namespace Wingnut.UI.Windows
{
    using System.Windows.Threading;

    using SourceChord.FluentWPF;

    using Wingnut.UI.ViewModels;

    /// <summary>
    /// Interaction logic for SelectDeviceWindow.xaml
    /// </summary>
    public partial class SelectDeviceWindow : AcrylicWindow
    {
        public SelectDeviceWindow()
        {
            InitializeComponent();
            this.DataContextChanged += this.OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SelectDeviceWindowViewModel viewModel)
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
