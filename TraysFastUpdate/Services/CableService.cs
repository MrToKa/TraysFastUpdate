﻿using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using TraysFastUpdate.Data.Repositories;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Contracts;

namespace TraysFastUpdate.Services
{
    public class CableService : ICableService
    {
        private readonly ITraysFastUpdateDbRepository _repository;

        public CableService(ITraysFastUpdateDbRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateCableAsync(Cable cable)
        {
            bool cableExists = await _repository.All<Cable>().AnyAsync(c => c.Tag == cable.Tag && c.CableType == cable.CableType);
            if (cableExists)
            {
                return;
            }

            await _repository.AddAsync(cable);
            await _repository.SaveChangesAsync();
        }

        public async Task DeleteCableAsync(int cableId)
        {
            var cable = await _repository.All<Cable>().FirstOrDefaultAsync(c => c.Id == cableId);
            if (cable == null)
            {
                return;
            }

            _repository.Delete(cable);
            await _repository.SaveChangesAsync();
        }

        public async Task<Cable> GetCableAsync(int cableId)
        {
            var cable = await _repository.All<Cable>().FirstOrDefaultAsync(c => c.Id == cableId);
            return cable ?? throw new InvalidOperationException($"Cable with ID {cableId} not found.");
        }

        public async Task<List<Cable>> GetCablesAsync()
        {
            return await _repository.All<Cable>().ToListAsync();
        }

        public async Task UpdateCableAsync(Cable cable)
        {
            var cableToUpdate = await _repository.All<Cable>().FirstOrDefaultAsync(c => c.Id == cable.Id);
            if (cableToUpdate == null)
            {
                return;
            }
            cableToUpdate.Tag = cable.Tag;
            cableToUpdate.CableType = cable.CableType;
            cableToUpdate.FromLocation = cable.FromLocation;
            cableToUpdate.ToLocation = cable.ToLocation;
            cableToUpdate.Routing = cable.Routing;
            await _repository.SaveChangesAsync();
        }

        public async Task UploadFromFileAsync(IBrowserFile file)
        {
            List<Cable> cableTypes = new List<Cable>();
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
                    Cable cable = new Cable();
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
                            cable.Tag = value;
                        }
                        else if (cell.CellReference == "B" + rowNumber)
                        {
                            var ctValue = await _repository.All<CableType>().FirstOrDefaultAsync(ct => ct.Type == value);
                            cable.CableType = ctValue;
                        }
                        else if (cell.CellReference == "C" + rowNumber)
                        {
                            cable.FromLocation = value;
                        }
                        else if (cell.CellReference == "D" + rowNumber)
                        {
                            cable.ToLocation = value;
                        }
                        else if (cell.CellReference == "E" + rowNumber)
                        {
                            cable.Routing = value;
                        }
                    }

                    cableTypes.Add(cable);
                }

                foreach (var cable in cableTypes)
                {
                    await this.CreateCableAsync(cable);
                }
            }
        }

        public async Task<List<Cable>> GetCablesOnTrayAsync(Tray tray)
        {
            var cables = await _repository.All<Cable>()
                .Include(c => c.CableType)
                .ToListAsync();
            var filteredCables = cables
                .Where(c => c.Routing.Split('/')
                    .Any(segment => string.Equals(segment, tray.Name, StringComparison.OrdinalIgnoreCase)))  // Case-insensitive comparison
                .ToList();

            return filteredCables;
        }
    }    
}
