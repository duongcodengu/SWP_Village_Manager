using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class Stock
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public int? Quantity { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual Product? Product { get; set; }
}
