using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Models;

[Table("returnitems")]
[Index("ReturnId", Name = "FK_ReturnItems_Returns")]
[Index("SaleItemId", Name = "FK_ReturnItems_SaleItems")]
public partial class Returnitem
{
    [Key]
    public int ReturnItemId { get; set; }

    public int ReturnId { get; set; }

    public int SaleItemId { get; set; }

    public int Quantity { get; set; }

    [Precision(12, 2)]
    public decimal RefundAmount { get; set; }

    [ForeignKey("ReturnId")]
    [InverseProperty("Returnitems")]
    public virtual Return Return { get; set; } = null!;

    [ForeignKey("SaleItemId")]
    [InverseProperty("Returnitems")]
    public virtual Saleitem SaleItem { get; set; } = null!;
}
