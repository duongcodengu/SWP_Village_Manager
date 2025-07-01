using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class ProductCategory
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? ImageUrl { get; set; }

    public int? ProductId { get; set; }

    public virtual Product? Product { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
