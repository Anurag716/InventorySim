using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Models;

[Table("products")]
[Index("CategoryId", Name = "FK_Products_Categories")]
[Index("PreferredSupplierId", Name = "FK_Products_Suppliers")]
[Index("Sku", Name = "SKU", IsUnique = true)]
public partial class Product
{
    [Key]
    public int ProductId { get; set; }

    [Column("SKU")]
    [StringLength(50)]
    public string Sku { get; set; } = null!;

    [StringLength(150)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public int? PreferredSupplierId { get; set; }

    [Precision(10, 2)]
    public decimal PurchasePrice { get; set; }

    [Precision(10, 2)]
    public decimal SellingPrice { get; set; }

    public int CurrentStock { get; set; }

    public int ReorderLevel { get; set; }

    [Required]
    public bool? IsActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("Products")]
    public virtual Category Category { get; set; } = null!;

    [InverseProperty("Product")]
    public virtual ICollection<Inventorytransaction> Inventorytransactions { get; set; } = new List<Inventorytransaction>();

    [ForeignKey("PreferredSupplierId")]
    [InverseProperty("Products")]
    public virtual Supplier? PreferredSupplier { get; set; }

    [InverseProperty("Product")]
    public virtual ICollection<Purchaseitem> Purchaseitems { get; set; } = new List<Purchaseitem>();

    [InverseProperty("Product")]
    public virtual ICollection<Saleitem> Saleitems { get; set; } = new List<Saleitem>();
}
