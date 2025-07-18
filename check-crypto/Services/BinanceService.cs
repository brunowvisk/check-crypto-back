using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using check_crypto.Models;

namespace check_crypto.Services
{
    public interface IBinanceService
    {
        Task<CryptoData?> GetCryptoPriceAsync(string symbol);
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
                var response = await _httpClient.GetStringAsync($"https://api.binance.com/api/v3/ticker/24hr?symbol={symbol.ToUpper()}USDT");
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