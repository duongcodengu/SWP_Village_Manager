using Microsoft.AspNetCore.Mvc;

namespace Village_Manager.Controllers
{
    public class AdminWarehouseController : Controller
    {
        // kiem tra quyen truy cap
        [HttpGet]
        [Route("adminwarehouse")]
        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (string.IsNullOrEmpty(username) || roleId != 1)
            {
                Response.StatusCode = 404;
                return View("404");
            }

            return View();
        }
    }
}
