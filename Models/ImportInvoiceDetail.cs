using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class ImportInvoiceDetail
{
    public int Id { get; set; }

    public int? ImportInvoiceId { get; set; }

    public int? ProductId { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public virtual ImportInvoice? ImportInvoice { get; set; }

    public virtual Product? Product { get; set; }
}
