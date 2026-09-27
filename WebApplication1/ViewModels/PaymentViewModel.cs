using System.ComponentModel.DataAnnotations;

namespace Inventory.ViewModels
{
    public class PaymentViewModel
    {
        public int SaleId { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}