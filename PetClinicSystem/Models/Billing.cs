    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace PetClinicSystem.Models
    {
        [Table("billing")]
        public class Billing
        {
            [Key]
            [Column("billing_id")]
            public int BillingId { get; set; }

            // FK → pets
            [Column("pet_id")]
            public int PetId { get; set; }

            [ForeignKey("PetId")]
            public Pet Pet { get; set; }

            // FK → accounts (staff)
            [Column("staff_id")]
            public int StaffId { get; set; }

            [ForeignKey("StaffId")]
            public Account Staff { get; set; }

            // Service Name
            [Column("service_name")]
            public string ServiceName { get; set; } = string.Empty;

            // Price per item
            [Column("service_price")]
            public decimal ServicePrice { get; set; }

            // Quantity
            [Column("quantity")]
            public int Quantity { get; set; }

            // Total = price * quantity
            [Column("total")]
            public decimal Total { get; set; }

            // Datetime
            [Column("billing_date")]
            public DateTime BillingDate { get; set; } = DateTime.Now;

            [Column("transaction_id")]
            public string TransactionId { get; set; } = string.Empty;

        }
    }
