using System.Data.SqlClient; 
using System.Linq; 
using System.Security.Claims;
using System.Threading.Tasks; 
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Village_Manager.Data;
using Village_Manager.Models;
using Microsoft.Data.SqlClient;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AccountController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpGet]
    public IActionResult GoogleLogin()
    {
        // Chuyển hướng đến Google để xác thực
        var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> GoogleResponse()
    {
        // 1. Lấy thông tin người dùng từ Google
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (result?.Principal == null)
        {
            // Nếu không lấy được thông tin, quay về trang login
            ViewBag.Error = "Không thể xác thực với Google. Vui lòng thử lại.";
            return View("Login");
        }

        var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
        var userEmail = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var userName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(userEmail))
        {
            ViewBag.Error = "Không lấy được thông tin email từ Google.";
            return View("Login");
        }

        // 2. Kiểm tra xem người dùng có tồn tại trong DB không
        var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            // 3. Nếu người dùng không tồn tại -> Tạo người dùng mới
            user = new User
            {
                Username = userName ?? userEmail, // Lấy tên từ Google, nếu không có thì dùng email
                Email = userEmail,
                Password = Guid.NewGuid().ToString(), // Tạo một mật khẩu ngẫu nhiên, vì họ sẽ không dùng nó
                RoleId = 3, // Mặc định là 'customer'
                Phone = "0000000000", // Cần một giá trị mặc định hoặc trang yêu cầu cập nhật SĐT
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // Kiểm tra nếu tài khoản bị khóa
        if (!user.IsActive)
        {
            ViewBag.Error = "Tài khoản của bạn đã bị khóa.";
            return View("Login");
        }

        // 4. Thiết lập Session cho người dùng
        SetUserSession(user);

        // 5. Chuyển hướng dựa trên vai trò
        return RedirectBasedOnRole(user);
    }

    private void SetUserSession(User user)
    {
        string connectionString = _configuration.GetConnectionString("DefaultConnection");
        string roleName = "";
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var cmd = new SqlCommand("SELECT name FROM Roles WHERE id = @roleId", conn);
            cmd.Parameters.AddWithValue("@roleId", user.RoleId);
            var result = cmd.ExecuteScalar();
            roleName = result?.ToString() ?? "";
        }

        // Xóa session cũ và thiết lập session mới
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetInt32("RoleId", user.RoleId);
        HttpContext.Session.SetString("RoleName", roleName);

        // Thiết lập thông tin cho Farmer và Shipper nếu có
        if (user.RoleId == 4) // Shipper
        {
            var shipper = _context.Shippers.FirstOrDefault(s => s.UserId == user.Id);
            if (shipper != null)
            {
                HttpContext.Session.SetInt32("ShipperId", shipper.Id);
                HttpContext.Session.SetString("ShipperName", shipper.FullName ?? "");
            }
        }
        else if (user.RoleId == 5) // Farmer
        {
            var farmer = _context.Farmers.FirstOrDefault(f => f.UserId == user.Id);
            if (farmer != null)
            {
                HttpContext.Session.SetInt32("FarmerId", farmer.Id);
                HttpContext.Session.SetString("FarmerName", farmer.FullName ?? "");
            }
        }
    }

    private IActionResult RedirectBasedOnRole(User user)
    {
        switch (user.RoleId)
        {
            case 1: // Admin
                return RedirectToAction("Index", "Home");
            case 2: // Staff
                return RedirectToAction("Index", "Home");
            case 5: // Farmer
                return RedirectToAction("Index", "Home"); 
            case 3: // Customer
                return RedirectToAction("Index", "Home"); 
            case 4: // Shipper
                return RedirectToAction("Index", "Home");
            default:
                return RedirectToAction("Login", "Home");
        }
    }
}