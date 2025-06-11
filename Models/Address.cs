using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class Address
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? AddressLine { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    public string? PostalCode { get; set; }

    public virtual User? User { get; set; }
}
