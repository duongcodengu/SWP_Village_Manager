using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class ReturnOrder
{
    public int Id { get; set; }

    public string? OrderType { get; set; }

    public int? OrderId { get; set; }

    public int? UserId { get; set; }

    public int? Quantity { get; set; }

    public string? Reason { get; set; }

    public DateTime? CreatedAt { get; set; }

<<<<<<< Updated upstream
    public virtual User? User { get; set; }
=======
        public string ImageUrl { get; set; }
        // Navigation property
        public virtual User User { get; set; }
    }
>>>>>>> Stashed changes
}
