namespace Village_Manager.Models
{
    public class AddProductModel
    {
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string ProductType { get; set; }
        public int Quantity { get; set; }
        public DateTime? ProcessingTime { get; set; }
        public int FarmerId { get; set; }

        public List<IFormFile> ImageFiles { get; set; }
        public string ImageDescription { get; set; }
    }
}
