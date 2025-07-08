using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models
{
    public class DeliveryIssue
    {
        [Key]
        public int Id { get; set; }
        public int DeliveryId { get; set; }
        public int ShipperId { get; set; }
        public string IssueType { get; set; }
        public string Description { get; set; }
        public DateTime ReportedAt { get; set; }
    }
} 