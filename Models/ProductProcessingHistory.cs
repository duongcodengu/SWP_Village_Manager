using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class ProductProcessingHistory
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public DateOnly? SentDate { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public int? Quantity { get; set; }

    public virtual ICollection<ProcessedProduct> ProcessedProducts { get; set; } = new List<ProcessedProduct>();

    public virtual Product? Product { get; set; }
}
