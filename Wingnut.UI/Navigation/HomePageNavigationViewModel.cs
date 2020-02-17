namespace Wingnut.UI.Navigation
{
    using Wingnut.UI.ViewModels;

    public class HomePageNavigationViewModel : NavigationSectionViewModel
    {
        public HomePageViewModel HomePage { get; }

        public HomePageNavigationViewModel(HomePageViewModel homePageViewModel)
        {
            this.HomePage = homePageViewModel;

            this.Glyph = "\uE80F";
            this.NavigationHeader = "Home";
            this.PageHeader = "Windows GUI for Network UPS Tools";
        }
    }
}
