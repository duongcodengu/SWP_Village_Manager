using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Village_Manager.Controllers.api
{
    [Route("api")]
    [ApiController]
    public class LocationApiController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public LocationApiController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var url = "https://provinces.open-api.vn/api/?depth=3";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Không thể lấy danh sách tỉnh.");

            var content = await response.Content.ReadAsStringAsync();
            return new ContentResult
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = 200
            };
        }
    }
}
