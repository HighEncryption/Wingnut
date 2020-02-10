namespace Wingnut.UI.Navigation
{
    using System.Diagnostics;

    using Wingnut.UI.Framework;

    public class NavigationSectionViewModel : ViewModelBase
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
                    App.Current.MainWindowViewModel.SelectedNavigationSection = this;
                }
            }
        }
    }
}