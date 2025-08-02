 using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Village_Manager.Data;
using Village_Manager.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Village_Manager.Extensions;

namespace Village_Manager.Controllers
{
    public class RoleController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoleController> _logger;

        public RoleController(AppDbContext context, ILogger<RoleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Starting to load all users with their roles...");

                if (!HttpContext.Session.IsAdmin())
                {
                    Response.StatusCode = 404;
                    return View("404");
                }

                var roles = await _context.Roles.ToListAsync();
                return View("~/Views/AdminWarehouse/Role.cshtml", roles);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading users: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading users.";
                return View("~/Views/AdminWarehouse/Role.cshtml", new List<Role>());
            }
        }

        [HttpGet]
        public IActionResult Add()
        {
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }
            return View("~/Views/AdminWarehouse/AddRole.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Role role)
        {
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }
            if (ModelState.IsValid)
            {
                // Check for duplicate role name (case-insensitive)
                bool exists = await _context.Roles.AnyAsync(r => r.Name.ToLower() == role.Name.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Name", "Role đã tồn tại!");
                    return View("~/Views/AdminWarehouse/AddRole.cshtml", role);
                }
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View("~/Views/AdminWarehouse/AddRole.cshtml", role);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Role role)
        {
            if (!HttpContext.Session.IsAdmin())
            {
                Response.StatusCode = 404;
                return View("404");
            }
            if (ModelState.IsValid)
            {
                // Check for duplicate role name (case-insensitive), excluding current role
                bool exists = await _context.Roles.AnyAsync(r => r.Name.ToLower() == role.Name.ToLower() && r.Id != role.Id);
                if (exists)
                {
                    ModelState.AddModelError("Name", "Role đã tồn tại!");
                    return View("~/Views/AdminWarehouse/EditRole.cshtml", role);
                }
                _context.Roles.Update(role);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View("~/Views/AdminWarehouse/EditRole.cshtml", role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
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
            // Check if any user is using this role
            bool isRoleInUse = await _context.Users.AnyAsync(u => u.RoleId == id);
            if (isRoleInUse)
            {
                TempData["ErrorMessage"] = "Không thể xóa role này vì đã có tài khoản sử dụng!";
                return RedirectToAction("Index");
            }
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
