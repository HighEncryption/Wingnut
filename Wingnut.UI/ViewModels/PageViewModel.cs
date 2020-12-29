namespace Wingnut.UI.ViewModels
{
    using System.Diagnostics;

    using Wingnut.UI.Framework;

    public abstract class PageViewModel : ViewModelBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string pageHeader;

        public string PageHeader
        {
            get => this.pageHeader;
            set => this.SetProperty(ref this.pageHeader, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string navigationHeader;

        public string NavigationHeader
        {
            get => this.navigationHeader;
            set => this.SetProperty(ref this.navigationHeader, value);
        }

        private string headerImage;

        public string HeaderImage
        {
            get => this.headerImage;
            set => this.SetProperty(ref this.headerImage, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string glyph;

        public string Glyph
        {
            get => this.glyph;
            set => this.SetProperty(ref this.glyph, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool isSelected;

        public bool IsSelected
        {
            get => this.isSelected;
            set
            {
                if (this.SetProperty(ref this.isSelected, value) && value)
                {
                    App.Current.MainWindowViewModel.SelectedPage = this;
                }
            }
        }

        public virtual bool IsEnabled =>
            App.Current.MainWindowViewModel.SelectedDevice != null;

        protected PageViewModel()
        {
            App.Current.MainWindowViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.SelectedDevice))
                {
                    this.RaisePropertyChanged(nameof(this.ActiveDevice));
                    this.RaisePropertyChanged(nameof(this.IsEnabled));
                }
            };
        }

        public UpsDeviceViewModel ActiveDevice =>
            App.Current.MainWindowViewModel.SelectedDevice as UpsDeviceViewModel;
    }
}