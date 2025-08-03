using BCrypt.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Utils;
using Village_Manager.Data;
using Village_Manager.Extensions;
using Village_Manager.Models;
using Village_Manager.ViewModel;

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
            .Where(c => c.Active)
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
        DefaultImage.Ensure(products, _env);
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
        ViewBag.Categories = _context.ProductCategories.Where(c => c.Active).ToList();
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
            ViewBag.Categories = _context.ProductCategories.Where(c => c.Active).ToList();
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

        ViewBag.Categories = _context.ProductCategories.Where(c => c.Active).ToList();
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
            .Include(p => p.Farmer)
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

        if (!string.IsNullOrEmpty(model.ProductType))
            product.ProductType = model.ProductType;

        if (model.Quantity != 0)
            product.Quantity = model.Quantity;
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
        return Redirect("/product");
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

        // Thực hiện soft delete
        user.DeletedAt = DateTime.Now;
        user.IsActive = false;
        _context.SaveChanges();

        var currentUserId = HttpContext.Session.GetInt32("UserId");
        Village_Manager.Extensions.LogHelper.SaveLog(_context, currentUserId, $"Soft deleted user: {user.Username} (ID: {user.Id})");
        TempData["Success"] = $"Đã xóa mềm user: {user.Username}";
        return RedirectToAction("AllUser");
    }

    // Xem danh sách user đã xóa
    [HttpGet]
    [Route("deleted-users")]
    public async Task<IActionResult> DeletedUsers(string searchUser, int page = 1, int roleId = 0, string sortOrder = "asc")
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }

        // Lấy danh sách roles để filter
        ViewBag.Roles = await _context.Roles.ToListAsync();

        // Query users đã xóa (sử dụng IgnoreQueryFilters để bỏ qua global filter)
        var query = _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.DeletedAt != null)
            .Include(u => u.Role)
            .AsQueryable();

        // Filter theo search
        if (!string.IsNullOrEmpty(searchUser))
        {
            query = query.Where(u => u.Username.Contains(searchUser) || u.Email.Contains(searchUser));
        }

        // Filter theo role
        if (roleId > 0)
        {
            query = query.Where(u => u.RoleId == roleId);
        }

        // Sort
        switch (sortOrder.ToLower())
        {
            case "desc":
                query = query.OrderByDescending(u => u.Username);
                break;
            default:
                query = query.OrderBy(u => u.Username);
                break;
        }

        // Pagination
        int pageSize = 10;
        var totalUsers = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
        page = Math.Max(1, Math.Min(page, totalPages));

        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.Phone,
                RoleName = u.Role.Name,
                u.CreatedAt,
                u.DeletedAt
            })
            .ToListAsync();

        ViewBag.SearchUser = searchUser;
        ViewBag.RoleId = roleId;
        ViewBag.SortOrder = sortOrder;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalUsers = totalUsers;

        return View(users);
    }

    // Khôi phục user đã xóa
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("restore-user/{id}")]
    public async Task<IActionResult> RestoreUser(int id)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }

        // Tìm user đã xóa
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt != null);

        if (user == null)
        {
            TempData["Error"] = "Không tìm thấy user đã xóa!";
            return RedirectToAction("DeletedUsers");
        }

        // Khôi phục user
        user.DeletedAt = null;
        user.IsActive = true;
        await _context.SaveChangesAsync();

        var currentUserId = HttpContext.Session.GetInt32("UserId");
        Village_Manager.Extensions.LogHelper.SaveLog(_context, currentUserId, $"Restored user: {user.Username} (ID: {user.Id})");
        TempData["Success"] = $"Đã khôi phục user: {user.Username}";
        return RedirectToAction("DeletedUsers");
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
                // Kiểm tra trùng số điện thoại
                if (await _context.Users.AnyAsync(u => u.Phone == user.Phone))
                {
                    ModelState.AddModelError("Phone", "Phone number is already used by another user");
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
                // Kiểm tra xem đã có farmer nào với số điện thoại này chưa
                if (!string.IsNullOrEmpty(newUser.Phone) && await _context.Farmers.AnyAsync(f => f.Phone == newUser.Phone))
                {
                    ModelState.AddModelError("Phone", "Phone number is already used by another farmer");
                    return View(user);
                }

                // Kiểm tra phone number cho farmer (bắt buộc phải có)
                if (string.IsNullOrEmpty(newUser.Phone))
                {
                    ModelState.AddModelError("Phone", "Phone number is required for farmer");
                    return View(user);
                }

                var newFarmer = new Farmer
                {
                    UserId = newUser.Id,
                    FullName = newUser.Username, // hoặc lấy từ form nếu có trường tên đầy đủ
                    Phone = newUser.Phone, // Đã kiểm tra không null ở trên
                    Address = "" // lấy từ form nếu có, hoặc để trống
                };
                _context.Farmers.Add(newFarmer);
                await _context.SaveChangesAsync();
            }

            // Thêm vào bảng Shipper nếu role là shipper
            var shipperRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == "shipper");
            if (shipperRole != null && newUser.RoleId == shipperRole.Id)
            {
                // Kiểm tra xem đã có shipper nào với số điện thoại này chưa
                if (!string.IsNullOrEmpty(newUser.Phone) && await _context.Shippers.AnyAsync(s => s.Phone == newUser.Phone))
                {
                    ModelState.AddModelError("Phone", "Phone number is already used by another shipper");
                    return View(user);
                }

                // Kiểm tra phone number cho shipper (bắt buộc phải có)
                if (string.IsNullOrEmpty(newUser.Phone))
                {
                    ModelState.AddModelError("Phone", "Phone number is required for shipper");
                    return View(user);
                }

                var newShipper = new Shipper
                {
                    UserId = newUser.Id,
                    FullName = newUser.Username, // hoặc lấy từ form nếu có trường tên đầy đủ
                    Phone = newUser.Phone, // Đã kiểm tra không null ở trên
                    VehicleInfo = "Vehicle info will be updated later", // Tạm thời để giá trị mặc định, có thể cập nhật sau
                    Status = "approved"
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
            // Kiểm tra trùng số điện thoại (trừ user hiện tại)
            if (!string.IsNullOrEmpty(user.Phone) && await _context.Users.AnyAsync(u => u.Phone == user.Phone && u.Id != id))
            {
                _logger.LogWarning($"Phone number already exists: {user.Phone}");
                ModelState.AddModelError("Phone", "Phone number is already used by another user");
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
        
        var result = _context.Shippers
            .Include(s => s.User)
            .Select(shipper => new ShipperDisplayViewModel
            {
                Id = shipper.Id,
                FullName = shipper.FullName,
                Phone = shipper.Phone,
                VehicleInfo = shipper.VehicleInfo,
                Status = shipper.Status,
                Address = "", // Address không có trong bảng Shipper, để trống hoặc lấy từ User
                UserId = shipper.UserId ?? 0,
                Username = shipper.User != null ? shipper.User.Username : "",
                Email = shipper.User != null ? shipper.User.Email : "",
                Created = shipper.User != null ? shipper.User.CreatedAt : null
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
                    
                    // Thông báo cho user nếu họ đang online
                    _context.Notifications.Add(new Notification
                    {
                        UserId = user.Id,
                        Content = "Tài khoản của bạn đã được cập nhật thành shipper. Vui lòng logout và login lại để truy cập vào trang shipper.",
                        CreatedAt = DateTime.Now,
                        IsRead = false
                    });
                }
                
                TempData["Success"] = "Đã duyệt yêu cầu thành công. Người dùng cần logout và login lại để truy cập vào trang shipper.";
            }
            else if (action == "reject")
            {
                request.Status = "rejected";
                TempData["Success"] = "Đã từ chối yêu cầu.";
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
            .ToList();

        var viewModel = shippedOrders.Select(o => new RetailOrderViewModel
        {
            Id = o.Id,
            UserName = o.User?.Username,
            Phone = o.User?.Phone,
            Address = _context.Deliveries.FirstOrDefault(d => d.OrderId == o.Id && d.OrderType == "retail")?.CustomerAddress ?? "Không có",
            OrderDate = o.OrderDate,
            Status = o.Status
        }).ToList();

        return View(viewModel);
    }

    // Trang: Hiển thị đơn hàng chờ duyệt
    [HttpGet("addorder")]
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
            Address = _context.Deliveries.FirstOrDefault(d => d.OrderId == o.Id && d.OrderType == "retail")?.CustomerAddress ?? "Không có",
            OrderDate = o.OrderDate,
            Status = o.Status
        }).ToList();
        return View(viewModel);
    }

    // POST: Admin duyệt đơn hàng
    [HttpPost("acceptorder")]
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

        // Lấy thông tin sản phẩm từ item đầu tiên (để hiển thị ảnh chính)
        var firstItem = order.RetailOrderItems.FirstOrDefault();
        
        var viewModel = new RetailOrderItemViewModel
        {
            Id = order.Id,
            OrderId = order.Id,
            UserName = order.User?.Username,
            Phone = order.User?.Phone,
            Address = _context.Deliveries.FirstOrDefault(d => d.OrderId == order.Id && d.OrderType == "retail")?.CustomerAddress ?? "Không có",
            OrderDate = order.OrderDate,
            Status = order.Status,
            
            // Thông tin sản phẩm (từ item đầu tiên để hiển thị ảnh chính)
            ProductId = firstItem?.ProductId,
            ProductName = firstItem?.Product?.Name ?? "Không có tên sản phẩm",
            Quantity = firstItem?.Quantity,
            UnitPrice = firstItem?.UnitPrice,
            ProductImageUrl = firstItem?.Product?.ProductImages?.FirstOrDefault()?.ImageUrl ?? "/images/default-product.png",
            Product = firstItem?.Product
        };

        // Truyền toàn bộ danh sách sản phẩm để hiển thị trong view
        ViewBag.OrderItems = order.RetailOrderItems.ToList();

        return View(viewModel);
    }

    [HttpPost("deleteorder/{id}")]
    public IActionResult DeleteOrder(int id)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }
        var order = _context.RetailOrders.FirstOrDefault(o => o.Id == id);

        if (order == null) {
            Response.StatusCode = 404;
            return View("404");
        }

        order.Status = "cancelled";
        _context.SaveChanges(); 

        return Redirect("/listorder"); 
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

    // Xem danh sách yêu cầu cung cấp từ farmer
    [HttpGet]
    [Route("supplyrequests")]
    public IActionResult SupplyRequests()
    {
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }

        var requests = _context.SupplyRequests
            .Include(sr => sr.Farmer)
                .ThenInclude(f => f.User)
            .Include(sr => sr.Requester)
            .OrderByDescending(sr => sr.RequestedAt)
            .ToList();

        // Load danh sách tất cả sản phẩm để admin có thể chọn
        var availableProducts = _context.Products
            .ToList()
            .Where(p => p.ApprovalStatus != null && p.ApprovalStatus.Trim().ToLower() == "accepted")
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                category = p.Category != null ? p.Category.Name : "",
                price = p.Price,
                currentQuantity = p.Quantity,
                expirationDate = p.ExpirationDate,
                productType = p.ProductType,
                processingTime = p.ProcessingTime,
                farmerId = p.FarmerId,
                approvalStatus = p.ApprovalStatus
            })
            .ToList();
        ViewBag.ApprovedProducts = availableProducts;

        return View(requests);
    }

    // Admin phản hồi yêu cầu cung cấp từ farmer
    [HttpPost]
    [Route("respondtosupply")]
    [ValidateAntiForgeryToken]
    public IActionResult RespondToSupply(int requestId, string response, string? note)
    {
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }

        var request = _context.SupplyRequests
            .FirstOrDefault(sr => sr.Id == requestId && sr.RequesterType == "farmer");

        if (request == null)
        {
            TempData["Error"] = "Không tìm thấy yêu cầu cung cấp.";
            return RedirectToAction("SupplyRequests");
        }

        if (request.Status != "pending")
        {
            TempData["Error"] = "Yêu cầu này đã được xử lý.";
            return RedirectToAction("SupplyRequests");
        }

        request.Status = response; // "accepted" hoặc "rejected"
        request.RespondedAt = DateTime.Now;

        // Nếu admin chấp nhận, cập nhật sản phẩm
        if (response == "accepted")
        {
            var product = _context.Products
                .FirstOrDefault(p => p.Name == request.ProductName && p.FarmerId == request.FarmerId);
            
            if (product != null)
            {
                // Cộng thêm số lượng
                product.Quantity += request.Quantity;
                
                // Cập nhật giá nếu có giá mới
                if (request.Price.HasValue)
                {
                    product.Price = request.Price.Value;
                }
            }
        }

        // Gửi thông báo cho farmer
        var content = response == "accepted" 
            ? $"Admin đã chấp nhận yêu cầu cung cấp {request.Quantity} {request.ProductName}. " +
              (request.Price.HasValue ? $"Giá mới: {request.Price.Value:N0} VNĐ. " : "") +
              $"Ghi chú: {note ?? "Không có"}"
            : $"Admin đã từ chối yêu cầu cung cấp {request.ProductName}. Ghi chú: {note ?? "Không có"}";
        
        _context.Notifications.Add(new Notification
        {
            UserId = request.RequesterId,
            Content = content,
            CreatedAt = DateTime.Now,
            IsRead = false
        });

        _context.SaveChanges();

        TempData["Success"] = $"Đã {response} yêu cầu cung cấp thành công!";
        return RedirectToAction("SupplyRequests");
    }

    // Admin yêu cầu cung cấp từ farmer
    [HttpPost]
    [Route("RequestSupply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestSupply(int productId, int farmerId, int quantity, decimal? price, string? note)
    {
        // Kiểm tra quyền admin
        if (!HttpContext.Session.IsAdmin())
        {
            Response.StatusCode = 404;
            return View("404");
        }

        try
        {
            // Kiểm tra sản phẩm
            var product = await _context.Products
                .Include(p => p.Farmer)
                .FirstOrDefaultAsync(p => p.Id == productId && p.FarmerId == farmerId);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Products");
            }

            // Kiểm tra farmer
            var farmer = await _context.Farmers
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == farmerId);

            if (farmer == null || farmer.User == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin farmer.";
                return RedirectToAction("Products");
            }

            // Validation giá
            if (price.HasValue && (price.Value < 1000 || price.Value % 1000 != 0))
            {
                TempData["Error"] = "Giá phải lớn hơn 1000 và là bội số của 1000.";
                return RedirectToAction("Products");
            }

            // Tạo yêu cầu cung cấp
            var supplyRequest = new SupplyRequest
            {
                RequesterType = "admin",
                RequesterId = HttpContext.Session.GetInt32("UserId").Value,
                ReceiverId = farmer.User.Id,
                FarmerId = farmerId,
                ProductName = product.Name,
                Quantity = quantity,
                Price = price,
                Status = "pending",
                RequestedAt = DateTime.Now
            };

            _context.SupplyRequests.Add(supplyRequest);

            // Gửi thông báo cho farmer
            var notification = new Notification
            {
                UserId = farmer.User.Id,
                Content = $"Admin yêu cầu cung cấp {quantity} {product.Name}" +
                         (price.HasValue ? $" với giá {price.Value:N0} VNĐ" : "") +
                         (note != null ? $". Ghi chú: {note}" : ""),
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            // Ghi log
            int? userId = HttpContext.Session.GetInt32("UserId");
            Village_Manager.Extensions.LogHelper.SaveLog(_context, userId, 
                $"Requested supply: {quantity} {product.Name} from farmer {farmer.FullName}");

            TempData["Success"] = "Đã gửi yêu cầu cung cấp thành công!";
            return RedirectToAction("Products");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error requesting supply: {ex.Message}");
            TempData["Error"] = "Đã xảy ra lỗi khi gửi yêu cầu cung cấp.";
            return RedirectToAction("Products");
        }
    }

}