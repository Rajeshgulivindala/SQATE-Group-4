using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class Inventory
    {
        [Key]
        public int ItemID { get; set; }

        public int? SupplierID { get; set; }

        [Required]
        [StringLength(20)]
        [Index(IsUnique = true)]
        public string ItemCode { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public int CurrentStock { get; set; } = 0;

        [Required]
        public int MinimumLevel { get; set; } = 10;

        [Required]
        public int MaximumLevel { get; set; } = 1000;

      
        public decimal? UnitPrice { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(100)]
        public string Location { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("SupplierID")]
        public virtual Supplier Supplier { get; set; }
    }
}