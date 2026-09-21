using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Models;

[Table("saleitems")]
[Index("ProductId", Name = "FK_SaleItems_Products")]
[Index("SaleId", Name = "FK_SaleItems_Sales")]
public partial class Saleitem
{
    [Key]
    public int SaleItemId { get; set; }

    public int SaleId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    [Precision(10, 2)]
    public decimal UnitPrice { get; set; }

    [Precision(12, 2)]
    public decimal DiscountAmount { get; set; }

    [Precision(12, 2)]
    public decimal Subtotal { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("Saleitems")]
    public virtual Product Product { get; set; } = null!;

    [InverseProperty("SaleItem")]
    public virtual ICollection<Returnitem> Returnitems { get; set; } = new List<Returnitem>();

    [ForeignKey("SaleId")]
    [InverseProperty("Saleitems")]
    public virtual Sale Sale { get; set; } = null!;
}
