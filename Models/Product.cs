using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models;

public partial class Product
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    public string Name { get; set; } = null!;
    [Required(ErrorMessage = "Loại sản phẩm là bắt buộc")]
    public int CategoryId { get; set; }
    [Range(0, double.MaxValue, ErrorMessage = "Giá không được âm")]
    public decimal Price { get; set; }

    public DateTime? ExpirationDate { get; set; }
    [Required(ErrorMessage = "Danh mục sản phẩm là bắt buộc")]
    public string? ProductType { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được âm")]
    public int Quantity { get; set; }

    public DateTime? ProcessingTime { get; set; }

    public int? FarmerId { get; set; }

    [Required]
    [Column("approval_status")]
    [RegularExpression("pending|accepted|rejected")]
    public string ApprovalStatus { get; set; } = "pending";

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ProductCategory Category { get; set; } = null!;

    public virtual Farmer? Farmer { get; set; }
    [NotMapped]
    public List<IFormFile>? ImageUpdate { get; set; }

    public virtual ICollection<ImportInvoiceDetail> ImportInvoiceDetails { get; set; } = new List<ImportInvoiceDetail>();

    public virtual ICollection<ProcessingOrder> ProcessingOrders { get; set; } = new List<ProcessingOrder>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductProcessingHistory> ProductProcessingHistories { get; set; } = new List<ProductProcessingHistory>();

    public virtual ICollection<RetailOrderItem> RetailOrderItems { get; set; } = new List<RetailOrderItem>();

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
