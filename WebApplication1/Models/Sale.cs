using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Models;

[Table("sales")]
[Index("CustomerId", Name = "FK_Sales_Customers")]
[Index("CashierUserId", Name = "FK_Sales_Users")]
[Index("SaleNumber", Name = "SaleNumber", IsUnique = true)]
public partial class Sale
{
    [Key]
    public int SaleId { get; set; }

    [StringLength(50)]
    public string SaleNumber { get; set; } = null!;

    public int? CustomerId { get; set; }

    public int CashierUserId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime SaleDate { get; set; }

    [Precision(12, 2)]
    public decimal Subtotal { get; set; }

    [Precision(12, 2)]
    public decimal DiscountAmount { get; set; }

    [Precision(12, 2)]
    public decimal TaxAmount { get; set; }

    [Precision(12, 2)]
    public decimal GrandTotal { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("CashierUserId")]
    [InverseProperty("Sales")]
    public virtual User CashierUser { get; set; } = null!;

    [ForeignKey("CustomerId")]
    [InverseProperty("Sales")]
    public virtual Customer? Customer { get; set; }

    [InverseProperty("Sale")]
    public virtual ICollection<Inventorytransaction> Inventorytransactions { get; set; } = new List<Inventorytransaction>();

    [InverseProperty("Sale")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    [InverseProperty("Sale")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [InverseProperty("Sale")]
    public virtual ICollection<Return> Returns { get; set; } = new List<Return>();

    [InverseProperty("Sale")]
    public virtual ICollection<Saleitem> Saleitems { get; set; } = new List<Saleitem>();
}
