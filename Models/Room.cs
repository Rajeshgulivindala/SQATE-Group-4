using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class Room
    {
        [Key]
        public int RoomID { get; set; }

        public int? DepartmentID { get; set; }

        [Required]
        [StringLength(10)]
        [Index(IsUnique = true)]
        public string RoomNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string RoomType { get; set; } // ICU, General, Surgery, Emergency

        public int Capacity { get; set; } = 1;

        [StringLength(500)]
        public string Equipment { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Available"; // Available, Occupied, Maintenance

        public int? Floor { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("DepartmentID")]
        public virtual Department Department { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; }

        public Room()
        {
            Appointments = new HashSet<Appointment>();
        }
    }
}