using TraysFastUpdate.Models;

namespace TraysFastUpdate.Services.Export;

public interface IFileExportService
{
    Task ExportTrayDocumentationAsync(Tray tray);
    Task ExportCanvasImageAsync(Excubo.Blazor.Canvas.Canvas canvas, string trayName, bool rotate = true);
    Task ExportTrayTableEntriesAsync();
}

public interface IWordExportService
{
    Task ExportWordReportAsync(string wwwrootPath, string templateFileName, Tray tray);
}

public interface IExcelExportService
{
    Task ExportTrayTableEntriesAsync();
}