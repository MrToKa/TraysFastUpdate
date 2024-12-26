using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using TraysFastUpdate.Data;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Contracts;

namespace TraysFastUpdate.Services
{
    public class TrayService : ITrayService
    {
        private readonly TraysFastUpdateDbContext _repository;

        public TrayService(TraysFastUpdateDbContext repository)
        {
            _repository = repository;
        }

        public async Task CreateTrayAsync(Tray tray)
        {
            bool trayExists = _repository.Trays.Any(t => t.Name == tray.Name);
            if (trayExists)
            {
                return;
            }

            await _repository.Trays.AddAsync(tray);
            await _repository.SaveChangesAsync();
        }

        public async Task DeleteTrayAsync(int trayId)
        {
            var tray = await _repository.Trays.FirstOrDefaultAsync(t => t.Id == trayId);
            if (tray == null)
            {
                return;
            }
            _repository.Trays.Remove(tray);
            await _repository.SaveChangesAsync();
        }

        public async Task<Tray> GetTrayAsync(int trayId)
        {
            var tray = await _repository.Trays.FirstOrDefaultAsync(t => t.Id == trayId);
            return tray ?? throw new InvalidOperationException($"Tray with ID {trayId} not found.");
        }

        public async Task<List<Tray>> GetTraysAsync()
        {
            return await _repository.Trays.ToListAsync();
        }

        public async Task UpdateTrayAsync(Tray trayId)
        {
            var trayToUpdate = await _repository.Trays.FirstOrDefaultAsync(t => t.Id == trayId.Id);
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
    }
}
