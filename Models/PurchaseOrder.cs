using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class PurchaseOrder
    {
        [Key]
        public int OrderID { get; set; }

        [Required]
        public int SupplierID { get; set; }

        public int? StaffID { get; set; } // Who created the order

        [Required]
        public DateTime OrderDate { get; set; }

        public DateTime? DeliveryDate { get; set; }

        
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Ordered, Delivered, Cancelled

        [StringLength(1000)]
        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("SupplierID")]
        public virtual Supplier Supplier { get; set; }

        [ForeignKey("StaffID")]
        public virtual Staff Staff { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }

        public PurchaseOrder()
        {
            OrderDetails = new HashSet<OrderDetail>();
        }
    }
}