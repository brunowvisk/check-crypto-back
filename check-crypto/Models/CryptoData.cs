namespace check_crypto.Models
{
    public class CryptoData
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public decimal Change24h { get; set; }
        public decimal High24h { get; set; }
        public decimal Low24h { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = string.Empty;
    }
}