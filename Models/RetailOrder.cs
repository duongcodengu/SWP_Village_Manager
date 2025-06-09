using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models
{
    [Table("RetailOrder")]
    public class RetailOrder
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("order_date")]
        public DateTime OrderDate { get; set; }
        [Column("status")]
        public string Status { get; set; } = string.Empty; // 'pending', 'confirmed', 'shipped', 'delivered', 'cancelled', 'returned'
        [Column("confirmed_at")]
        public DateTime ConfirmedAt { get; set; }
    }
}
