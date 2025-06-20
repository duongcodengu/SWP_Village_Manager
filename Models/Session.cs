using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class Session
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? SessionToken { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public virtual User? User { get; set; }
}
