using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class SupplyRequest
{
    public int Id { get; set; }

    public string RequesterType { get; set; } = null!;

    public int RequesterId { get; set; }

    public int ReceiverId { get; set; }

    public int FarmerId { get; set; }

    public string ProductName { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal? Price { get; set; }

    public string? Status { get; set; }

    public DateTime? RequestedAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public virtual Farmer Farmer { get; set; } = null!;

    public virtual User Receiver { get; set; } = null!;

    public virtual User Requester { get; set; } = null!;
}
