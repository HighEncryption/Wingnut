namespace Wingnut.UI.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

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
}