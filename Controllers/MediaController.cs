using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using Village_Manager.Extensions;
using Village_Manager.Models;
namespace Village_Manager.Controllers
{
    public class InsertImagesRequest
    {
        public List<int> ? ImageIds { get; set; }
        public string ? Section { get; set; }
    }

    public class MediaController : Controller
    {
        private readonly AppDbContext _context;
        public MediaController(AppDbContext context)
        {
            _context = context;
        }
        // 1. Truy xuất trang Media + truyền dữ liệu ảnh hiện có
        public IActionResult Index()
        {
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }
            var images = _context.HomepageImages
                .Include(h => h.ProductImage)
                .ThenInclude(p => p.Product)
                .ToList();

            return View("~/Views/AdminWarehouse/Media.cshtml", images);
        }

        // 2. Lấy danh sách category (để hiện dropdown)
        [HttpGet]
        public IActionResult GetCategories()
        {
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }
            var categories = _context.ProductCategories
                .Select(c => new
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToList();

            return Json(categories);
        }

        // Upload ảnh mới
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file, int productId)
        {
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Không có file được chọn" });

            try
            {
                // Tạo tên file unique
                var fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Lưu vào database
                var productImage = new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = "/uploads/" + fileName,
                    Description = file.FileName,
                    UploadedAt = DateTime.Now
                };

                _context.ProductImages.Add(productImage);
                await _context.SaveChangesAsync();

                return Json(new { success = true, imageId = productImage.Id, imageUrl = productImage.ImageUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi upload: " + ex.Message });
            }
        }

        // Sắp xếp thứ tự ảnh
        [HttpPost]
        public IActionResult UpdateImageOrder([FromBody] List<ImageOrderRequest> requests)
        {
            try
            {
                foreach (var request in requests)
                {
                    var image = _context.HomepageImages.Find(request.Id);
                    if (image != null)
                    {
                        image.DisplayOrder = request.Order;
                    }
                }
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API luu anh moi vao bang homepageImage
        [HttpPost]
        public IActionResult InsertImageToSection([FromBody] HomepageImage input)
        {
            // Validate
            if (input == null || string.IsNullOrEmpty(input.Section) || input.ProductImageId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid input." });
            }

            // Số lượng tối đa ảnh theo từng section
           
            var maxImagesPerSection = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "banner", 3 },
        { "topsave", 10 },
        { "FoodCupboard", 5 },
        { "bestseller", 12 }
    };

            // Kiểm tra nếu section có cấu hình giới hạn
            if (maxImagesPerSection.ContainsKey(input.Section))
            {
                int currentCount = _context.HomepageImages
                    .Count(h => h.Section == input.Section && h.IsActive);

                if (currentCount >= maxImagesPerSection[input.Section])
                {
                    return Json(new { success = false, message = $"Section '{input.Section}' chỉ cho phép tối đa {maxImagesPerSection[input.Section]} ảnh." });
                }
            }

            // Thêm mới
            var maxOrder = _context.HomepageImages
                .Where(h => h.Section == input.Section)
                .Select(h => (int?)h.DisplayOrder)
                .Max() ?? 0;
            var newImage = new HomepageImage
            {
                Section = input.Section,
                ProductImageId = input.ProductImageId,
                IsActive = true,
                DisplayOrder = maxOrder + 1,
                Position = null // ← NULL cho các section khác
            };

            _context.HomepageImages.Add(newImage);
            _context.SaveChanges();

            return Json(new { success = true });
        }
        // 3. Lấy ảnh theo category ID
        [HttpGet]
        public IActionResult GetImagesByCategory(int categoryId)
        {
            var images = _context.ProductImages
                .Include(pi => pi.Product)
                .Where(pi => pi.Product.CategoryId == categoryId)
                .Select(pi => new
                {
                    pi.Id,
                    pi.ImageUrl,
                    ProductName = pi.Product.Name
                })
                .ToList();

            return Json(images);
        }

        // 4. Lưu ảnh được chọn từ modal vào HomepageImage
        [HttpPost]
        public IActionResult InsertSelectedImages([FromBody] InsertImagesRequest request)
        {
            if (request.ImageIds == null || !request.ImageIds.Any() || string.IsNullOrWhiteSpace(request.Section))
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

            // SỬA - Kiểm tra ảnh đã tồn tại (chỉ cho product images, không phải banners)
            var existingImageIds = _context.HomepageImages
                .Where(h => h.Section == request.Section &&
                            h.ProductImageId.HasValue && // Chỉ kiểm tra khi có ProductImageId
                            request.ImageIds.Contains(h.ProductImageId.Value) &&
                            h.IsActive)
                .Select(h => h.ProductImageId.Value)
                .ToList();

            var newImageIds = request.ImageIds.Except(existingImageIds).ToList();

            if (!newImageIds.Any())
            {
                return Json(new { success = false, message = "Tất cả ảnh đã tồn tại trong section này!" });
            }

            var addedImages = new List<object>();
            
          
              var maxOrder = _context.HomepageImages
            .Where(h => h.Section == request.Section)
            .Select(h => (int?)h.DisplayOrder)
               .DefaultIfEmpty()
                  .Max() ?? 0;

            foreach (var imageId in newImageIds) // Chỉ thêm ảnh mới
            {
                var productImage = _context.ProductImages
                    .Include(pi => pi.Product)
                    .FirstOrDefault(pi => pi.Id == imageId);

                if (productImage != null)
                {
                    maxOrder++;
                    var homepageImage = new HomepageImage
                    {
                        ProductImageId = productImage.Id, // Không null cho product images
                        Section = request.Section,
                        IsActive = true,
                        DisplayOrder = maxOrder
                    };
                    _context.HomepageImages.Add(homepageImage);
                    addedImages.Add(new
                    {
                        id = homepageImage.ProductImageId,
                        imageUrl = productImage.ImageUrl,
                        productName = productImage.Product.Name
                    });
                }
            }

            _context.SaveChanges();

            var message = existingImageIds.Any()
                ? $"Đã thêm {newImageIds.Count} ảnh mới. {existingImageIds.Count} ảnh đã tồn tại."
                : $"Đã thêm {newImageIds.Count} ảnh thành công!";

            return Json(new { success = true, images = addedImages, message = message });
        }

        [HttpPost]
        public IActionResult ReplaceImage(int homepageImageId, int newProductImageId)
        {
            var homepageImage = _context.HomepageImages.Find(homepageImageId);
            if (homepageImage != null)
            {
                homepageImage.ProductImageId = newProductImageId;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        // Trang chọn ảnh mới thay thế (hoặc thêm mới)
        public IActionResult SelectImage(int homepageImageId)
        {
            var products = _context.Products.Include(p => p.ProductImages).ToList();
            ViewBag.HomepageImageId = homepageImageId;
            return View(products);
        }
        // Thêm ảnh mới vào section
        [HttpPost]
        public IActionResult AddToSection(int productImageId, string section)
        {
            var exists = _context.HomepageImages.Any(h => h.ProductImageId == productImageId && h.Section == section);
            if (!exists)
            {
                var maxOrder = _context.HomepageImages
                    .Where(h => h.Section == section)
                    .Select(h => (int?)h.DisplayOrder)
                    .Max() ?? 0;

                var homepageImage = new HomepageImage
                {
                    ProductImageId = productImageId,
                    Section = section,
                    DisplayOrder = maxOrder + 1,
                    IsActive = true,
                    Position = null // ← NULL cho các section khác
                };
                _context.HomepageImages.Add(homepageImage);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
        [HttpDelete]
        public IActionResult DeleteImage(int id)
        {
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }
            var homepageImage = _context.HomepageImages.Find(id);
            if (homepageImage != null)
            {
                _context.HomepageImages.Remove(homepageImage);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        [HttpPost]
        public async Task<IActionResult> UploadBanner(IFormFile file, string section, string position)
        {
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Không có file được chọn" });
            if (string.IsNullOrEmpty(position))
                return Json(new { success = false, message = "Vui lòng chọn vị trí banner!" });

            try
            {
                var fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Kiểm tra đã có banner ở vị trí này chưa
                var existing = _context.HomepageImages.FirstOrDefault(h => h.Section == section && h.Position == position && h.IsActive);
                if (existing != null)
                {
                    return Json(new { success = false, message = "Đã có banner ở vị trí này!" });
                }

                var homepageImage = new HomepageImage
                {
                    ProductImageId = null,
                    Section = section,
                    Banner = "/uploads/" + fileName,
                    Position = position,
                    IsActive = true,
                    DisplayOrder = 0
                };

                _context.HomepageImages.Add(homepageImage);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    imageId = homepageImage.Id,
                    imageUrl = homepageImage.Banner,
                    message = "Upload banner thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi upload: " + ex.Message });
            }
        }


        public class ImageOrderRequest
        {
            public int Id { get; set; }
            public int Order { get; set; }
        }
    }
}