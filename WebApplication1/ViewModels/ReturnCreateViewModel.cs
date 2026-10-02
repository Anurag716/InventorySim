using System.ComponentModel.DataAnnotations;

namespace Inventory.ViewModels
{
    public class ReturnCreateViewModel
    {
        public decimal SaleSubtotal { get; set; }

        public decimal SaleDiscountAmount { get; set; }

        [Required]
        public int SaleId { get; set; }

        public string SaleNumber { get; set; } = string.Empty;

        public string CustomerName { get; set; } = "Walk-in Customer";

        public DateTime SaleDate { get; set; }

        public List<ReturnItemViewModel> Items { get; set; }
            = new List<ReturnItemViewModel>();

        [Required(ErrorMessage = "Return reason is required.")]
        [StringLength(500)]
        public string? Reason { get; set; }

        public decimal RefundAmount { get; set; }
    }

    public class ReturnItemViewModel
    {
        public int SaleItemId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public int PurchasedQuantity { get; set; }

        public int AlreadyReturnedQuantity { get; set; }

        public int ReturnableQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int ReturnQuantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal RefundAmount { get; set; }

      
    }
}