using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    [Table("pets")]
    public class Pet
    {
        [Key]
        [Column("pet_id")]
        public int PetId { get; set; }

        [Column("pet_code")]
        public string PetCode { get; set; } = string.Empty;

        [Column("owner_id")]
        public int OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        [ValidateNever]
        public Owner? Owner { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("species")]
        public string Species { get; set; } = string.Empty;

        [Column("breed")]
        public string Breed { get; set; } = string.Empty;

        [Column("gender")]
        public string? Gender { get; set; } = string.Empty;

        [Column("birthdate")]
        public DateTime BirthDate { get; set; }

        [Column("color_markings")]
        public string ColorMarkings { get; set; } = string.Empty;

        [Column("weight")]
        public decimal Weight { get; set; }

        [Column("medical_history")]
        public string MedicalHistory { get; set; } = string.Empty;
    }

}