using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    [Table("services")]
    public class Service
    {
        [Key]
        [Column("service_id")]
        public int ServiceId { get; set; }

        [Column("name")]       // <--- Correct DB column
        public string Name { get; set; }

        [Column("category")]
        public string Category { get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        [Column("duration_minutes")]
        public int DurationMinutes { get; set; }
    }
}
