using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Excubo.Blazor.Canvas;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services;
using TraysFastUpdate.Services.Contracts;
using TraysFastUpdate.Services.Drawing;

namespace TraysFastUpdate.Components.Pages.TrayPages;

public partial class Details : IDisposable
{
    [Parameter] public int Id { get; set; }
    [Parameter] public int TraysCount { get; set; }

    [Inject] protected ITrayService TrayService { get; set; } = default!;
    [Inject] protected ICableService CableService { get; set; } = default!;
    [Inject] protected ITrayDrawingService DrawingService { get; set; } = default!;
    [Inject] protected TrayNavigationService NavigationService { get; set; } = default!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;

    protected Tray TrayModel { get; set; } = new();
    protected List<Cable> CablesOnTray { get; set; } = new();
    protected Dictionary<string, Dictionary<string, List<Cable>>> CableBundles { get; set; } = new();
    protected Canvas? canvas;
    protected int canvasScale = 3;
    protected TrayNavigationData? NavigationData { get; set; }
    
    private bool _dataLoaded = false;
    private bool _canvasInitialized = false;
    private bool _shouldRedraw = false;
    private bool _jsRuntimeReady = false;
    private System.Timers.Timer? _canvasInitTimer;

    protected double CanvasWidth => (TrayModel.Width * canvasScale) + 100;
    protected double CanvasHeight => (TrayModel.Height * canvasScale) + 100;
    protected List<int> TrayIds => NavigationData?.TrayIds ?? new List<int>();
    protected int _currentTrayIndex => NavigationData?.CurrentTrayIndex ?? 0;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            NavigationData = await NavigationService.InitializeNavigationAsync(Id);
            Id = NavigationData.CurrentTrayId;
            TraysCount = NavigationData.TotalTraysCount;

            await LoadTrayDataAsync();
            _dataLoaded = true;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, "initializing page");
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (NavigationData != null)
        {
            NavigationData = NavigationService.UpdateCurrentIndex(NavigationData, Id);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Wait for JavaScript runtime to be ready
            await WaitForJSRuntimeAsync();
            
            // Initialize canvas with more robust approach
            await InitializeCanvasAsync();
        }
    }

    private async Task WaitForJSRuntimeAsync()
    {
        try
        {
            // Test if JSRuntime is available by calling a simple method
            var maxAttempts = 15; // Increase attempts
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    // Use a safer test that doesn't rely on eval
                    await JSRuntime.InvokeAsync<IJSObjectReference>("console.log", "JSRuntime test");
                    _jsRuntimeReady = true;
                    Console.WriteLine($"JSRuntime is ready after {i + 1} attempts");
                    return;
                }
                catch (Exception ex) when (ex.Message.Contains("JavaScript") || ex.Message.Contains("jsRuntime") || ex.Message.Contains("runtime"))
                {
                    Console.WriteLine($"JSRuntime attempt {i + 1} failed: {ex.Message}");
                    await Task.Delay(300 + (i * 100)); // Progressive delay: 300ms, 400ms, 500ms, etc.
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"JSRuntime attempt {i + 1} failed with unexpected error: {ex.Message}");
                    if (i >= 5) // After a few attempts, treat other errors as success
                    {
                        _jsRuntimeReady = true;
                        Console.WriteLine("Assuming JSRuntime is ready despite test failure");
                        return;
                    }
                    await Task.Delay(300 + (i * 100));
                }
            }
            
            // If all attempts failed, still try to proceed
            _jsRuntimeReady = true;
            Console.WriteLine("JSRuntime ready check timed out, proceeding anyway");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error waiting for JSRuntime: {ex.Message}");
            _jsRuntimeReady = true; // Assume it's ready and let the canvas code handle errors
        }
    }

    private async Task InitializeCanvasAsync()
    {
        try
        {
            if (!_jsRuntimeReady)
            {
                Console.WriteLine("JSRuntime not ready, cannot initialize canvas");
                _shouldRedraw = true;
                StartCanvasRetryTimer();
                return;
            }

            // Start with canvas flag
            _canvasInitialized = true;
            await InvokeAsync(StateHasChanged);
            
            // Wait for the canvas element to be rendered in DOM
            await Task.Delay(800); // Increased delay
            
            if (_dataLoaded && TrayModel.Width > 0 && TrayModel.Height > 0)
            {
                await AttemptCanvasDrawing();
            }
            else
            {
                _shouldRedraw = true;
                StartCanvasRetryTimer();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing canvas: {ex.Message}");
            _shouldRedraw = true;
            StartCanvasRetryTimer();
        }
    }

    private void StartCanvasRetryTimer()
    {
        _canvasInitTimer?.Dispose();
        _canvasInitTimer = new System.Timers.Timer(3000); // Try every 3 seconds
        _canvasInitTimer.Elapsed += async (sender, e) => 
        {
            if (_shouldRedraw && _dataLoaded && TrayModel.Width > 0 && TrayModel.Height > 0)
            {
                await InvokeAsync(async () =>
                {
                    // Re-check JSRuntime availability
                    if (!_jsRuntimeReady)
                    {
                        await WaitForJSRuntimeAsync();
                    }
                    
                    if (_jsRuntimeReady)
                    {
                        await AttemptCanvasDrawing();
                    }
                });
            }
        };
        _canvasInitTimer.Start();
    }

    private async Task AttemptCanvasDrawing()
    {
        try
        {
            await DrawTrayAsync();
            // If successful, stop the retry timer
            _canvasInitTimer?.Stop();
            _shouldRedraw = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Canvas drawing attempt failed: {ex.Message}");
            // Timer will continue to retry
        }
    }

    private async Task LoadTrayDataAsync()
    {
        try
        {
            TrayModel = await TrayService.GetTrayAsync(Id);
            await TrayService.UpdateTrayAsync(TrayModel);
            CablesOnTray = await CableService.GetCablesOnTrayAsync(TrayModel);
            CableBundles = await CableService.GetCablesBundlesOnTrayAsync(TrayModel);
            
            // Force a re-render after data is loaded
            await InvokeAsync(StateHasChanged);
            
            // If canvas is initialized and we should redraw, do it now
            if (_canvasInitialized && _jsRuntimeReady && TrayModel.Width > 0 && TrayModel.Height > 0)
            {
                await Task.Delay(400); // Small delay to ensure UI is updated
                await AttemptCanvasDrawing();
            }
            else if (TrayModel.Width > 0 && TrayModel.Height > 0)
            {
                _shouldRedraw = true;
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, "loading tray data");
        }
    }

    protected async Task HandleTrayPropertyChange()
    {
        try
        {
            await InvokeAsync(StateHasChanged);
            if (_jsRuntimeReady)
            {
                await DrawTrayAsync();
            }
            else
            {
                _shouldRedraw = true;
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, "updating tray properties");
        }
    }

    protected async Task HandleValidSubmit()
    {
        try
        {
            await TrayService.UpdateTrayAsync(TrayModel);
            ShowSuccessMessage("Tray details updated successfully!");
            await HandleTrayPropertyChange();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, "updating tray details");
        }
    }

    public async Task DrawTrayAsync()
    {
        try
        {
            // Ensure JSRuntime is ready
            if (!_jsRuntimeReady)
            {
                Console.WriteLine("JSRuntime not ready, cannot draw on canvas");
                _shouldRedraw = true;
                return;
            }

            // Ensure canvas is initialized and has valid dimensions
            if (!_canvasInitialized)
            {
                Console.WriteLine("Canvas not yet initialized, will retry later");
                _shouldRedraw = true;
                return;
            }

            if (canvas == null)
            {
                Console.WriteLine("Canvas reference is null");
                return;
            }

            if (TrayModel.Width <= 0 || TrayModel.Height <= 0)
            {
                Console.WriteLine($"Invalid tray dimensions: {TrayModel.Width} x {TrayModel.Height}");
                return;
            }

            Console.WriteLine($"Drawing tray: {TrayModel.Name}, Canvas: {CanvasWidth}x{CanvasHeight}, Cables: {CablesOnTray.Count}, Bundles: {CableBundles.Count}");
            
            // Add retry logic for canvas context with progressive delays
            var maxRetries = 2; // Reduce retries since we have better initialization
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await DrawingService.DrawTrayLayoutAsync(canvas, TrayModel, CablesOnTray, CableBundles, canvasScale);
                    Console.WriteLine("Tray drawing completed successfully");
                    _shouldRedraw = false;
                    _canvasInitTimer?.Stop(); // Stop retry timer on success
                    return;
                }
                catch (Exception ex) when ((ex.Message.Contains("jsRuntime") || ex.Message.Contains("context") || ex.Message.Contains("null")) && attempt < maxRetries)
                {
                    Console.WriteLine($"Canvas drawing attempt {attempt} failed: {ex.Message}, retrying...");
                    await Task.Delay(attempt * 1000); // 1s, 2s delays
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Drawing error: {ex.Message}");
            await HandleExceptionAsync(ex, "drawing tray layout");
        }
    }

    protected async Task ExportTrayData()
    {
        try
        {
            // Show loading state
            ShowInfoMessage("Generating documentation...");

            // Step 1: Save canvas as image first
            if (canvas != null && _canvasInitialized && _jsRuntimeReady)
            {
                try
                {
                    Console.WriteLine("Starting canvas image export...");
                    await TrayService.ExportCanvasImageAsync(canvas, TrayModel.Name);
                    Console.WriteLine("Canvas image exported successfully");
                    ShowInfoMessage("Canvas saved, generating Word document...");
                }
                catch (Exception canvasEx) when (canvasEx.Message.Contains("canceled") || canvasEx.Message.Contains("timeout"))
                {
                    Console.WriteLine($"Canvas export failed: {canvasEx.Message}, trying fallback...");
                    ShowInfoMessage("Primary canvas export failed, trying alternative method...");
                    
                    try
                    {
                        await ExportCanvasFallbackAsync();
                        Console.WriteLine("Fallback canvas export succeeded");
                        ShowInfoMessage("Canvas saved (fallback method), generating Word document...");
                    }
                    catch (Exception fallbackEx)
                    {
                        Console.WriteLine($"Fallback canvas export also failed: {fallbackEx.Message}");
                        ShowErrorMessage("Failed to save canvas image. Proceeding with document generation...");
                    }
                }
                catch (Exception canvasEx)
                {
                    Console.WriteLine($"Canvas export failed: {canvasEx.Message}");
                    ShowErrorMessage("Failed to save canvas image. Proceeding with document generation...");
                }
            }
            else
            {
                Console.WriteLine("Canvas not ready for export, proceeding without canvas image");
                ShowInfoMessage("Canvas not ready, generating Word document only...");
            }

            // Step 2: Generate Word document (which will include the saved image)
            try
            {
                Console.WriteLine("Starting Word document generation...");
                await TrayService.ExportToFileAsync(TrayModel);
                Console.WriteLine("Word document generated successfully");
                ShowSuccessMessage("Tray documentation exported successfully!");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Template file not found: {ex.Message}");
                ShowErrorMessage($"Template file not found. Please ensure the appropriate template file exists in the wwwroot directory.");
                throw;
            }
            catch (Exception docEx)
            {
                Console.WriteLine($"Word document generation failed: {docEx.Message}");
                ShowErrorMessage($"Failed to generate Word document: {docEx.Message}");
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Export error: {ex.Message}");
            await HandleExceptionAsync(ex, "exporting tray documentation");
        }
    }

    protected async Task ExportCanvasOnly()
    {
        try
        {
            if (canvas != null && _canvasInitialized && _jsRuntimeReady)
            {
                ShowInfoMessage("Saving canvas image...");
                Console.WriteLine("Starting canvas-only export...");
                
                // First try the extension method
                try
                {
                    await TrayService.ExportCanvasImageAsync(canvas, TrayModel.Name);
                    Console.WriteLine("Canvas-only export completed successfully");
                    ShowSuccessMessage($"Canvas image saved successfully as {TrayModel.Name}.jpg");
                    return;
                }
                catch (Exception ex) when (ex.Message.Contains("canceled") || ex.Message.Contains("timeout"))
                {
                    Console.WriteLine($"Extension method failed: {ex.Message}, trying fallback method...");
                    ShowInfoMessage("Primary method failed, trying alternative approach...");
                    
                    // Fallback: Try direct canvas export without retry
                    await ExportCanvasFallbackAsync();
                    ShowSuccessMessage($"Canvas image saved successfully as {TrayModel.Name}.jpg (fallback method)");
                }
            }
            else
            {
                var issues = new List<string>();
                if (canvas == null) issues.Add("Canvas is null");
                if (!_canvasInitialized) issues.Add("Canvas not initialized");
                if (!_jsRuntimeReady) issues.Add("JSRuntime not ready");
                
                var errorMsg = $"Canvas is not ready for export: {string.Join(", ", issues)}";
                Console.WriteLine(errorMsg);
                ShowErrorMessage("Canvas is not ready for export. Please wait for the drawing to complete.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Canvas export error: {ex.Message}");
            await HandleExceptionAsync(ex, "exporting canvas image");
        }
    }

    private async Task ExportCanvasFallbackAsync()
    {
        try
        {
            Console.WriteLine("Using fallback canvas export method...");
            
            // Method 1: Try direct canvas API
            try
            {
                var dataUrl = await canvas!.ToDataURLAsync("image/jpeg", 0.8);
                
                if (!string.IsNullOrEmpty(dataUrl) && dataUrl.Contains("base64,"))
                {
                    await SaveCanvasDataAsync(dataUrl);
                    Console.WriteLine($"Fallback method 1 (direct API) completed: {dataUrl.Length} chars");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fallback method 1 failed: {ex.Message}");
            }
            
            // Method 2: Try JavaScript helper
            try
            {
                Console.WriteLine("Trying JavaScript helper method...");
                
                // Get canvas info for debugging
                var canvasInfo = await JSRuntime.InvokeAsync<object>("canvasHelper.getCanvasInfo", "canvasId");
                Console.WriteLine($"Canvas info: {System.Text.Json.JsonSerializer.Serialize(canvasInfo)}");
                
                // Validate canvas first
                var isValid = await JSRuntime.InvokeAsync<bool>("canvasHelper.validateCanvas", "canvasId");
                if (!isValid)
                {
                    throw new InvalidOperationException("Canvas validation failed via JavaScript");
                }
                
                // Export using JavaScript helper
                var dataUrl = await JSRuntime.InvokeAsync<string>("canvasHelper.exportCanvas", "canvasId", "image/jpeg", 0.8);
                
                if (!string.IsNullOrEmpty(dataUrl) && dataUrl.Contains("base64,"))
                {
                    await SaveCanvasDataAsync(dataUrl);
                    Console.WriteLine($"Fallback method 2 (JavaScript helper) completed: {dataUrl.Length} chars");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fallback method 2 failed: {ex.Message}");
            }
            
            throw new InvalidOperationException("All fallback methods failed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"All fallback canvas export methods failed: {ex.Message}");
            throw;
        }
    }

    private async Task SaveCanvasDataAsync(string dataUrl)
    {
        var base64Data = dataUrl.Split(',')[1];
        var imageBytes = Convert.FromBase64String(base64Data);
        
        string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        string imagesPath = Path.Combine(wwwrootPath, "images");
        
        if (!Directory.Exists(imagesPath))
        {
            Directory.CreateDirectory(imagesPath);
        }
        
        string imagePath = Path.Combine(imagesPath, $"{TrayModel.Name}.jpg");
        await File.WriteAllBytesAsync(imagePath, imageBytes);
        
        Console.WriteLine($"Canvas data saved to: {imagePath} ({imageBytes.Length} bytes)");
    }

    protected async Task DebugCanvasState()
    {
        try
        {
            var debugInfo = new List<string>
            {
                $"Canvas null: {canvas == null}",
                $"Canvas initialized: {_canvasInitialized}",
                $"JSRuntime ready: {_jsRuntimeReady}",
                $"Data loaded: {_dataLoaded}",
                $"Should redraw: {_shouldRedraw}",
                $"Tray dimensions: {TrayModel.Width}x{TrayModel.Height}",
                $"Tray purpose: {TrayModel.Purpose}",
                $"Expected template: {GetExpectedTemplate(TrayModel.Purpose)}",
                $"Cables count: {CablesOnTray.Count}",
                $"Bundles count: {CableBundles.Count}"
            };

            var message = string.Join("\n", debugInfo);
            Console.WriteLine($"Canvas Debug Info:\n{message}");
            
            ShowInfoMessage("Debug info logged to console");
            
            // Try a simple canvas test if possible
            if (canvas != null && _canvasInitialized && _jsRuntimeReady)
            {
                try
                {
                    await using var ctx = await canvas.GetContext2DAsync();
                    if (ctx != null)
                    {
                        await ctx.SaveAsync();
                        await ctx.RestoreAsync();
                        ShowSuccessMessage("Canvas context test passed!");
                    }
                    else
                    {
                        ShowErrorMessage("Canvas context is null");
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMessage($"Canvas context test failed: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Debug error: {ex.Message}");
            ShowErrorMessage($"Debug failed: {ex.Message}");
        }
    }

    private string GetExpectedTemplate(string trayPurpose)
    {
        return trayPurpose == "Type A (Pink color) for MV cables" 
            ? "ReportMacroTemplate_MV.docx" 
            : "ReportMacroTemplate_Space.docx";
    }

    protected void GoToPreviousTray()
    {
        if (NavigationData != null)
        {
            var prevId = NavigationService.GetPreviousTrayId(NavigationData);
            if (prevId.HasValue)
            {
                NavigationManager.NavigateTo($"/trays/details/{prevId.Value}", true);
            }
        }
    }

    protected void GoToNextTray()
    {
        if (NavigationData != null)
        {
            var nextId = NavigationService.GetNextTrayId(NavigationData);
            if (nextId.HasValue)
            {
                NavigationManager.NavigateTo($"/trays/details/{nextId.Value}", true);
            }
        }
    }

    public void Dispose()
    {
        _canvasInitTimer?.Dispose();
    }
}
