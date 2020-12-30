namespace Wingnut.Data
{
    using System;
    using System.Diagnostics.Contracts;
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
        public static readonly DateTime UnixEpoch = 
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static bool OlderThan(this DateTime dateTime, TimeSpan timeSpan)
        {
            return dateTime.ToUniversalTime() < DateTime.UtcNow.Subtract(timeSpan);
        }

        [Pure]
        public static long ToEpochSeconds(this DateTime datetime)
        {
            return Convert.ToInt64((datetime - UnixEpoch).TotalSeconds);
        }
    }
}