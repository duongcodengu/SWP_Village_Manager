using Microsoft.EntityFrameworkCore;
using Village_Manager.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Village_Manager.Models.Dto;
using Village_Manager.Data;

namespace Village_Manager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreateRoleController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CreateRoleController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("search-user")]
        public async Task<IActionResult> SearchUser(string query)
        {
            var users = await _context.Users
                .Where(u => u.Username.Contains(query))
                .Select(u => new { u.Id, u.Username, u.Email })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound("User not found");
            // Gán role (giả sử có bảng Role và user có RoleId)
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);
            if (role == null)
            {
                role = new Role { Name = dto.RoleName };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }
            user.RoleId = role.Id;
            // TODO: Lưu permissions vào bảng riêng nếu có
            await _context.SaveChangesAsync();
            return Ok(new { message = "Role and permissions assigned successfully" });
        }
    }
}
