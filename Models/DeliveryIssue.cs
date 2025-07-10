using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models
{
    public class DeliveryIssue
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("delivery_id")]
        public int DeliveryId { get; set; }

        [Column("shipper_id")]
        public int ShipperId { get; set; }

        [Column("issue_type")]
        public string IssueType { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("reported_at")]
        public DateTime ReportedAt { get; set; }
    }
} 