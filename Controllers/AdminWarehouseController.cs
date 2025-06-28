using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Controllers
{
    public class AdminWarehouseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public AdminWarehouseController(AppDbContext context, IConfiguration configuration, IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
        }

        // kiểm tra quyền truy cập
        [HttpGet]
        [Route("adminwarehouse")]
        public IActionResult Dashboard()
        {
            var username = HttpContext.Session.GetString("Username");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (string.IsNullOrEmpty(username) || roleId != 1)
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
                    ImageUrl = c.ImageUrl
                }).ToList<dynamic>();
            ViewBag.Categories = categories;

            // Tổng doanh thu confirmed
            decimal currentYear = DateTime.Now.Year;

            // Bán lẻ (Retail)
            var retailRevenue = _context.RetailOrders
                .Where(ro => ro.Status == "confirmed"
                    && ro.ConfirmedAt.HasValue
                    && ro.ConfirmedAt.Value.Year == currentYear)
                .Join(_context.RetailOrderItems,
                      ro => ro.Id,
                      ri => ri.OrderId,
                      (ro, ri) => ri.Quantity * ri.UnitPrice)
                .Sum();

            // Bán buôn (Wholesale)
            var wholesaleRevenue = _context.WholesaleOrders
                .Where(wo => wo.Status == "confirmed"
                    && wo.ConfirmedAt.HasValue
                    && wo.ConfirmedAt.Value.Year == currentYear)
                .Join(_context.WholesaleOrderItems,
                      wo => wo.Id,
                      wi => wi.OrderId,
                      (wo, wi) => wi.Quantity * wi.UnitPrice)
                .Sum();

            decimal totalRevenue = (retailRevenue ?? 0) + (wholesaleRevenue ?? 0);
            ViewBag.TotalRevenue = totalRevenue;

            return View();
        }
        //ListProduct
        [HttpGet]
        [Route("products")]
        public IActionResult Products()
        {
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
            var product = _context.Products
                       .Include(p => p.ProductImages)
                       .Include(p => p.Category)
                       .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }
        //Add Product
        [HttpGet]
        [Route("addproduct")]
        public IActionResult AddProduct()
        {
            return View();
        }

        [HttpPost]
        [Route("addproduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(IFormCollection form, List<IFormFile> images)
        {
            try
            {
                // Create new Product instance
                var product = new Product
                {
                    Name = form["name"],
                    ProductType = form["product_type"],
                    CategoryId = int.Parse(form["category_id"]),
                    Quantity = int.Parse(form["quantity"]),
                    Price = decimal.Parse(form["price"]),
                    ExpirationDate = string.IsNullOrWhiteSpace(form["expiration_date"]) ? null : DateTime.Parse(form["expiration_date"]),
                    ProcessingTime = string.IsNullOrWhiteSpace(form["processing_time"]) ? null : DateTime.Parse(form["processing_time"]),
                    FarmerId = int.TryParse(form["farmer_id"], out int farmerId) ? farmerId : (int?)null
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync(); 

                if (images != null && images.Count > 0)
                {
                    foreach (var file in images)
                    {
                        if (file.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
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
                                ImageUrl = "/uploads/" + uniqueFileName,
                                Description = form["image_description"],
                                UploadedAt = DateTime.Now
                            };

                            _context.ProductImages.Add(productImage);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                return Redirect("/products");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error adding product: " + ex.Message);
                return View();
            }
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            return View(product); 
        }

        // DeleteProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            var images = _context.ProductImages.Where(p => p.ProductId == id).ToList();

            foreach (var image in images)
            {
                var filePath = Path.Combine(_env.WebRootPath, "uploads", image.ImageUrl);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.ProductImages.RemoveRange(images);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Redirect("/products");
        }

        //Update Product
        [HttpGet]
        [Route("updateproduct")]
        public async Task<IActionResult> UpdateProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }


        [HttpPost]
        [Route("updateproduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(Product model)
        {
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

            if (model.ExpirationDate != default)
                product.ExpirationDate = model.ExpirationDate;

            if (!string.IsNullOrEmpty(model.ProductType))
                product.ProductType = model.ProductType;

            if (model.Quantity != 0)
                product.Quantity = model.Quantity;

            if (model.ProcessingTime != default)
                product.ProcessingTime = model.ProcessingTime;

            if (model.FarmerId != 0)
                product.FarmerId = model.FarmerId;
            
            // Nếu có ảnh mới, thì cập nhật
            if (model.ImageUpdate != null && model.ImageUpdate.Any())
            {
                _context.ProductImages.RemoveRange(product.ProductImages);

                foreach (var file in model.ImageUpdate)
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
            return Redirect("/products");
        }
        [HttpGet]
        [Route("searchProduct")]
        public async Task<IActionResult> SearchProduct(string search)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                productsQuery = productsQuery.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    (p.Category != null && p.Category.Name.ToLower().Contains(search)) ||
                    p.ProductType.ToLower().Contains(search)
                );
            }

            var products = await productsQuery.ToListAsync();
            return View("Products",products);
        }

        [HttpGet]
        [Route("alluser")]
        public IActionResult AllUser() => View();
    }
}
