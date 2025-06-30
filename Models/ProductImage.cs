            using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class ProductImage
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual Product? Product { get; set; }
}
