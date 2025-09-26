using System;

namespace HospitalManagementSystem.Views.UserControls
{
    // A data model for a patient, matching the fields from the database.
    public class Patient
    {
        public int PatientID { get; set; }
        public string PatientCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DOB { get; set; }
        public string Gender { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string EmergencyContact { get; set; }
        public string EmergencyPhone { get; set; }
        public string InsuranceProvider { get; set; }
        public string InsuranceNumber { get; set; }
        public string BloodType { get; set; }
        public string Allergies { get; set; }
        public string MedicalConditions { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
