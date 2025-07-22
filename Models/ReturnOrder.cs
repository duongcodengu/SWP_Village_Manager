using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models
{
    [Table("ReturnOrder")]
    public class ReturnOrder
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("order_type")]
        public string OrderType { get; set; }

        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("reason")]
        public string Reason { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        [Column("image_url")]
        public string ImageUrl { get; set; }
    }

}
