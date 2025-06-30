using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class WholesaleOrder
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? Status { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public virtual User? User { get; set; }

    public virtual ICollection<WholesaleOrderItem> WholesaleOrderItems { get; set; } = new List<WholesaleOrderItem>();
}
