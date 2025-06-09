using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models
{
    [Table("Product")]
    public class Product
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name", TypeName = "nvarchar(100)")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("category_id")]
        public int CategoryId { get; set; }

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Column("expiration_date", TypeName = "date")]
        public DateTime? ExpirationDate { get; set; }

        [Required]
        [Column("product_type", TypeName = "nvarchar(20)")]
        public string ProductType { get; set; } = string.Empty;// "processed" hoặc "raw"

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("processing_time", TypeName = "date")]
        public DateTime? ProcessingTime { get; set; }

        [Column("farmer_id")]
        public int? FarmerId { get; set; }
    }
}
