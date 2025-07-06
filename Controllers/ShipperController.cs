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
            var shipperId = HttpContext.Session.GetInt32("ShipperId");
            Console.WriteLine($"[DEBUG] Session ShipperId: {shipperId}");
            if (!shipperId.HasValue)
                return RedirectToAction("Login", "Home");

            // Tổng đơn giao
            var totalDeliveries = _context.Deliveries.Count(d => d.ShipperId == shipperId);

            // Đơn chờ nhận
            var pendingDeliveries = _context.Deliveries.Count(d => d.ShipperId == shipperId && d.Status == "assigned");

            // Đơn hoàn thành
            var completedDeliveries = _context.Deliveries.Count(d => d.ShipperId == shipperId && d.Status == "delivered");

            // Đơn hàng gần nhất
            var recentDeliveries = _context.Deliveries
                .Where(d => d.ShipperId == shipperId)
                .OrderByDescending(d => d.StartTime)
                .Take(5)
                .ToList();

            ViewBag.TotalDeliveries = totalDeliveries;
            ViewBag.PendingDeliveries = pendingDeliveries;
            ViewBag.CompletedDeliveries = completedDeliveries;
            ViewBag.RecentDeliveries = recentDeliveries;

            return View();
        }
        public IActionResult OrdersShipper()
        {
            return View();
        }
        public IActionResult DeliveriesShipper()
        {
            var shipperId = HttpContext.Session.GetInt32("ShipperId");
            if (!shipperId.HasValue)
                return RedirectToAction("Login", "Home");

            var deliveries = _context.Deliveries
                .Where(d => d.ShipperId == shipperId && (d.Status == "assigned" || d.Status == "in_transit"))
                .OrderBy(d => d.StartTime)
                .ToList();

            return View(deliveries);
        }
        public IActionResult ProfileShipper()
        {
            return View();
        }
        public IActionResult HistoryShipper()
        {
            return View();
        }
        public IActionResult NotificationsShipper()
        {
            return View();
        }
        public IActionResult LogsShipper()
        {
            return View();
        }

        [HttpPost]
        public IActionResult StartDelivery(int id)
        {
            var delivery = _context.Deliveries.FirstOrDefault(d => d.Id == id);
            if (delivery != null && delivery.Status == "assigned")
            {
                delivery.Status = "in_transit";
                _context.SaveChanges();
            }
            return RedirectToAction("DeliveriesShipper");
        }

        [HttpPost]
        public IActionResult ConfirmDelivery(int id)
        {
            var delivery = _context.Deliveries.FirstOrDefault(d => d.Id == id);
            if (delivery != null && delivery.Status == "in_transit")
            {
                delivery.Status = "delivered";
                delivery.EndTime = DateTime.Now;
                _context.SaveChanges();
            }
            return RedirectToAction("DeliveriesShipper");
        }

        [HttpPost]
        public IActionResult ReportIssue(int id, string reason)
        {
            var delivery = _context.Deliveries.FirstOrDefault(d => d.Id == id);
            if (delivery != null)
            {
                delivery.Status = "failed";
                _context.SaveChanges();

                // Lưu lý do vào bảng DeliveryIssue
                var shipperId = HttpContext.Session.GetInt32("ShipperId");
                _context.DeliveryIssues.Add(new DeliveryIssue
                {
                    DeliveryId = id,
                    ShipperId = shipperId ?? 0,
                    IssueType = "other",
                    Description = reason,
                    ReportedAt = DateTime.Now
                });
                _context.SaveChanges();
            }
            return RedirectToAction("DeliveriesShipper");
        }
    }
}
