using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models
{
    public class DeliveryProof
    {
        [Key]
        public int? Id { get; set; }
        public int? DeliveryId { get; set; }
        public int? ShipperId { get; set; }
        public string? ImagePath { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 