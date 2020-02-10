namespace Wingnut.UI.Navigation
{
    using System.Collections.ObjectModel;

    public class NavigationSectionGroupViewModel : NavigationSectionViewModel
    {
        private ObservableCollection<NavigationSectionViewModel> sections;

        public ObservableCollection<NavigationSectionViewModel> Sections =>
            this.sections ?? (this.sections = new ObservableCollection<NavigationSectionViewModel>());
    }
}