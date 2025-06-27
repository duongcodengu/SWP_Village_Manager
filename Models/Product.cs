using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int CategoryId { get; set; }

    public decimal Price { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    public string? ProductType { get; set; }

    public int Quantity { get; set; }

    public DateOnly? ProcessingTime { get; set; }

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

}
