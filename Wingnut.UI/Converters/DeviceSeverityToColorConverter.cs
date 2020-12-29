namespace Wingnut.UI.Converters
{
    using System;
    using System.Collections;
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

    [ValueConversion(typeof(ICollection), typeof(Visibility))]
    public class CollectionToVisibilityConverter : IValueConverter
    {
        public bool EmptyIsCollapsed { get; set; }

        public bool ReverseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ICollection collection = value as ICollection;

            bool result = collection != null && collection.Count > 0;

            if (this.ReverseValue)
            {
                result = !result;
            }

            if (result)
            {
                return Visibility.Visible;
            }

            return this.EmptyIsCollapsed ? Visibility.Collapsed : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public bool ReverseValue { get; set; }
        public bool CollapsedWhenFalse { get; set; }

        public BooleanToVisibilityConverter()
        {
            this.ReverseValue = false;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
            {
                return DependencyProperty.UnsetValue;
            }

            bool boolValue = (bool)value;

            if (this.ReverseValue)
            {
                if (boolValue)
                {
                    return this.CollapsedWhenFalse ? Visibility.Collapsed : Visibility.Hidden;
                }

                return Visibility.Visible;
            }

            if (boolValue)
            {
                return Visibility.Visible;
            }

            return this.CollapsedWhenFalse ? Visibility.Collapsed : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;

            if (visibility == Visibility.Visible)
            {
                return !this.ReverseValue;
            }

            return this.ReverseValue;
        }
    }

    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringToVisibilityConverter : IValueConverter
    {
        public bool ReverseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;

            if (string.IsNullOrEmpty(str))
            {
                return this.ReverseValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return this.ReverseValue ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
