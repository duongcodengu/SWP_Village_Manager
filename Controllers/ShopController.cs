using Microsoft.AspNetCore.Mvc;

namespace Village_Manager.Controllers
{
    public class ShopController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
