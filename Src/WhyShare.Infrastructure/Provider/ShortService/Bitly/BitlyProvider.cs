using BitlyDotNET.Implementations;
using BitlyDotNET.Interfaces;
using WhyShare.Infrastructure.Interfaces;

namespace WhyShare.Infrastructure.Provider.ShortService.Bitly
{
    /// <summary>
    /// Bitly.com
    /// </summary>
    public class BitlyProvider : IShortProvider
    {
        private readonly BitlyService _client;

        public BitlyProvider(string user, string apiKey)
        {
            _client = new BitlyService(user, apiKey);
        }

        public string Url(string url)
        {
            var status = _client.Shorten(url, out var shortened);

            return status == StatusCode.OK ? shortened : url;
        }
    }
}
