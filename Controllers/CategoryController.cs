using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Village_Manager.Data;
using Village_Manager.Models;

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
            var categories = _context.ProductCategories
       .Select(c => new
       {
           c.Id,
           c.Name,
           c.ImageUrl
       })
       .ToList();
            ViewBag.Categories = categories;
            return View("Views/AdminWarehouse/Listcate.cshtml");

        }
        // Hien thi danh sach category ra trang home
        [HttpGet("")]
        public IActionResult Index()
        {
            var categories = _context.ProductCategory.ToList();
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
  
        public IActionResult addCategory( ProductCategory category , IFormFile ImgFile)
        {
            if (String.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError("Name", "Category name is required");
                return View(category);
            }
            if(_context.ProductCategories.Any(c => c.Name == category.Name))
            {
                ModelState.AddModelError("Name", "Category name already exit!!");
                return View(category);
            }
            if(ImgFile == null || ImgFile.Length == 0)
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

        // edit category
        [HttpGet("editCategory/{id}")]
        public IActionResult EditCategory( int id)
        {
            var Category = _context.ProductCategory.FirstOrDefault(c => c.Id == id);    
            if (Category == null) return NotFound();
            
            return View("/Views/AdminWarehouse/EditCategory.cshtml" , Category);
        }
        [HttpPost("editCategory/{id}")]
        [ActionName("EditCategory")]
        public IActionResult EditCategory(int id , ProductCategory category , IFormFile imgFile)
        {
            if (id != category.Id) return BadRequest();
            var exitCate = _context.ProductCategory.FirstOrDefault(c => c.Id == id);
            if (exitCate == null) return NotFound();
            if (ModelState.IsValid)
            {
                exitCate.Name = category.Name;
                if(imgFile != null && imgFile.Length > 0)
                {
                    var filename = Guid.NewGuid() + Path.GetExtension(imgFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/categories", filename);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        imgFile.CopyTo(stream);
                    }
                    exitCate.ImageUrl = "/images/categories/" + filename;

                }
                _context.ProductCategories.Update(exitCate);
                _context.SaveChanges();
                return RedirectToAction("Listcate");
            }
            category.ImageUrl = exitCate.ImageUrl;
            return View("Views/AdminWarehouse/Listcate.cshtml", category);
        }
       [HttpGet("DeleteCategory/{id}")]
       public IActionResult DeleteCategory( int id)
        { 

            var category = _context.ProductCategories.FirstOrDefault(c => c.Id == id);
            if (category == null) return NotFound();
            _context.ProductCategory.Remove(category);
            _context.SaveChanges();
            return RedirectToAction("Listcate");


        }

    }
 

}
