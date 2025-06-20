using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class WholesaleCustomer
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string ContactPerson { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public virtual User? User { get; set; }
}
