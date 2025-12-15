using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    [Table("vaccinations")]
    public class Vaccination
    {
        [Key]
        [Column("vaccination_id")]
        public int VaccinationId { get; set; }

        [Column("pet_id")]
        public int PetId { get; set; }
        public Pet Pet { get; set; }

        [Column("vaccine_name")]
        public string VaccineName { get; set; } = string.Empty;

        [Column("date_given")]
        public DateTime? DateGiven { get; set; }

        [Column("next_due_date")]
        public DateTime? NextDueDate { get; set; }

        [Column("staff_id")]
        public int StaffId { get; set; }
        public Account Staff { get; set; }
    }
}
