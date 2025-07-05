using Microsoft.AspNetCore.Mvc;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Controllers
{
    public class ShipperController : Controller
    {
        private readonly AppDbContext _context;
        public ShipperController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("dashboardshipper")]
        public IActionResult DashboardShipper()
        {
            return View();
        }
        //public IActionResult OrdersShipper()
        //{
        //    return View();
        //}
        //public IActionResult DeliveriesShipper()
        //{
        //    return View();
        //}
        //public IActionResult ProfileShipper()
        //{
        //    return View();
        //}
        //public IActionResult HistoryShipper()
        //{
        //    return View();
        //}
        //public IActionResult NotificationsShipper()
        //{
        //    return View();
        //}
        //public IActionResult LogsShipper()
        //{
        //    return View();
        //}
    }
}
