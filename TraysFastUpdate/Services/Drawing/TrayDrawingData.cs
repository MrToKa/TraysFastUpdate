using TraysFastUpdate.Models;

namespace TraysFastUpdate.Services.Drawing;

public class TrayDrawingData
{
    public Tray Tray { get; set; } = default!;
    public List<Cable> CablesOnTray { get; set; } = new();
    public Dictionary<string, Dictionary<string, List<Cable>>> CableBundles { get; set; } = new();
    public int CanvasScale { get; set; }
    
    public List<Cable> BottomRowPowerCables { get; set; } = new();
    public List<Cable> BottomRowControlCables { get; set; } = new();
    public List<Cable> BottomRowVFDCables { get; set; } = new();

    public void ClearBottomRowCables()
    {
        BottomRowPowerCables.Clear();
        BottomRowControlCables.Clear();
        BottomRowVFDCables.Clear();
    }
}