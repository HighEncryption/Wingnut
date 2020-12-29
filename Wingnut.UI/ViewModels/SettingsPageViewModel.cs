namespace Wingnut.UI.ViewModels
{
    public class SettingsPageViewModel : PageViewModel
    {
        public SettingsPageViewModel()
        {
            this.NavigationHeader = "Settings";
            this.PageHeader = "Settings";
            this.Glyph = "\uE713";
        }

        public override bool IsEnabled => true;
    }
}