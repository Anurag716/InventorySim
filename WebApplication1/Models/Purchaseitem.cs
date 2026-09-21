using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Models;

[Table("purchaseitems")]
[Index("ProductId", Name = "FK_PurchaseItems_Products")]
[Index("PurchaseId", Name = "FK_PurchaseItems_Purchases")]
public partial class Purchaseitem
{
    [Key]
    public int PurchaseItemId { get; set; }

    public int PurchaseId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    [Precision(10, 2)]
    public decimal UnitPrice { get; set; }

    [Precision(12, 2)]
    public decimal Subtotal { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("Purchaseitems")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("PurchaseId")]
    [InverseProperty("Purchaseitems")]
    public virtual Purchase Purchase { get; set; } = null!;
}
