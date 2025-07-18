using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace check_crypto.Models
{
    public class CryptoHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [StringLength(10)]
        public string CryptoSymbol { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal Price { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal Volume { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal Change24h { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}