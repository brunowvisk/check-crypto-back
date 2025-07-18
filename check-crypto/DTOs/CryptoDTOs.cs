using System.ComponentModel.DataAnnotations;

namespace check_crypto.DTOs
{
    public class CryptoDataDto
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public decimal Change24h { get; set; }
        public decimal High24h { get; set; }
        public decimal Low24h { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    public class CryptoHistoryDto
    {
        public Guid Id { get; set; }
        public string CryptoSymbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public decimal Change24h { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CryptoPriceRequestDto
    {
        [Required]
        public string Symbol { get; set; } = string.Empty;
    }

    public class CryptoHistoryRequestDto
    {
        [Required]
        public string Symbol { get; set; } = string.Empty;
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Limit { get; set; } = 100;
    }

    public class ChartDataPointDto
    {
        public DateTime Timestamp { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
    }

    public class CryptoChartDto
    {
        public string Symbol { get; set; } = string.Empty;
        public List<ChartDataPointDto> DataPoints { get; set; } = new();
        public decimal CurrentPrice { get; set; }
        public decimal Change24h { get; set; }
        public decimal High24h { get; set; }
        public decimal Low24h { get; set; }
        public decimal Volume24h { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class RealTimeUpdateDto
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public decimal Change24h { get; set; }
        public decimal High24h { get; set; }
        public decimal Low24h { get; set; }
        public DateTime Timestamp { get; set; }
    }
}