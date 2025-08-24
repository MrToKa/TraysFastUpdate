using Excubo.Blazor.Canvas;
using Microsoft.JSInterop;

namespace TraysFastUpdate.Extensions;

public static class CanvasExtensions
{
    /// <summary>
    /// Converts canvas to data URL with comprehensive validation and retry logic
    /// </summary>
    public static async Task<string> ToDataURLWithRetryAsync(this Canvas canvas, string format = "image/jpeg", double quality = 0.9, int maxRetries = 3)
    {
        Exception? lastException = null;
        
        // First, validate that the canvas is properly rendered
        if (!await ValidateCanvasAsync(canvas))
        {
            throw new InvalidOperationException("Canvas is not properly rendered or accessible in the DOM");
        }
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Console.WriteLine($"Canvas export attempt {attempt}/{maxRetries}");
                
                // Create a more generous timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90)); // Increased timeout
                
                // Try direct canvas export first
                var dataUrl = await TryDirectCanvasExportAsync(canvas, format, quality);
                
                if (!string.IsNullOrEmpty(dataUrl) && dataUrl.Contains("base64,"))
                {
                    Console.WriteLine($"Canvas export successful on attempt {attempt} (data length: {dataUrl.Length})");
                    return dataUrl;
                }
                
                throw new InvalidOperationException($"Invalid data URL returned on attempt {attempt}");
            }
            catch (TaskCanceledException ex)
            {
                lastException = ex;
                Console.WriteLine($"Canvas export attempt {attempt} was canceled: {ex.Message}");
                
                if (attempt < maxRetries)
                {
                    Console.WriteLine($"Waiting {3000 * attempt}ms before retry...");
                    await Task.Delay(3000 * attempt); // Longer delay for cancellation issues
                }
            }
            catch (JSException ex) when (ex.Message.Contains("null"))
            {
                lastException = ex;
                Console.WriteLine($"Canvas export attempt {attempt} failed - canvas is null in DOM: {ex.Message}");
                
                if (attempt < maxRetries)
                {
                    Console.WriteLine($"Waiting {2000 * attempt}ms for canvas to be available...");
                    await Task.Delay(2000 * attempt);
                    
                    // Re-validate canvas before next attempt
                    if (!await ValidateCanvasAsync(canvas))
                    {
                        throw new InvalidOperationException("Canvas remains inaccessible after retry delay");
                    }
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                Console.WriteLine($"Canvas export attempt {attempt} failed: {ex.Message}");
                
                if (attempt < maxRetries)
                {
                    Console.WriteLine($"Waiting {1500 * attempt}ms before retry...");
                    await Task.Delay(1500 * attempt);
                }
            }
        }
        
        throw new InvalidOperationException($"Failed to export canvas after {maxRetries} attempts. Last error: {lastException?.Message}", lastException);
    }
    
    /// <summary>
    /// Validates that the canvas is properly rendered and accessible
    /// </summary>
    private static async Task<bool> ValidateCanvasAsync(Canvas canvas)
    {
        try
        {
            // Check if canvas context can be obtained
            await using var ctx = await canvas.GetContext2DAsync();
            if (ctx == null)
            {
                Console.WriteLine("Canvas context is null - canvas not properly initialized");
                return false;
            }
            
            // Try a simple operation to ensure canvas is functional
            await ctx.SaveAsync();
            await ctx.RestoreAsync();
            
            Console.WriteLine("Canvas validation successful");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Canvas validation failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Direct canvas export with error handling
    /// </summary>
    private static async Task<string> TryDirectCanvasExportAsync(Canvas canvas, string format, double quality)
    {
        try
        {
            // Convert ValueTask to Task for timeout handling
            var dataUrlTask = canvas.ToDataURLAsync(format, quality).AsTask();
            var timeoutTask = Task.Delay(60000); // 60 second timeout
            
            var completedTask = await Task.WhenAny(dataUrlTask, timeoutTask);
            
            if (completedTask == dataUrlTask)
            {
                return await dataUrlTask;
            }
            else
            {
                throw new TimeoutException("Canvas export operation timed out after 60 seconds");
            }
        }
        catch (JSException ex) when (ex.Message.Contains("toDataURL"))
        {
            // This specifically handles the "Cannot read properties of null (reading 'toDataURL')" error
            throw new InvalidOperationException("Canvas element is null or not properly rendered in DOM", ex);
        }
    }
}