using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PetClinicSystem.Models
{
    [Table("prescriptions")]
    public class Prescription
    {
        [Key]
        [Column("prescription_id")]
        public int PrescriptionId { get; set; }

        [Column("pet_id")]
        public int PetId { get; set; }

        // Navigation property – DO NOT VALIDATE
        [ValidateNever]
        public Pet? Pet { get; set; }

        [Column("staff_id")]
        public int StaffId { get; set; }

        // Navigation property – DO NOT VALIDATE
        [ValidateNever]
        public Account? Staff { get; set; }

        [Column("medication")]
        [Required]
        public string Medication { get; set; } = "";

        [Column("dosage")]
        [Required]
        public string Dosage { get; set; } = "";

        [Column("frequency")]
        [Required]
        public string Frequency { get; set; } = "";

        [Column("duration")]
        [Required]
        public string Duration { get; set; } = "";

        [Column("notes")]
        public string Notes { get; set; } = "";

        [Column("date")]
        public DateTime Date { get; set; }

        // ------ NOT MAPPED: for UI / stock updates only ------
        [NotMapped]
        public int? SelectedDrugId { get; set; }

        [NotMapped]
        public int? DispensedQuantity { get; set; }
    }
}
