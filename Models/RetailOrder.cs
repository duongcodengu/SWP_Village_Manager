using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models;
[Table("RetailOrder")]
public  class RetailOrder
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? Status { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    [Column("discount_code_id")]
    public int? DiscountCodeId { get; set; }

    // Navigation property
    [ForeignKey("DiscountCodeId")]
    public virtual DiscountCodes? DiscountCode { get; set; }

    public virtual ICollection<RetailOrderItem> RetailOrderItems { get; set; } = new List<RetailOrderItem>();

    public virtual User? User { get; set; }
}
