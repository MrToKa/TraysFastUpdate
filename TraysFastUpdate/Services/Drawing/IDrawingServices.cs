using Excubo.Blazor.Canvas;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Drawing;

namespace TraysFastUpdate.Services.Drawing;

public interface ITrayDrawingService
{
    Task DrawTrayLayoutAsync(Canvas canvas, Tray tray, List<Cable> cablesOnTray, 
        Dictionary<string, Dictionary<string, List<Cable>>> cableBundles, int canvasScale);
}

public interface ICableBundleDrawer
{
    Task<(double leftStartX, double bottomStartY)> DrawPowerBundlesAsync(
        Excubo.Blazor.Canvas.Contexts.Context2D ctx, TrayDrawingData data, 
        Dictionary<string, List<Cable>> bundles, double leftStartX, double bottomStartY, double spacing);
        
    Task<(double rightStartX, double bottomStartY)> DrawControlBundlesAsync(
        Excubo.Blazor.Canvas.Contexts.Context2D ctx, TrayDrawingData data, 
        Dictionary<string, List<Cable>> bundles, double rightStartX, double bottomStartY, double spacing);
        
    Task<(double leftStartX, double bottomStartY)> DrawMvBundlesAsync(
        Excubo.Blazor.Canvas.Contexts.Context2D ctx, TrayDrawingData data, 
        Dictionary<string, List<Cable>> bundles, double leftStartX, double bottomStartY, double spacing);
        
    Task<(double rightStartX, double bottomStartY)> DrawVfdBundlesAsync(
        Excubo.Blazor.Canvas.Contexts.Context2D ctx, TrayDrawingData data, 
        Dictionary<string, List<Cable>> bundles, double rightStartX, double bottomStartY, double spacing);
}