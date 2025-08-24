using DocumentFormat.OpenXml;
using wp = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using a = DocumentFormat.OpenXml.Drawing;
using pic = DocumentFormat.OpenXml.Drawing.Pictures;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Drawing;
using TraysFastUpdate.Data.Repositories;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Contracts;
using TraysFastUpdate.Services.Calculations;
using TraysFastUpdate.Services.Export;
using MudBlazor;
using System.Linq;

namespace TraysFastUpdate.Services
{
    public class TrayService : ITrayService
    {
        private readonly ITraysFastUpdateDbRepository _repository;
        private readonly ICableService _cableService;
        private readonly ITrayCalculationService _calculationService;
        private readonly IFileExportService _fileExportService;

        public TrayService(
            ITraysFastUpdateDbRepository repository,
            ICableService cableService,
            ITrayCalculationService calculationService,
            IFileExportService fileExportService)
        {
            _repository = repository;
            _cableService = cableService;
            _calculationService = calculationService;
            _fileExportService = fileExportService;
        }

        public async Task CreateTrayAsync(Tray tray)
        {
            bool trayExists = _repository.All<Tray>().Any(t => t.Name == tray.Name);
            if (trayExists)
            {
                Tray trayToUpdate = new Tray()
                {
                    Id = await _repository.All<Tray>().Where(t => t.Name == tray.Name).Select(t => t.Id).FirstOrDefaultAsync(),
                    Name = tray.Name,
                    Type = tray.Type,
                    Purpose = tray.Purpose,
                    Width = tray.Width,
                    Height = tray.Height,
                    Length = tray.Length,
                    Weight = tray.Weight
                };

                await UpdateTrayAsync(trayToUpdate);
                return;
            }

            tray.ResultSpaceAvailable = "N/A";
            tray.ResultSpaceOccupied = "N/A";

            await _repository.AddAsync(tray);
            await _repository.SaveChangesAsync();
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
            if (_repository.All<Tray>().Count() == 0)
            {
                return new List<Tray>();
            }

            return await _repository.All<Tray>().OrderBy(x => x.Id).ToListAsync();
        }

        public async Task UpdateTrayAsync(Tray tray)
        {
            var trayToUpdate = await _repository.All<Tray>().FirstOrDefaultAsync(t => t.Id == tray.Id);
            if (trayToUpdate == null)
            {
                return;
            }
            
            trayToUpdate.Name = tray.Name;
            trayToUpdate.Type = tray.Type;
            trayToUpdate.Purpose = tray.Purpose;
            trayToUpdate.Width = tray.Width;
            trayToUpdate.Height = tray.Height;
            trayToUpdate.Length = tray.Length;
            trayToUpdate.Weight = tray.Weight;

            await _calculationService.CalculateWeightsAsync(trayToUpdate);
            await _calculationService.CalculateSpaceUtilizationAsync(trayToUpdate);
            await _repository.SaveChangesAsync();
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
                            tray.Length = double.Parse(value, CultureInfo.InvariantCulture);
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

        public async Task<int> GetTraysCountAsync()
        {
            var traysCount = await _repository.All<Tray>().CountAsync();
            return traysCount;
        }

        public async Task ExportToFileAsync(Tray tray)
        {
            await _fileExportService.ExportTrayDocumentationAsync(tray);
        }

        public async Task ExportCanvasImageAsync(Excubo.Blazor.Canvas.Canvas canvas, string trayName, bool rotate = true)
        {
            await _fileExportService.ExportCanvasImageAsync(canvas, trayName, rotate);
        }

        public async Task ExportTrayTableEntriesAsync()
        {
            await _fileExportService.ExportTrayTableEntriesAsync();
        }

        // ... keep existing Word export methods for now but consider moving to separate service
    }
}
