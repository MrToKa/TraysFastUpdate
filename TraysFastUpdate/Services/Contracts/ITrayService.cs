using Microsoft.AspNetCore.Components.Forms;
using TraysFastUpdate.Models;

namespace TraysFastUpdate.Services.Contracts
{
    public interface ITrayService
    {
        Task<Tray> GetTrayAsync(int trayId);
        Task<List<Tray>> GetTraysAsync();
        Task CreateTrayAsync(Tray tray);
        Task UpdateTrayAsync(Tray trayId);
        Task DeleteTrayAsync(int trayId);
        Task UploadFromFileAsync(IBrowserFile file);
        Task ExportToFileAsync(Tray tray);
    }
}
