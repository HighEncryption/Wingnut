namespace Wingnut.UI.ViewModels
{
    using System;

    internal static class FormatHelpers
    {
        public static string FormatVoltage(double? value)
        {
            if (value == null)
            {
                return null;
            }

            return $"{value.Value:###.0} V";
        }

        public static string FormatCurrent(double? value)
        {
            if (value == null)
            {
                return null;
            }

            return $"{value.Value:##0.0} A";
        }

        public static string FormatFrequency(double? value)
        {
            if (value == null)
            {
                return null;
            }

            return $"{value.Value:###.0} Hz";
        }

        public static string FormatPercentage(double? value)
        {
            if (value == null)
            {
                return null;
            }

            return $"{value.Value:##0.0} %";
        }

        public static string FormatDate(DateTime? value, string format)
        {
            return value?.ToString(format);
        }

        public static string FormatMinutes(TimeSpan? value)
        {
            if (value == null)
            {
                return null;
            }

            return $"{value.Value.TotalMinutes} minutes";
        }

        public static string FormatTemperature(double? value, bool convertCtoF)
        {
            if (value == null)
            {
                return null;
            }

            var temp = value.Value;

            if (convertCtoF)
            {
                temp = (temp * 9) / 5 + 32;
            }

            return $"{temp:###.0}\u00B0 F";
        }

        public static string FormatEnum(Enum value)
        {
            return value?.ToString();
        }
    }
}