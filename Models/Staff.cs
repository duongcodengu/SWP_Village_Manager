using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class Staff
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Role { get; set; }

    public virtual User? User { get; set; }
}
