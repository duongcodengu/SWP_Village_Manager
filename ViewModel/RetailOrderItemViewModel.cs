using Village_Manager.Models;

namespace Village_Manager.ViewModel
{
    public class RetailOrderItemViewModel
    {
        public int? OrderId { get; set; }
        public int? ProductId { get; set; }
        public string ProductName { get; set; } 
        public int? Quantity { get; set; }
        public string ProductImageUrl { get; set; }
        public decimal? UnitPrice { get; set; }
        public Product Product { get; set; }
        public string? UserName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? Status { get; set; }
        public int Id { get; set; }

    }
}
