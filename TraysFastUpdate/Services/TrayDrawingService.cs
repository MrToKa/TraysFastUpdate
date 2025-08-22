using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using TraysFastUpdate.Common.Constants;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Drawing;

namespace TraysFastUpdate.Services;

public class TrayDrawingService : ITrayDrawingService
{
    private readonly ICableBundleDrawer _bundleDrawer;

    public TrayDrawingService(ICableBundleDrawer bundleDrawer)
    {
        _bundleDrawer = bundleDrawer;
    }

    public async Task DrawTrayLayoutAsync(Canvas canvas, Tray tray, List<Cable> cablesOnTray, 
        Dictionary<string, Dictionary<string, List<Cable>>> cableBundles, int canvasScale)
    {
        if (canvas == null)
        {
            throw new ArgumentNullException(nameof(canvas), "Canvas cannot be null");
        }

        if (tray == null)
        {
            throw new ArgumentNullException(nameof(tray), "Tray cannot be null");
        }

        try
        {
            var drawingData = new TrayDrawingData
            {
                Tray = tray,
                CablesOnTray = cablesOnTray ?? new List<Cable>(),
                CableBundles = cableBundles ?? new Dictionary<string, Dictionary<string, List<Cable>>>(),
                CanvasScale = canvasScale
            };

            // Add null check and better error handling for canvas context with retry logic
            Context2D? ctx = null;
            var maxRetries = 3;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"Attempting to get canvas context (attempt {attempt}/{maxRetries})");
                    ctx = await canvas.GetContext2DAsync();
                    
                    if (ctx == null)
                    {
                        throw new InvalidOperationException($"Canvas context is null on attempt {attempt}");
                    }
                    
                    Console.WriteLine("Canvas context obtained successfully");
                    break;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    Console.WriteLine($"Canvas context attempt {attempt} failed: {ex.Message}");
                    await Task.Delay(500 * attempt); // Progressive delay
                    ctx = null;
                }
            }

            if (ctx == null)
            {
                throw new InvalidOperationException("Failed to get canvas 2D context after all retry attempts - JSRuntime may not be ready");
            }

            await using (ctx)
            {
                // Test the context by performing a simple operation
                try
                {
                    await ctx.SaveAsync();
                    await ctx.RestoreAsync();
                    Console.WriteLine("Canvas context test successful");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Canvas context test failed: {ex.Message}", ex);
                }

                await DrawBaseTrayStructureAsync(ctx, drawingData);
                await DrawCableBundlesAsync(ctx, drawingData);
                await DrawSeparatorsAsync(ctx, drawingData);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in TrayDrawingService.DrawTrayLayoutAsync: {ex.Message}");
            throw;
        }
    }

    private async Task DrawBaseTrayStructureAsync(Context2D ctx, TrayDrawingData data)
    {
        if (ctx == null || data?.Tray == null) return;

        try
        {
            var canvasWidth = (data.Tray.Width * data.CanvasScale) + 100;
            var canvasHeight = (data.Tray.Height * data.CanvasScale) + 100;

            // Clear canvas with error handling
            try
            {
                await ctx.FillStyleAsync("white");
                await ctx.FillRectAsync(0, 0, canvasWidth, canvasHeight);
                Console.WriteLine("Canvas cleared successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing canvas: {ex.Message}");
                throw;
            }

            // Draw components
            await DrawTitleAsync(ctx, data);
            await DrawHeightLabelAsync(ctx, data);
            await DrawTrayRectangleAsync(ctx, data);
            await DrawWidthLabelAsync(ctx, data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DrawBaseTrayStructureAsync: {ex.Message}");
            throw;
        }
    }

    private async Task DrawTitleAsync(Context2D ctx, TrayDrawingData data)
    {
        if (ctx == null || data?.Tray == null) return;

        try
        {
            await ctx.SaveAsync();
            await ctx.FontAsync("24px Arial");
            await ctx.FillStyleAsync("black");
            await ctx.TextAlignAsync(TextAlign.Center);
            await ctx.TextBaseLineAsync(TextBaseLine.Middle);
            await ctx.FillTextAsync($"Cables bundles laying concept for tray {data.Tray.Name}", 
                data.Tray.Width * data.CanvasScale / 2 + 50, 30);
            await ctx.RestoreAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DrawTitleAsync: {ex.Message}");
            // Don't re-throw title drawing errors as they're not critical
        }
    }

    private async Task DrawHeightLabelAsync(Context2D ctx, TrayDrawingData data)
    {
        if (ctx == null || data?.Tray == null) return;

        try
        {
            await ctx.SaveAsync();
            await ctx.FontAsync("24px Arial");
            await ctx.FillStyleAsync("black");
            await ctx.TextAlignAsync(TextAlign.Center);
            await ctx.TextBaseLineAsync(TextBaseLine.Middle);

            // Translate and rotate to make the text vertical (top to bottom)
            await ctx.TranslateAsync(TrayConstants.TextPadding, 50 + data.Tray.Height * data.CanvasScale / 2);
            await ctx.RotateAsync(Math.PI / 2); // Rotate 90 degrees clockwise

            await ctx.FillTextAsync($"Useful tray height: {data.Tray.Height - TrayConstants.CProfileHeight} mm", 0, 0);
            await ctx.RestoreAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DrawHeightLabelAsync: {ex.Message}");
            // Don't re-throw label drawing errors as they're not critical
        }
    }

    private async Task DrawTrayRectangleAsync(Context2D ctx, TrayDrawingData data)
    {
        if (ctx == null || data?.Tray == null) return;

        try
        {
            // Set the line width to match the C-profile thickness
            double lineWidth = 1;

            // Draw main rectangle
            await ctx.StrokeStyleAsync("black");
            await ctx.LineWidthAsync(lineWidth);
            await ctx.StrokeRectAsync(50, 50, data.Tray.Width * data.CanvasScale, 
                (data.Tray.Height - TrayConstants.CProfileHeight) * data.CanvasScale);
            
            // Draw C-profile
            await ctx.StrokeRectAsync(50, 50 + (data.Tray.Height - TrayConstants.CProfileHeight) * data.CanvasScale, 
                data.Tray.Width * data.CanvasScale, TrayConstants.CProfileHeight * data.CanvasScale);
            
            // Fill C-profile
            await ctx.FillStyleAsync("#D3D3D3");
            await ctx.FillRectAsync(50, 50 + (data.Tray.Height - TrayConstants.CProfileHeight) * data.CanvasScale, 
                data.Tray.Width * data.CanvasScale, TrayConstants.CProfileHeight * data.CanvasScale);
                
            Console.WriteLine("Tray rectangle drawn successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DrawTrayRectangleAsync: {ex.Message}");
            throw; // This is critical, so re-throw
        }
    }

    private async Task DrawWidthLabelAsync(Context2D ctx, TrayDrawingData data)
    {
        if (ctx == null || data?.Tray == null) return;

        try
        {
            await ctx.SaveAsync();
            await ctx.FontAsync("24px Arial");
            await ctx.FillStyleAsync("black");
            await ctx.TextAlignAsync(TextAlign.Center);
            await ctx.TextBaseLineAsync(TextBaseLine.Middle);
            await ctx.FillTextAsync($"Useful tray width: {data.Tray.Width} mm", 
                (data.Tray.Width * data.CanvasScale) / 2 + 50, 
                50 + (data.Tray.Height * data.CanvasScale) + TrayConstants.TextPadding);
            await ctx.RestoreAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DrawWidthLabelAsync: {ex.Message}");
            // Don't re-throw label drawing errors as they're not critical
        }
    }

    private async Task DrawCableBundlesAsync(Context2D ctx, TrayDrawingData data)
    {
        if (ctx == null || data?.CableBundles == null) return;

        try
        {
            data.ClearBottomRowCables();
            
            double spacing = TrayConstants.Spacing * data.CanvasScale;
            double leftStartX = 50 + spacing;
            double rightStartX = 50 + data.Tray.Width * data.CanvasScale - spacing;
            double bottomStartY = 50 + (data.Tray.Height - TrayConstants.CProfileHeight) * data.CanvasScale;

            foreach (var bundle in data.CableBundles)
            {
                try
                {
                    switch (bundle.Key)
                    {
                        case TrayConstants.CablePurposes.Power:
                            (leftStartX, bottomStartY) = await _bundleDrawer.DrawPowerBundlesAsync(ctx, data, bundle.Value, leftStartX, bottomStartY, spacing);
                            break;
                        case TrayConstants.CablePurposes.Control:
                            (rightStartX, bottomStartY) = await _bundleDrawer.DrawControlBundlesAsync(ctx, data, bundle.Value, rightStartX, bottomStartY, spacing);
                            break;
                        case TrayConstants.CablePurposes.MV:
                            (leftStartX, bottomStartY) = await _bundleDrawer.DrawMvBundlesAsync(ctx, data, bundle.Value, leftStartX, bottomStartY, spacing);
                            break;
                        case TrayConstants.CablePurposes.VFD:
                            (rightStartX, bottomStartY) = await _bundleDrawer.DrawVfdBundlesAsync(ctx, data, bundle.Value, rightStartX, bottomStartY, spacing);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error drawing bundle {bundle.Key}: {ex.Message}");
                    // Continue with other bundles even if one fails
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DrawCableBundlesAsync: {ex.Message}");
            // Don't re-throw as cable drawing is not critical for basic tray structure
        }
    }

    private async Task DrawSeparatorsAsync(Context2D ctx, TrayDrawingData data)
    {
        if (ctx == null || data?.Tray == null) return;

        try
        {
            if (data.Tray.Purpose == TrayConstants.TrayPurposes.TypeB && 
                data.BottomRowPowerCables.Count > 0 && data.BottomRowVFDCables.Count > 0)
            {
                await DrawSeparatorLineAsync(ctx, data, data.BottomRowPowerCables, data.BottomRowVFDCables);
            }
            else if (data.Tray.Purpose == TrayConstants.TrayPurposes.TypeBC && 
                     data.BottomRowPowerCables.Count > 0 && data.BottomRowControlCables.Count > 0)
            {
                await DrawSeparatorLineAsync(ctx, data, data.BottomRowPowerCables, data.BottomRowControlCables);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DrawSeparatorsAsync: {ex.Message}");
            // Don't re-throw separator drawing errors as they're not critical
        }
    }

    private async Task DrawSeparatorLineAsync(Context2D ctx, TrayDrawingData data, 
        List<Cable> leftCables, List<Cable> rightCables)
    {
        if (ctx == null || data?.Tray == null || leftCables == null || rightCables == null) return;

        try
        {
            double trayFreeSpace = data.Tray.Width - (leftCables.Sum(x => x.CableType?.Diameter ?? 0 + 1) + 
                                                     rightCables.Sum(x => x.CableType?.Diameter ?? 0 + 1));
            double separatorX = (leftCables.Sum(x => x.CableType?.Diameter ?? 0 + 1) + trayFreeSpace / 2) * data.CanvasScale;

            await ctx.SaveAsync();
            await ctx.StrokeStyleAsync("black");
            await ctx.LineWidthAsync(2);
            await ctx.BeginPathAsync();
            await ctx.MoveToAsync(50 + separatorX, 50 + (data.Tray.Height - TrayConstants.CProfileHeight) * data.CanvasScale);
            await ctx.LineToAsync(50 + separatorX, 50 + (data.Tray.Height - TrayConstants.CProfileHeight) * data.CanvasScale - 
                                  (data.Tray.Height - TrayConstants.CProfileHeight * 2) * data.CanvasScale);
            await ctx.StrokeAsync();
            await ctx.RestoreAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DrawSeparatorLineAsync: {ex.Message}");
            // Don't re-throw separator line drawing errors as they're not critical
        }
    }
}