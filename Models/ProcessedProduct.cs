using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class ProcessedProduct
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public int? HistoryId { get; set; }

    public DateOnly? ProcessedDate { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public string? ProcessingNote { get; set; }

    public decimal? TotalWeight { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public virtual ProductProcessingHistory? History { get; set; }
}
