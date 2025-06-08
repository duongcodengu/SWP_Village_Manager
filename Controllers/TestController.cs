using Microsoft.AspNetCore.Mvc;
using Village_Manager.Data;

namespace Village_Manager.Controllers
{
    public class TestController : Controller
    {
        private readonly DBContext _context; 

        public TestController(DBContext context)
        {
            _context = context;
        }

        public IActionResult CheckDb()
        {
            try
            {
                // Thử truy vấn số lượng user trong bảng Users
                int userCount = _context.Users.Count();
                return Content($"Đã kết nối database! Số lượng user: {userCount}");
            }
            catch (Exception ex)
            {
                return Content("Kết nối thất bại: " + ex.Message);
            }
        }
    }
}
