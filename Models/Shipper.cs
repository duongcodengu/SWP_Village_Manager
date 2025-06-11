using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class Shipper
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? FullName { get; set; }

    public string Phone { get; set; } = null!;

    public string? VehicleInfo { get; set; }

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual User? User { get; set; }
}
