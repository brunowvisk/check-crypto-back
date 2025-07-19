using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using check_crypto.Models;
using check_crypto.DTOs;

namespace check_crypto.Services
{
    public interface IBinanceService
    {
        Task<CryptoData?> GetCryptoPriceAsync(string symbol);
        Task<List<CryptoCandleDto>> GetHistoricalDataAsync(string symbol, string timeframe = "1h", int hours = 24);
        Task StartWebSocketAsync(string symbol, CancellationToken cancellationToken);
        event Action<CryptoData>? OnPriceUpdate;
    }

    public class BinanceService : IBinanceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BinanceService> _logger;
        private ClientWebSocket? _webSocket;

        public event Action<CryptoData>? OnPriceUpdate;

        public BinanceService(HttpClient httpClient, ILogger<BinanceService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<CryptoData?> GetCryptoPriceAsync(string symbol)
        {
            try
            {
                var httpResponse = await _httpClient.GetAsync($"https://api.binance.com/api/v3/ticker/24hr?symbol={symbol.ToUpper()}USDT");
                
                LogRateLimitHeaders(httpResponse.Headers, "ticker/24hr");
                
                var response = await httpResponse.Content.ReadAsStringAsync();
                var binanceData = JsonConvert.DeserializeObject<BinanceTickerResponse>(response);

                if (binanceData == null) return null;

                return new CryptoData
                {
                    Symbol = symbol.ToUpper(),
                    Price = decimal.Parse(binanceData.LastPrice),
                    Volume = decimal.Parse(binanceData.Volume),
                    Change24h = decimal.Parse(binanceData.PriceChangePercent),
                    High24h = decimal.Parse(binanceData.HighPrice),
                    Low24h = decimal.Parse(binanceData.LowPrice),
                    Timestamp = DateTime.UtcNow,
                    Source = "Binance"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching crypto price from Binance for symbol: {Symbol}", symbol);
                return null;
            }
        }

        public async Task<List<CryptoCandleDto>> GetHistoricalDataAsync(string symbol, string timeframe = "1h", int hours = 24)
        {
            try
            {
                var binanceIntervals = new Dictionary<string, string>
                {
                    { "1m", "1m" },
                    { "5m", "5m" },
                    { "15m", "15m" },
                    { "30m", "30m" },
                    { "1h", "1h" },
                    { "4h", "4h" },
                    { "1d", "1d" }
                };

                var interval = binanceIntervals.ContainsKey(timeframe) ? binanceIntervals[timeframe] : "1h";
                var limit = Math.Ceiling((double)hours / GetIntervalHours(interval));
                var actualLimit = Math.Min((int)limit, 1000);

                var httpResponse = await _httpClient.GetAsync(
                    $"https://api.binance.com/api/v3/klines?symbol={symbol.ToUpper()}USDT&interval={interval}&limit={actualLimit}");

                LogRateLimitHeaders(httpResponse.Headers, "klines");

                var response = await httpResponse.Content.ReadAsStringAsync();
                var klines = JsonConvert.DeserializeObject<decimal[][]>(response);
                
                if (klines == null) return new List<CryptoCandleDto>();

                return klines.Select(kline => new CryptoCandleDto
                {
                    Timestamp = (long)kline[0],
                    Open = kline[1],
                    High = kline[2],
                    Low = kline[3],
                    Close = kline[4],
                    Volume = kline[5]
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching historical data from Binance for symbol: {Symbol}", symbol);
                return new List<CryptoCandleDto>();
            }
        }

        private double GetIntervalHours(string interval)
        {
            return interval switch
            {
                "1m" => 1.0 / 60,
                "5m" => 5.0 / 60,
                "15m" => 15.0 / 60,
                "30m" => 30.0 / 60,
                "1h" => 1.0,
                "4h" => 4.0,
                "1d" => 24.0,
                _ => 1.0
            };
        }

        private void LogRateLimitHeaders(System.Net.Http.Headers.HttpResponseHeaders headers, string endpoint)
        {
            var rateLimitInfo = new List<string>();

            if (headers.TryGetValues("x-mbx-used-weight", out var usedWeight))
                rateLimitInfo.Add($"Used Weight: {string.Join(",", usedWeight)}");

            if (headers.TryGetValues("x-mbx-used-weight-1m", out var usedWeight1m))
                rateLimitInfo.Add($"Used Weight 1m: {string.Join(",", usedWeight1m)}");

            if (headers.TryGetValues("x-mbx-order-count-10s", out var orderCount10s))
                rateLimitInfo.Add($"Order Count 10s: {string.Join(",", orderCount10s)}");

            if (headers.TryGetValues("x-mbx-order-count-1d", out var orderCount1d))
                rateLimitInfo.Add($"Order Count 1d: {string.Join(",", orderCount1d)}");

            if (headers.TryGetValues("retry-after", out var retryAfter))
                rateLimitInfo.Add($"Retry After: {string.Join(",", retryAfter)}");

            if (rateLimitInfo.Any())
            {
                _logger.LogInformation("Binance API Rate Limits for {Endpoint}: {RateLimits}", 
                    endpoint, string.Join(" | ", rateLimitInfo));
            }
        }

        public async Task StartWebSocketAsync(string symbol, CancellationToken cancellationToken)
        {
            try
            {
                _webSocket = new ClientWebSocket();
                var uri = new Uri($"wss://stream.binance.com:9443/ws/{symbol.ToLower()}usdt@ticker");
                
                await _webSocket.ConnectAsync(uri, cancellationToken);
                _logger.LogInformation("Connected to Binance WebSocket for {Symbol}", symbol);

                var buffer = new byte[1024 * 4];
                
                while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var tickerData = JsonConvert.DeserializeObject<BinanceWebSocketResponse>(message);
                        
                        if (tickerData != null)
                        {
                            var cryptoData = new CryptoData
                            {
                                Symbol = symbol.ToUpper(),
                                Price = decimal.Parse(tickerData.CurrentClosePrice),
                                Volume = decimal.Parse(tickerData.TotalTradedVolume),
                                Change24h = decimal.Parse(tickerData.PriceChangePercent),
                                High24h = decimal.Parse(tickerData.HighPrice),
                                Low24h = decimal.Parse(tickerData.LowPrice),
                                Timestamp = DateTime.UtcNow,
                                Source = "Binance WebSocket"
                            };

                            OnPriceUpdate?.Invoke(cryptoData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Binance WebSocket for symbol: {Symbol}", symbol);
            }
            finally
            {
                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                _webSocket?.Dispose();
            }
        }

        public void Dispose()
        {
            _webSocket?.Dispose();
        }
    }

    public class BinanceTickerResponse
    {
        [JsonProperty("lastPrice")]
        public string LastPrice { get; set; } = string.Empty;

        [JsonProperty("volume")]
        public string Volume { get; set; } = string.Empty;

        [JsonProperty("priceChangePercent")]
        public string PriceChangePercent { get; set; } = string.Empty;

        [JsonProperty("highPrice")]
        public string HighPrice { get; set; } = string.Empty;

        [JsonProperty("lowPrice")]
        public string LowPrice { get; set; } = string.Empty;
    }

    public class BinanceWebSocketResponse
    {
        [JsonProperty("c")]
        public string CurrentClosePrice { get; set; } = string.Empty;

        [JsonProperty("v")]
        public string TotalTradedVolume { get; set; } = string.Empty;

        [JsonProperty("P")]
        public string PriceChangePercent { get; set; } = string.Empty;

        [JsonProperty("h")]
        public string HighPrice { get; set; } = string.Empty;

        [JsonProperty("l")]
        public string LowPrice { get; set; } = string.Empty;
    }
}