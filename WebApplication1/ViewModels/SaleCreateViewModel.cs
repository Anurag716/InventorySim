using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventory.ViewModels
{
    public class SaleCreateViewModel
    {
        public int? CustomerId { get; set; }

        public List<SaleItemViewModel> Items { get; set; }
            = new List<SaleItemViewModel>();

        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; }

        [Range(0, 100)]
        public decimal TaxPercentage { get; set; }

        public IEnumerable<SelectListItem> Customers { get; set; }
            = new List<SelectListItem>();

        public IEnumerable<ProductOptionViewModel> Products { get; set; }
            = new List<ProductOptionViewModel>();
    }

    public class SaleItemViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }
    }
}