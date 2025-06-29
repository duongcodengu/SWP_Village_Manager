    using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

[Route("[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public TestController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("hihi")]
    public IActionResult CheckDb()
    {
        string connString = _configuration.GetConnectionString("DefaultConnection");

        try
        {
            using (var connection = new SqlConnection(connString))
            {
                connection.Open();
                return Ok("✅ Kết nối thành công với SQL Server!");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"❌ Lỗi khi kết nối SQL: {ex.Message}");
        }
    }
}
