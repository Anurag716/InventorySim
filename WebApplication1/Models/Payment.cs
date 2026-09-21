using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Models;

[Table("payments")]
[Index("SaleId", Name = "FK_Payments_Sales")]
public partial class Payment
{
    [Key]
    public int PaymentId { get; set; }

    public int SaleId { get; set; }

    [StringLength(30)]
    public string PaymentType { get; set; } = null!;

    [StringLength(20)]
    public string PaymentMethod { get; set; } = null!;

    [Precision(12, 2)]
    public decimal Amount { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime PaymentDate { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    public int? ReturnId { get; set; }

    [ForeignKey("SaleId")]
    [InverseProperty("Payments")]
    public virtual Sale Sale { get; set; } = null!;
}
