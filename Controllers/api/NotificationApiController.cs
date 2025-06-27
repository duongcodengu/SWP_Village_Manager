using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Village_Manager.Data;
using Microsoft.EntityFrameworkCore;

namespace Village_Manager.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return Unauthorized(new { message = "Chưa đăng nhập hoặc hết phiên." });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            var unreadCount = notifications.Count(n => n.IsRead == false);

            return Ok(new
            {
                unreadCount,
                notifications = notifications.Select(n => new
                {
                    content = n.Content,
                    created_at = n.CreatedAt
                })
            });
        }
    }
}
