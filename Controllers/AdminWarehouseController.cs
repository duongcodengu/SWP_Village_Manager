using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Village_Manager.Data;
using Village_Manager.Extensions;
using Village_Manager.Models;
using Village_Manager.ViewModel;
using BCrypt.Net;

namespace Village_Manager.Controllers;
public class AdminWarehouseController : Controller
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AdminWarehouseController> _logger;
    private readonly EmailSettings _emailSettings;

    public AdminWarehouseController(AppDbContext context, IConfiguration configuration, IWebHostEnvironment env, ILogger<AdminWarehouseController> logger, IOptions<EmailSettings> emailSettings)
    {
        _context = context;
        _configuration = configuration;
        _env = env;
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    // Dashboard: Trang tổng quan quản trị, hiển thị số liệu tổng hợp
    [HttpGet]
    [Route("adminwarehouse")]
    public IActionResult Dashboard()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        // Lấy tổng số khách hàng
        int totalCustomers = _context.Users.Count();
        ViewBag.TotalCustomers = totalCustomers;
        // Lấy tổng số sản phẩm
        int totalProducts = _context.Products.Count(p => p.ApprovalStatus == "accepted");
        ViewBag.TotalProducts = totalProducts;
        // Lấy tổng số đơn hàng
        int totalDeliveredOrders = _context.RetailOrders
            .Count(o => o.Status == "delivered");
        ViewBag.TotalOrders = totalDeliveredOrders;
        // Lấy danh sách category
        var categories = _context.ProductCategories
            .Select(c => new { Name = c.Name, ImageUrl = c.ImageUrl })
            .ToList<dynamic>(); ViewBag.Categories = categories;
        // Tổng doanh thu confirmed
        decimal currentYear = DateTime.Now.Year;

        // Bán lẻ (Retail)
        var retailRevenue = _context.Payments
            .Where(p => p.OrderType == "retail"
                && p.PaymentType == "receive"
                && ((DateTime)p.PaidAt).Year == currentYear)
            .Join(_context.RetailOrders,
                  p => p.OrderId,
                  o => o.Id,
                  (p, o) => new { Payment = p, Order = o })
            .Where(x => x.Order.Status == "delivered")
            .Sum(x => x.Payment.Amount);

        decimal totalRevenue = (retailRevenue ?? 0);
        ViewBag.TotalRevenue = totalRevenue;

        // last 4 đơn hàng
        var retailOrders = (from ro in _context.RetailOrders
                            join u in _context.Users on ro.UserId equals u.Id
                            join roi in _context.RetailOrderItems on ro.Id equals roi.OrderId into roItems
                            from roiGroup in roItems.DefaultIfEmpty()
                            group roiGroup by new { ro.Id, u.Username, ro.OrderDate, ro.Status } into g
                            select new
                            {
                                OrderId = g.Key.Id,
                                Username = g.Key.Username,
                                DatePlaced = g.Key.OrderDate,
                                OrderStatus = g.Key.Status,
                                TotalPrice = g.Sum(x => (x != null) ? x.Quantity * x.UnitPrice : 0),
                                PaymentStatus = _context.Payments.Any(p =>
                                    p.OrderId == g.Key.Id &&
                                    p.OrderType == "retail" &&
                                    p.PaymentType == "receive") ? "Paid" : "Unpaid"
                            });

        var latestOrders = retailOrders
                            .OrderByDescending(x => x.DatePlaced)
                            .Take(4)
                            .ToList();

        ViewBag.LatestOrders = latestOrders.ToList();

        return View();
    }
    //ListProduct
    [HttpGet]
    [Route("product")]
    public IActionResult Products()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var products = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .ToList();
        return View(products);
    }
    // ProductDetail
    [HttpGet]
    [Route("productdetail")]
    public IActionResult ProductDetail(int id)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var product = _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.Category)
                    .Include(p => p.Farmer)
                    .FirstOrDefault(p => p.Id == id);

        if (product == null)
            return NotFound();

        return View(product);
    }
    //Add Product
    [HttpGet("addproduct")]
    public IActionResult AddProduct()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        ViewBag.Farmers = _context.Farmers.ToList();
        ViewBag.Categories = _context.ProductCategories.ToList();
        return View();
    }

    [HttpPost("addproduct")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(IFormCollection form, List<IFormFile> images)
    {
        try
        {
            DateTime? expirationDate = string.IsNullOrWhiteSpace(form["expiration_date"])
                ? null
                : DateTime.Parse(form["expiration_date"]);

            var product = new Product
            {
                Name = form["name"],
                ProductType = form["product_type"],
                Quantity = int.Parse(form["quantity"]),
                Price = decimal.Parse(form["price"]),
                ExpirationDate = expirationDate,
                ProcessingTime = string.IsNullOrWhiteSpace(form["processing_time"]) ? null : DateTime.Parse(form["processing_time"]),
                FarmerId = int.TryParse(form["farmer_id"], out int farmerId) ? farmerId : (int?)null,
                ApprovalStatus = "accepted",
            };

            if (int.TryParse(form["category_id"], out int categoryId))
            {
                product.CategoryId = categoryId;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Save images
            if (images != null && images.Count > 0)
            {
                foreach (var file in images)
                {
                    if (file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var productImage = new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/images/" + uniqueFileName,
                            Description = form["image_description"],
                            UploadedAt = DateTime.Now
                        };

                        _context.ProductImages.Add(productImage);
                    }
                }

                await _context.SaveChangesAsync();
            }

            // Ghi log
            int? userId = HttpContext.Session.GetInt32("UserId");
            Village_Manager.Extensions.LogHelper.SaveLog(_context, userId, $"Added product: {product.Name}");

            return Redirect("/product");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Error adding product: " + ex.Message);
            ViewBag.Farmers = _context.Farmers.ToList();
            ViewBag.Categories = _context.ProductCategories.ToList();
            return View();
        }
    }

    public async Task<IActionResult> Delete(int? id)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        if (id == null)
            return NotFound();

        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        return View("/product");
    }

    // DeleteProduct
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        // Đổi trạng thái thay vì xóa
        product.ApprovalStatus = "rejected";
        await _context.SaveChangesAsync();
        
        // Ghi log
        int? userId = HttpContext.Session.GetInt32("UserId");
        Village_Manager.Extensions.LogHelper.SaveLog(_context, userId, $"Deleted product: {product.Name}");

        return Redirect("/product");
    }

    //Update Product
    [HttpGet]
    [Route("updateproduct")]
    public async Task<IActionResult> UpdateProduct(int id)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        ViewBag.Categories = _context.ProductCategories.ToList();
        ViewBag.Farmers = _context.Farmers.ToList();
        return View(product);
    }


    [HttpPost]
    [Route("updateproduct")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProduct(Product model, List<IFormFile> ImageUpdate)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == model.Id);

        if (product == null)
        {
            return NotFound();
        }

        // Cập nhật chỉ nếu dữ liệu được cung cấp
        if (!string.IsNullOrEmpty(model.Name))
            product.Name = model.Name;

        if (model.CategoryId != 0)
            product.CategoryId = model.CategoryId;

        if (model.Price != 0)
            product.Price = model.Price;

        //if (model.ExpirationDate != default)
        //    product.ExpirationDate = model.ExpirationDate;

        if (!string.IsNullOrEmpty(model.ProductType))
            product.ProductType = model.ProductType;

        if (model.Quantity != 0)
            product.Quantity = model.Quantity;

        //if (model.ProcessingTime != default)
        //    product.ProcessingTime = model.ProcessingTime;

        if (model.FarmerId != 0)
            product.FarmerId = model.FarmerId;

        // Nếu có ảnh mới, thì cập nhật
        if (ImageUpdate != null && ImageUpdate.Any())
        {
            _context.ProductImages.RemoveRange(product.ProductImages);

            foreach (var file in ImageUpdate)
            {
                if (file.Length > 0)
                {
                    var fileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName())
                                    + Path.GetExtension(file.FileName);
                    var path = Path.Combine("wwwroot/images", fileName);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    product.ProductImages.Add(new ProductImage
                    {
                        ImageUrl = "/images/" + fileName
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        // Sau khi cập nhật thành công:
        int? userId = HttpContext.Session.GetInt32("UserId");
        Village_Manager.Extensions.LogHelper.SaveLog(_context, userId, $"Updated product: {model.Name}");
        return Redirect("/products");
    }
    [HttpGet]
    [Route("searchProduct")]
    public async Task<IActionResult> SearchProduct(string search)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        if (string.IsNullOrEmpty(search))
        {
            return Redirect("products"); 
        }

        var productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .AsQueryable();

        search = search.ToLower();
        productsQuery = productsQuery.Where(p =>
            p.Name.ToLower().Contains(search) ||
            (p.Category != null && p.Category.Name.ToLower().Contains(search)) ||
            p.ProductType.ToLower().Contains(search)
        );

        var products = await productsQuery.ToListAsync();
        return View("Products", products);
    }
    [HttpGet]
    [Route("alluser")]
    public async Task<IActionResult> AllUser(string searchUser, int page = 1, int roleId = 0, string sortOrder = "asc")
    {
        try
        {
            _logger.LogInformation("Starting to load all users...");
            // Lấy thông tin session
            var username = HttpContext.Session.GetString("Username");
            var sessionRoleId = HttpContext.Session.GetInt32("RoleId");
            // Kiểm tra quyền admin
            if (string.IsNullOrEmpty(username) || sessionRoleId != 1)
            {
                _logger.LogWarning($"Unauthorized access attempt. Username: {username}, RoleId: {sessionRoleId}");
                Response.StatusCode = 404;
                return View("404");
            }
            // Phân trang, tìm kiếm, lọc role
            int pageSize = 10;
            var usersQuery = _context.Users.Include(u => u.Role).AsQueryable();
            if (!string.IsNullOrEmpty(searchUser))
            {
                usersQuery = usersQuery.Where(u => u.Username.Contains(searchUser));
            }
            if (roleId > 0)
            {
                usersQuery = usersQuery.Where(u => u.RoleId == roleId);
            }
            // Sắp xếp theo tên
            if (sortOrder == "desc")
                usersQuery = usersQuery.OrderByDescending(u => u.Username);
            else
                usersQuery = usersQuery.OrderBy(u => u.Username);
            int totalUsers = await usersQuery.CountAsync();
            var users = await usersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
            ViewBag.SearchUser = searchUser;
            ViewBag.RoleId = roleId;
            ViewBag.Roles = await _context.Roles.ToListAsync(); // Để hiển thị dropdown lọc role
            ViewBag.SortOrder = sortOrder;
            return View(users);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading users: {ex.Message}");
            TempData["ErrorMessage"] = "An error occurred while loading users.";
            ViewBag.Roles = _context.Roles.ToList();
            return View(new List<User>());
        }
    }

    // Delete: Xóa user theo id
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("delete/{id}")]
    public IActionResult Delete(int id)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        // Tìm user theo id
        var user = _context.Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return NotFound();
        }
        if (user.Id == 1)
        {
            TempData["Error"] = "Không thể xóa tài khoản Super Admin!";
            return RedirectToAction("AllUser");
        }
        // Xóa các bản ghi liên quan ở Farmer
        var farmers = _context.Farmers.Where(f => f.UserId == id).ToList();
        if (farmers.Any())
        {
            _context.Farmers.RemoveRange(farmers);
        }

        // Xóa các bản ghi liên quan ở Shipper (nếu có)
        var shippers = _context.Shippers.Where(s => s.UserId == id).ToList();
        if (shippers.Any())
        {
            _context.Shippers.RemoveRange(shippers);
        }

        // Có thể thêm các bảng liên quan khác nếu cần...

        // Xóa user
        _context.Users.Remove(user);
        _context.SaveChanges();

        var currentUserId = HttpContext.Session.GetInt32("UserId");
        Village_Manager.Extensions.LogHelper.SaveLog(_context, currentUserId, $"Deleted user: {user.Username} (ID: {user.Id})");
        return RedirectToAction("AllUser");
    }

    // AddUser (GET): Hiển thị form thêm user mới
    [HttpGet]
    [Route("adduser")]
    public IActionResult AddUser()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        // Lấy danh sách role để hiển thị trong form
        ViewBag.Roles = _context.Roles.ToList();
        return View();
    }

    // AddUser (POST): Xử lý thêm user mới
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("adduser")]
    public async Task<IActionResult> AddUser(User user, string confirmPassword)
    {
        try
        {
            // Kiểm tra quyền admin
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }
            _logger.LogInformation("Starting user creation process...");
            ViewBag.Roles = await _context.Roles.ToListAsync();
            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password) || string.IsNullOrEmpty(user.Email) || user.RoleId <= 0)
            {
                ModelState.AddModelError("", "Please fill in all required fields");
                return View(user);
            }
            // Kiểm tra xác nhận mật khẩu
            if (user.Password != confirmPassword)
            {
                ModelState.AddModelError("Password", "Passwords do not match");
                return View(user);
            }
            // Kiểm tra độ dài mật khẩu
            if (user.Password.Length != 6)
            {
                ModelState.AddModelError("Password", "Password must be exactly 6 characters long");
                return View(user);
            }
            // Kiểm tra username/email đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "Username already exists");
                return View(user);
            }
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(user);
            }
            // Kiểm tra định dạng số điện thoại (10 chữ số, bắt đầu bằng 0)
            if (!string.IsNullOrEmpty(user.Phone))
            {
                if (user.Phone.Length != 10 || !user.Phone.All(char.IsDigit) || user.Phone[0] != '0')
                {
                    ModelState.AddModelError("Phone", "Phone number must be exactly 10 digits and start with 0");
                    return View(user);
                }
            }
            // Tạo user mới
            var newUser = new User
            {
                Username = user.Username.Trim(),
                Email = user.Email.Trim(),
                Password = BCrypt.Net.BCrypt.HashPassword(user.Password),
                RoleId = user.RoleId,
                Phone = user.Phone?.Trim(),
                CreatedAt = DateTime.Now
            };
            // Lưu vào database
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Thêm vào bảng Farmer nếu role là farmer
            var farmerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == "farmer");
            if (farmerRole != null && newUser.RoleId == farmerRole.Id)
            {
                var newFarmer = new Farmer
                {
                    UserId = newUser.Id,
                    FullName = newUser.Username, // hoặc lấy từ form nếu có trường tên đầy đủ
                    Phone = newUser.Phone,
                    Address = "" // lấy từ form nếu có, hoặc để trống
                };
                _context.Farmers.Add(newFarmer);
                await _context.SaveChangesAsync();
            }

            // Thêm vào bảng Shipper nếu role là shipper
            var shipperRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == "shipper");
            if (shipperRole != null && newUser.RoleId == shipperRole.Id)
            {
                var newShipper = new Shipper
                {
                    UserId = newUser.Id,
                    FullName = newUser.Username, // hoặc lấy từ form nếu có trường tên đầy đủ
                    Phone = newUser.Phone,
                    VehicleInfo = null // cho phép null
                };
                _context.Shippers.Add(newShipper);
                await _context.SaveChangesAsync();
            }

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            Village_Manager.Extensions.LogHelper.SaveLog(_context, currentUserId, $"Added new user: {newUser.Username} (ID: {newUser.Id})");
            TempData["SuccessMessage"] = "Tạo người dùng thành công!";
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

    [HttpGet]
    [Route("/edituser/{id}")]
    public async Task<IActionResult> EditUser(int id)
    {
        try
        {
            // Kiểm tra quyền admin
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }
            _logger.LogInformation($"Loading user for edit. UserId: {id}");
            // Lấy user theo id
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

    // EditUser (POST): Xử lý cập nhật thông tin user
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("edituser/{id}")]
    public async Task<IActionResult> EditUser(int id, User user, string newPassword)
    {
        try
        {
            // Kiểm tra quyền admin
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }
            _logger.LogInformation($"Processing user update. UserId: {id}");
            ViewBag.Roles = await _context.Roles.ToListAsync();
            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Email) || user.RoleId <= 0)
            {
                _logger.LogWarning($"Invalid user data. Username: {user.Username}, Email: {user.Email}, RoleId: {user.RoleId}");
                ModelState.AddModelError("", "Please fill in all required fields");
                return View(user);
            }
            // Kiểm tra định dạng số điện thoại (10 chữ số, bắt đầu bằng 0)
            if (!string.IsNullOrEmpty(user.Phone))
            {
                if (user.Phone.Length != 10 || !user.Phone.All(char.IsDigit) || user.Phone[0] != '0')
                {
                    ModelState.AddModelError("Phone", "Phone number must be exactly 10 digits and start with 0");
                    return View(user);
                }
            }
            // Lấy user hiện tại
            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                _logger.LogWarning($"User not found for update. UserId: {id}");
                return NotFound();
            }
            // Kiểm tra username/email đã tồn tại chưa (trừ user hiện tại)
            if (await _context.Users.AnyAsync(u => u.Username == user.Username && u.Id != id))
            {
                _logger.LogWarning($"Username already exists: {user.Username}");
                ModelState.AddModelError("Username", "Username already exists");
                return View(user);
            }
            if (await _context.Users.AnyAsync(u => u.Email == user.Email && u.Id != id))
            {
                _logger.LogWarning($"Email already exists: {user.Email}");
                ModelState.AddModelError("Email", "Email already exists");
                return View(user);
            }
            // Không cho đổi username của tài khoản đặc biệt (Id == 1)
            if (id == 1 && user.Username != existingUser.Username)
            {
                ModelState.AddModelError("Username", "Không thể đổi username của tài khoản đặc biệt!");
                return View(user);
            }
            // Không cho đổi role của tài khoản đặc biệt (Id == 1)
            if (id == 1 && user.RoleId != existingUser.RoleId)
            {
                ModelState.AddModelError("RoleId", "Không thể đổi vai trò của tài khoản đặc biệt!");
                return View(user);
            }
            // Cập nhật thông tin user
            existingUser.Username = user.Username.Trim();
            existingUser.Email = user.Email.Trim();
            existingUser.RoleId = user.RoleId;
            existingUser.Phone = user.Phone?.Trim();
            // Nếu có nhập mật khẩu mới thì kiểm tra đúng 8 ký tự
            if (!string.IsNullOrEmpty(newPassword))
            {
                existingUser.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _logger.LogInformation($"Password updated for user. UserId: {id}");
            }
            // Lưu thay đổi
            await _context.SaveChangesAsync();

            // Cập nhật hoặc tạo Farmer nếu role là farmer
            var farmerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == "farmer");
            if (farmerRole != null && existingUser.RoleId == farmerRole.Id)
            {
                var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == existingUser.Id);
                if (farmer == null)
                {
                    // Nếu chưa có thì tạo mới
                    var newFarmer = new Farmer
                    {
                        UserId = existingUser.Id,
                        FullName = existingUser.Username, // hoặc lấy từ form nếu có trường tên đầy đủ
                        Phone = existingUser.Phone,
                        Address = "" // lấy từ form nếu có, hoặc để trống
                    };
                    _context.Farmers.Add(newFarmer);
                }
                else
                {
                    // Nếu đã có thì cập nhật thông tin
                    farmer.FullName = existingUser.Username; // hoặc lấy từ form nếu có trường tên đầy đủ
                    farmer.Phone = existingUser.Phone;
                    // farmer.Address = ... // lấy từ form nếu có
                }
                await _context.SaveChangesAsync();
            }

            // Cập nhật hoặc tạo Shipper nếu role là shipper
            var shipperRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == "shipper");
            if (shipperRole != null && existingUser.RoleId == shipperRole.Id)
            {
                var shipper = await _context.Shippers.FirstOrDefaultAsync(s => s.UserId == existingUser.Id);
                if (shipper == null)
                {
                    var newShipper = new Shipper
                    {
                        UserId = existingUser.Id,
                        FullName = existingUser.Username, // hoặc lấy từ form nếu có trường tên đầy đủ
                        Phone = existingUser.Phone,
                        VehicleInfo = null // cho phép null
                    };
                    _context.Shippers.Add(newShipper);
                }
                else
                {
                    shipper.FullName = existingUser.Username;
                    shipper.Phone = existingUser.Phone;
                    // shipper.VehicleInfo = ... // lấy từ form nếu có
                }
                await _context.SaveChangesAsync();
            }

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            Village_Manager.Extensions.LogHelper.SaveLog(_context, currentUserId, $"Updated user: {existingUser.Username} (ID: {existingUser.Id})");
            _logger.LogInformation($"User updated successfully. UserId: {id}");
            TempData["SuccessMessage"] = "Tạo người dùng thành công!";
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

    // view to change profile setting
    [HttpGet("profilesetting")]
    public IActionResult Index()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Home");
        }
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        return View("~/Views/AdminWarehouse/ProfileSetting.cshtml", user);
    }

    // view to changge create role 
    [HttpGet("createrole")]
    public IActionResult CreateRole()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Home");
        }
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        return View("~/Views/AdminWarehouse/CreateRole.cshtml", user);
    }

    // Logs: Hiển thị lịch sử thao tác (log) của admin
    [HttpGet]
    [Route("logs")]
    public IActionResult Logs(int page = 1)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        int pageSize = 20;
        // Lấy danh sách log, join với user để lấy tên
        var logs = (from l in _context.Logs
                    join u in _context.Users on l.UserId equals u.Id into userJoin
                    from u in userJoin.DefaultIfEmpty()
                    orderby l.CreatedAt descending
                    select new Village_Manager.Models.Dto.LogViewModel
                    {
                        Username = u != null ? u.Username : "Unknown",
                        Action = l.Action,
                        CreatedAt = l.CreatedAt
                    })
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)_context.Logs.Count() / pageSize);
        return View("~/Views/AdminWarehouse/Logs.cshtml", logs);
    }

    // Lấy danh sách farmer
    [HttpGet]
    [Route("famer")]
    public IActionResult Famer()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var farmers = (from f in _context.Farmers
                       join u in _context.Users on f.UserId equals u.Id
                       join r in _context.Roles on u.RoleId equals r.Id
                       where r.Name == "farmer"
                       select new
                       {
                           FarmerId = f.Id,
                           FarmerName = f.FullName,
                           FarmerPhone = f.Phone,
                           FarmerAddress = f.Address,
                           Username = u.Username,
                           Email = u.Email,
                           CreatedAt = u.CreatedAt,
                           UserId = u.Id
                       }).ToList();

        ViewBag.Farmers = farmers.ToList();

        return View();
    }

    //update farmer thông tin
    [HttpPost]
    [Route("famer/update")]
    public IActionResult UpdateFarmer(int FarmerId, string FarmerName, string FarmerPhone, string FarmerAddress)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var farmer = _context.Farmers.FirstOrDefault(f => f.Id == FarmerId);
        if (farmer != null)
        {
            farmer.FullName = FarmerName;
            farmer.Phone = FarmerPhone;
            farmer.Address = FarmerAddress;

            _context.SaveChanges();
        }

        return RedirectToAction("Famer");
    }

    // xóa role farmer
    [HttpPost]
    [Route("famer/change-role")]
    public IActionResult ChangeRole(int UserId)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }

        int newRoleId = 3;

        var user = _context.Users.FirstOrDefault(u => u.Id == UserId);
        if (user != null)
        {
            user.RoleId = newRoleId;
            // đổi role
            _context.SaveChanges();
        }

        return RedirectToAction("Famer");
    }
    [HttpGet]
    [Route("addfamer")]
    public IActionResult AddFamer()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var pending = _context.FarmerRegistrationRequests
            .Where(r => r.Status == "pending")
            .OrderByDescending(r => r.RequestedAt)
            .ToList();

        return View(pending);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }

        var request = await _context.FarmerRegistrationRequests.FindAsync(id);

        if (request == null || request.Status != "pending")
            return NotFound();

        request.Status = "approved";
        request.ReviewedAt = DateTime.Now;
        request.ReviewedBy = HttpContext.Session.GetInt32("UserId");

        // Kiểm tra xem Farmer đã tồn tại chưa
        var existingFarmer = await _context.Farmers
            .FirstOrDefaultAsync(f => f.UserId == request.UserId);

        if (existingFarmer != null)
        {
            // Cập nhật thông tin
            existingFarmer.FullName = request.FullName;
            existingFarmer.Phone = request.Phone;
            existingFarmer.Address = request.Address;
        }
        else
        {
            // Tạo mới
            _context.Farmers.Add(new Farmer
            {
                UserId = request.UserId,
                FullName = request.FullName,
                Phone = request.Phone,
                Address = request.Address
            });
        }

        // Cập nhật vai trò người dùng
        var user = await _context.Users.FindAsync(request.UserId);
        if (user != null)
        {
            user.RoleId = 5; // Role 5 = Farmer?
        }

        await _context.SaveChangesAsync();

        // Ghi log
        int? userId = HttpContext.Session.GetInt32("UserId");
        Village_Manager.Extensions.LogHelper.SaveLog(
            _context,
            userId,
            $"Approved farmer: {request.FullName}"
        );

        return RedirectToAction("AddFamer");
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var request = await _context.FarmerRegistrationRequests.FindAsync(id);

        if (request == null || request.Status != "pending")
            return NotFound();

        request.Status = "rejected";
        request.RequestedAt = DateTime.Now;
        request.ReviewedBy = HttpContext.Session.GetInt32("UserId");

        await _context.SaveChangesAsync();
        // Ghi log
        int? userId = HttpContext.Session.GetInt32("UserId");
        Village_Manager.Extensions.LogHelper.SaveLog(_context, userId, $"Rejected farmer: {request.FullName}");
        return RedirectToAction("AddFamer"); 
    }
    // view to pending products
    [HttpGet]
    [Route("pendingproducts")]
    public IActionResult PendingProducts()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var pendingProducts = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Farmer)
            .Include(p => p.ProductImages)
            .Where(p => p.ApprovalStatus == "pending")
            .ToList();
        return View(pendingProducts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("approveproduct")]
    public IActionResult ApproveProduct(int id, string action)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var product = _context.Products.FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound();

        if (action == "accept")
            product.ApprovalStatus = "accepted";
        else if (action == "reject")
            product.ApprovalStatus = "rejected";

        _context.SaveChanges();
        // Ghi log
        int? userId = HttpContext.Session.GetInt32("UserId");
        Village_Manager.Extensions.LogHelper.SaveLog(_context, userId, $"Duyệt sản phẩm: {product?.Name} - Hành động: {action}");
        return RedirectToAction("Products");
    }
    [HttpGet]
    [Route("shipper")]
    public IActionResult Shipper()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var result = (from shipper in _context.Shippers
                      join request in _context.ShipperRegistrationRequests
                      on shipper.UserId equals request.UserId
                      select new ShipperDisplayViewModel
                      {
                          Id = shipper.Id,
                          FullName = shipper.FullName,
                          Phone = shipper.Phone,
                          VehicleInfo = shipper.VehicleInfo,
                          Status = shipper.Status,
                          Address = request.Address,
                          UserId = request.UserId,
                          Username = shipper.User.Username,
                          Email = shipper.User.Email,
                          Created = shipper.User.CreatedAt
                      }).ToList();

        return View(result);
    }

    [HttpGet]
    [Route("admin/shipper-requests")]
    public IActionResult ShipperRequests()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var result = _context.ShipperRegistrationRequests
            .Include(r => r.User)
            .Where(r => r.Status == "pending")
            .Select(r => new ShipperRequestViewModel
            {
                Id = r.Id,
                FullName = r.FullName,
                Phone = r.Phone,
                VehicleInfo = r.VehicleInfo,
                Address = r.Address,
                RequestedAt = r.RequestedAt,
                Username = r.User.Username,
                Email = r.User.Email
            })
            .OrderByDescending(r => r.RequestedAt)
            .ToList();

        return View(result);
    }

    [HttpPost]
    [Route("admin_shipperrequest_update")]
    public async Task<IActionResult> UpdateShipperRequest(int id, string action)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var request = await _context.ShipperRegistrationRequests
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null || request.Status != "pending")
        {
            TempData["Error"] = "Yêu cầu không hợp lệ hoặc đã xử lý.";
            return Redirect("/admin/shipper-requests");
        }

        try
        {
            request.ReviewedAt = DateTime.Now;
            request.ReviewedBy = HttpContext.Session.GetInt32("UserId");

            if (action == "accept")
            {
                request.Status = "approved";

                // Tạo shipper mới
                var newShipper = new Shipper
                {
                    UserId = request.UserId,
                    FullName = request.FullName,
                    Phone = request.Phone,
                    VehicleInfo = request.VehicleInfo,
                    Status = "approved"
                };

                _context.Shippers.Add(newShipper);

                // Cập nhật role người dùng nếu là "customer"
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
                if (user != null && user.RoleId == 3)
                {
                    user.RoleId = 4;
                    _context.Users.Update(user);
                }
            }
            else if (action == "reject")
            {
                request.Status = "rejected";
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật yêu cầu thành công.";
        }
        catch (Exception e)
        {
            TempData["Error"] = "Đã xảy ra lỗi khi xử lý yêu cầu.";
        }

        return Redirect("/admin/shipper-requests");
    }
    [HttpGet("listorder")]
    public IActionResult ListRequestOrder()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var shippedOrders = _context.RetailOrders
            .Include(o => o.User)
            .Where(o => o.Status == "confirmed")
            .ToList();

        var viewModel = shippedOrders.Select(o => new RetailOrderViewModel
        {
            Id = o.Id,
            UserName = o.User?.Username,
            Phone = o.User?.Phone,
            Address = _context.UserLocations
                        .FirstOrDefault(l => l.UserId == o.UserId)?.Address ?? "Không có",
            OrderDate = o.OrderDate,
            Status = o.Status
        }).ToList();

        return View(viewModel);
    }

    // Trang: Hiển thị đơn hàng chờ duyệt
    [HttpGet("addOrder")]
    public IActionResult AddOrder()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var pendingOrders = _context.RetailOrders
        .Include(o => o.User)
        .Where(o => o.Status == "pending")
        .ToList();

        var viewModel = pendingOrders.Select(o => new RetailOrderViewModel
        {
            Id = o.Id,
            Users = o.User,
            UserName = o.User?.Username,
            Phone = o.User?.Phone,
            Address = _context.UserLocations
                        .FirstOrDefault(l => l.UserId == o.UserId)?.Address ?? "Không có",
            OrderDate = o.OrderDate,
            Status = o.Status
        }).ToList();
        return View(viewModel);
    }

    // POST: Admin duyệt đơn hàng
    [HttpPost("acceptOrder")]
    public IActionResult AcceptOrder(int id)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var order = _context.RetailOrders.FirstOrDefault(o => o.Id == id);
        if (order == null)
        {
            return NotFound();
        }

        order.Status = "confirmed";
        _context.SaveChanges();

        return RedirectToAction("ListRequestOrder");
    }
    [HttpGet("order/detail/{id}")]
    public IActionResult OrderDetail(int id)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var order = _context.RetailOrders
            .Include(o => o.User)
            .Include(o => o.RetailOrderItems)
                .ThenInclude(ri => ri.Product)
                    .ThenInclude(p => p.ProductImages)
            .FirstOrDefault(o => o.Id == id);

        if (order == null)
        {
            ViewBag.Error = "Tài khoản này chưa có đơn mua hàng nào.";
            return View("OrderDetail", null);
        }

        var firstItem = order.RetailOrderItems.FirstOrDefault(); // lấy 1 item làm ví dụ

        var viewModel = new RetailOrderItemViewModel
        {
            Id = order.Id,
            OrderId = order.Id,
            UserName = order.User?.Username,
            Phone = order.User?.Phone,
            Address = _context.UserLocations.FirstOrDefault(l => l.UserId == order.UserId)?.Address ?? "Không có",
            OrderDate = order.OrderDate,
            Status = order.Status,

            ProductId = firstItem?.ProductId ?? 0,
            ProductName = firstItem?.Product?.Name,
            ProductImageUrl = firstItem?.Product?.ProductImages?.FirstOrDefault()?.ImageUrl ?? "/images/default.jpg",
            Quantity = firstItem?.Quantity ?? 0,
            UnitPrice = firstItem?.UnitPrice ?? 0,
            Product = firstItem?.Product
        };

        return View(viewModel);
    }

    [HttpPost("deletOrrder/{id}")]
    public IActionResult DeleteOrder(int id)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var order = _context.RetailOrders
                            .Include(o => o.RetailOrderItems) // tên navigation property
                            .FirstOrDefault(o => o.Id == id);

        if (order == null)
            return NotFound();

        // Xoá các bản ghi con
        if (order.RetailOrderItems != null)
        {
            _context.RetailOrderItems.RemoveRange(order.RetailOrderItems);
        }

        // Xoá đơn hàng chính
        _context.RetailOrders.Remove(order);
        _context.SaveChanges();

        return Redirect("/listOrder"); 
    }


    // Ban user (set IsActive = false)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("banuser/{id}")]
    public IActionResult BanUser(int id)
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
        if (user.Id == 1)
        {
            TempData["Error"] = "Không thể ban tài khoản Super Admin!";
            return RedirectToAction("AllUser");
        }
        user.IsActive = false;
        _context.SaveChanges();
        // Ghi log
        int? userId = HttpContext.Session.GetInt32("UserId");
        Village_Manager.Extensions.LogHelper.SaveLog(_context, userId, $"Banned account: {user?.Username}");
        TempData["SuccessMessage"] = $"Đã khóa tài khoản {user.Username}";
        return RedirectToAction("AllUser");
    }

    // Unban user (set IsActive = true)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("unbanuser/{id}")]
    public IActionResult UnbanUser(int id)
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
        if (user.Id == 1)
        {
            TempData["Error"] = "Không thể mở khóa tài khoản Super Admin!";
            return RedirectToAction("AllUser");
        }
        user.IsActive = true;
        _context.SaveChanges();
        // Ghi log
        int? userId = HttpContext.Session.GetInt32("UserId");
        Village_Manager.Extensions.LogHelper.SaveLog(_context, userId, $"Unbanned account: {user?.Username}");
        TempData["SuccessMessage"] = $"Đã mở khóa tài khoản {user.Username}";
        return RedirectToAction("AllUser");
    }

    //support
    [HttpGet]
    [Route("support")]
    public async Task<IActionResult> Support()
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var tickets = await _context.ContactMessages
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return View(tickets); 
    }

    [HttpPost]
    [Route("support/reply")]
    public async Task<IActionResult> Reply(int Id, string Email, string Content)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
        message.To.Add(new MailboxAddress("", Email));
        message.Subject = "Phản hồi từ bộ phận hỗ trợ";
        message.Body = new TextPart("plain") { Text = Content };

        using var client = new MailKit.Net.Smtp.SmtpClient();
        await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.AppPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        TempData["Success"] = "Success";
        return RedirectToAction("Support");
    }

    [HttpGet("/adminwarehouse/role/add")]
    public IActionResult AddRole()
    {
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        return View("~/Views/AdminWarehouse/AddRole.cshtml");
    }

    [HttpPost("/adminwarehouse/role/add")]
    [ValidateAntiForgeryToken]
    public IActionResult AddRole(Role model)
    {
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        if (!ModelState.IsValid)
        {
            return View("~/Views/AdminWarehouse/AddRole.cshtml", model);
        }
        _context.Roles.Add(model);
        _context.SaveChanges();
        // Ghi log ngay sau khi lưu role
        int? userId = HttpContext.Session.GetInt32("UserId");
        Village_Manager.Extensions.LogHelper.SaveLog(_context, userId, $"Added role: {model.Name}");
        return RedirectToAction("Dashboard");
    }

    [HttpGet("/adminwarehouse/role/edit/{id}")]
    public async Task<IActionResult> EditRole(int id)
    {
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var role = await _context.Roles.FindAsync(id);
        if (role == null)
        {
            return NotFound();
        }
        return View("~/Views/AdminWarehouse/EditRole.cshtml", role);
    }

    [HttpPost("/adminwarehouse/role/edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRole(int id, Role model)
    {
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var role = await _context.Roles.FindAsync(id);
        if (role == null)
        {
            return NotFound();
        }
        role.Name = model.Name;
        await _context.SaveChangesAsync();
        // Ghi log ngay sau khi lưu role
        int? userId = HttpContext.Session.GetInt32("UserId");
        Village_Manager.Extensions.LogHelper.SaveLog(_context, userId, $"Updated role: {model.Name}");
        return RedirectToAction("Dashboard");
    }

    // API để lấy danh sách sản phẩm cho Media
    [HttpGet]
    [Route("GetProducts")]
    public IActionResult GetProducts()
    {
        var products = _context.Products
            .Where(p => p.ApprovalStatus == "accepted")
            .Select(p => new { id = p.Id, name = p.Name })
            .ToList();

        return Json(products);
    }
}