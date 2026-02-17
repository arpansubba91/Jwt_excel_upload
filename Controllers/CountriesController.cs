using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mySystem.Services;
using System.Security.Claims;

namespace mySystem.Controllers
{
    [ApiController]
    [Route("api/countries")]
    [Authorize]
    public class CountriesController : ControllerBase
    {
        private readonly ICountryService _countryService;
        private readonly ILogger<CountriesController> _logger;

        public CountriesController(ICountryService countryService, ILogger<CountriesController> logger)
        {
            _countryService = countryService;
            _logger = logger;
        }

        [HttpPost("upload")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> Upload([FromForm] IFormFile file)
{
    try
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "No file selected" });

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(new { success = false, message = "Invalid user" });
        }

        var (success, message, count) = await _countryService.ProcessExcelUploadAsync(file, userId);

        if (!success)
        {
            return BadRequest(new
            {
                success = false,
                message = message
            });
        }

        return Ok(new
        {
            success = true,
            message = message,
            countriesUploaded = count,
            uploadedAt = DateTime.UtcNow,
            uploadedByUserId = userId
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = "Error uploading file: " + ex.Message
        });
    }
}
}
}