using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models;

public partial class RetailOrder
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    [Column("product_id")]
    public int ProductId { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? Status { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public virtual ICollection<RetailOrderItem> RetailOrderItems { get; set; } = new List<RetailOrderItem>();

    public virtual User? User { get; set; }
    public Product Product { get; set; }
}
