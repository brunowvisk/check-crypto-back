using Newtonsoft.Json;
using check_crypto.Models;

namespace check_crypto.Services
{
    public interface ICoinGeckoService
    {
        Task<CryptoData?> GetCryptoPriceAsync(string symbol);
        Task<List<CryptoData>> GetMultipleCryptoPricesAsync(string[] symbols);
    }

    public class CoinGeckoService : ICoinGeckoService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CoinGeckoService> _logger;
        private readonly Dictionary<string, string> _symbolMapping;

        public CoinGeckoService(HttpClient httpClient, ILogger<CoinGeckoService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            _symbolMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "BTC", "bitcoin" },
                { "ETH", "ethereum" },
                { "BNB", "binancecoin" },
                { "ADA", "cardano" },
                { "DOT", "polkadot" },
                { "XRP", "ripple" },
                { "LINK", "chainlink" },
                { "XLM", "stellar" },
                { "USDT", "tether" },
                { "USDC", "usd-coin" },
                { "DOGE", "dogecoin" },
                { "SOL", "solana" },
                { "AVAX", "avalanche-2" },
                { "MATIC", "matic-network" },
                { "SAND", "the-sandbox" },
                { "AAVE", "aave" },
                { "PAXG", "pax-gold" },
                { "IMX", "immutable-x" }
            };
        }

        public async Task<CryptoData?> GetCryptoPriceAsync(string symbol)
        {
            try
            {
                var coinId = GetCoinId(symbol);
                if (string.IsNullOrEmpty(coinId))
                {
                    _logger.LogWarning("Symbol {Symbol} not found in mapping", symbol);
                    return null;
                }

                var response = await _httpClient.GetStringAsync($"https://api.coingecko.com/api/v3/simple/price?ids={coinId}&vs_currencies=usd&include_24hr_change=true&include_24hr_vol=true&include_last_updated_at=true");
                var data = JsonConvert.DeserializeObject<Dictionary<string, CoinGeckoPrice>>(response);

                if (data == null || !data.ContainsKey(coinId))
                {
                    return null;
                }

                var priceData = data[coinId];
                
                return new CryptoData
                {
                    Symbol = symbol.ToUpper(),
                    Price = priceData.Usd,
                    Volume = priceData.Usd24hVol ?? 0,
                    Change24h = priceData.Usd24hChange ?? 0,
                    High24h = 0,
                    Low24h = 0,
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(priceData.LastUpdatedAt ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()).DateTime,
                    Source = "CoinGecko"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching crypto price from CoinGecko for symbol: {Symbol}", symbol);
                return null;
            }
        }

        public async Task<List<CryptoData>> GetMultipleCryptoPricesAsync(string[] symbols)
        {
            var result = new List<CryptoData>();
            
            try
            {
                var coinIds = symbols.Select(GetCoinId).Where(id => !string.IsNullOrEmpty(id)).ToArray();
                
                if (coinIds.Length == 0)
                {
                    return result;
                }

                var idsParam = string.Join(",", coinIds);
                var response = await _httpClient.GetStringAsync($"https://api.coingecko.com/api/v3/simple/price?ids={idsParam}&vs_currencies=usd&include_24hr_change=true&include_24hr_vol=true&include_last_updated_at=true");
                var data = JsonConvert.DeserializeObject<Dictionary<string, CoinGeckoPrice>>(response);

                if (data == null)
                {
                    return result;
                }

                foreach (var symbol in symbols)
                {
                    var coinId = GetCoinId(symbol);
                    if (!string.IsNullOrEmpty(coinId) && data.ContainsKey(coinId))
                    {
                        var priceData = data[coinId];
                        result.Add(new CryptoData
                        {
                            Symbol = symbol.ToUpper(),
                            Price = priceData.Usd,
                            Volume = priceData.Usd24hVol ?? 0,
                            Change24h = priceData.Usd24hChange ?? 0,
                            High24h = 0,
                            Low24h = 0,
                            Timestamp = DateTimeOffset.FromUnixTimeSeconds(priceData.LastUpdatedAt ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()).DateTime,
                            Source = "CoinGecko"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching multiple crypto prices from CoinGecko");
            }

            return result;
        }

        private string GetCoinId(string symbol)
        {
            return _symbolMapping.TryGetValue(symbol.ToUpper(), out var coinId) ? coinId : string.Empty;
        }
    }

    public class CoinGeckoPrice
    {
        [JsonProperty("usd")]
        public decimal Usd { get; set; }

        [JsonProperty("usd_24h_vol")]
        public decimal? Usd24hVol { get; set; }

        [JsonProperty("usd_24h_change")]
        public decimal? Usd24hChange { get; set; }

        [JsonProperty("last_updated_at")]
        public long? LastUpdatedAt { get; set; }
    }
}