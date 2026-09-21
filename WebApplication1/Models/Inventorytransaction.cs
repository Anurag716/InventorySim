using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Models;

[Table("inventorytransactions")]
[Index("ProductId", Name = "FK_InventoryTransactions_Products")]
[Index("PurchaseId", Name = "FK_InventoryTransactions_Purchases")]
[Index("ReturnId", Name = "FK_InventoryTransactions_Returns")]
[Index("SaleId", Name = "FK_InventoryTransactions_Sales")]
[Index("CreatedByUserId", Name = "FK_InventoryTransactions_Users")]
public partial class Inventorytransaction
{
    [Key]
    public int InventoryTransactionId { get; set; }

    public int ProductId { get; set; }

    [StringLength(30)]
    public string ChangeType { get; set; } = null!;

    public int QuantityChange { get; set; }

    public int ResultingStock { get; set; }

    public int? PurchaseId { get; set; }

    public int? SaleId { get; set; }

    public int? ReturnId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public int CreatedByUserId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("Inventorytransactions")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("Inventorytransactions")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("PurchaseId")]
    [InverseProperty("Inventorytransactions")]
    public virtual Purchase? Purchase { get; set; }

    [ForeignKey("ReturnId")]
    [InverseProperty("Inventorytransactions")]
    public virtual Return? Return { get; set; }

    [ForeignKey("SaleId")]
    [InverseProperty("Inventorytransactions")]
    public virtual Sale? Sale { get; set; }
}
