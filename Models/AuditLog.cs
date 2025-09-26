using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class AuditLog
    {
        [Key]
        public int LogID { get; set; }

        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [StringLength(45)]
        public string IPAddress { get; set; }

        [StringLength(500)]
        public string AdditionalData { get; set; }

        // Navigation property
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
    }
}