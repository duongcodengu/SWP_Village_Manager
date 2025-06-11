using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class ProcessingReceipt
{
    public int Id { get; set; }

    public int? ProcessingOrderId { get; set; }

    public DateOnly? ReceivedDate { get; set; }

    public int? ActualQuantity { get; set; }

    public virtual ProcessingOrder? ProcessingOrder { get; set; }
}
