using TraysFastUpdate.Common.Constants;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Contracts;

namespace TraysFastUpdate.Services.Calculations;

public interface ITrayCalculationService
{
    Task CalculateWeightsAsync(Tray tray);
    Task CalculateSpaceUtilizationAsync(Tray tray);
}

public class TrayCalculationService : ITrayCalculationService
{
    private readonly ICableService _cableService;

    public TrayCalculationService(ICableService cableService)
    {
        _cableService = cableService;
    }

    public async Task CalculateWeightsAsync(Tray tray)
    {
        await CalculateSupportsWeightAsync(tray);
        await CalculateTrayOwnWeightAsync(tray);
        await CalculateCablesWeightAsync(tray);
        await CalculateTotalWeightAsync(tray);
    }

    public async Task CalculateSpaceUtilizationAsync(Tray tray)
    {
        if (string.IsNullOrEmpty(tray.ResultCablesWeightPerMeter) || tray.ResultCablesWeightPerMeter == "No cables on this tray")
        {
            tray.ResultSpaceOccupied = "N/A";
            tray.ResultSpaceAvailable = "N/A";
            tray.SpaceOccupied = 0;
            tray.SpaceAvailable = 100;
            return;
        }

        var bundles = await _cableService.GetCablesBundlesOnTrayAsync(tray);
        var spaceCalculator = new SpaceCalculator();
        var result = spaceCalculator.CalculateSpaceUtilization(tray, bundles);
        
        tray.SpaceOccupied = result.SpaceOccupied;
        tray.SpaceAvailable = result.SpaceAvailable;
        tray.ResultSpaceOccupied = result.ResultSpaceOccupied;
        tray.ResultSpaceAvailable = result.ResultSpaceAvailable;
    }

    private async Task CalculateSupportsWeightAsync(Tray tray)
    {
        var calculator = new SupportsWeightCalculator();
        var result = calculator.Calculate(tray);
        
        tray.SupportsCount = result.SupportsCount;
        tray.SupportsWeightLoadPerMeter = result.WeightLoadPerMeter;
        tray.SupportsTotalWeight = result.TotalWeight;
        tray.ResultSupportsCount = result.ResultSupportsCount;
        tray.ResultSupportsWeightLoadPerMeter = result.ResultWeightLoadPerMeter;
        tray.ResultSupportsTotalWeight = result.ResultTotalWeight;
    }

    private async Task CalculateTrayOwnWeightAsync(Tray tray)
    {
        var calculator = new TrayOwnWeightCalculator();
        var result = calculator.Calculate(tray);
        
        tray.TrayWeightLoadPerMeter = result.WeightLoadPerMeter;
        tray.TrayOwnWeightLoad = result.OwnWeightLoad;
        tray.ResultTrayWeightLoadPerMeter = result.ResultWeightLoadPerMeter;
        tray.ResultTrayOwnWeightLoad = result.ResultOwnWeightLoad;
    }

    private async Task CalculateCablesWeightAsync(Tray tray)
    {
        var cablesOnTray = await _cableService.GetCablesOnTrayAsync(tray);
        var calculator = new CablesWeightCalculator();
        var result = calculator.Calculate(tray, cablesOnTray);
        
        tray.CablesWeightPerMeter = result.WeightPerMeter;
        tray.CablesWeightLoad = result.WeightLoad;
        tray.ResultCablesWeightPerMeter = result.ResultWeightPerMeter;
        tray.ResultCablesWeightLoad = result.ResultWeightLoad;
    }

    private async Task CalculateTotalWeightAsync(Tray tray)
    {
        var calculator = new TotalWeightCalculator();
        var result = calculator.Calculate(tray);
        
        tray.TotalWeightLoadPerMeter = result.WeightLoadPerMeter;
        tray.TotalWeightLoad = result.WeightLoad;
        tray.ResultTotalWeightLoadPerMeter = result.ResultWeightLoadPerMeter;
        tray.ResultTotalWeightLoad = result.ResultWeightLoad;
    }
}