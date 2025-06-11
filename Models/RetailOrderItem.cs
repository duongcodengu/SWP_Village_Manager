using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class RetailOrderItem
{
    public int Id { get; set; }

    public int? OrderId { get; set; }

    public int? ProductId { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public virtual RetailOrder? Order { get; set; }

    public virtual Product? Product { get; set; }
}
