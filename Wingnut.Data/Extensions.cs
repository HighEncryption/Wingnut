namespace Wingnut.Data
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;

    public static class SecureStringExtensions
    {
        public static string GetDecrypted(this SecureString secureString)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        public static SecureString FromString(string value)
        {
            SecureString secureString = new SecureString();
            foreach (char c in value)
            {
                secureString.AppendChar(c);
            }

            secureString.MakeReadOnly();

            return secureString;
        }
    }

    public static class DateTimeExtensions
    {
        public static bool OlderThan(this DateTime dateTime, TimeSpan timeSpan)
        {
            return dateTime.ToUniversalTime() < DateTime.UtcNow.Subtract(timeSpan);
        }
    }

}