using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class Supplier
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string ContactInfo { get; set; } = null!;
}
