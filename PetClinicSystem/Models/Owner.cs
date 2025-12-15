using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    [Table("owners")]
    public class Owner
    {
        [Key]
        [Column("owner_id")]
        public int OwnerId { get; set; }

        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Column("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Column("emergency_contact1")]
        public string EmergencyContact1 { get; set; } = string.Empty;

        [Column("emergency_contact2")]
        public string EmergencyContact2 { get; set; } = string.Empty;

        // ⭐ REQUIRED FOREIGN KEY → FIXES YOUR ISSUE
        [Column("account_id")]
        public int AccountId { get; set; }

        // ⭐ NAVIGATION PROPERTY
        public Account Account { get; set; }

        // Existing pets list
        public List<Pet> Pets { get; set; } = new List<Pet>();
    }
}
