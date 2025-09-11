using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class MedicalRecord
    {
        [Key]
        public int RecordID { get; set; }

        [Required]
        public int PatientID { get; set; }

        [Required]
        public int StaffID { get; set; }

        [Required]
        public DateTime VisitDate { get; set; }

        [StringLength(500)]
        public string ChiefComplaint { get; set; }

        [StringLength(500)]
        public string Diagnosis { get; set; }

        [StringLength(1000)]
        public string Treatment { get; set; }

        [StringLength(1000)]
        public string Prescription { get; set; }

        [StringLength(500)]
        public string VitalSigns { get; set; } // Blood pressure, temperature, etc.

        [StringLength(2000)]
        public string Notes { get; set; }

        public DateTime? FollowUpDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        [StringLength(100)]
        public string UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("PatientID")]
        public virtual Patient Patient { get; set; }

        [ForeignKey("StaffID")]
        public virtual Staff Staff { get; set; }
    }
}