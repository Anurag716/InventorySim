using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Models;

[Table("returns")]
[Index("SaleId", Name = "FK_Returns_Sales")]
[Index("ProcessedByUserId", Name = "FK_Returns_Users")]
[Index("ReturnNumber", Name = "ReturnNumber", IsUnique = true)]
public partial class Return
{
    [Key]
    public int ReturnId { get; set; }

    [StringLength(50)]
    public string ReturnNumber { get; set; } = null!;

    public int SaleId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ReturnDate { get; set; }

    public int ProcessedByUserId { get; set; }

    [Precision(12, 2)]
    public decimal RefundAmount { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    [StringLength(500)]
    public string? Reason { get; set; }

    [InverseProperty("Return")]
    public virtual ICollection<Inventorytransaction> Inventorytransactions { get; set; } = new List<Inventorytransaction>();

    [ForeignKey("ProcessedByUserId")]
    [InverseProperty("Returns")]
    public virtual User ProcessedByUser { get; set; } = null!;

    [InverseProperty("Return")]
    public virtual ICollection<Returnitem> Returnitems { get; set; } = new List<Returnitem>();

    [ForeignKey("SaleId")]
    [InverseProperty("Returns")]
    public virtual Sale Sale { get; set; } = null!;
}
