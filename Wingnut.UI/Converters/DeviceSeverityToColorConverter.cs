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

    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleToPercentageStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double))
            {
                return DependencyProperty.UnsetValue;
            }

            double d = (double) value;

            if (Math.Abs(d) < double.Epsilon)
            {
                return "0 %";
            }

            if (Math.Abs(Math.Abs(d) - 100) < double.Epsilon)
            {
                return "100 %";
            }

            return d.ToString("###.0") + " %";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public static class ColorExtensions
    {
        public static Color FromHex(string hex)
        {
            hex = hex.TrimStart('#');

            byte[] bytes = HexToBytes(hex);

            if (bytes.Length == 4)
            {
                return Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);
            }

            if (bytes.Length == 3)
            {
                return Color.FromRgb(bytes[0], bytes[1], bytes[2]);
            }

            throw new InvalidOperationException("Color format is incorrect");
        }

        public static byte[] HexToBytes(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new byte[0];
            }

            byte[] bytes = new byte[input.Length / 2];
            for (int i = 0; i < input.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(input.Substring(i, 2), 16);
            }

            return bytes;
        }
    }
}
