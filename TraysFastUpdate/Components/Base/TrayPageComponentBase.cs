using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace TraysFastUpdate.Components.Base;

public abstract class TrayPageComponentBase : ComponentBase
{
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;

    protected bool IsLoading { get; set; } = true;
    protected string? ErrorMessage { get; set; }

    protected virtual void ShowSuccessMessage(string message)
    {
        Snackbar.Add(message, Severity.Success);
    }

    protected virtual void ShowErrorMessage(string message)
    {
        Snackbar.Add(message, Severity.Error);
        ErrorMessage = message;
    }

    protected virtual void ShowInfoMessage(string message)
    {
        Snackbar.Add(message, Severity.Info);
    }

    protected virtual void NavigateToTrays()
    {
        NavigationManager.NavigateTo("/trays");
    }

    protected virtual async Task HandleExceptionAsync(Exception ex, string operation)
    {
        Console.WriteLine($"Error during {operation}: {ex.Message}");
        ShowErrorMessage($"Error during {operation}: {ex.Message}");
        await InvokeAsync(StateHasChanged);
    }
}