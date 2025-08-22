using System.Text;
using TraysFastUpdate.Common.Constants;
using TraysFastUpdate.Models;

namespace TraysFastUpdate.Services.Calculations;

public class SpaceCalculator
{
    public SpaceUtilizationResult CalculateSpaceUtilization(Tray tray, Dictionary<string, Dictionary<string, List<Cable>>> bundles)
    {
        if (tray.Purpose == TrayConstants.TrayPurposes.TypeA)
        {
            return new SpaceUtilizationResult
            {
                SpaceOccupied = 0,
                SpaceAvailable = 100,
                ResultSpaceOccupied = "N/A",
                ResultSpaceAvailable = "N/A"
            };
        }

        double bottomRow = 0;
        var cablesBottomRow = new List<Cable>();

        foreach (var bundle in bundles)
        {
            bottomRow += ProcessBundle(bundle, tray.Height, cablesBottomRow);
        }

        double spaceAvailable = Math.Round(100 - (bottomRow / tray.Width * 100), 2);
        var groupedByDiameterCables = cablesBottomRow.GroupBy(x => x.CableType.Diameter).ToList();

        return new SpaceUtilizationResult
        {
            SpaceOccupied = bottomRow,
            SpaceAvailable = spaceAvailable,
            ResultSpaceOccupied = BuildSpaceOccupiedResult(groupedByDiameterCables, bottomRow),
            ResultSpaceAvailable = BuildSpaceAvailableResult(bottomRow, tray.Width, spaceAvailable)
        };
    }

    private double ProcessBundle(KeyValuePair<string, Dictionary<string, List<Cable>>> bundle, double trayHeight, List<Cable> cablesBottomRow)
    {
        double bottomRow = 0;

        foreach (var sortedBundle in bundle.Value.OrderByDescending(x => x.Value[0].CableType.Diameter))
        {
            var (rows, columns) = CalculateRowsAndColumns(trayHeight - TrayConstants.CProfileHeight, 1, sortedBundle.Value, bundle.Key);
            bottomRow += ProcessBundleByType(bundle.Key, sortedBundle, rows, cablesBottomRow);
        }

        return bottomRow;
    }

    private double ProcessBundleByType(string bundleType, KeyValuePair<string, List<Cable>> sortedBundle, int rows, List<Cable> cablesBottomRow)
    {
        return bundleType switch
        {
            TrayConstants.CablePurposes.Power => ProcessPowerBundle(sortedBundle, rows, cablesBottomRow),
            TrayConstants.CablePurposes.Control => ProcessControlBundle(sortedBundle, rows, cablesBottomRow),
            TrayConstants.CablePurposes.VFD => ProcessVFDBundle(sortedBundle, rows, cablesBottomRow),
            _ => 0
        };
    }

    private double ProcessPowerBundle(KeyValuePair<string, List<Cable>> sortedBundle, int rows, List<Cable> cablesBottomRow)
    {
        double bottomRow = 0;
        var sortedCables = sortedBundle.Value.OrderByDescending(x => x.CableType.Diameter).ToList();

        if (IsHexagonalPackingBundle(sortedBundle.Key))
        {
            foreach (var cable in sortedCables)
            {
                int cableIndex = sortedCables.IndexOf(cable);
                if (cableIndex != 0 && cableIndex % 2 == 0 && cable.CableType.Diameter <= 45)
                    continue;

                bottomRow += cable.CableType.Diameter + TrayConstants.Spacing;
                cablesBottomRow.Add(cable);
            }
        }
        else
        {
            ProcessVerticalStacking(sortedCables, rows, cablesBottomRow, ref bottomRow);
        }

        return bottomRow;
    }

    private double ProcessControlBundle(KeyValuePair<string, List<Cable>> sortedBundle, int rows, List<Cable> cablesBottomRow)
    {
        double bottomRow = 0;
        var sortedCables = sortedBundle.Value.OrderByDescending(x => x.CableType.Diameter).ToList();
        ProcessVerticalStacking(sortedCables, rows, cablesBottomRow, ref bottomRow);
        return bottomRow;
    }

    private double ProcessVFDBundle(KeyValuePair<string, List<Cable>> sortedBundle, int rows, List<Cable> cablesBottomRow)
    {
        double bottomRow = 0;

        if (IsGroupedVFDBundle(sortedBundle.Key))
        {
            var groupByToLocation = sortedBundle.Value.GroupBy(x => x.ToLocation).ToList();
            foreach (var cableGroup in groupByToLocation)
            {
                cableGroup.ToList().ForEach(cable =>
                {
                    int cableIndex = cableGroup.ToList().IndexOf(cable);
                    if (cableIndex != 0 && cableIndex % 2 == 0 && cable.CableType.Diameter <= 45)
                        return;

                    bottomRow += cable.CableType.Diameter + TrayConstants.Spacing;
                    cablesBottomRow.Add(cable);
                });
            }
        }
        else
        {
            var sortedCables = sortedBundle.Value.OrderByDescending(x => x.CableType.Diameter).ToList();
            ProcessVerticalStacking(sortedCables, rows, cablesBottomRow, ref bottomRow);
        }

        return bottomRow;
    }

    private void ProcessVerticalStacking(List<Cable> sortedCables, int rows, List<Cable> cablesBottomRow, ref double bottomRow)
    {
        int row = 0;
        foreach (var cable in sortedCables)
        {
            if (row == 0)
            {
                bottomRow += cable.CableType.Diameter + TrayConstants.Spacing;
                cablesBottomRow.Add(cable);
            }
            row++;
            if (row == rows)
                row = 0;
        }
    }

    private static bool IsHexagonalPackingBundle(string bundleKey)
    {
        return bundleKey == "40.1-44.5" || bundleKey == "44.6 - 60";
    }

    private static bool IsGroupedVFDBundle(string bundleKey)
    {
        return bundleKey == "30.1-42" || bundleKey == "42.1-60";
    }

    private static (int rows, int columns) CalculateRowsAndColumns(double trayHeight, int spacing, List<Cable> bundle, string purpose)
    {
        int rows = 0;
        int columns = 0;
        double diameter = bundle.Max(x => x.CableType.Diameter);

        switch (purpose)
        {
            case TrayConstants.CablePurposes.Power:
                rows = Math.Min((int)Math.Floor(trayHeight / diameter), 2);
                columns = (int)Math.Floor((double)bundle.Count / rows);
                break;
            case TrayConstants.CablePurposes.Control:
                rows = Math.Min((int)Math.Floor(trayHeight / diameter), 2);
                columns = Math.Min((int)Math.Ceiling((double)bundle.Count / rows), 20);
                break;
            case TrayConstants.CablePurposes.VFD:
                rows = Math.Min((int)Math.Floor(trayHeight / diameter), 2);
                columns = (int)Math.Floor((double)bundle.Count / rows);
                break;
        }

        if (bundle.Count == 2)
            return (1, 2);

        if (rows > columns)
        {
            rows = (int)Math.Floor(Math.Ceiling(Math.Sqrt(bundle.Count)));
            columns = (int)Math.Floor(Math.Ceiling(Math.Sqrt(bundle.Count)));
        }

        return (rows, columns);
    }

    private static string BuildSpaceOccupiedResult(List<IGrouping<double, Cable>> groupedByDiameterCables, double bottomRow)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < groupedByDiameterCables.Count - 1; i++)
        {
            sb.Append($"({groupedByDiameterCables[i].Key} * {groupedByDiameterCables[i].Count()}) + {TrayConstants.Spacing} * {groupedByDiameterCables[i].Count()} + ");
        }
        sb.Append($"({groupedByDiameterCables[groupedByDiameterCables.Count - 1].Key} * {groupedByDiameterCables[groupedByDiameterCables.Count - 1].Count()}) + {TrayConstants.Spacing} * {groupedByDiameterCables[groupedByDiameterCables.Count - 1].Count()} = {Math.Round(bottomRow, 3)} [mm]");
        return sb.ToString();
    }

    private static string BuildSpaceAvailableResult(double bottomRow, double trayWidth, double spaceAvailable)
    {
        return $"100 - ({Math.Round(bottomRow, 3)} / {Math.Round(trayWidth, 3)} * 100) = {Math.Round(spaceAvailable, 2)} [%]";
    }
}

public record SpaceUtilizationResult
{
    public double SpaceOccupied { get; init; }
    public double SpaceAvailable { get; init; }
    public string ResultSpaceOccupied { get; init; } = string.Empty;
    public string ResultSpaceAvailable { get; init; } = string.Empty;
}