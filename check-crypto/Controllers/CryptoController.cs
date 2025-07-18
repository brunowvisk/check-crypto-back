using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using check_crypto.Services;
using check_crypto.DTOs;
using check_crypto.Data;
using check_crypto.Models;

namespace check_crypto.Controllers
{
    [ApiController]
    [Route("api/crypto")]
    public class CryptoController : ControllerBase
    {
        private readonly IBinanceService _binanceService;
        private readonly ICoinGeckoService _coinGeckoService;
        private readonly AppDbContext _context;
        private readonly ILogger<CryptoController> _logger;

        public CryptoController(
            IBinanceService binanceService,
            ICoinGeckoService coinGeckoService,
            AppDbContext context,
            ILogger<CryptoController> logger)
        {
            _binanceService = binanceService;
            _coinGeckoService = coinGeckoService;
            _context = context;
            _logger = logger;
        }

        [HttpGet("price/{symbol}")]
        public async Task<IActionResult> GetCryptoPrice(string symbol)
        {
            try
            {
                var binanceData = await _binanceService.GetCryptoPriceAsync(symbol);
                
                if (binanceData != null)
                {
                    return Ok(new CryptoDataDto
                    {
                        Symbol = binanceData.Symbol,
                        Price = binanceData.Price,
                        Volume = binanceData.Volume,
                        Change24h = binanceData.Change24h,
                        High24h = binanceData.High24h,
                        Low24h = binanceData.Low24h,
                        Timestamp = binanceData.Timestamp,
                        Source = binanceData.Source
                    });
                }

                var coinGeckoData = await _coinGeckoService.GetCryptoPriceAsync(symbol);
                
                if (coinGeckoData != null)
                {
                    return Ok(new CryptoDataDto
                    {
                        Symbol = coinGeckoData.Symbol,
                        Price = coinGeckoData.Price,
                        Volume = coinGeckoData.Volume,
                        Change24h = coinGeckoData.Change24h,
                        High24h = coinGeckoData.High24h,
                        Low24h = coinGeckoData.Low24h,
                        Timestamp = coinGeckoData.Timestamp,
                        Source = coinGeckoData.Source
                    });
                }

                return NotFound(new { message = $"Data not found for {symbol}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting crypto price for {Symbol}", symbol);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("prices")]
        public async Task<IActionResult> GetMultipleCryptoPrices([FromQuery] string[] symbols)
        {
            try
            {
                if (symbols == null || symbols.Length == 0)
                {
                    return BadRequest(new { message = "At least one symbol must be provided" });
                }

                var tasks = symbols.Select(async symbol =>
                {
                    var binanceData = await _binanceService.GetCryptoPriceAsync(symbol);
                    if (binanceData != null) return binanceData;
                    
                    return await _coinGeckoService.GetCryptoPriceAsync(symbol);
                });

                var results = await Task.WhenAll(tasks);
                var validResults = results.Where(r => r != null).ToList()!;

                var dtos = validResults.Select(data => new CryptoDataDto
                {
                    Symbol = data!.Symbol,
                    Price = data.Price,
                    Volume = data.Volume,
                    Change24h = data.Change24h,
                    High24h = data.High24h,
                    Low24h = data.Low24h,
                    Timestamp = data.Timestamp,
                    Source = data.Source
                }).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multiple crypto prices");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("history")]
        [Authorize]
        public async Task<IActionResult> SaveCryptoHistory([FromBody] CryptoPriceRequestDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var cryptoData = await _binanceService.GetCryptoPriceAsync(request.Symbol) ??
                               await _coinGeckoService.GetCryptoPriceAsync(request.Symbol);

                if (cryptoData == null)
                {
                    return NotFound(new { message = $"Data not found for {request.Symbol}" });
                }

                var history = new CryptoHistory
                {
                    UserId = userId,
                    CryptoSymbol = cryptoData.Symbol,
                    Price = cryptoData.Price,
                    Volume = cryptoData.Volume,
                    Change24h = cryptoData.Change24h,
                    Timestamp = DateTime.UtcNow
                };

                _context.CryptoHistories.Add(history);
                await _context.SaveChangesAsync();

                return Ok(new CryptoHistoryDto
                {
                    Id = history.Id,
                    CryptoSymbol = history.CryptoSymbol,
                    Price = history.Price,
                    Volume = history.Volume,
                    Change24h = history.Change24h,
                    Timestamp = history.Timestamp
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving crypto history");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetCryptoHistory([FromQuery] CryptoHistoryRequestDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var query = _context.CryptoHistories
                    .Where(h => h.UserId == userId && h.CryptoSymbol == request.Symbol.ToUpper());

                if (request.StartDate.HasValue)
                {
                    query = query.Where(h => h.Timestamp >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(h => h.Timestamp <= request.EndDate.Value);
                }

                var histories = await query
                    .OrderByDescending(h => h.Timestamp)
                    .Take(request.Limit ?? 100)
                    .ToListAsync();

                var dtos = histories.Select(h => new CryptoHistoryDto
                {
                    Id = h.Id,
                    CryptoSymbol = h.CryptoSymbol,
                    Price = h.Price,
                    Volume = h.Volume,
                    Change24h = h.Change24h,
                    Timestamp = h.Timestamp
                }).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting crypto history");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("supported-symbols")]
        public IActionResult GetSupportedSymbols()
        {
            var symbols = new[]
            {
                "BTC", "ETH", "BNB", "ADA", "DOT", "XRP", "LINK",
                "XLM", "USDT", "USDC", "DOGE", "SOL", "AVAX", "MATIC",
                "SAND", "AAVE", "PAXG", "IMX"
            };

            return Ok(new { symbols });
        }

        [HttpGet("historical/{symbol}")]
        public async Task<IActionResult> GetHistoricalData(string symbol, [FromQuery] string timeframe = "1h", [FromQuery] int hours = 24)
        {
            try
            {
                var historicalData = await _binanceService.GetHistoricalDataAsync(symbol, timeframe, hours);
                
                if (!historicalData.Any())
                {
                    return NotFound(new { message = $"Historical data not found for {symbol}" });
                }

                return Ok(historicalData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting historical data for {Symbol}", symbol);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("chart/{symbol}")]
        public async Task<IActionResult> GetChartData(string symbol, int limit = 100)
        {
            try
            {
                // Get current price data
                var currentData = await _binanceService.GetCryptoPriceAsync(symbol);
                if (currentData == null)
                {
                    return NotFound(new { message = $"Data not found for {symbol}" });
                }

                // Get historical data points (last 'limit' records)
                var historyData = await _context.CryptoHistories
                    .Where(h => h.CryptoSymbol == symbol.ToUpper())
                    .OrderByDescending(h => h.Timestamp)
                    .Take(limit)
                    .OrderBy(h => h.Timestamp)
                    .ToListAsync();

                var dataPoints = historyData.Select(h => new ChartDataPointDto
                {
                    Timestamp = h.Timestamp,
                    Price = h.Price,
                    Volume = h.Volume
                }).ToList();

                // If no historical data, create a single point with current data
                if (!dataPoints.Any())
                {
                    dataPoints.Add(new ChartDataPointDto
                    {
                        Timestamp = DateTime.UtcNow,
                        Price = currentData.Price,
                        Volume = currentData.Volume
                    });
                }

                var chartData = new CryptoChartDto
                {
                    Symbol = symbol.ToUpper(),
                    DataPoints = dataPoints,
                    CurrentPrice = currentData.Price,
                    Change24h = currentData.Change24h,
                    High24h = currentData.High24h,
                    Low24h = currentData.Low24h,
                    Volume24h = currentData.Volume,
                    LastUpdated = DateTime.UtcNow
                };

                return Ok(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart data for {Symbol}", symbol);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}