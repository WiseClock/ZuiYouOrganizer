using System;
using System.Security.Cryptography;
using System.Text;

namespace ZuiYouNameOrganizer
{
    class UtilHelper
    {
        private static MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

        public static string GetTimeMD5()
        {
            return GetMD5(GetTimeStamp());
        }

        public static string GetMD5(string input)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString().ToLower();
        }

        public static string GetTimeStamp()
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            return unixTimestamp.ToString();
        }

        public static void PrintGapLine()
        {
            Console.WriteLine(new string('-', 80));
        }
    }
}
