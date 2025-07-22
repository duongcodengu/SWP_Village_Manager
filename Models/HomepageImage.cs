using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models;


    public partial class HomepageImage
    {
    [Key]
    public int Id { get; set; }

  
    public int? ProductImageId { get; set; }

    [ForeignKey("ProductImageId")]
    public virtual ProductImage ProductImage { get; set; }

    [Required]
    [StringLength(50)]
    
    public string Section { get; set; } = "default";

    public int DisplayOrder { get; set; } = 0;
    [StringLength(500)]
    public string? Banner { get; set; }
    [StringLength(50)]
    public string? Position { get; set; }
    public bool IsActive { get; set; } = true;
}


