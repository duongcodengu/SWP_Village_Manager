using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class Report
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public DateTime? CreatedAt { get; set; }
}
