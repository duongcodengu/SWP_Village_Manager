using System;

namespace Village_Manager.Models.Dto
{
    public class UpdateProfileDto
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
    }
} 