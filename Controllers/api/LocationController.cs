using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Village_Manager.Data;
using Village_Manager.Models;
using Village_Manager.ViewModel;

namespace Village_Manager.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        public LocationController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
            [HttpPost("save")]
            public async Task<IActionResult> SaveLocation([FromBody] LocationRequest request)
            {
                if (request.UserId <= 0)
                    return BadRequest("UserId không hợp lệ.");

                // Kiểm tra xem đã có bản ghi chưa
                var existing = await _context.UserLocations
                    .FirstOrDefaultAsync(l => l.UserId == request.UserId);

                // Gọi API reverse geocode
                string apiKey = _config["LocationIQ:ApiKey"];
                string reverseUrl = $"https://us1.locationiq.com/v1/reverse.php?key={apiKey}&lat={request.Latitude}&lon={request.Longitude}&format=json";

                string address = "";
                using (var http = new HttpClient())
                {
                    try
                    {
                        var response = await http.GetStringAsync(reverseUrl);
                        using var doc = JsonDocument.Parse(response);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("display_name", out var displayName))
                        {
                            address = displayName.GetString();
                        }
                    }
                    catch
                    {
                        address = "Không xác định";
                    }
                }

                if (existing != null)
                {
                    // Cập nhật bản ghi cũ
                    existing.Label = request.Label;
                    existing.Address = address;
                    existing.Latitude = request.Latitude;
                    existing.Longitude = request.Longitude;
                    existing.CreatedAt = DateTime.Now;
                }
                else
                {
                    // Tạo bản ghi mới nếu chưa có
                    var location = new UserLocation
                    {
                        UserId = request.UserId,
                        Label = request.Label,
                        Address = address,
                        Latitude = request.Latitude,
                        Longitude = request.Longitude
                    };

                    _context.UserLocations.Add(location);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Location saved or updated successfully." });
            }
        

        [HttpPost("accept")]
        public async Task<IActionResult> AcceptGeolocation([FromBody] AcceptGeoRequest req)
        {
            var user = await _context.Users.FindAsync(req.UserId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.HasAcceptedGeolocation = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Geolocation accepted" });
        }
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] UserLocationViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Kiểm tra lat, l có hợp lệ không
            if (model.Latitude < -90 || model.Latitude > 90 || model.Longitude < -180 || model.Longitude > 180)
            {
                return BadRequest(new { message = "Tọa độ không hợp lệ." });
            }

            // Kiểm tra nếu tọa độ là 0,0 (có thể là lỗi hoặc vị trí không hợp lệ)
            if (model.Latitude == 0 && model.Longitude == 0)
            {
                return BadRequest(new { message = "Không thể xác định vị trí hợp lệ từ địa chỉ." });
            }

            // Kiểm tra trùng địa chỉ cho user:
            bool exists = _context.UserLocations.Any(u =>
                u.UserId == model.UserId &&
                Math.Abs(u.Latitude - model.Latitude) < 0.0001 &&
                Math.Abs(u.Longitude - model.Longitude) < 0.0001);

            if (exists)
            {
                return BadRequest(new { message = "Vị trí này đã tồn tại trong hệ thống." });
            }

            // Tạo mới nếu hợp lệ
            var location = new UserLocation
            {
                UserId = model.UserId,
                Label = model.Label,
                Address = model.Address,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserLocations.Add(location);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã thêm thành công." });
        }


        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UserLocationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Trả về lỗi rõ ràng
            }
            var location = await _context.UserLocations.FindAsync(model.Id);
            if (location == null)
                return NotFound(new { message = "Không tìm thấy địa chỉ." });

            location.Label = model.Label;
            location.Latitude = model.Latitude;
            location.Longitude = model.Longitude;
            location.CreatedAt = DateTime.UtcNow;

            // Reverse geocode: lấy lại địa chỉ từ lat/lng
            string apiKey = _config["LocationIQ:ApiKey"];
            string reverseUrl = $"https://us1.locationiq.com/v1/reverse.php?key={apiKey}&lat={model.Latitude}&lon={model.Longitude}&format=json";

            using var http = new HttpClient();
            try
            {
                var response = await http.GetStringAsync(reverseUrl);
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                if (root.TryGetProperty("display_name", out var displayName))
                {
                    location.Address = displayName.GetString();
                }
            }
            catch
            {
                location.Address = model.Address; // fallback nếu API fail
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật thành công." });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var location = await _context.UserLocations.FindAsync(id);
            if (location == null)
                return NotFound(new { message = "Không tìm thấy địa chỉ." });

            _context.UserLocations.Remove(location);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xoá thành công." });
        }

    }








    public class AcceptGeoRequest
    {
        public int UserId { get; set; }
    }
    public class LocationRequest
    {
        public int UserId { get; set; }
        public string Label { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
