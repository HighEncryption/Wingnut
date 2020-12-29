namespace Wingnut.UI.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;

    using Wingnut.Data;

    [ValueConversion(typeof(DeviceSeverityType), typeof(SolidColorBrush))]
    public class DeviceSeverityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DeviceSeverityType))
            {
                return DependencyProperty.UnsetValue;
            }

            switch ((DeviceSeverityType)value)
            {
                case DeviceSeverityType.OK:
                    return new SolidColorBrush(ColorExtensions.FromHex("#03CE00"));
                case DeviceSeverityType.Warning:
                    return new SolidColorBrush(ColorExtensions.FromHex("#EAE133"));
                case DeviceSeverityType.Error:
                    return new SolidColorBrush(ColorExtensions.FromHex("#FF3939"));
                default:
                    return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
