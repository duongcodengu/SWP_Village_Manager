namespace Village_Manager.Models
{
    public class DiscountCodes
    {
        public int Id { get; set; }

        public string Code { get; set; } = null!;

        public int DiscountPercent { get; set; }

        public string Status { get; set; } = "active";

        public int UsageLimit { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ExpiredAt { get; set; }
    }
}
