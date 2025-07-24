using Village_Manager.Models;
using static Village_Manager.Controllers.FamerController;
namespace Village_Manager.ViewModel
{
    public class FamerDashboardViewModel
    {
        public User User { get; set; }
        public Farmer Famer { get; set; }
        public List<Product> ProductList { get; set; } // Danh sách sản phẩm gốc
        public List<ProductWithSalesViewModel> ProductWithSalesList { get; set; } // Danh sách sản phẩm kèm số lượng đã bán
        public List<RetailOrder> OngoingOrders { get; set; }
    }
}
