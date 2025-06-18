using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class TransportRoute
{
    public int Id { get; set; }

    public string? StartLocation { get; set; }

    public string? EndLocation { get; set; }

    public decimal? DistanceKm { get; set; }

    public string? EstimatedTime { get; set; }
}
