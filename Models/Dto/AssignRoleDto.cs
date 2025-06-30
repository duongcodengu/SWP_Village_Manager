using System.Collections.Generic;

namespace Village_Manager.Models.Dto
{
    public class AssignRoleDto
    {
        public int UserId { get; set; }
        public string? RoleName { get; set; }
        public List<string>? Permissions { get; set; }
    }
} 