namespace Wingnut.UI.ViewModels
{
    using System;

    using Wingnut.Data.Models;
    using Wingnut.UI.Framework;

    public class DevicePropertyViewModel : ViewModelBase
    {
        public string Header { get; }

        public DevicePropertyViewModel(
            string header, 
            Ups ups, 
            string propertyName,
            Func<string> converter)
        {
            this.Header = header;

            this.Value = converter();

            ups.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == propertyName)
                {
                    this.Value = converter();
                }
            };
        }

        private string value;

        public string Value
        {
            get => this.value;
            set => this.SetProperty(ref this.value, value);
        }
    }
}