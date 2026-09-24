using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventory.ViewModels
{
    public class PurchaseEditViewModel
    {
        public int PurchaseId { get; set; }

        public string PurchaseNumber { get; set; } = string.Empty;

        [Required]
        public int SupplierId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; }

        public List<PurchaseEditItemViewModel> Items { get; set; }
            = new List<PurchaseEditItemViewModel>();

        public IEnumerable<SelectListItem> Suppliers { get; set; }
            = new List<SelectListItem>();

        public IEnumerable<ProductOptionViewModel> Products { get; set; }
            = new List<ProductOptionViewModel>();
    }

    public class PurchaseEditItemViewModel
    {
        public int PurchaseItemId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }
}