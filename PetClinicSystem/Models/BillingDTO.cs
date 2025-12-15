using System;

namespace PetClinicSystem.Models
{
    public class BillingDTO
    {
        public int PetId { get; set; }
        public string ServiceName { get; set; }
        public decimal ServicePrice { get; set; }
        public int Quantity { get; set; }
    }
}
