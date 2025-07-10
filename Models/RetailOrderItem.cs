using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models;

public partial class RetailOrderItem
{
    public int Id { get; set; }
    [Column("order_id")]
    public int OrderId { get; set; }
    [Column("product_id")]
    public int ProductId { get; set; }

    public int? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }

    public virtual Product? Product { get; set; }
    public RetailOrder Order { get; set; }
}
