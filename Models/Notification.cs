using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class Notification
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Content { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsRead { get; set; }

    public virtual User? User { get; set; }
}
