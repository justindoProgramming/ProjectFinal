using System.Collections.Generic;

namespace PetClinicSystem.Models.ViewModels
{
    public class ClientRecordsViewModel
    {
        public List<Pet> Pets { get; set; } = new();
        public List<MedicalRecord> MedicalRecords { get; set; } = new();
        public List<Prescription> Prescriptions { get; set; } = new();
        public List<Vaccination> Vaccinations { get; set; } = new();
    }
}
