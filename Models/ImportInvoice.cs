using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class ImportInvoice
{
    public int Id { get; set; }

    public string? SupplierName { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? PurchaseTime { get; set; }

    public virtual ICollection<ImportInvoiceDetail> ImportInvoiceDetails { get; set; } = new List<ImportInvoiceDetail>();
}
