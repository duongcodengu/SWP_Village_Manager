using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models;

public partial class Delivery
{
    public int Id { get; set; }

    public string? OrderType { get; set; }

    public int? OrderId { get; set; }

    public int? ShipperId { get; set; }

    public decimal? ShippingFee { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [Column("status")]
    public string? Status { get; set; }
    [Column("customer_name")]
    public string? CustomerName { get; set; }
    [Column("customer_address")]
    public string? CustomerAddress { get; set; }
    [Column("customer_phone")]
    public string? CustomerPhone { get; set; }

    public virtual Shipper? Shipper { get; set; }
}
