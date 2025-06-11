using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? OrderId { get; set; }

    public string? OrderType { get; set; }

    public decimal? Amount { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? Method { get; set; }

    public string? PaymentType { get; set; }

    public virtual User? User { get; set; }
}
