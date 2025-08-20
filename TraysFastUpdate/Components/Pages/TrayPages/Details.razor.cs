using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Excubo.Blazor.Canvas;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services;
using TraysFastUpdate.Services.Contracts;

namespace TraysFastUpdate.Components.Pages.TrayPages;

public partial class Details : ComponentBase
{
    [Parameter] public int Id { get; set; }
    [Parameter] public int TraysCount { get; set; }

    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected ITrayService TrayService { get; set; } = default!;
    [Inject] protected ICableService CableService { get; set; } = default!;
    [Inject] protected MudBlazor.ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected TrayDrawingService DrawingService { get; set; } = default!;
    [Inject] protected TrayNavigationService NavigationService { get; set; } = default!;

    protected Tray TrayModel { get; set; } = new();
    protected List<Cable> CablesOnTray { get; set; } = new();
    protected Dictionary<string, Dictionary<string, List<Cable>>> CableBundles { get; set; } = new();
    protected Canvas canvas = new();
    protected int canvasScale = 3;
    protected TrayNavigationData? NavigationData { get; set; }

    protected double CanvasWidth => (TrayModel.Width * canvasScale) + 100;
    protected double CanvasHeight => (TrayModel.Height * canvasScale) + 100;
    protected List<int> TrayIds => NavigationData?.TrayIds ?? new List<int>();
    protected int _currentTrayIndex => NavigationData?.CurrentTrayIndex ?? 0;

    protected override async Task OnInitializedAsync()
    {
        NavigationData = await NavigationService.InitializeNavigationAsync(Id);
        Id = NavigationData.CurrentTrayId;
        TraysCount = NavigationData.TotalTraysCount;

        await LoadTrayDataAsync();
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
            await DrawTray();
        }
    }

    private async Task LoadTrayDataAsync()
    {
        TrayModel = await TrayService.GetTrayAsync(Id);
        await TrayService.UpdateTrayAsync(TrayModel);
        CablesOnTray = await CableService.GetCablesOnTrayAsync(TrayModel);
        CableBundles = await CableService.GetCablesBundlesOnTrayAsync(TrayModel);
    }

    protected async Task HandleTrayPropertyChange()
    {
        await InvokeAsync(StateHasChanged);
        await DrawTray();
    }

    protected async Task HandleValidSubmit()
    {
        try
        {
            await TrayService.UpdateTrayAsync(TrayModel);
            Snackbar.Add("Tray details updated successfully!", MudBlazor.Severity.Success);
            await HandleTrayPropertyChange();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error updating tray details: {ex.Message}", MudBlazor.Severity.Error);
        }
    }

    protected async Task DrawTray()
    {
        await Task.Delay(100);
        try
        {
            await DrawingService.DrawTrayLayoutAsync(canvas, TrayModel, CablesOnTray, CableBundles, canvasScale);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during canvas drawing: {ex.Message}");
        }
    }

    protected async Task ExportTrayData()
    {
        try
        {
            await TrayService.ExportToFileAsync(TrayModel);
            Snackbar.Add("Tray data exported successfully!", MudBlazor.Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error exporting tray data: {ex.Message}", MudBlazor.Severity.Error);
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
}
