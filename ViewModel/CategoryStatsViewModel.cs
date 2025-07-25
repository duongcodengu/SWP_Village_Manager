using Village_Manager.Models;

namespace Village_Manager.ViewModel
{
    public class CategoryStatsViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<Product> Products { get; set; }
        public List<Farmer> Farmers { get; set; }
        public int ProductCount => Products?.Count ?? 0;
        public int FarmerCount => Farmers?.Count ?? 0;
    }
}
