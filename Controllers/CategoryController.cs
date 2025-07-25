using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Village_Manager.Data;
using Village_Manager.Models;
using Village_Manager.ViewModel;
namespace Village_Manager.Controllers
{
    [Route("category")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        public CategoryController(AppDbContext context)
        {
            _context = context;
        }
        // hiển thị trang list category
        // cần update hỉnh ảnh khi hiển thị ở trang darkboard và home

        [HttpGet]
        [Route("listcate")]
        public IActionResult Listcate()
        {
            // Bước 1: Lấy dữ liệu về bộ nhớ
            var categoriesRaw = _context.ProductCategories
                .Include(c => c.Products)
                .ThenInclude(p => p.Farmer)
                .ToList();

            // Bước 2: Xử lý trên bộ nhớ
            var categories = categoriesRaw
                .Select(c => new CategoryStatsViewModel
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    Products = c.Products.ToList(),
                    Farmers = c.Products
                        .Where(p => p.Farmer != null)
                        .Select(p => p.Farmer!)
                        .GroupBy(f => f.Id)
                        .Select(g => g.First())
                        .ToList()
                })
                .ToList();

            return View("Views/AdminWarehouse/Listcate.cshtml", categories);
        }
        [HttpGet]
[Route("category-details")]
public IActionResult CategoryDetails()
{
    var categoryDetails = _context.ProductCategories
        .Select(c => new CategoryStatsViewModel
        {
            CategoryId = c.Id,
            CategoryName = c.Name,
            Products = c.Products.ToList(),
            Farmers = c.Products
       .Where(p => p.Farmer != null)
        .Select(p => p.Farmer!)
         .Distinct()
       .ToList()
        })
        .ToList();

    return View("Views/AdminWarehouse/CategoryDetails.cshtml", categoryDetails);
}
        // Hien thi danh sach category ra trang home
        [HttpGet("")]
        public IActionResult Index()
        {
            var categories = _context.ProductCategories.ToList();
            return View(categories);
        }
        // add Category 
        // NOTE : cần thêm kiểm tra tệp ảnh, đảm bảo chỉ cho tải tệp có đuôi ảnh hợp lệ( sửa sau), 
        // giới hạn kích thước tệp ảnh , còn try catch cho trường hợp lỗi ảnh nữa!!
        [HttpGet("addCategory")]
        public IActionResult addCategory()
        {
            return View("Views/AdminWarehouse/addCategory.cshtml");
        }
        [HttpPost("addCategory")]

        public IActionResult addCategory(ProductCategory category, IFormFile ImgFile)
        {
            if (String.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError("Name", "Category name is required");
                return View(category);
            }
            if (_context.ProductCategories.Any(c => c.Name == category.Name))
            {
                ModelState.AddModelError("Name", "Category name already exit!!");
                return View(category);
            }
            if (ImgFile == null || ImgFile.Length == 0)
            {
                ModelState.AddModelError("Image", "Please upload a category image");
                return View(category);
            }
            // kiem tra tep anh user nhap
            if (ImgFile != null && ImgFile.Length > 0)
            {
                // path de lưu ảnh
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "upload");
                // nếu path lưu ảnh chưa tồn tại
                if (!Directory.Exists(uploadPath))
                {
                    // tự động tạo path 
                    Directory.CreateDirectory(uploadPath);
                }
                // tạo 1 tên cho tệp , để không bị trùng
                var filename = Guid.NewGuid().ToString() + Path.GetExtension(ImgFile.FileName);
                var filePath = Path.Combine(uploadPath, filename);
                // lưu path 
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImgFile.CopyTo(stream);
                }
                // lưu đường dẫn 
                category.ImageUrl = "/upload/" + filename;

            }
            // lưu ảnh vào database 
            // nếu các model state đều hợp lệ lưu vào database sau đó chuyển và trang hiển thị danh sách listcate
            if (ModelState.IsValid)
            {
                _context.ProductCategories.Add(category);
                _context.SaveChanges();
                return RedirectToAction("Listcate");
            }

            return View();
        }

        [HttpGet("editCategory/{id}")]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View("Views/Adminwarehouse/EditCategory.cshtml", category);
        }
        [HttpPost("editCategory/{Id}")]
        [ActionName("EditCategory")]
        public async Task<IActionResult> EditCategory( ProductCategory category, IFormFile imgFile)
        {
            var exitCate = await _context.ProductCategories.FirstOrDefaultAsync(c => c.Id == category.Id);
            if (exitCate == null)
            {
                return NotFound("Không tìm thấy danh mục");
            }

            // validate name
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError("Name", "Tên không được để trống");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(category.Name, @"^[\p{L}0-9\s\-&]+$"))
            {
                ModelState.AddModelError("Name", "Tên không hợp lệ (chỉ cho phép chữ, số, khoảng trắng, -, &))");
            }

            // validate image file
            if (imgFile != null && imgFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imgFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageUrl", "File không hợp lệ (vui lòng chọn file có định dạng .jpg, .jpeg, .png, .gif)");
                }
            }

            // nếu có lỗi thì trả lại view với thông báo
            if (!ModelState.IsValid)
            {
                category.ImageUrl = exitCate.ImageUrl;
              
                return View("Views/AdminWarehouse/EditCategory.cshtml", category);
            }

            // nếu có ảnh mới thì xử lý
            if (imgFile != null && imgFile.Length > 0)
            {
                // xoá ảnh cũ nếu tồn tại
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", exitCate.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                // lưu ảnh mới
                var filename = Guid.NewGuid().ToString() + Path.GetExtension(imgFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/categories", filename);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imgFile.CopyToAsync(stream);
                }

                category.ImageUrl = "/images/categories/" + filename;
            }
            else
            {
                
                category.ImageUrl = exitCate.ImageUrl;
                
            }

            // cập nhật dữ liệu
            exitCate.Name = category.Name;
            exitCate.ImageUrl = category.ImageUrl;

            _context.ProductCategories.Update(exitCate);
            await _context.SaveChangesAsync();
            return RedirectToAction("Listcate");
        }


        [HttpGet("DeleteCategory/{id}")]
        public IActionResult DeleteCategory(int id)
        {

            var category = _context.ProductCategories.FirstOrDefault(c => c.Id == id);
            if (category == null) return NotFound();
            _context.ProductCategories.Remove(category);
            _context.SaveChanges();
            return RedirectToAction("Listcate");


        }

    }


}
