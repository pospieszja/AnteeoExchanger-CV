using System.Security.Cryptography;
using System.Text;

namespace AnteeoExchanger.Helpers
{
    public static class Helper
    {
        public static string MD5Hash(byte[] input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();

            byte[] bytes = md5provider.ComputeHash(input);

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString().ToUpper();
        }
    }
}
