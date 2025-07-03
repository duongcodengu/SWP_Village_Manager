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

        [HttpGet("all-unread")]
        public async Task<IActionResult> GetAll()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return Unauthorized(new { message = "Chưa đăng nhập hoặc hết phiên." });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead == false)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            var unreadCount = notifications.Count;

            return Ok(new
            {
                unreadCount = unreadCount,
                notifications = notifications.Select(n => new {
                    n.Id,
                    n.Content,
                    is_read = n.IsRead
                })
            });
        }

        [HttpPost("read/{id}")]
        public IActionResult Read(int id)
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null) return Unauthorized();

            var notif = _context.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == currentUserId);
            if (notif != null && notif.IsRead == false)
            {
                notif.IsRead = true;
                _context.SaveChanges();
            }
            return Ok();
        }

        [HttpPost("readall")]
        public IActionResult ReadAll()
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null) return Unauthorized();

            var notifs = _context.Notifications.Where(n => n.UserId == currentUserId && n.IsRead == false).ToList();
            foreach (var n in notifs) n.IsRead = true;
            _context.SaveChanges();
            return Ok();
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] bool all = false)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized(new { message = "Chưa đăng nhập hoặc hết phiên." });

            var query = _context.Notifications.Where(n => n.UserId == userId);
            if (!all)
                query = query.Where(n => n.IsRead == false);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            var unreadCount = notifications.Count(n => n.IsRead == false);

            return Ok(new
            {
                unreadCount = unreadCount,
                notifications = notifications.Select(n => new {
                    n.Id,
                    n.Content,
                    n.CreatedAt,
                    is_read = n.IsRead
                })
            });
        }
    }
}
