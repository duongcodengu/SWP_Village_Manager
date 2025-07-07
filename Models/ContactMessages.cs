using System.ComponentModel.DataAnnotations;

namespace Village_Manager.Models
{
    public class ContactMessages
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
