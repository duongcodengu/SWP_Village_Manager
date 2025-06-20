using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Village_Manager.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int CategoryId { get; set; }

    public decimal Price { get; set; }

    public DateTime? ExpirationDate { get; set; }
    [Required]
    [RegularExpression("processed|raw")]
    public string? ProductType { get; set; }

    public int Quantity { get; set; }

    public DateTime? ProcessingTime { get; set; }

    public int? FarmerId { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ProductCategory Category { get; set; } = null!;

    public virtual Farmer? Farmer { get; set; }

    public virtual ICollection<ImportInvoiceDetail> ImportInvoiceDetails { get; set; } = new List<ImportInvoiceDetail>();

    public virtual ICollection<ProcessingOrder> ProcessingOrders { get; set; } = new List<ProcessingOrder>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductProcessingHistory> ProductProcessingHistories { get; set; } = new List<ProductProcessingHistory>();

    public virtual ICollection<RetailOrderItem> RetailOrderItems { get; set; } = new List<RetailOrderItem>();

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    public virtual ICollection<WholesaleOrderItem> WholesaleOrderItems { get; set; } = new List<WholesaleOrderItem>();
}
