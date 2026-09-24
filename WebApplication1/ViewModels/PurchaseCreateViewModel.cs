using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventory.ViewModels
{
    public class PurchaseCreateViewModel
    {
        [Required]
        public int SupplierId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        public List<PurchaseItemViewModel> Items { get; set; }
            = new List<PurchaseItemViewModel>();

        public IEnumerable<SelectListItem> Suppliers { get; set; }
            = new List<SelectListItem>();

        public IEnumerable<ProductOptionViewModel> Products { get; set; }
            = new List<ProductOptionViewModel>();
    }

    public class PurchaseItemViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }

    public class ProductOptionViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public decimal PurchasePrice { get; set; }
    }
}