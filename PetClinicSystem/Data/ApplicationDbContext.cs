using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Models;
using System.Security.Principal;

namespace PetClinicSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Owner> Owners { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<Schedule> Schedule { get; set; }
        public DbSet<Billing> Billing { get; set; }
        public DbSet<Drug> Drugs { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<Vaccination> Vaccinations { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Service> Service { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }

    }
}
