using System.Text;
using TraysFastUpdate.Common.Constants;
using TraysFastUpdate.Models;

namespace TraysFastUpdate.Services.Calculations;

public class TrayOwnWeightCalculator
{
    public TrayOwnWeightResult Calculate(Tray tray)
    {
        double weightLoadPerMeter = Math.Round(tray.Weight + (tray.SupportsWeightLoadPerMeter ?? 0), 3);
        double ownWeightLoad = Math.Round(weightLoadPerMeter * tray.Length / 1000, 3);

        return new TrayOwnWeightResult
        {
            WeightLoadPerMeter = weightLoadPerMeter,
            OwnWeightLoad = ownWeightLoad,
            ResultWeightLoadPerMeter = BuildWeightLoadPerMeterResult(tray.Weight, tray.SupportsWeightLoadPerMeter ?? 0, weightLoadPerMeter),
            ResultOwnWeightLoad = BuildOwnWeightLoadResult(weightLoadPerMeter, tray.Length, ownWeightLoad)
        };
    }

    private static string BuildWeightLoadPerMeterResult(double trayWeight, double supportsWeight, double total)
    {
        return $"{Math.Round(trayWeight, 3)} + {Math.Round(supportsWeight, 3)} = {Math.Round(total, 3)} [kg/m]";
    }

    private static string BuildOwnWeightLoadResult(double weightPerMeter, double length, double totalWeight)
    {
        return $"{Math.Round(weightPerMeter, 3)} * ({Math.Round(length, 3)} / 1000) = {Math.Round(totalWeight, 3)} [kg]";
    }
}

public class CablesWeightCalculator
{
    public CablesWeightResult Calculate(Tray tray, List<Cable> cablesOnTray)
    {
        if (cablesOnTray.Count == 0)
        {
            return new CablesWeightResult
            {
                WeightPerMeter = 0,
                WeightLoad = 0,
                ResultWeightPerMeter = "No cables on this tray",
                ResultWeightLoad = "No cables on this tray"
            };
        }

        var cables = new List<Cable>(cablesOnTray);
        
        // Add grounding cable if applicable
        if (ShouldAddGroundingCable(tray.Purpose))
        {
            cables.Add(CreateGroundingCable());
        }

        double totalWeight = cables.Sum(c => c.CableType.Weight);
        double weightPerMeter = Math.Round(totalWeight, 3);
        double weightLoad = Math.Round(weightPerMeter * tray.Length / 1000, 3);

        return new CablesWeightResult
        {
            WeightPerMeter = weightPerMeter,
            WeightLoad = weightLoad,
            ResultWeightPerMeter = BuildWeightPerMeterResult(cables),
            ResultWeightLoad = BuildWeightLoadResult(weightPerMeter, tray.Length, weightLoad)
        };
    }

    private static bool ShouldAddGroundingCable(string purpose)
    {
        return purpose == TrayConstants.TrayPurposes.TypeB || 
               purpose == TrayConstants.TrayPurposes.TypeBC;
    }

    private static Cable CreateGroundingCable()
    {
        return new Cable
        {
            CableType = new CableType
            {
                Diameter = TrayConstants.GroundingCableDiameter,
                Weight = TrayConstants.GroundingCableWeight
            }
        };
    }

    private static string BuildWeightPerMeterResult(List<Cable> cables)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < cables.Count - 1; i++)
        {
            sb.Append($"{cables[i].CableType.Weight} + ");
        }
        sb.Append($"{cables[cables.Count - 1].CableType.Weight} = ");
        sb.Append($"{Math.Round(cables.Sum(c => c.CableType.Weight), 3)} [kg/m]");
        return sb.ToString();
    }

    private static string BuildWeightLoadResult(double weightPerMeter, double length, double weightLoad)
    {
        return $"{Math.Round(weightPerMeter, 3)} * ({Math.Round(length, 3)} / 1000) = {Math.Round(weightLoad, 3)} [kg]";
    }
}

public class TotalWeightCalculator
{
    public TotalWeightResult Calculate(Tray tray)
    {
        if (tray.ResultCablesWeightPerMeter == "No cables on this tray")
        {
            return new TotalWeightResult
            {
                WeightLoadPerMeter = tray.TrayWeightLoadPerMeter ?? 0,
                WeightLoad = tray.TrayOwnWeightLoad ?? 0,
                ResultWeightLoadPerMeter = tray.ResultTrayWeightLoadPerMeter ?? string.Empty,
                ResultWeightLoad = tray.ResultTrayOwnWeightLoad ?? string.Empty
            };
        }

        double weightLoadPerMeter = Math.Round((tray.TrayWeightLoadPerMeter ?? 0) + (tray.CablesWeightPerMeter ?? 0), 3);
        double weightLoad = Math.Round((tray.TrayOwnWeightLoad ?? 0) + (tray.CablesWeightLoad ?? 0), 3);

        return new TotalWeightResult
        {
            WeightLoadPerMeter = weightLoadPerMeter,
            WeightLoad = weightLoad,
            ResultWeightLoadPerMeter = BuildWeightLoadPerMeterResult(tray.TrayWeightLoadPerMeter ?? 0, tray.CablesWeightPerMeter ?? 0, weightLoadPerMeter),
            ResultWeightLoad = BuildWeightLoadResult(tray.TrayOwnWeightLoad ?? 0, tray.CablesWeightLoad ?? 0, weightLoad)
        };
    }

    private static string BuildWeightLoadPerMeterResult(double trayWeight, double cablesWeight, double total)
    {
        return $"{Math.Round(trayWeight, 3)} + {Math.Round(cablesWeight, 3)} = {Math.Round(total, 3)} [kg/m]";
    }

    private static string BuildWeightLoadResult(double trayWeight, double cablesWeight, double total)
    {
        return $"{Math.Round(trayWeight, 3)} + {Math.Round(cablesWeight, 3)} = {Math.Round(total, 3)} [kg]";
    }
}

public record TrayOwnWeightResult
{
    public double WeightLoadPerMeter { get; init; }
    public double OwnWeightLoad { get; init; }
    public string ResultWeightLoadPerMeter { get; init; } = string.Empty;
    public string ResultOwnWeightLoad { get; init; } = string.Empty;
}

public record CablesWeightResult
{
    public double WeightPerMeter { get; init; }
    public double WeightLoad { get; init; }
    public string ResultWeightPerMeter { get; init; } = string.Empty;
    public string ResultWeightLoad { get; init; } = string.Empty;
}

public record TotalWeightResult
{
    public double WeightLoadPerMeter { get; init; }
    public double WeightLoad { get; init; }
    public string ResultWeightLoadPerMeter { get; init; } = string.Empty;
    public string ResultWeightLoad { get; init; } = string.Empty;
}