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
                    return;
                }
                catch (Exception ex) when (ex.Message.Contains("JavaScript") || ex.Message.Contains("jsRuntime") || ex.Message.Contains("runtime"))
                {
                    await Task.Delay(300 + (i * 100)); // Progressive delay: 300ms, 400ms, 500ms, etc.
                }
                catch (Exception ex)
                {
                    if (i >= 5) // After a few attempts, treat other errors as success
                    {
                        _jsRuntimeReady = true;
                        return;
                    }
                    await Task.Delay(300 + (i * 100));
                }
            }
            
            // If all attempts failed, still try to proceed
            _jsRuntimeReady = true;
        }
        catch (Exception ex)
        {
            _jsRuntimeReady = true; // Assume it's ready and let the canvas code handle errors
        }
    }

    private async Task InitializeCanvasAsync()
    {
        try
        {
            if (!_jsRuntimeReady)
            {
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
        catch (Exception)
        {
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
                _shouldRedraw = true;
                return;
            }

            // Ensure canvas is initialized and has valid dimensions
            if (!_canvasInitialized)
            {
                _shouldRedraw = true;
                return;
            }

            if (canvas == null)
            {
                return;
            }

            if (TrayModel.Width <= 0 || TrayModel.Height <= 0)
            {
                return;
            }
            
            // Add retry logic for canvas context with progressive delays
            var maxRetries = 2; // Reduce retries since we have better initialization
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await DrawingService.DrawTrayLayoutAsync(canvas, TrayModel, CablesOnTray, CableBundles, canvasScale);
                    _shouldRedraw = false;
                    _canvasInitTimer?.Stop(); // Stop retry timer on success
                    return;
                }
                catch (Exception ex) when ((ex.Message.Contains("jsRuntime") || ex.Message.Contains("context") || ex.Message.Contains("null")) && attempt < maxRetries)
                {
                    await Task.Delay(attempt * 1000); // 1s, 2s delays
                }
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, "drawing tray layout");
        }
    }

    protected async Task ExportTrayData()
    {
        try
        {
            ShowInfoMessage("Generating documentation...");
            if (canvas != null && _canvasInitialized && _jsRuntimeReady)
            {
                try
                {
                    await TrayService.ExportCanvasImageAsync(canvas, TrayModel.Name, true);
                    ShowInfoMessage("Canvas saved, generating Word document...");
                }
                catch (Exception canvasEx) when (canvasEx.Message.Contains("canceled") || canvasEx.Message.Contains("timeout"))
                {
                    ShowInfoMessage("Primary canvas export failed, trying alternative method...");
                    try
                    {
                        await ExportCanvasFallbackAsync(true);
                        ShowInfoMessage("Canvas saved (fallback method), generating Word document...");
                    }
                    catch (Exception)
                    {
                        ShowErrorMessage("Failed to save canvas image. Proceeding with document generation...");
                    }
                }
                catch (Exception)
                {
                    ShowErrorMessage("Failed to save canvas image. Proceeding with document generation...");
                }
            }
            else
            {
                ShowInfoMessage("Canvas not ready, generating Word document only...");
            }
            try
            {
                await TrayService.ExportToFileAsync(TrayModel);
                ShowSuccessMessage("Tray documentation exported successfully!");
            }
            catch (FileNotFoundException)
            {
                ShowErrorMessage($"Template file not found. Please ensure the appropriate template file exists in the wwwroot directory.");
                throw;
            }
            catch (Exception docEx)
            {
                ShowErrorMessage($"Failed to generate Word document: {docEx.Message}");
                throw;
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex, "exporting tray documentation");
        }
    }

    protected async Task ExportCanvasOnly()
    {
        // Method removed as per request
    }

    protected async Task DebugCanvasState()
    {
        // Method removed as per request
    }

    private async Task ExportCanvasFallbackAsync(bool rotate = true)
    {
        try
        {
            try
            {
                var serverResult = await JSRuntime.InvokeAsync<object>("canvasHelper.exportCanvasToServer", "canvasId", TrayModel.Name, "image/jpeg", 0.9, rotate);
                string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string imagePath = Path.Combine(wwwrootPath, "images", $"{TrayModel.Name}.jpg");
                if (File.Exists(imagePath))
                {
                    return;
                }
                else
                {
                    throw new InvalidOperationException("Server endpoint did not create the image file");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Server-side canvas export failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            throw;
        }
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
