using System.Globalization;
using System.Text;
using TraysFastUpdate.Common.Constants;
using TraysFastUpdate.Models;

namespace TraysFastUpdate.Services.Calculations;

public class SupportsWeightCalculator
{
    public SupportsWeightResult Calculate(Tray tray)
    {
        double distance = GetDistanceByType(tray.Type);
        int supportsCount = CalculateSupportsCount(tray.Length, distance);
        double totalWeight = supportsCount * TrayConstants.SupportsWeight;
        double weightLoadPerMeter = Math.Round((totalWeight / tray.Length) * 1000, 3);

        return new SupportsWeightResult
        {
            SupportsCount = supportsCount,
            TotalWeight = Math.Round(totalWeight, 3),
            WeightLoadPerMeter = weightLoadPerMeter,
            ResultSupportsCount = BuildSupportsCountResult(tray.Length, distance, supportsCount),
            ResultTotalWeight = BuildTotalWeightResult(supportsCount, totalWeight),
            ResultWeightLoadPerMeter = BuildWeightLoadPerMeterResult(totalWeight, tray.Length, weightLoadPerMeter)
        };
    }

    private static double GetDistanceByType(string trayType)
    {
        return trayType.StartsWith(TrayConstants.TrayTypes.KL) 
            ? TrayConstants.KLDistance 
            : TrayConstants.WSLDistance;
    }

    private static int CalculateSupportsCount(double length, double distance)
    {
        double calculatedCount = length / 1000 / distance + 1;
        return calculatedCount < 0.2 
            ? (int)Math.Floor(calculatedCount) 
            : (int)Math.Ceiling(calculatedCount);
    }

    private static string BuildSupportsCountResult(double length, double distance, int supportsCount)
    {
        var sb = new StringBuilder();
        sb.Append($"({Math.Round(length / 1000, 3)} * 1000) / {distance} ? ");
        sb.Append($"{Math.Round(length / 1000 / distance + 1, 3)} = {supportsCount} [pcs.]");
        return sb.ToString();
    }

    private static string BuildTotalWeightResult(int supportsCount, double totalWeight)
    {
        return $"{supportsCount} * {TrayConstants.SupportsWeight} = {Math.Round(totalWeight, 3)} [kg]";
    }

    private static string BuildWeightLoadPerMeterResult(double totalWeight, double length, double weightLoadPerMeter)
    {
        return $"{Math.Round(totalWeight, 3)} / ({Math.Round(length, 3)} * 1000) = {weightLoadPerMeter} [kg/m]";
    }
}

public record SupportsWeightResult
{
    public int SupportsCount { get; init; }
    public double TotalWeight { get; init; }
    public double WeightLoadPerMeter { get; init; }
    public string ResultSupportsCount { get; init; } = string.Empty;
    public string ResultTotalWeight { get; init; } = string.Empty;
    public string ResultWeightLoadPerMeter { get; init; } = string.Empty;
}