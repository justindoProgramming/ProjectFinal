using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    [Table("drugs")]
    public class Drug
    {
        [Key]
        [Column("product_id")]
        public int DrugId { get; set; }

        [Column("name")]
        [Required]
        public string DrugName { get; set; } = string.Empty;

        [Column("quantity")]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Column("minimum_stock")]
        [Range(0, int.MaxValue)]
        public int MinimumStock { get; set; } = 10;   // ⭐ NEW

        [Column("expiration_date")]
        public DateTime ExpiryDate { get; set; }

        [Column("unit_price")]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Column("dosage_type")]
        public string DosageType { get; set; } = string.Empty;

        [Column("base_price")]
        public decimal? BasePrice { get; set; }

        [Column("restock_notes")]
        public string? RestockNotes { get; set; }

        [Column("date_added")]
        public DateTime? DateAdded { get; set; }


        // ===============================
        // Computed Status (NOT Mapped)
        // ===============================

        [NotMapped]
        public bool IsLowStock => Quantity <= MinimumStock;

        [NotMapped]
        public bool IsOutOfStock => Quantity == 0;

        [NotMapped]
        public bool IsExpired => ExpiryDate < DateTime.Today;

        [NotMapped]
        public bool IsExpiringSoon => ExpiryDate <= DateTime.Today.AddDays(30);
    }
}
