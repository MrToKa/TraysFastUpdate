﻿using DocumentFormat.OpenXml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Excubo.Blazor.Canvas;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using TraysFastUpdate.Data.Repositories;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Contracts;

namespace TraysFastUpdate.Services
{
    public class TrayService : ITrayService
    {
        private const double supportsWeight = 5.416;
        private const double KLDistance = 1.5;
        private const double WSLDistance = 5.5;
        private const double CProfileHeight = 15;
        private const double spacing = 1;

        private readonly ITraysFastUpdateDbRepository _repository;
        private readonly ICableService _cableService;

        public TrayService(ITraysFastUpdateDbRepository repository,
            ICableService cableService)
        {
            _repository = repository;
            _cableService = cableService;
        }

        public async Task CreateTrayAsync(Tray tray)
        {
            bool trayExists = _repository.All<Tray>().Any(t => t.Name == tray.Name);
            if (trayExists)
            {
                return;
            }


            await _repository.AddAsync(tray);
            await _repository.SaveChangesAsync();

            await TrayWeightCalculations(tray);
        }
        public async Task DeleteTrayAsync(int trayId)
        {
            var tray = await _repository.All<Tray>().FirstOrDefaultAsync(t => t.Id == trayId);
            if (tray == null)
            {
                return;
            }
            _repository.Delete(tray);
            await _repository.SaveChangesAsync();
        }
        public async Task<Tray> GetTrayAsync(int trayId)
        {
            var tray = await _repository.All<Tray>().FirstOrDefaultAsync(t => t.Id == trayId);
            return tray ?? throw new InvalidOperationException($"Tray with ID {trayId} not found.");
        }
        public async Task<List<Tray>> GetTraysAsync()
        {
            return await _repository.All<Tray>().ToListAsync();
        }
        public async Task UpdateTrayAsync(Tray trayId)
        {
            var trayToUpdate = await _repository.All<Tray>().FirstOrDefaultAsync(t => t.Id == trayId.Id);
            if (trayToUpdate == null)
            {
                return;
            }
            trayToUpdate.Name = trayId.Name;
            trayToUpdate.Type = trayId.Type;
            trayToUpdate.Purpose = trayId.Purpose;
            trayToUpdate.Width = trayId.Width;
            trayToUpdate.Height = trayId.Height;
            trayToUpdate.Length = trayId.Length;
            trayToUpdate.Weight = trayId.Weight;
            await _repository.SaveChangesAsync();

            await TrayWeightCalculations(trayToUpdate);
            await CalculateFreePercentages(trayToUpdate);
        }
        public async Task UploadFromFileAsync(IBrowserFile file)
        {
            List<Tray> trays = new List<Tray>();
            string? value = null;

            using MemoryStream memoryStream = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            MemoryStream stream = memoryStream;

            using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);

            if (document is not null)
            {
                WorkbookPart workbookPart = document.WorkbookPart;
                WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                for (int i = 1; i < sheetData.Elements<Row>().Count(); i++)
                {
                    Tray tray = new Tray();
                    Row row = sheetData.Elements<Row>().ElementAt(i);
                    // get the row number from outerxml
                    int rowNumber = int.Parse(row.RowIndex);

                    foreach (Cell cell in row.Elements<Cell>())
                    {
                        value = cell.InnerText;

                        if (cell.DataType != null && cell.DataType == CellValues.SharedString)
                        {
                            SharedStringTablePart stringTablePart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                            if (stringTablePart != null)
                                value = stringTablePart.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
                        }

                        if (cell.CellReference == "A" + rowNumber)
                        {
                            tray.Name = value;
                        }
                        else if (cell.CellReference == "B" + rowNumber)
                        {
                            tray.Type = value;
                        }
                        else if (cell.CellReference == "C" + rowNumber)
                        {
                            tray.Purpose = value;
                        }
                        else if (cell.CellReference == "D" + rowNumber)
                        {
                            tray.Width = double.Parse(value);
                        }
                        else if (cell.CellReference == "E" + rowNumber)
                        {
                            tray.Height = double.Parse(value);
                        }
                        else if (cell.CellReference == "F" + rowNumber)
                        {
                            tray.Length = double.Parse(value);
                        }
                        else if (cell.CellReference == "G" + rowNumber)
                        {
                            tray.Weight = double.Parse(value);
                        }
                    }

                    trays.Add(tray);
                }

                foreach (var tray in trays)
                {
                    await CreateTrayAsync(tray);
                }
            }
        }
        private async Task TrayWeightCalculations(Tray tray) 
        {
            await CalculateTraySupportsWeight(tray);
            await CalculateTrayOwnWeight(tray);
            await CalculateTrayCablesWeight(tray);
            await CalculateTrayTotalWeight(tray);
        }
        private async Task CalculateTraySupportsWeight(Tray tray)
        {
            double totalWeight = 0;
            double distance = 0;
            int supportsCount = 0;

            if (tray.Type.StartsWith("KL"))
            {
                distance = KLDistance;
            }
            else if (tray.Type.StartsWith("WSL"))
            {
                distance = WSLDistance;
            }

            supportsCount = (int)Math.Round(tray.Length / 1000 / distance + 1, MidpointRounding.AwayFromZero);
            totalWeight = supportsCount * supportsWeight;
            
            tray.SupportsCount = supportsCount;
            tray.SupportsWeightLoadPerMeter = Math.Round((totalWeight / tray.Length) * 1000, 3);
            tray.SupportsTotalWeight = Math.Round(totalWeight, 3);

            await _repository.SaveChangesAsync();
        }
        private async Task CalculateTrayOwnWeight(Tray tray)
        {
            tray.TrayWeightLoadPerMeter = Math.Round((double)(tray.Weight + tray.SupportsWeightLoadPerMeter), 3);
            tray.TrayOwnWeightLoad = Math.Round((double)(tray.TrayWeightLoadPerMeter * tray.Length / 1000), 3);

            await _repository.SaveChangesAsync();
        }
        private async Task CalculateTrayCablesWeight(Tray tray)
        {
            double cablesWeight = 0;
            double cablesWeightPerMeter = 0;

            List<Cable> cablesOnTray = await _cableService.GetCablesOnTrayAsync(tray);

            foreach (var cable in cablesOnTray)
            {
                cablesWeight += cable.CableType.Weight;
            }

            cablesWeightPerMeter = Math.Round(cablesWeight, 3);
            tray.CablesWeightPerMeter = cablesWeightPerMeter;
            tray.CablesWeightLoad = Math.Round((double)(cablesWeightPerMeter * tray.Length / 1000), 3);

            await _repository.SaveChangesAsync();
        }
        private async Task CalculateTrayTotalWeight(Tray tray)
        {
            tray.TotalWeightLoadPerMeter = Math.Round((double)(tray.TrayWeightLoadPerMeter + tray.CablesWeightPerMeter), 3);
            tray.TotalWeightLoad = Math.Round((double)(tray.TrayOwnWeightLoad + tray.CablesWeightLoad), 3);
            await _repository.SaveChangesAsync();
        }
        private async Task CalculateFreePercentages(Tray tray)
        {    
            var bundles = await _cableService.GetCablesBundlesOnTrayAsync(tray);

            double bottomRow = 1;

            foreach (var bundle in bundles) 
            {
                if (bundle.Key == "Power")
                {
                    var sortedBundles = bundle.Value.OrderByDescending(x => x.Value[0].CableType.Diameter).ToList();

                    foreach (var sortedBundle in sortedBundles)
                    {
                        (int rows, int columns) = calculateRowsAndColumns(tray.Height - CProfileHeight, 1, sortedBundle.Value, "Power");

                        int row = 0;
                        int column = 0;

                        var sortedCables = sortedBundle.Value.OrderByDescending(x => x.CableType.Diameter).ToList();

                        if (sortedBundle.Key == "42.1-60")
                        {
                            foreach (var cable in sortedCables)
                            {
                                int cableIndex = sortedCables.IndexOf(cable);
                                if (cableIndex != 0 && cableIndex % 2 == 0)
                                {
                                    continue;
                                }

                                bottomRow += cable.CableType.Diameter;
                                bottomRow += spacing;
                            }
                        }
                        else
                        {
                            foreach (var cable in sortedCables)
                            {
                                if (row == 0)
                                {
                                    bottomRow += cable.CableType.Diameter;
                                    bottomRow += spacing;
                                }
                                row++;
                                if (row == rows)
                                {
                                    row = 0;
                                    column++;
                                }
                            }
                        }
                    }

                }
                else if (bundle.Key == "Control")
                {
                    var sortedBundles = bundle.Value.OrderByDescending(x => x.Value[0].CableType.Diameter).ToList();

                    foreach (var sortedBundle in sortedBundles)
                    {
                        (int rows, int columns) = calculateRowsAndColumns(tray.Height - CProfileHeight, 1, sortedBundle.Value, "Control");

                        int row = 0;
                        int column = 0;

                        var sortedCables = sortedBundle.Value.OrderByDescending(x => x.CableType.Diameter).ToList();

                        foreach (var cable in sortedCables)
                        {
                            if (row == 0)
                            {
                                bottomRow += cable.CableType.Diameter;
                                bottomRow += spacing;
                            }
                            row++;
                            if (row == rows)
                            {
                                row = 0;
                                column++;
                            }
                        }
                    }

                }
                else if (bundle.Key == "MV")
                {
                }
            }

            tray.SpaceOccupied = bottomRow;
            tray.SpaceAvailable = Math.Round((double)(100 - (bottomRow / tray.Width * 100)), 2);
        }

        private (int, int) calculateRowsAndColumns(double trayHeight, int spacing, List<Cable> bundle, string purpose)
        {
            int rows = 0;
            int columns = 0;
            double diameter = bundle.Max(x => x.CableType.Diameter);

            if (purpose == "Power")
            {
                rows = Math.Min((int)Math.Floor((trayHeight - spacing) / (diameter + spacing)), 3);
                columns = (int)Math.Floor((double)bundle.Count / rows);
            }
            else if (purpose == "Control")
            {
                rows = Math.Min((int)Math.Floor((trayHeight - spacing) / (diameter + spacing)), 7);
                columns = Math.Min((int)Math.Ceiling((double)bundle.Count / rows), 20);
            }

            if (rows > columns)
            {
                rows = (int)Math.Floor(Math.Ceiling(Math.Sqrt(bundle.Count)));
                columns = (int)Math.Floor(Math.Ceiling(Math.Sqrt(bundle.Count)));
            }

            return (rows, columns);
        }
    }
}
