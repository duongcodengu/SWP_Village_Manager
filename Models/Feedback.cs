using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class Feedback
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? OrderId { get; set; }

    public string? OrderType { get; set; }

    public string? Content { get; set; }

    public int? Rating { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
