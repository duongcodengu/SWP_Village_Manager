using Village_Manager.Models;

namespace Village_Manager.ViewModel
{
    public class RetailOrderViewModel
    {

        public int Id { get; set; }
        public string UserName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }
        public DateTime? OrderDate { get; set; }
        public string Status { get; set; }
        public List<RetailOrderItemViewModel> Items { get; set; }
        public  virtual User Users { get; set; }

        public Product Product { get; set; }
    }
}
