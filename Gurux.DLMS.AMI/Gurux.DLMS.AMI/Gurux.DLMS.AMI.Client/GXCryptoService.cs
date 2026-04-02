using Gurux.DLMS.AMI.Module;
using System.Net.Http.Headers;
using System.Text;

namespace Gurux.DLMS.AMI.Client
{
    internal class GXCryptoService : IAmiCryptoService
    {
        private readonly IServiceProvider _serviceProvider;

        public GXCryptoService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private async Task<byte[]> ReadAsync(byte[] encrypted, string url)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
                var content = new ByteArrayContent(encrypted);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
        }

        public async Task<byte[]> DecryptAsync(byte[] encrypted)
        {
            return await ReadAsync(encrypted, "api/Decrypt");
        }

        public async Task<byte[]> EncryptAsync(byte[] plainText)
        {
            return await ReadAsync(plainText, "api/Encrypt");
        }

        public async Task<string> DecryptAsync(string encrypted)
        {
            byte[] value = await ReadAsync(Convert.FromBase64String(encrypted), "api/Decrypt");
            return Convert.ToBase64String(value);
        }

        public async Task<string> EncryptAsync(string plainText)
        {
            byte[] value = await ReadAsync(ASCIIEncoding.UTF8.GetBytes(plainText), "api/Encrypt");
            return Convert.ToBase64String(value);
        }
    }
}