using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
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
    }
}
