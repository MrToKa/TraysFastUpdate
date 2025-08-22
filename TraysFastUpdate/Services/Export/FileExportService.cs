using TraysFastUpdate.Common.Constants;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Export;

namespace TraysFastUpdate.Services.Export;

public class FileExportService : IFileExportService
{
    private readonly IWordExportService _wordExportService;
    private readonly IExcelExportService _excelExportService;

    public FileExportService(IWordExportService wordExportService, IExcelExportService excelExportService)
    {
        _wordExportService = wordExportService;
        _excelExportService = excelExportService;
    }

    public async Task ExportTrayDocumentationAsync(Tray tray)
    {
        string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        
        // Ensure the files directory exists
        string filesPath = Path.Combine(wwwrootPath, "files");
        if (!Directory.Exists(filesPath))
        {
            Directory.CreateDirectory(filesPath);
        }
        
        string reportType = GetReportTemplate(tray.Purpose);
        
        try
        {
            await _wordExportService.ExportWordReportAsync(wwwrootPath, reportType, tray);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting Word document: {ex.Message}");
            throw;
        }
    }

    public async Task ExportCanvasImageAsync(Excubo.Blazor.Canvas.Canvas canvas, string trayName)
    {
        string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        string directoryPath = Path.Combine("wwwroot", "images");
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string filePath = Path.Combine(wwwrootPath, "images", $"{trayName}.jpg");
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        try
        {
            var dataUrl = await canvas.ToDataURLAsync("image/jpeg", 0.8f);
            var base64 = dataUrl.Split(',')[1];
            var bytes = Convert.FromBase64String(base64);
            await File.WriteAllBytesAsync(filePath, bytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting canvas image: {ex.Message}");
            throw;
        }
    }

    public async Task ExportTrayTableEntriesAsync()
    {
        await _excelExportService.ExportTrayTableEntriesAsync();
    }

    private static string GetReportTemplate(string trayPurpose)
    {
        return trayPurpose == TrayConstants.TrayPurposes.TypeA 
            ? "ReportMacroTemplate_MV.docx" 
            : "ReportMacroTemplate_Space.docx";
    }
}