using TraysFastUpdate.Common.Constants;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Export;
using TraysFastUpdate.Extensions;
using TraysFastUpdate.Services.Contracts;
using SkiaSharp;

namespace TraysFastUpdate.Services.Export;

public class FileExportService : IFileExportService
{
    private readonly IWordExportService _wordExportService;
    private readonly IExcelExportService _excelExportService;
    private readonly ServerSideCanvasService _serverSideCanvasService;
    private readonly ICableService _cableService;

    public FileExportService(IWordExportService wordExportService, IExcelExportService excelExportService, 
        ServerSideCanvasService serverSideCanvasService, ICableService cableService)
    {
        _wordExportService = wordExportService;
        _excelExportService = excelExportService;
        _serverSideCanvasService = serverSideCanvasService;
        _cableService = cableService;
    }

    public async Task ExportTrayDocumentationAsync(Tray tray)
    {
        try
        {
            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            
            // Ensure the files directory exists
            string filesPath = Path.Combine(wwwrootPath, "files");
            if (!Directory.Exists(filesPath))
            {
                Directory.CreateDirectory(filesPath);
                Console.WriteLine($"Created files directory: {filesPath}");
            }
            
            // Ensure the images directory exists
            string imagesPath = Path.Combine(wwwrootPath, "images");
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
                Console.WriteLine($"Created images directory: {imagesPath}");
            }
            
            // Select the appropriate template based on tray purpose
            string templateFileName = GetReportTemplate(tray.Purpose);
            string templatePath = Path.Combine(wwwrootPath, templateFileName);
            
            // Check if template exists
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file not found: {templatePath}. Please ensure the template file exists in the wwwroot directory.");
            }
            
            Console.WriteLine($"Using template: {templatePath}");
            Console.WriteLine($"Exporting documentation for tray: {tray.Name}");
            
            await _wordExportService.ExportWordReportAsync(wwwrootPath, templateFileName, tray);
            
            Console.WriteLine("Word document export completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting Word document: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task ExportCanvasImageAsync(Excubo.Blazor.Canvas.Canvas canvas, string trayName)
    {
        try
        {
            Console.WriteLine($"Starting canvas image export for tray: {trayName}");
            
            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string imagesPath = Path.Combine(wwwrootPath, "images");
            
            // Ensure the images directory exists
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
                Console.WriteLine($"Created images directory: {imagesPath}");
            }

            string imagePath = Path.Combine(imagesPath, $"{trayName}.jpg");
            Console.WriteLine($"Target image path: {imagePath}");
            
            // Delete existing file if it exists to avoid conflicts
            if (File.Exists(imagePath))
            {
                File.Delete(imagePath);
                Console.WriteLine($"Deleted existing image file: {imagePath}");
            }
            
            // Try canvas export with enhanced retry logic
            Console.WriteLine("Converting canvas to data URL...");
            var imageData = await canvas.ToDataURLWithRetryAsync("image/jpeg", 0.9);
            Console.WriteLine($"Canvas data URL length: {imageData?.Length ?? 0}");
            
            if (string.IsNullOrEmpty(imageData) || !imageData.Contains("base64,"))
            {
                throw new InvalidOperationException("Canvas export returned invalid data");
            }
            
            // Remove the data URL prefix (data:image/jpeg;base64,)
            var base64Data = imageData.Split(',')[1];
            var imageBytes = Convert.FromBase64String(base64Data);
            Console.WriteLine($"Image data size: {imageBytes.Length} bytes");
            
            // Save the image to the file system
            await File.WriteAllBytesAsync(imagePath, imageBytes);
            
            // Verify file was created
            if (File.Exists(imagePath))
            {
                var fileInfo = new FileInfo(imagePath);
                Console.WriteLine($"Canvas image saved successfully: {imagePath} ({fileInfo.Length} bytes)");
            }
            else
            {
                throw new InvalidOperationException("Image file was not created successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Canvas export failed: {ex.Message}");
            Console.WriteLine($"Attempting server-side rendering as fallback...");
            
            // Fallback to server-side rendering
            await ExportCanvasImageServerSideAsync(trayName);
        }
    }

    public async Task ExportCanvasImageServerSideAsync(string trayName)
    {
        try
        {
            Console.WriteLine($"Starting server-side canvas image generation for tray: {trayName}");
            
            // Get tray data
            var trays = await _cableService.GetCablesAsync(); // This needs to be updated to get tray by name
            var tray = trays.FirstOrDefault(c => c.Tag == trayName)?.CableType; // This is a placeholder - need proper tray lookup
            
            if (tray == null)
            {
                // For now, create a simple placeholder implementation
                Console.WriteLine("Tray data not available for server-side rendering, creating placeholder image");
                await CreatePlaceholderImageAsync(trayName);
                return;
            }
            
            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string imagesPath = Path.Combine(wwwrootPath, "images");
            
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }
            
            string imagePath = Path.Combine(imagesPath, $"{trayName}.jpg");
            
            // This would need proper implementation with tray data
            // For now, create a placeholder
            await CreatePlaceholderImageAsync(trayName);
            
            Console.WriteLine($"Server-side canvas image saved: {imagePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in server-side canvas generation: {ex.Message}");
            throw new InvalidOperationException($"Failed to generate image using server-side rendering: {ex.Message}", ex);
        }
    }
    
    private async Task CreatePlaceholderImageAsync(string trayName)
    {
        try
        {
            using var surface = SKSurface.Create(new SKImageInfo(800, 600));
            var canvas = surface.Canvas;
            
            canvas.Clear(SKColors.White);
            
            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 24,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextAlign = SKTextAlign.Center
            };
            
            canvas.DrawText($"Tray: {trayName}", 400, 300, paint);
            canvas.DrawText("Canvas export unavailable", 400, 350, paint);
            
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
            
            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string imagesPath = Path.Combine(wwwrootPath, "images");
            
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }
            
            string imagePath = Path.Combine(imagesPath, $"{trayName}.jpg");
            await File.WriteAllBytesAsync(imagePath, data.ToArray());
            
            Console.WriteLine($"Placeholder image created: {imagePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating placeholder image: {ex.Message}");
            throw;
        }
    }

    public async Task ExportTrayTableEntriesAsync()
    {
        try
        {
            Console.WriteLine("Starting tray table export...");
            await _excelExportService.ExportTrayTableEntriesAsync();
            Console.WriteLine("Tray table export completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting tray table: {ex.Message}");
            throw;
        }
    }

    private static string GetReportTemplate(string trayPurpose)
    {
        return trayPurpose == TrayConstants.TrayPurposes.TypeA 
            ? "ReportMacroTemplate_MV.docx" 
            : "ReportMacroTemplate_Space.docx";
    }
}