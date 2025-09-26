using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class Supplier
    {
        [Key]
        public int SupplierID { get; set; }

        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; }

        [StringLength(100)]
        public string ContactPerson { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(255)]
        public string Address { get; set; }

        [StringLength(50)]
        public string City { get; set; }

        [StringLength(50)]
        public string Country { get; set; }

        [StringLength(100)]
        public string PaymentTerms { get; set; }

       
        public decimal? Rating { get; set; } // 1.0 to 5.0

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        public virtual ICollection<Inventory> InventoryItems { get; set; }
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; }

        public Supplier()
        {
            InventoryItems = new HashSet<Inventory>();
            PurchaseOrders = new HashSet<PurchaseOrder>();
        }
    }
}