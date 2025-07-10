using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models
{
    public class HiddenProduct
    {

        
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("reason", TypeName = "TEXT")]
        public string? Reason { get; set; }

        [Column("hidden_at")]
        public DateTime HiddenAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
