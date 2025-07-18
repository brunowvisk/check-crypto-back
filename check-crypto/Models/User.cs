using System.ComponentModel.DataAnnotations;

namespace check_crypto.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();
        
        public virtual ICollection<CryptoHistory> CryptoHistories { get; set; } = new List<CryptoHistory>();
    }
}