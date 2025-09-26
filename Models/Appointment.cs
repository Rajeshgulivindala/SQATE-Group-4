using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentID { get; set; }

        [Required]
        public int PatientID { get; set; }

        [Required]
        public int StaffID { get; set; }

        public int? RoomID { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        public int Duration { get; set; } = 30; // minutes

        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Scheduled";

        [StringLength(255)]
        public string Reason { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("PatientID")]
        public virtual Patient Patient { get; set; }

        [ForeignKey("StaffID")]
        public virtual Staff Staff { get; set; }

        [ForeignKey("RoomID")]
        public virtual Room Room { get; set; }
    }
}