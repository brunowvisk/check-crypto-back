using Microsoft.AspNetCore.SignalR;
using check_crypto.Hubs;
using check_crypto.DTOs;

namespace check_crypto.Services
{
    public class CryptoUpdateService : BackgroundService
    {
        private readonly IHubContext<CryptoHub> _hubContext;
        private readonly IBinanceService _binanceService;
        private readonly ICoinGeckoService _coinGeckoService;
        private readonly ILogger<CryptoUpdateService> _logger;
        private readonly List<string> _trackedSymbols = new() { "BTC", "ETH", "BNB", "ADA", "DOT" };

        public CryptoUpdateService(
            IHubContext<CryptoHub> hubContext, 
            IBinanceService binanceService, 
            ICoinGeckoService coinGeckoService,
            ILogger<CryptoUpdateService> logger)
        {
            _hubContext = hubContext;
            _binanceService = binanceService;
            _coinGeckoService = coinGeckoService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateCryptoPrices();
                    await Task.Delay(5000, stoppingToken); // 5 seconds
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating crypto prices");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        private async Task UpdateCryptoPrices()
        {
            var tasks = _trackedSymbols.Select(async symbol =>
            {
                try
                {
                    var cryptoData = await _binanceService.GetCryptoPriceAsync(symbol);
                    if (cryptoData != null)
                    {
                        var update = new RealTimeUpdateDto
                        {
                            Symbol = symbol,
                            Price = cryptoData.Price,
                            Volume = cryptoData.Volume,
                            Change24h = cryptoData.Change24h,
                            High24h = cryptoData.High24h,
                            Low24h = cryptoData.Low24h,
                            Timestamp = DateTime.UtcNow
                        };

                        await _hubContext.Clients.Group($"crypto_{symbol}")
                            .SendAsync("PriceUpdate", update);

                        _logger.LogInformation("Updated price for {Symbol}: ${Price}", symbol, cryptoData.Price);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating price for {Symbol}", symbol);
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}