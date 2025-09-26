using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class Patient
    {
        [Key]
        public int PatientID { get; set; }

        [Required]
        [StringLength(20)]
        [Index(IsUnique = true)]
        public string PatientCode { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        public DateTime DOB { get; set; }

        [StringLength(1)]
        public string Gender { get; set; } // M, F, O

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(255)]
        public string Address { get; set; }

        [StringLength(100)]
        public string EmergencyContact { get; set; }

        [StringLength(20)]
        public string EmergencyPhone { get; set; }

        [StringLength(100)]
        public string InsuranceProvider { get; set; }

        [StringLength(50)]
        public string InsuranceNumber { get; set; }

        [StringLength(5)]
        public string BloodType { get; set; }

        [StringLength(500)]
        public string Allergies { get; set; }

        [StringLength(500)]
        public string MedicalConditions { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Appointment> Appointments { get; set; }
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; }
    }
}