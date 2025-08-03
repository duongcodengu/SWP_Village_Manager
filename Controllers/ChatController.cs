using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Controllers
{
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
        {
            _context = context;
        }

        [Route("chatbox")]


        [HttpGet]
        public async Task<IActionResult> ChatBox(int? receiverId)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null) return RedirectToAction("Login", "Home");

            var currentUser = await _context.Users.FindAsync(currentUserId);
            var currentRole = await _context.Roles.FindAsync(currentUser.RoleId);

            bool isStaff = currentRole.Name == "staff";
            ViewBag.IsStaff = isStaff;
            ViewBag.CurrentUserId = currentUserId;

            if (isStaff)
            {
                // Lấy danh sách khách hàng
                var customers = await _context.Users
                    .Where(u => u.Id != currentUserId && u.RoleId != 1 && u.IsActive)
                    .ToListAsync();

                ViewBag.ChatUsers = customers;
                ViewBag.ReceiverId = receiverId ?? customers.FirstOrDefault()?.Id;
            }
            else
            {
                // Lấy nhân viên duy nhất
                var staff = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 2 && u.IsActive);
                if (staff == null) return Content("Không có nhân viên hỗ trợ");

                ViewBag.StaffUser = staff;
                ViewBag.ReceiverId = staff.Id;
            }

            return View("ChatBox");
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> LoadMessages(int receiverId)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null) return Unauthorized();

            var currentUser = await _context.Users.FindAsync(currentUserId);
            var isStaff = (await _context.Roles.FindAsync(currentUser.RoleId))?.Name == "staff";

            IQueryable<ChatMessages> query;

            if (isStaff)
            {
                // Staff: load tin nhắn giữa staff này và customer cụ thể
                query = _context.ChatMessages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == receiverId) ||
                                (m.SenderId == receiverId && m.ReceiverId == currentUserId));
            }
            else
            {
                // Customer: load tin nhắn với tất cả staff
                var staffIds = await _context.Users
                    .Where(u => u.RoleId == 2 && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync();

                var rawMessages = await _context.ChatMessages
                    .Where(m => (m.SenderId == currentUserId && staffIds.Contains(m.ReceiverId)) ||
                                (staffIds.Contains(m.SenderId) && m.ReceiverId == currentUserId))
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                // 🔹 Loại bỏ tin nhắn trùng nhau (customer gửi cho nhiều staff)
                var distinctMessages = rawMessages
                    .GroupBy(m => new { m.SenderId, m.MessageContent, SentAt = m.SentAt.ToString("yyyy-MM-dd HH:mm:ss") })
                    .Select(g => g.First())
                    .ToList();

                return PartialView("_MessageList", distinctMessages);
            }

            var messages = await query
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return PartialView("_MessageList", messages);
        }



        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto dto)
        {
            var senderId = HttpContext.Session.GetInt32("UserId");
            if (senderId == null) return Unauthorized();

            var sender = await _context.Users.FindAsync(senderId);
            if (sender == null) return NotFound("Người gửi không tồn tại.");

            var messageContent = dto.Message?.Trim();
            if (string.IsNullOrEmpty(messageContent)) return BadRequest("Nội dung trống.");

            // Nếu là customer → gửi đến tất cả staff
            var senderRole = await _context.Roles.FindAsync(sender.RoleId);
            if (senderRole?.Name == "customer")
            {
                var allStaff = await _context.Users
                    .Where(u => u.RoleId == 2 && u.IsActive)
                    .ToListAsync();

                foreach (var staff in allStaff)
                {
                    var message = new ChatMessages
                    {
                        SenderId = sender.Id,
                        ReceiverId = staff.Id,
                        MessageContent = messageContent,
                        SentAt = DateTime.Now
                    };
                    _context.ChatMessages.Add(message);
                }
            }
            else
            {
                // Gửi như bình thường
                var message = new ChatMessages
                {
                    SenderId = sender.Id,
                    ReceiverId = dto.ReceiverId,
                    MessageContent = messageContent,
                    SentAt = DateTime.Now
                };
                _context.ChatMessages.Add(message);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> GetMessages(int withUserId)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null) return Unauthorized();

            var messages = await _context.ChatMessages
                .Where(m =>
                    (m.SenderId == currentUserId && m.ReceiverId == withUserId) ||
                    (m.SenderId == withUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return Json(messages);
        }

        [HttpGet]
        public async Task<JsonResult> GetUnreadCounts()
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null) return Json(new { });

            var unread = await _context.ChatMessages
                .Where(m => m.ReceiverId == currentUserId && !m.IsRead)
                .GroupBy(m => m.SenderId)
                .Select(g => new { SenderId = g.Key, Count = g.Count() })
                .ToListAsync();

            return Json(unread);
        }
    }

    public class ChatMessageDto
    {
        public int ReceiverId { get; set; }
        public string Message { get; set; }
    }
}



