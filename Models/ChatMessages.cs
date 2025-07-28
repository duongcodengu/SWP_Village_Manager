using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Village_Manager.Models
{
    public class ChatMessages
    {
        [Key]
        public int Id { get; set; }

        [Column("sender_id")]
        public int SenderId { get; set; }

        [Column("receiver_id")]
        public int ReceiverId { get; set; }

        [Column("message")]
        public string MessageContent { get; set; }

        [Column("sent_at")]
        public DateTime SentAt { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; }

        // Navigation properties
        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
    }
}
