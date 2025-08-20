using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using TraysFastUpdate.Models;

namespace TraysFastUpdate.Services;

public class TrayDrawingService
{
    private const int TextPadding = 20;
    private const int CProfileHeight = 15;

    public async Task DrawTrayLayoutAsync(Canvas canvas, Tray tray, List<Cable> cablesOnTray, 
        Dictionary<string, Dictionary<string, List<Cable>>> cableBundles, int canvasScale)
    {
        var drawingData = new TrayDrawingData
        {
            Tray = tray,
            CablesOnTray = cablesOnTray,
            CableBundles = cableBundles,
            CanvasScale = canvasScale
        };

        await using var ctx = await canvas.GetContext2DAsync();
        
        await DrawBaseTrayStructureAsync(ctx, drawingData);
        await DrawCableBundlesAsync(ctx, drawingData);
        await DrawSeparatorsAsync(ctx, drawingData);
    }

    private async Task DrawBaseTrayStructureAsync(Context2D ctx, TrayDrawingData data)
    {
        var canvasWidth = (data.Tray.Width * data.CanvasScale) + 100;
        var canvasHeight = (data.Tray.Height * data.CanvasScale) + 100;

        // Clear canvas
        await ctx.FillStyleAsync("white");
        await ctx.FillRectAsync(0, 0, canvasWidth, canvasHeight);

        // Draw title
        await DrawTitleAsync(ctx, data);
        
        // Draw height label
        await DrawHeightLabelAsync(ctx, data);
        
        // Draw tray rectangle and C-profile
        await DrawTrayRectangleAsync(ctx, data);
        
        // Draw width label
        await DrawWidthLabelAsync(ctx, data);
    }

    private async Task DrawTitleAsync(Context2D ctx, TrayDrawingData data)
    {
        await ctx.SetTransformAsync(1, 0, 0, 1, 0, 0);
        await ctx.SaveAsync();
        await ctx.FontAsync("24px Arial");
        await ctx.FillStyleAsync("black");
        await ctx.TextAlignAsync(TextAlign.Center);
        await ctx.TextBaseLineAsync(TextBaseLine.Middle);
        await ctx.FillTextAsync($"Cables bundles laying concept for tray {data.Tray.Name}", 
            data.Tray.Width * data.CanvasScale / 2, 30);
        await ctx.RestoreAsync();
    }

    private async Task DrawHeightLabelAsync(Context2D ctx, TrayDrawingData data)
    {
        await ctx.SaveAsync();
        await ctx.FontAsync("24px Arial");
        await ctx.FillStyleAsync("black");
        await ctx.TextAlignAsync(TextAlign.Center);
        await ctx.TextBaseLineAsync(TextBaseLine.Middle);
        await ctx.TranslateAsync(TextPadding, 50 + data.Tray.Height * data.CanvasScale / 2);
        await ctx.RotateAsync(Math.PI / 2);
        await ctx.FillTextAsync($"Useful tray height: {data.Tray.Height - CProfileHeight} mm", 0, 0);
        await ctx.RestoreAsync();
    }

    private async Task DrawTrayRectangleAsync(Context2D ctx, TrayDrawingData data)
    {
        // Draw main rectangle
        await ctx.StrokeStyleAsync("black");
        await ctx.StrokeRectAsync(50, 50, data.Tray.Width * data.CanvasScale, 
            (data.Tray.Height - CProfileHeight) * data.CanvasScale);
        
        // Draw C-profile
        await ctx.StrokeRectAsync(50, 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale, 
            data.Tray.Width * data.CanvasScale, CProfileHeight * data.CanvasScale);
        
        // Fill C-profile
        await ctx.FillStyleAsync("#D3D3D3");
        await ctx.FillRectAsync(50, 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale, 
            data.Tray.Width * data.CanvasScale, CProfileHeight * data.CanvasScale);
    }

    private async Task DrawWidthLabelAsync(Context2D ctx, TrayDrawingData data)
    {
        await ctx.FontAsync("24px Arial");
        await ctx.FillStyleAsync("black");
        await ctx.TextAlignAsync(TextAlign.Center);
        await ctx.TextBaseLineAsync(TextBaseLine.Middle);
        await ctx.FillTextAsync($"Useful tray width: {data.Tray.Width} mm", 
            (data.Tray.Width * data.CanvasScale) / 2, 
            50 + (data.Tray.Height * data.CanvasScale) + TextPadding);
        await ctx.SetTransformAsync(1, 0, 0, 1, 0, 0);
    }

    private async Task DrawCableBundlesAsync(Context2D ctx, TrayDrawingData data)
    {
        data.ClearBottomRowCables();
        
        double spacing = 1 * data.CanvasScale;
        double leftStartX = 50 + spacing;
        double rightStartX = 50 + data.Tray.Width * data.CanvasScale - spacing;
        double bottomStartY = 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale;

        var bundleDrawer = new CableBundleDrawer();

        foreach (var bundle in data.CableBundles)
        {
            switch (bundle.Key)
            {
                case "Power":
                    (leftStartX, bottomStartY) = await bundleDrawer.DrawPowerBundlesAsync(ctx, data, bundle.Value, leftStartX, bottomStartY, spacing);
                    break;
                case "Control":
                    (rightStartX, bottomStartY) = await bundleDrawer.DrawControlBundlesAsync(ctx, data, bundle.Value, rightStartX, bottomStartY, spacing);
                    break;
                case "MV":
                    (leftStartX, bottomStartY) = await bundleDrawer.DrawMvBundlesAsync(ctx, data, bundle.Value, leftStartX, bottomStartY, spacing);
                    break;
                case "VFD":
                    (rightStartX, bottomStartY) = await bundleDrawer.DrawVfdBundlesAsync(ctx, data, bundle.Value, rightStartX, bottomStartY, spacing);
                    break;
            }
        }
    }

    private async Task DrawSeparatorsAsync(Context2D ctx, TrayDrawingData data)
    {
        if (data.Tray.Purpose == "Type B (Green color) for LV cables" && 
            data.BottomRowPowerCables.Count > 0 && data.BottomRowVFDCables.Count > 0)
        {
            await DrawSeparatorLineAsync(ctx, data, data.BottomRowPowerCables, data.BottomRowVFDCables);
        }
        else if (data.Tray.Purpose == "Type BC (Teal color) for LV and Instrumentation and  Control cables, divided by separator" && 
                 data.BottomRowPowerCables.Count > 0 && data.BottomRowControlCables.Count > 0)
        {
            await DrawSeparatorLineAsync(ctx, data, data.BottomRowPowerCables, data.BottomRowControlCables);
        }
    }

    private async Task DrawSeparatorLineAsync(Context2D ctx, TrayDrawingData data, 
        List<Cable> leftCables, List<Cable> rightCables)
    {
        double trayFreeSpace = data.Tray.Width - (leftCables.Sum(x => x.CableType.Diameter + 1) + 
                                                 rightCables.Sum(x => x.CableType.Diameter + 1));
        double separatorX = (leftCables.Sum(x => x.CableType.Diameter + 1) + trayFreeSpace / 2) * data.CanvasScale;

        await ctx.SetTransformAsync(1, 0, 0, 1, 0, 0);
        await ctx.SaveAsync();
        await ctx.StrokeStyleAsync("black");
        await ctx.LineWidthAsync(2);
        await ctx.BeginPathAsync();
        await ctx.MoveToAsync(50 + separatorX, 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale);
        await ctx.LineToAsync(50 + separatorX, 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale - 
                              (data.Tray.Height - CProfileHeight * 2) * data.CanvasScale);
        await ctx.StrokeAsync();
        await ctx.RestoreAsync();
    }
}

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