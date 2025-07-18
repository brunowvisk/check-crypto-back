using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace check_crypto.Models
{
    public class Alert
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [StringLength(10)]
        public string CryptoSymbol { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal MinPrice { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal MaxPrice { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? TriggeredAt { get; set; }
        
        [Column(TypeName = "decimal(18,8)")]
        public decimal? TriggeredPrice { get; set; }
        
        public string? TriggeredType { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}