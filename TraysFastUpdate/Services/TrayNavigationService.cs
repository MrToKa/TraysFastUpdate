using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Contracts;

namespace TraysFastUpdate.Services;

public class TrayNavigationService
{
    private readonly ITrayService _trayService;

    public TrayNavigationService(ITrayService trayService)
    {
        _trayService = trayService;
    }

    public async Task<TrayNavigationData> InitializeNavigationAsync(int currentId)
    {
        var trays = await _trayService.GetTraysAsync();
        var trayIds = trays.OrderBy(t => t.Id).Select(t => t.Id).ToList();
        var currentIndex = trayIds.IndexOf(currentId);
        
        if (currentIndex == -1 && trayIds.Count > 0)
        {
            currentId = trayIds[0];
            currentIndex = 0;
        }

        return new TrayNavigationData
        {
            TrayIds = trayIds,
            CurrentTrayIndex = currentIndex,
            CurrentTrayId = currentId,
            TotalTraysCount = trayIds.Count
        };
    }

    public TrayNavigationData UpdateCurrentIndex(TrayNavigationData navigationData, int newId)
    {
        var newIndex = navigationData.TrayIds.IndexOf(newId);
        if (newIndex == -1 && navigationData.TrayIds.Count > 0)
        {
            newId = navigationData.TrayIds[0];
            newIndex = 0;
        }

        return navigationData with
        {
            CurrentTrayIndex = newIndex,
            CurrentTrayId = newId
        };
    }

    public int? GetPreviousTrayId(TrayNavigationData navigationData)
    {
        return navigationData.CurrentTrayIndex > 0 
            ? navigationData.TrayIds[navigationData.CurrentTrayIndex - 1] 
            : null;
    }

    public int? GetNextTrayId(TrayNavigationData navigationData)
    {
        return navigationData.CurrentTrayIndex < navigationData.TrayIds.Count - 1 
            ? navigationData.TrayIds[navigationData.CurrentTrayIndex + 1] 
            : null;
    }
}

public record TrayNavigationData
{
    public List<int> TrayIds { get; init; } = new();
    public int CurrentTrayIndex { get; init; }
    public int CurrentTrayId { get; init; }
    public int TotalTraysCount { get; init; }
}