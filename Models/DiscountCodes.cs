using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models
{
    public class DiscountCodes
    {
        public int Id { get; set; }

        [Column("code")]
        public string Code { get; set; } = null!;

        [Column("discount_percent")]
        public int DiscountPercent { get; set; }

        [Column("status")]
        public string Status { get; set; } = "active";

        [Column("usage_limit")]
        public int UsageLimit { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("expired_at")]
        public DateTime? ExpiredAt { get; set; }

        // Navigation collection: một mã áp cho nhiều đơn hàng
        public virtual ICollection<RetailOrder> RetailOrders { get; set; } = new List<RetailOrder>();
    }
}