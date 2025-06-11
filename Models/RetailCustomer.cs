using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class RetailCustomer
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? FullName { get; set; }

    public string Phone { get; set; } = null!;

    public virtual User? User { get; set; }
}
