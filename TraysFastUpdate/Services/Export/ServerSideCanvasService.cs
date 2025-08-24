using SkiaSharp;
using TraysFastUpdate.Models;
using TraysFastUpdate.Common.Constants;

namespace TraysFastUpdate.Services.Export;

public class ServerSideCanvasService
{
    public async Task<byte[]> GenerateTrayImageAsync(Tray tray, List<Cable> cablesOnTray, 
        Dictionary<string, Dictionary<string, List<Cable>>> cableBundles, int canvasScale = 3)
    {
        try
        {
            Console.WriteLine($"Generating server-side image for tray: {tray.Name}");
            
            var canvasWidth = (int)((tray.Width * canvasScale) + 100);
            var canvasHeight = (int)((tray.Height * canvasScale) + 100);
            
            using var surface = SKSurface.Create(new SKImageInfo(canvasWidth, canvasHeight));
            var canvas = surface.Canvas;
            
            // Clear canvas with white background
            canvas.Clear(SKColors.White);
            
            // Draw tray structure
            await DrawTrayStructureAsync(canvas, tray, canvasScale);
            
            // Draw cables (simplified version)
            await DrawCablesAsync(canvas, tray, cablesOnTray, canvasScale);
            
            // Create image
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
            
            var imageBytes = data.ToArray();
            Console.WriteLine($"Server-side image generated: {imageBytes.Length} bytes");
            
            return imageBytes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating server-side image: {ex.Message}");
            throw;
        }
    }
    
    private async Task DrawTrayStructureAsync(SKCanvas canvas, Tray tray, int canvasScale)
    {
        await Task.CompletedTask; // Async for consistency
        
        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };
        
        using var fillPaint = new SKPaint
        {
            Color = SKColor.Parse("#D3D3D3"),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        
        // Draw title
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 24,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };
        
        var titleText = $"Cables bundles laying concept for tray {tray.Name}";
        var titleX = (float)(tray.Width * canvasScale / 2 + 50);
        canvas.DrawText(titleText, titleX, 30, titlePaint);
        
        // Draw main rectangle
        var mainRect = new SKRect(50, 50, 
            (float)(50 + tray.Width * canvasScale), 
            (float)(50 + (tray.Height - TrayConstants.CProfileHeight) * canvasScale));
        canvas.DrawRect(mainRect, paint);
        
        // Draw C-profile
        var cProfileRect = new SKRect(50, 
            (float)(50 + (tray.Height - TrayConstants.CProfileHeight) * canvasScale),
            (float)(50 + tray.Width * canvasScale),
            (float)(50 + tray.Height * canvasScale));
        
        canvas.DrawRect(cProfileRect, paint);
        canvas.DrawRect(cProfileRect, fillPaint);
        
        // Draw width label
        using var labelPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 24,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial"),
            TextAlign = SKTextAlign.Center
        };
        
        var widthText = $"Useful tray width: {tray.Width} mm";
        var widthX = (float)(tray.Width * canvasScale / 2 + 50);
        var widthY = (float)(50 + tray.Height * canvasScale + TrayConstants.TextPadding);
        canvas.DrawText(widthText, widthX, widthY, labelPaint);
        
        // Draw height label (rotated)
        canvas.Save();
        canvas.Translate(TrayConstants.TextPadding, (float)(50 + tray.Height * canvasScale / 2));
        canvas.RotateDegrees(90);
        var heightText = $"Useful tray height: {tray.Height - TrayConstants.CProfileHeight} mm";
        canvas.DrawText(heightText, 0, 0, labelPaint);
        canvas.Restore();
    }
    
    private async Task DrawCablesAsync(SKCanvas canvas, Tray tray, List<Cable> cablesOnTray, int canvasScale)
    {
        await Task.CompletedTask; // Async for consistency
        
        if (cablesOnTray == null || !cablesOnTray.Any()) return;
        
        using var cablePaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };
        
        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 12,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial"),
            TextAlign = SKTextAlign.Center
        };
        
        // Simplified cable drawing - just draw circles in a grid
        var spacing = TrayConstants.Spacing * canvasScale;
        var startX = 50 + spacing;
        var startY = 50 + spacing;
        var cablesPerRow = Math.Max(1, (int)((tray.Width * canvasScale - 2 * spacing) / 50)); // 50px per cable
        
        for (int i = 0; i < cablesOnTray.Count; i++)
        {
            var cable = cablesOnTray[i];
            var row = i / cablesPerRow;
            var col = i % cablesPerRow;
            
            var x = (float)(startX + col * 50);
            var y = (float)(startY + row * 30);
            
            // Skip if outside tray bounds
            if (y > 50 + (tray.Height - TrayConstants.CProfileHeight) * canvasScale - 20) break;
            
            var radius = (float)(cable.CableType?.Diameter ?? 10) / 2 * canvasScale;
            if (radius < 5) radius = 5; // Minimum visible size
            if (radius > 25) radius = 25; // Maximum size for layout
            
            canvas.DrawCircle(x, y, radius, cablePaint);
            
            // Draw cable number
            canvas.DrawText((i + 1).ToString(), x, y + 4, textPaint);
        }
    }
}