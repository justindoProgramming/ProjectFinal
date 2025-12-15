using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    [Table("time_slots")]
    public class TimeSlot
    {
        [Key]
        [Column("slot_id")]
        public int SlotId { get; set; }

        [Column("start_time")]
        public TimeSpan StartTime { get; set; }

        [Column("end_time")]
        public TimeSpan EndTime { get; set; }

        // OPTIONAL: Convenience display (not mapped)
        [NotMapped]
        public string Display => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }
}
