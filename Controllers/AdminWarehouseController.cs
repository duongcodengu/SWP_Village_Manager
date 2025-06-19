using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Village_Manager.Data;
using Village_Manager.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Village_Manager.Extensions;

namespace Village_Manager.Controllers
{
    public class AdminWarehouseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminWarehouseController> _logger;

        public AdminWarehouseController(AppDbContext context, IConfiguration configuration, ILogger<AdminWarehouseController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // kiểm tra quyền truy cập
        [HttpGet]
        [Route("adminwarehouse")]
        public IActionResult Dashboard()
        {
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }

            // Tổng số khách hàng
            int totalCustomers = _context.Users.Count();
            ViewBag.TotalCustomers = totalCustomers;

            // Tổng số sản phẩm
            int totalProducts = _context.Products.Count();
            ViewBag.TotalProducts = totalProducts;

            // Tổng số đơn hàng
            int totalRetailOrders = _context.RetailOrders.Count();
            int totalWholesaleOrders = _context.WholesaleOrders.Count();
            int totalOrders = totalRetailOrders + totalWholesaleOrders;
            ViewBag.TotalOrders = totalOrders;

            // Lấy category (name, image_url)
            var categories = _context.ProductCategory
                .Select(c => new
                {
                    Name = c.Name,
                }).ToList<dynamic>();
            ViewBag.Categories = categories;

            // Tổng doanh thu delivered
            decimal totalRevenue = 0;
            // RetailOrder
            var retailRevenue = (from ro in _context.RetailOrders
                                 where ro.Status == "delivered"
                                 join ri in _context.RetailOrderItems on ro.Id equals ri.OrderId
                                 select ri.Quantity * ri.UnitPrice).Sum();
            // WholesaleOrder
            var wholesaleRevenue = (from wo in _context.WholesaleOrders
                                    where wo.Status == "delivered"
                                    join wi in _context.WholesaleOrderItems on wo.Id equals wi.OrderId
                                    select wi.Quantity * wi.UnitPrice).Sum();

            totalRevenue = (retailRevenue ?? 0) + (wholesaleRevenue ?? 0);
            ViewBag.TotalRevenue = totalRevenue;

            return View();
        }

        // Show all users in the data
        [HttpGet]
        [Route("adminwarehouse/alluser")]
        public async Task<IActionResult> AllUser(string searchUser)
        {
            try
            {
                _logger.LogInformation("Starting to load all users...");
                
                var username = HttpContext.Session.GetString("Username");
                var roleId = HttpContext.Session.GetInt32("RoleId");

                _logger.LogInformation($"Session info - Username: {username}, RoleId: {roleId}");

                if (string.IsNullOrEmpty(username) || roleId != 1)
                {
                    _logger.LogWarning($"Unauthorized access attempt. Username: {username}, RoleId: {roleId}");
                    Response.StatusCode = 404;
                    return View("404");
                }

                // Lọc user theo username nếu có searchUser
                var usersQuery = _context.Users.Include(u => u.Role).AsQueryable();
                if (!string.IsNullOrEmpty(searchUser))
                {
                    usersQuery = usersQuery.Where(u => u.Username.Contains(searchUser));
                }
                var users = await usersQuery.OrderByDescending(u => u.CreatedAt).ToListAsync();

                _logger.LogInformation($"Successfully loaded {users.Count} users");
                ViewBag.SearchUser = searchUser; // Để giữ lại từ khóa trên view
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading users: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading users.";
                return View(new List<User>());
            }
        }

        // delete user by id
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("adminwarehouse/delete/{id}")]
        public IActionResult Delete(int id)
        {
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            return RedirectToAction("AllUser");
        }

        [HttpGet]
        [Route("adminwarehouse/adduser")]
        public IActionResult AddUser()
        {
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }

            ViewBag.Roles = _context.Roles.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("adminwarehouse/adduser")]
        public async Task<IActionResult> AddUser(User user, string confirmPassword)
        {
            try
            {
                if (!HttpContext.Session.IsAdmin())
                {
                    Response.StatusCode = 404;
                    return View("404");
                }

                _logger.LogInformation("Starting user creation process...");
                ViewBag.Roles = await _context.Roles.ToListAsync();

                // Basic validation
                if (string.IsNullOrEmpty(user.Username) || 
                    string.IsNullOrEmpty(user.Password) || 
                    string.IsNullOrEmpty(user.Email) || 
                    user.RoleId <= 0)
                {
                    ModelState.AddModelError("", "Please fill in all required fields");
                    return View(user);
                }

                // Check password match
                if (user.Password != confirmPassword)
                {
                    ModelState.AddModelError("Password", "Passwords do not match");
                    return View(user);
                }

                // Check if username exists
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                {
                    ModelState.AddModelError("Username", "Username already exists");
                    return View(user);
                }

                // Check if email exists
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists");
                    return View(user);
                }

                // Create new user
                var newUser = new User
                {
                    Username = user.Username.Trim(),
                    Email = user.Email.Trim(),
                    Password = HashPassword(user.Password),
                    RoleId = user.RoleId,
                    CreatedAt = DateTime.Now
                };

                // Save to database
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "User created successfully!";
                return RedirectToAction("AllUser");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating user: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while creating the user. Please try again.");
                ViewBag.Roles = await _context.Roles.ToListAsync();
                return View(user);
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        [HttpGet]
        [Route("adminwarehouse/edituser/{id}")]
        public async Task<IActionResult> EditUser(int id)
        {
            try
            {
                if (!HttpContext.Session.IsAdmin())
                {
                    Response.StatusCode = 404;
                    return View("404");
                }

                _logger.LogInformation($"Loading user for edit. UserId: {id}");

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"User not found. UserId: {id}");
                    return NotFound();
                }

                ViewBag.Roles = await _context.Roles.ToListAsync();
                _logger.LogInformation($"Successfully loaded user for edit. UserId: {id}");
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading user for edit: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading user data.";
                return RedirectToAction("AllUser");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("adminwarehouse/edituser/{id}")]
        public async Task<IActionResult> EditUser(int id, User user, string newPassword)
        {
            try
            {
                if (!HttpContext.Session.IsAdmin())
                {
                    Response.StatusCode = 404;
                    return View("404");
                }

                _logger.LogInformation($"Processing user update. UserId: {id}");

                ViewBag.Roles = await _context.Roles.ToListAsync();

                // Basic validation
                if (string.IsNullOrEmpty(user.Username) || 
                    string.IsNullOrEmpty(user.Email) || 
                    user.RoleId <= 0)
                {
                    _logger.LogWarning($"Invalid user data. Username: {user.Username}, Email: {user.Email}, RoleId: {user.RoleId}");
                    ModelState.AddModelError("", "Please fill in all required fields");
                    return View(user);
                }

                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null)
                {
                    _logger.LogWarning($"User not found for update. UserId: {id}");
                    return NotFound();
                }

                // Check if username exists (excluding current user)
                if (await _context.Users.AnyAsync(u => u.Username == user.Username && u.Id != id))
                {
                    _logger.LogWarning($"Username already exists: {user.Username}");
                    ModelState.AddModelError("Username", "Username already exists");
                    return View(user);
                }

                // Check if email exists (excluding current user)
                if (await _context.Users.AnyAsync(u => u.Email == user.Email && u.Id != id))
                {
                    _logger.LogWarning($"Email already exists: {user.Email}");
                    ModelState.AddModelError("Email", "Email already exists");
                    return View(user);
                }

                // Update user properties
                existingUser.Username = user.Username.Trim();
                existingUser.Email = user.Email.Trim();
                existingUser.RoleId = user.RoleId;

                // Update password if provided
                if (!string.IsNullOrEmpty(newPassword))
                {
                    existingUser.Password = HashPassword(newPassword);
                    _logger.LogInformation($"Password updated for user. UserId: {id}");
                }

                // Save changes
                await _context.SaveChangesAsync();
                _logger.LogInformation($"User updated successfully. UserId: {id}");

                TempData["SuccessMessage"] = "User updated successfully!";
                return RedirectToAction("AllUser");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while updating the user. Please try again.");
                ViewBag.Roles = await _context.Roles.ToListAsync();
                return View(user);
            }
        }
    }
}
