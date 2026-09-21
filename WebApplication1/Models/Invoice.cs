using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Models;

[Table("invoices")]
[Index("SaleId", Name = "FK_Invoices_Sales")]
[Index("InvoiceNumber", Name = "InvoiceNumber", IsUnique = true)]
public partial class Invoice
{
    [Key]
    public int InvoiceId { get; set; }

    [StringLength(50)]
    public string InvoiceNumber { get; set; } = null!;

    public int SaleId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime InvoiceDate { get; set; }

    [Precision(12, 2)]
    public decimal TotalAmount { get; set; }

    [ForeignKey("SaleId")]
    [InverseProperty("Invoices")]
    public virtual Sale Sale { get; set; } = null!;
}
