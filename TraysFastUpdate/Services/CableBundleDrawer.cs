using Excubo.Blazor.Canvas.Contexts;
using Excubo.Blazor.Canvas;
using TraysFastUpdate.Models;

namespace TraysFastUpdate.Services;

public class CableBundleDrawer
{
    private const int CProfileHeight = 15;

    public async Task<(double leftStartX, double bottomStartY)> DrawPowerBundlesAsync(Context2D ctx, TrayDrawingData data, 
        Dictionary<string, List<Cable>> bundles, double leftStartX, double bottomStartY, double spacing)
    {
        var sortedBundles = bundles.OrderByDescending(x => x.Value[0].CableType.Diameter).ToList();

        foreach (var sortedBundle in sortedBundles)
        {
            var (rows, columns) = CalculateRowsAndColumns(data.Tray.Height - CProfileHeight, 1, sortedBundle.Value, "Power");
            var sortedCables = sortedBundle.Value.OrderByDescending(x => x.CableType.Diameter).ToList();
            var biggestCableInBundle = sortedBundle.Value.OrderByDescending(x => x.CableType.Diameter).First();

            if (sortedBundle.Key == "40.1-45" || sortedBundle.Key == "45-60")
            {
                (leftStartX, bottomStartY) = await DrawHexagonalPackingAsync(ctx, data, sortedCables, leftStartX, bottomStartY, spacing);
            }
            else
            {
                (leftStartX, bottomStartY) = await DrawVerticalStackingAsync(ctx, data, sortedCables, leftStartX, bottomStartY, spacing, rows, data.BottomRowPowerCables);
            }

            if (sortedBundles.IndexOf(sortedBundle) != sortedBundles.Count - 1)
            {
                data.BottomRowPowerCables.Add(biggestCableInBundle);
                data.BottomRowPowerCables.Add(biggestCableInBundle);
            }

            leftStartX = 50 + spacing + data.BottomRowPowerCables.Sum(x => x.CableType.Diameter + 1) * data.CanvasScale;
            bottomStartY = 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale;
        }

        return (leftStartX, bottomStartY);
    }

    public async Task<(double rightStartX, double bottomStartY)> DrawControlBundlesAsync(Context2D ctx, TrayDrawingData data, 
        Dictionary<string, List<Cable>> bundles, double rightStartX, double bottomStartY, double spacing)
    {
        var sortedBundles = bundles.OrderByDescending(x => x.Value[0].CableType.Diameter).ToList();

        foreach (var sortedBundle in sortedBundles)
        {
            var (rows, columns) = CalculateRowsAndColumns(data.Tray.Height - CProfileHeight, 1, sortedBundle.Value, "Control");
            var sortedCables = sortedBundle.Value.OrderByDescending(x => x.CableType.Diameter).ToList();
            var biggestCableInBundle = sortedBundle.Value.OrderByDescending(x => x.CableType.Diameter).First();

            rightStartX = await DrawVerticalStackingFromRightAsync(ctx, data, sortedCables, rightStartX, bottomStartY, spacing, rows, data.BottomRowControlCables);

            if (sortedBundles.IndexOf(sortedBundle) != sortedBundles.Count - 1)
            {
                data.BottomRowControlCables.Add(biggestCableInBundle);
                data.BottomRowControlCables.Add(biggestCableInBundle);
            }

            rightStartX = 50 + data.Tray.Width * data.CanvasScale - spacing - data.BottomRowControlCables.Sum(x => x.CableType.Diameter + 1) * data.CanvasScale;
            bottomStartY = 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale;
        }

        return (rightStartX, bottomStartY);
    }

    public async Task<(double leftStartX, double bottomStartY)> DrawMvBundlesAsync(Context2D ctx, TrayDrawingData data, 
        Dictionary<string, List<Cable>> bundles, double leftStartX, double bottomStartY, double spacing)
    {
        var sortedBundles = bundles.OrderByDescending(x => x.Value[0].CableType.Diameter).ToList();

        foreach (var sortedBundle in sortedBundles)
        {
            (leftStartX, bottomStartY) = await DrawPhaseRotationBundlesAsync(ctx, data, sortedBundle.Value, leftStartX, bottomStartY, spacing);
        }

        return (leftStartX, bottomStartY);
    }

    public async Task<(double rightStartX, double bottomStartY)> DrawVfdBundlesAsync(Context2D ctx, TrayDrawingData data, 
        Dictionary<string, List<Cable>> bundles, double rightStartX, double bottomStartY, double spacing)
    {
        var sortedBundles = bundles.OrderByDescending(x => x.Value[0].CableType.Diameter).ToList();

        foreach (var sortedBundle in sortedBundles)
        {
            var (rows, columns) = CalculateRowsAndColumns(data.Tray.Height - CProfileHeight, 1, sortedBundle.Value, "VFD");

            if (sortedBundle.Key == "30.1-40" || sortedBundle.Key == "40.1-45")
            {
                rightStartX = await DrawGroupedVfdCablesAsync(ctx, data, sortedBundle.Value, rightStartX, bottomStartY, spacing, sortedBundles, sortedBundle);
            }
            else
            {
                rightStartX = await DrawStandardVfdCablesAsync(ctx, data, sortedBundle.Value, rightStartX, bottomStartY, spacing, rows, sortedBundles, sortedBundle);
            }
        }

        rightStartX = 50 + data.Tray.Width * data.CanvasScale - spacing - data.BottomRowVFDCables.Sum(x => x.CableType.Diameter + 1) * data.CanvasScale;
        bottomStartY = 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale;

        return (rightStartX, bottomStartY);
    }

    private async Task<(double leftStartX, double bottomStartY)> DrawHexagonalPackingAsync(Context2D ctx, TrayDrawingData data, List<Cable> sortedCables, 
        double leftStartX, double bottomStartY, double spacing)
    {
        int row = 0;
        foreach (var cable in sortedCables)
        {
            int cableIndex = sortedCables.IndexOf(cable);
            if (cableIndex != 0 && cableIndex % 2 == 0 && cable.CableType.Diameter <= 45 && data.Tray.Height - CProfileHeight > 45)
            {
                bottomStartY -= ((cable.CableType.Diameter * data.CanvasScale) / 2) * (Math.Sqrt(3) / 2) + 
                               ((cable.CableType.Diameter * data.CanvasScale) / 2) - spacing * 2;
                leftStartX = 50 + spacing + data.BottomRowPowerCables.Sum(x => x.CableType.Diameter + 1) * data.CanvasScale - 
                           (cable.CableType.Diameter * data.CanvasScale + spacing) * 1.5;
                row = 1;
            }

            await DrawCableAsync(ctx, data, cable, leftStartX, bottomStartY);
            bottomStartY = 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale;

            if (row == 0)
            {
                data.BottomRowPowerCables.Add(cable);
                leftStartX = 50 + spacing + data.BottomRowPowerCables.Sum(x => x.CableType.Diameter + 1) * data.CanvasScale;
            }

            if (row == 1)
            {
                row = 0;
                leftStartX = 50 + spacing + data.BottomRowPowerCables.Sum(x => x.CableType.Diameter + 1) * data.CanvasScale;
                bottomStartY = 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale;
            }
        }

        return (leftStartX, bottomStartY);
    }

    private async Task<(double leftStartX, double bottomStartY)> DrawVerticalStackingAsync(Context2D ctx, TrayDrawingData data, List<Cable> sortedCables, 
        double leftStartX, double bottomStartY, double spacing, int rows, List<Cable> bottomRowCables)
    {
        int row = 0;
        int column = 0;

        foreach (var cable in sortedCables)
        {
            await DrawCableAsync(ctx, data, cable, leftStartX, bottomStartY);
            bottomStartY -= cable.CableType.Diameter * data.CanvasScale + spacing;

            if (row == 0)
            {
                bottomRowCables.Add(cable);
            }

            row++;
            if (row == rows)
            {
                row = 0;
                column++;
                leftStartX = 50 + spacing + bottomRowCables.Sum(x => x.CableType.Diameter + 1) * data.CanvasScale;
                bottomStartY = 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale;
            }
        }

        return (leftStartX, bottomStartY);
    }

    private async Task<double> DrawVerticalStackingFromRightAsync(Context2D ctx, TrayDrawingData data, List<Cable> sortedCables, 
        double rightStartX, double bottomStartY, double spacing, int rows, List<Cable> bottomRowCables)
    {
        int row = 0;

        foreach (var cable in sortedCables)
        {
            double radius = cable.CableType.Diameter / 2 * data.CanvasScale;
            await ctx.BeginPathAsync();
            await ctx.ArcAsync(rightStartX - radius, bottomStartY - radius, radius, 0, Math.PI * 2);
            await ctx.ClosePathAsync();
            await ctx.StrokeAsync();
            
            await DrawCableNumberAsync(ctx, data, cable, rightStartX - radius, bottomStartY - radius);
            bottomStartY -= cable.CableType.Diameter * data.CanvasScale + spacing;

            if (row == 0)
            {
                bottomRowCables.Add(cable);
            }

            row++;
            if (row == rows)
            {
                row = 0;
                rightStartX = 50 + data.Tray.Width * data.CanvasScale - spacing - bottomRowCables.Sum(x => x.CableType.Diameter + 1) * data.CanvasScale;
                bottomStartY = 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale;
            }
        }

        return rightStartX;
    }

    private async Task<(double leftStartX, double bottomStartY)> DrawPhaseRotationBundlesAsync(Context2D ctx, TrayDrawingData data, List<Cable> bundleCables, 
        double leftStartX, double bottomStartY, double spacing)
    {
        var sortedCables = bundleCables.OrderByDescending(x => x.CableType.Diameter).ToList();
        var phaseRotations = ApplyPhaseRotation(sortedCables);

        int row = 0;
        int cableIndex = 2;
        double leftStartBottom = leftStartX;
        double leftStartTop = leftStartX + (phaseRotations.ElementAt(0).CableType.Diameter / 2 + 0.5) * data.CanvasScale;

        foreach (var cable in phaseRotations)
        {
            if (cableIndex == phaseRotations.IndexOf(cable))
            {
                bottomStartY -= ((cable.CableType.Diameter * data.CanvasScale) / 2) * (Math.Sqrt(3) / 2) + 
                               ((cable.CableType.Diameter * data.CanvasScale) / 2 - spacing * 2);
                leftStartX = leftStartTop;
                row = 1;
                cableIndex += 3;
            }

            await DrawCableAsync(ctx, data, cable, leftStartX, bottomStartY);
            bottomStartY = 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale;

            if (row == 0)
            {
                data.BottomRowPowerCables.Add(cable);
                leftStartX += (cable.CableType.Diameter + 1) * data.CanvasScale;
                leftStartBottom = leftStartX;
            }

            if (row == 1)
            {
                row = 0;
                leftStartBottom += (cable.CableType.Diameter + 1) * data.CanvasScale * 2;
                leftStartX = leftStartBottom;
                leftStartTop += (cable.CableType.Diameter + 1) * data.CanvasScale * 4;
                bottomStartY = 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale;
            }
        }

        return (leftStartX, bottomStartY);
    }

    private async Task<double> DrawGroupedVfdCablesAsync(Context2D ctx, TrayDrawingData data, List<Cable> bundleCables, 
        double rightStartX, double bottomStartY, double spacing, 
        List<KeyValuePair<string, List<Cable>>> sortedBundles, KeyValuePair<string, List<Cable>> sortedBundle)
    {
        var groupedByToLocationCables = bundleCables.GroupBy(x => x.ToLocation).ToList();

        foreach (var group in groupedByToLocationCables)
        {
            var sortedCables = group.OrderByDescending(x => x.CableType.Diameter).ToList();
            double rightStartBottom = bottomStartY;
            double rightStartTop = rightStartX - (sortedCables.ElementAt(0).CableType.Diameter / 2 + 0.5) * data.CanvasScale;
            int row = 0;

            foreach (var cable in sortedCables)
            {
                if (sortedCables.IndexOf(cable) == 2 && cable.CableType.Diameter <= 45)
                {
                    bottomStartY -= ((cable.CableType.Diameter * data.CanvasScale) / 2) * (Math.Sqrt(3) / 2) + 
                                   ((cable.CableType.Diameter * data.CanvasScale) / 2 - spacing * 2);
                    rightStartX = rightStartTop;
                    row = 1;
                }

                double radius = cable.CableType.Diameter / 2 * data.CanvasScale;
                await ctx.BeginPathAsync();
                await ctx.ArcAsync(rightStartX - radius, bottomStartY - radius, radius, 0, Math.PI * 2);
                await ctx.ClosePathAsync();
                await ctx.StrokeAsync();
                
                await DrawCableNumberAsync(ctx, data, cable, rightStartX - radius, bottomStartY - radius);

                if (row == 0)
                {
                    data.BottomRowVFDCables.Add(cable);
                    rightStartX -= (cable.CableType.Diameter + 1) * data.CanvasScale;
                }
                if (row == 1)
                {
                    row = 0;
                    bottomStartY = rightStartBottom;
                    rightStartX -= ((cable.CableType.Diameter * data.CanvasScale) * 3.5);
                }
            }
            
            if (sortedBundles.IndexOf(sortedBundle) != sortedBundles.Count - 1)
            {
                data.BottomRowVFDCables.Add(group.First());
                data.BottomRowVFDCables.Add(group.First());
            }
        }

        return rightStartX;
    }

    private async Task<double> DrawStandardVfdCablesAsync(Context2D ctx, TrayDrawingData data, List<Cable> bundleCables, 
        double rightStartX, double bottomStartY, double spacing, int rows,
        List<KeyValuePair<string, List<Cable>>> sortedBundles, KeyValuePair<string, List<Cable>> sortedBundle)
    {
        var sortedCables = bundleCables.OrderByDescending(x => x.CableType.Diameter).ToList();
        var biggestCableInBundle = bundleCables.OrderByDescending(x => x.CableType.Diameter).First();
        int row = 0;
        int column = 0;

        foreach (var cable in sortedCables)
        {
            double radius = cable.CableType.Diameter / 2 * data.CanvasScale;
            await ctx.BeginPathAsync();
            await ctx.ArcAsync(rightStartX - radius, bottomStartY - radius, radius, 0, Math.PI * 2);
            await ctx.ClosePathAsync();
            await ctx.StrokeAsync();
            
            await DrawCableNumberAsync(ctx, data, cable, rightStartX - radius, bottomStartY - radius);
            bottomStartY -= cable.CableType.Diameter * data.CanvasScale + spacing;

            if (row == 0)
            {
                data.BottomRowVFDCables.Add(cable);
            }

            row++;
            if (row == rows)
            {
                row = 0;
                column++;
                rightStartX -= cable.CableType.Diameter * data.CanvasScale + spacing;
                bottomStartY = 50 + (data.Tray.Height - CProfileHeight) * data.CanvasScale;
            }
        }
        
        if (sortedBundles.IndexOf(sortedBundle) != sortedBundles.Count - 1)
        {
            data.BottomRowVFDCables.Add(biggestCableInBundle);
            data.BottomRowVFDCables.Add(biggestCableInBundle);
        }

        return rightStartX;
    }

    private async Task DrawCableAsync(Context2D ctx, TrayDrawingData data, Cable cable, double x, double y)
    {
        double radius = cable.CableType.Diameter / 2 * data.CanvasScale;
        await ctx.BeginPathAsync();
        await ctx.ArcAsync(x + radius, y - radius, radius, 0, Math.PI * 2);
        await ctx.ClosePathAsync();
        await ctx.StrokeAsync();
        
        await DrawCableNumberAsync(ctx, data, cable, x + radius, y - radius);
    }

    private async Task DrawCableNumberAsync(Context2D ctx, TrayDrawingData data, Cable cable, double x, double y)
    {
        await ctx.FontAsync("12px Arial");
        await ctx.FillStyleAsync("black");
        await ctx.TextAlignAsync(TextAlign.Center);
        await ctx.TextBaseLineAsync(TextBaseLine.Middle);
        int cableNumber = data.CablesOnTray.IndexOf(cable) + 1;
        await ctx.FillTextAsync(cableNumber.ToString(), x, y);
    }

    private List<Cable> ApplyPhaseRotation(List<Cable> sortedCables)
    {
        var phaseRotations = new List<Cable>();
        int blockSize = 6;
        
        for (int i = 0; i < sortedCables.Count; i += blockSize)
        {
            var block = sortedCables.Skip(i).Take(blockSize).ToList();
            var firstHalf = block.Take(blockSize / 2).Skip(1).Concat(block.Take(1));
            var secondHalf = block.Skip(blockSize / 2).Reverse();
            phaseRotations.AddRange(firstHalf.Concat(secondHalf));
        }
        
        return phaseRotations;
    }

    public static (int rows, int columns) CalculateRowsAndColumns(double trayHeight, int spacing, List<Cable> bundle, string purpose)
    {
        int rows = 0;
        int columns = 0;
        double diameter = bundle.Max(x => x.CableType.Diameter);

        if (purpose == "Power")
        {
            rows = Math.Min((int)Math.Floor(trayHeight / diameter), 2);
            columns = (int)Math.Floor((double)bundle.Count / rows);
        }
        else if (purpose == "Control")
        {
            rows = Math.Min((int)Math.Floor(trayHeight / diameter), 2);
            columns = Math.Min((int)Math.Ceiling((double)bundle.Count / rows), 20);
        }
        else if (purpose == "VFD")
        {
            rows = Math.Min((int)Math.Floor(trayHeight / diameter), 2);
            columns = (int)Math.Floor((double)bundle.Count / rows);
        }

        if (bundle.Count == 2)
        {
            return (1, 2);
        }

        if (rows > columns)
        {
            rows = (int)Math.Floor(Math.Ceiling(Math.Sqrt(bundle.Count)));
            columns = (int)Math.Floor(Math.Ceiling(Math.Sqrt(bundle.Count)));
        }

        return (rows, columns);
    }
}