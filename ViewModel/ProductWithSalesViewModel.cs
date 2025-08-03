using Village_Manager.Models;

namespace Village_Manager.ViewModel
{
    public class ProductWithSalesViewModel
    {
        public Product Product { get; set; }
        public int SoldQuantity { get; set; }
        public int StockQuantity { get; set; } // Số lượng tồn kho từ bảng Product
    }
}
