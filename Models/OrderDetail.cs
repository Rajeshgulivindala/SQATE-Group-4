using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementSystem.Models
{
    public class OrderDetail
    {
        [Key]
        public int DetailID { get; set; }

        [Required]
        public int OrderID { get; set; }

        [Required]
        public int ItemID { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        public decimal SubTotal { get; set; }

        // Navigation properties
        [ForeignKey("OrderID")]
        public virtual PurchaseOrder PurchaseOrder { get; set; }

        [ForeignKey("ItemID")]
        public virtual Inventory Item { get; set; }
    }
}