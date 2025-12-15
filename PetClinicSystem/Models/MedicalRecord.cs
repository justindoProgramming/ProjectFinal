using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    [Table("medicalrecords")]
    public class MedicalRecord
    {
        [Key]
        [Column("record_id")]
        public int RecordId { get; set; }

        [ForeignKey("Pet")]
        [Column("pet_id")]
        public int PetId { get; set; }
        public Pet Pet { get; set; }

        [ForeignKey("Staff")]
        [Column("staff_id")]
        public int StaffId { get; set; }
        public Account Staff { get; set; }

        [Column("prescription_id")]
        public int? PrescriptionId { get; set; }
        public Prescription? Prescription { get; set; }

        [Column("vaccination_id")]
        public int? VaccinationId { get; set; }
        public Vaccination? Vaccination { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("diagnosis")]
        public string Diagnosis { get; set; }

        [Column("treatment")]
        public string Treatment { get; set; }

        [Column("date")]
        public DateTime? Date { get; set; }

        // NEW: archive flag
        [Column("is_archived")]
        public bool IsArchived { get; set; } = false;
    }
}
