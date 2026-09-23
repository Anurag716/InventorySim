using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Models;

[Table("suppliers")]
public partial class Supplier
{
    [Key]
    public int SupplierId { get; set; }

    [StringLength(150)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public bool? IsActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("PreferredSupplier")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    [InverseProperty("Supplier")]
    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
