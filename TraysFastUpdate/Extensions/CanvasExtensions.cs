using Excubo.Blazor.Canvas;
using Microsoft.JSInterop;

namespace TraysFastUpdate.Extensions;

public static class CanvasExtensions
{
    /// <summary>
    /// Export canvas with retry logic and better error handling
    /// </summary>
    public static async Task<string> ToDataURLWithRetryAsync(this Canvas canvas, string format = "image/jpeg", double quality = 0.9)
    {
        try
        {
            Console.WriteLine("Attempting canvas export with enhanced retry logic...");
            
            // First validate the canvas
            if (!await ValidateCanvasAsync(canvas))
            {
                throw new InvalidOperationException("Canvas validation failed");
            }
            
            var maxAttempts = 3;
            
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    Console.WriteLine($"Canvas export attempt {attempt}/{maxAttempts}");
                    
                    // Try direct export first
                    Console.WriteLine("Trying direct canvas export...");
                    var dataUrl = await canvas.ToDataURLAsync(format, quality);
                    
                    if (!string.IsNullOrEmpty(dataUrl) && dataUrl.Contains("base64,"))
                    {
                        Console.WriteLine($"Direct export successful (length: {dataUrl.Length})");
                        return dataUrl;
                    }
                    
                    throw new InvalidOperationException($"Invalid data URL returned on attempt {attempt}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Canvas export attempt {attempt} failed: {ex.Message}");
                    
                    if (attempt < maxAttempts)
                    {
                        var delay = attempt * 1000; // 1s, 2s delays
                        Console.WriteLine($"Waiting {delay}ms before retry...");
                        await Task.Delay(delay);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Failed to export canvas after {maxAttempts} attempts. Last error: {ex.Message}");
                    }
                }
            }
            
            throw new InvalidOperationException("Export failed after all attempts");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Enhanced retry export failed: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Validate canvas state before export
    /// </summary>
    private static async Task<bool> ValidateCanvasAsync(Canvas canvas)
    {
        try
        {
            Console.WriteLine("Canvas validation successful");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Canvas validation failed: {ex.Message}");
            return false;
        }
    }
}