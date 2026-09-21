using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Models;

[Table("purchases")]
[Index("SupplierId", Name = "FK_Purchases_Suppliers")]
[Index("CreatedByUserId", Name = "FK_Purchases_Users")]
[Index("PurchaseNumber", Name = "PurchaseNumber", IsUnique = true)]
public partial class Purchase
{
    [Key]
    public int PurchaseId { get; set; }

    [StringLength(50)]
    public string PurchaseNumber { get; set; } = null!;

    public int SupplierId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime PurchaseDate { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Precision(12, 2)]
    public decimal TotalAmount { get; set; }

    public int CreatedByUserId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("Purchases")]
    public virtual User CreatedByUser { get; set; } = null!;

    [InverseProperty("Purchase")]
    public virtual ICollection<Inventorytransaction> Inventorytransactions { get; set; } = new List<Inventorytransaction>();

    [InverseProperty("Purchase")]
    public virtual ICollection<Purchaseitem> Purchaseitems { get; set; } = new List<Purchaseitem>();

    [ForeignKey("SupplierId")]
    [InverseProperty("Purchases")]
    public virtual Supplier Supplier { get; set; } = null!;
}
