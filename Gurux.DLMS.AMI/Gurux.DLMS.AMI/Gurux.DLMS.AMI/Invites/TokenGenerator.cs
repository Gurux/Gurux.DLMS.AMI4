using System.Security.Cryptography;

namespace Gurux.DLMS.AMI.Data
{  
    public static class TokenGenerator
    {
        public static string CreateUrlSafeToken(int bytes = 16)
        {
            Span<byte> buf = stackalloc byte[bytes];
            RandomNumberGenerator.Fill(buf);
            return Convert.ToBase64String(buf).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
