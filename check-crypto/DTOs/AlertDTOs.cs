using System.ComponentModel.DataAnnotations;

namespace check_crypto.DTOs
{
    public class CreateAlertDto
    {
        [Required]
        [StringLength(10)]
        public string CryptoSymbol { get; set; } = string.Empty;
        
        [Required]
        [Range(0.00000001, double.MaxValue)]
        public decimal MinPrice { get; set; }
        
        [Required]
        [Range(0.00000001, double.MaxValue)]
        public decimal MaxPrice { get; set; }
    }

    public class UpdateAlertDto
    {
        [Range(0.00000001, double.MaxValue)]
        public decimal? MinPrice { get; set; }
        
        [Range(0.00000001, double.MaxValue)]
        public decimal? MaxPrice { get; set; }
        
        public bool? IsActive { get; set; }
    }

    public class AlertDto
    {
        public int Id { get; set; }
        public string CryptoSymbol { get; set; } = string.Empty;
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? TriggeredAt { get; set; }
        public decimal? TriggeredPrice { get; set; }
        public string? TriggeredType { get; set; }
    }

    public class AlertHistoryDto
    {
        public int Id { get; set; }
        public string CryptoSymbol { get; set; } = string.Empty;
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public DateTime TriggeredAt { get; set; }
        public decimal TriggeredPrice { get; set; }
        public string TriggeredType { get; set; } = string.Empty;
    }
}