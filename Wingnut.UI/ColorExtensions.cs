namespace Wingnut.UI
{
    using System;
    using System.Windows.Media;

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