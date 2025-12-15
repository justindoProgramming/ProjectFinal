using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    [Table("accounts")]
    public class Account
    {
        [Key]
        [Column("account_id")]
        public int AccountId { get; set; }

        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("password_hash")]
        public string PasswordHash { get; set; }

        // 0 = Client, 1 = Admin
        [Column("is_admin")]
        public int IsAdmin { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // NEW: Link to Owner (1:1)
        public Owner Owner { get; set; }
    }
}
