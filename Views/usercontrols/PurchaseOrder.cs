using System;

namespace HospitalManagementSystem.Views.UserControls
{
    // A data model for a purchase order.
    // This class is now defined in a single, shared file to avoid ambiguity errors.
    public class PurchaseOrder
    {
        public int OrderId { get; set; }
        public int SupplierId { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        
    }
}
