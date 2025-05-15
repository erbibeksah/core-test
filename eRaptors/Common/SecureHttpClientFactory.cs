
namespace eRaptors.Common
{
    public class SecureHttpClientFactory
    {
        private static readonly HttpClientHandler Handler = new()
        {
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        };

        public static HttpClient CreateClient()
        {
            return new HttpClient(Handler);
        }
    }
}
