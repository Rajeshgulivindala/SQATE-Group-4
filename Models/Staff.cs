using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class Staff
    {
        [Key]
        public int StaffID { get; set; }

        [Required]
        [Index(IsUnique = true)]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        public int? DepartmentID { get; set; }

        [Required]
        [StringLength(20)]
        [Index(IsUnique = true)]
        public string EmployeeCode { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [StringLength(50)]
        public string Position { get; set; }

        [StringLength(100)]
        [Index(IsUnique = true)]
        public string Email { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(255)]
        public string Address { get; set; }

        [Required]
        public DateTime HireDate { get; set; }

     
        public decimal? Salary { get; set; }

        [StringLength(500)]
        public string Qualifications { get; set; }

        [StringLength(50)]
        public string LicenseNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;


        [ForeignKey("DepartmentID")]
        public virtual Department Department { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; }
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; }
    }
}
