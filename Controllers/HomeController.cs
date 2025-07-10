using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using Village_Manager.Data;
using Village_Manager.Models;
//using Village_Manager.Extensions;

namespace Village_Manager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailSettings _emailSettings;
        private static Dictionary<string, (string Otp, DateTime Expire)> otpStore = new();

        public HomeController(ILogger<HomeController> logger, AppDbContext context, IConfiguration configuration, IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
            _emailSettings = emailSettings.Value;
        }

        public IActionResult Index()
        {
            var categories = _context.ProductCategories.Select(c => new
            {
                c.Id,
                c.Name,
                c.ImageUrl

            })
                .ToList();
            ViewBag.ProductCategories = categories;
            return View();
           
        }

        //login
        [HttpGet]
        [Route("login")]
        public IActionResult Login() => View();

        // Xử lý đăng nhập
        [HttpPost]
        [Route("login")]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password && u.IsActive);
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            Console.WriteLine(connectionString);
            if (user != null)
            {
                // lấy tên role name
                int roleId = user.RoleId;
                string roleName = "";
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT name FROM Roles WHERE id = @roleId", conn);
                    cmd.Parameters.AddWithValue("@roleId", roleId);

                    var result = cmd.ExecuteScalar();
                    roleName = result.ToString() ?? "";
                }

                // Xóa session cũ trước khi set mới
                HttpContext.Session.Clear();
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetInt32("RoleId", user.RoleId);
                HttpContext.Session.SetString("RoleName", roleName ?? "");

                // Set thêm ShipperId nếu là shipper
                if (user.RoleId == 4)
                {
                    var shipper = _context.Shippers.FirstOrDefault(s => s.UserId == user.Id);
                    if (shipper != null)
                    {
                        HttpContext.Session.SetInt32("ShipperId", shipper.Id);
                        HttpContext.Session.SetString("ShipperName", shipper.FullName ?? "");
                    }
                }
                // Set thêm FarmerId nếu là farmer
                if (user.RoleId == 5)
                {
                    var farmer = _context.Farmers.FirstOrDefault(f => f.UserId == user.Id);
                    if (farmer != null)
                    {
                        HttpContext.Session.SetInt32("FarmerId", farmer.Id);
                        HttpContext.Session.SetString("FarmerName", farmer.FullName ?? "");
                    }
                }
                if (user.RoleId == 4)
                {
                    var shipper = _context.Shippers.FirstOrDefault(f => f.UserId == user.Id);
                    if (shipper != null)
                    {
                        HttpContext.Session.SetInt32("ShipperId", shipper.Id);
                        HttpContext.Session.SetString("ShipperName", shipper.FullName ?? "");
                        HttpContext.Session.SetInt32("UserId", user.Id);
                    }
                }

                // role admin
                switch (user.RoleId)
                    {
                        case 1: // Admin
                            return RedirectToAction("Index", "Home");

                        case 2: // Staff
                            return RedirectToAction("Index", "Home");

                        case 3: // Customer
                            return RedirectToAction("IndexCustomer", "Customer");

                        case 4: // Shipper
                            return RedirectToAction("DashboardShipper", "Shipper");

                        case 5: // Farmer
                            return RedirectToAction("Index", "Home");

                        default:
                            return RedirectToAction("Login", "Home");
                    }
            }
            ViewBag.Error = user == null ? "Tài khoản bị khóa hoặc thông tin không đúng!" : "Email hoặc mật khẩu không đúng!";

            return View();
        }
        //Contact us
        [HttpGet]
        [Route("contact-us")]
        public IActionResult ContactUs() => View();

        [HttpPost]
        [Route("contact-us")]
        public async Task<IActionResult> ContactUs(string FirstName, string LastName, string Email, string PhoneNumber, string Message, DateTime CreatedAt)
        {
            var request = new ContactMessages
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                PhoneNumber = PhoneNumber,
                Message = Message,
                CreatedAt = DateTime.Now
            };

            _context.ContactMessages.Add(request);

            var admins = await _context.Users
                .Where(u => u.RoleId == 1)
                .ToListAsync();

            string message = $"Có ticket cần sử lý từ {Email}";

            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Content = message,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Yêu cầu đã được gửi. Chúng tôi sẽ liên hệ với bạn sớm nhất";
            return RedirectToAction("ContactUs");
        }

        // đăng ký
        [HttpGet]
        [Route("signup")]
        public IActionResult SignUp() => View();


        // Đăng xuất
        [Route("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Route("changepassword")]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Route("changepassword")]
        public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                ViewBag.Error = "Không tìm thấy người dùng.";
                return View();
            }

            if (user.Password != oldPassword)
            {
                ViewBag.Error = "Mật khẩu cũ không chính xác.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu mới và xác nhận không khớp.";
                return View();
            }

            user.Password = newPassword;
            _context.SaveChanges();

            ViewBag.Message = "Đổi mật khẩu thành công!";
            return View();
        }

        [HttpGet]
        [Route("forgot")]
        public IActionResult Forgot()
        {
            return View();
        }

        // Gửi OTP
        [HttpPost("/forgot")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var normalizedEmail = email.Trim().ToLower();
            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
                return BadRequest("Email không tồn tại.");

            var otp = new Random().Next(100000, 999999).ToString();
            otpStore[normalizedEmail] = (otp, DateTime.UtcNow.AddMinutes(10));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("", normalizedEmail));
            message.Subject = "Mã OTP đặt lại mật khẩu";
            message.Body = new TextPart("plain") { Text = $"Mã OTP của bạn là: {otp} (hiệu lực 10 phút)." };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.AppPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return Ok();
        }

        [HttpPost("/verify-otp")]
        public IActionResult VerifyOtp(string email, string Otp0, string Otp1, string Otp2, string Otp3, string Otp4, string Otp5)
        {
            var normalizedEmail = email.Trim().ToLower();
            var otp = $"{Otp0}{Otp1}{Otp2}{Otp3}{Otp4}{Otp5}";

            if (otp.Length != 6 || !otp.All(char.IsDigit))
                return BadRequest("OTP không hợp lệ.");

            if (otpStore.TryGetValue(normalizedEmail, out var record))
            {
                if (record.Otp == otp && DateTime.UtcNow <= record.Expire)
                {
                    otpStore.Remove(normalizedEmail);
                    return RedirectToAction("ResetPassword", new { email });
                }
            }

            return BadRequest("OTP không đúng hoặc đã hết hạn.");
        }

        [HttpGet("/reset-password")]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Forgot");
            }

            ViewBag.Email = email;
            return View();
        }

        [HttpPost("/reset-password-post")]
        public async Task<IActionResult> ResetPasswordPost(string email, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null)
            {
                return RedirectToAction("Forgot");
            }

            user.Password = newPassword;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đặt lại mật khẩu thành công.";
            return RedirectToAction("Login", "Home");
        }

    }
}
