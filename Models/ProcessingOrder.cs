using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class ProcessingOrder
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public int? Quantity { get; set; }

    public DateOnly? SendDate { get; set; }

    public DateOnly? ExpectedReturnDate { get; set; }

    public virtual ICollection<ProcessingReceipt> ProcessingReceipts { get; set; } = new List<ProcessingReceipt>();

    public virtual Product? Product { get; set; }
}
