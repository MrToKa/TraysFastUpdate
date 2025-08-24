using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace TraysFastUpdate.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CanvasController : ControllerBase
{
    private readonly ILogger<CanvasController> _logger;

    public CanvasController(ILogger<CanvasController> logger)
    {
        _logger = logger;
    }

    [HttpPost("save-image")]
    public async Task<IActionResult> SaveCanvasImage([FromBody] CanvasImageRequest request)
    {
        try
        {
            _logger.LogInformation($"Received canvas image save request for tray: {request.TrayName}{(request.Rotated ? " (rotated 90° CCW)" : "")}");

            if (string.IsNullOrEmpty(request.ImageData))
            {
                return BadRequest(new { error = "No image data provided" });
            }

            if (string.IsNullOrEmpty(request.TrayName))
            {
                return BadRequest(new { error = "No tray name provided" });
            }

            // Decode base64 image data
            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(request.ImageData);
            }
            catch (FormatException)
            {
                return BadRequest(new { error = "Invalid base64 image data" });
            }

            // Ensure images directory exists
            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string imagesPath = Path.Combine(wwwrootPath, "images");
            
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
                _logger.LogInformation($"Created images directory: {imagesPath}");
            }

            // Always save as TrayName.jpg (no rotation suffix)
            string fileName = $"{request.TrayName}.jpg";
            string filePath = Path.Combine(imagesPath, fileName);
            
            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
            
            _logger.LogInformation($"Canvas image saved successfully: {filePath} ({imageBytes.Length} bytes){(request.Rotated ? " - rotated 90° CCW" : "")}");

            // Return success response
            return Ok(new 
            { 
                success = true,
                fileName = fileName,
                filePath = filePath,
                fileSize = imageBytes.Length,
                rotated = request.Rotated,
                message = $"Canvas image saved successfully{(request.Rotated ? " with 90° CCW rotation" : "")}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving canvas image for tray: {request?.TrayName}");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "Canvas API is working", timestamp = DateTime.Now });
    }
}

public class CanvasImageRequest
{
    public string ImageData { get; set; } = string.Empty;
    public string TrayName { get; set; } = string.Empty;
    public string Format { get; set; } = "jpeg";
    public bool Rotated { get; set; } = false;
}