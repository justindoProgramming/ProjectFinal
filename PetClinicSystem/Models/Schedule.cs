using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    [Table("schedule")]
    public class Schedule
    {
        [Key]
        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        // PET
        [Column("pet_id")]
        public int PetId { get; set; }
        public Pet? Pet { get; set; }

        // STAFF
        [Column("staff_id")]
        public int StaffId { get; set; }
        public Account? Staff { get; set; }

        // SERVICE (FK → services.service_id)
        [Column("service_id")]
        public int? ServiceId { get; set; }
        public Service? Service { get; set; }

        // Legacy service name column (still in your DB)
        [Column("service")]
        public string? ServiceName { get; set; }

        // DATE
        [Column("schedule_dateold")]
        public DateTime? ScheduleDateOld { get; set; }

        // SLOT
        [Column("slot_id")]
        public int? SlotId { get; set; }
        public TimeSlot? Slot { get; set; }

        // STATUS
        [Column("status")]
        public string? Status { get; set; }

       
    }
}
