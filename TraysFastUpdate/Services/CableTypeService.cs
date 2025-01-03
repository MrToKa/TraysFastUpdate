using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using TraysFastUpdate.Data.Repositories;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Contracts;

namespace TraysFastUpdate.Services
{
    public class CableTypeService : ICableTypeService
    {
        private readonly ITraysFastUpdateDbRepository _repository;

        public CableTypeService(ITraysFastUpdateDbRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateCableTypeAsync(CableType cableType)
        {
            bool cableTypeExists = await _repository.All<CableType>().AnyAsync(c => c.Type == cableType.Type);
            if (cableTypeExists)
            {
                await UpdateCableTypeAsync(cableType);
                return;
            }

            await _repository.AddAsync(cableType);
            await _repository.SaveChangesAsync();
        }

        public async Task DeleteCableTypeAsync(int cableTypeId)
        {
            var cableType = await _repository.All<CableType>().FirstOrDefaultAsync(c => c.Id == cableTypeId);
            if (cableType == null)
            {
                return;
            }
            _repository.Delete(cableType);
            await _repository.SaveChangesAsync();
        }

        public async Task<List<CableType>> GetCablesTypesAsync()
        {
            return await _repository.All<CableType>().ToListAsync();
        }

        public async Task<CableType> GetCableTypeAsync(int cableTypeId)
        {
            var cableType = await _repository.All<CableType>().FirstOrDefaultAsync(c => c.Id == cableTypeId);
            return cableType ?? throw new InvalidOperationException($"CableType with ID {cableTypeId} not found.");
        }

        public async Task UpdateCableTypeAsync(CableType cableType)
        {
            var cableTypeToUpdate = await _repository.All<CableType>().FirstOrDefaultAsync(c => c.Type == cableType.Type);
            if (cableTypeToUpdate == null)
            {
                return;
            }
            cableTypeToUpdate.Type = cableType.Type;
            cableTypeToUpdate.Purpose = cableType.Purpose;
            cableTypeToUpdate.Diameter = cableType.Diameter;
            cableTypeToUpdate.Weight = cableType.Weight;
            await _repository.SaveChangesAsync();
        }

        public async Task UploadFromFileAsync(IBrowserFile file)
        {
            List<CableType> cableTypes = new List<CableType>();
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
                    CableType cableType = new CableType();
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
                            cableType.Type = value;
                        }
                        else if (cell.CellReference == "B" + rowNumber)
                        {
                            cableType.Purpose = value;
                        }
                        else if (cell.CellReference == "C" + rowNumber)
                        {
                            cableType.Diameter = double.Parse(value);
                        }
                        else if (cell.CellReference == "D" + rowNumber)
                        {
                            cableType.Weight = double.Parse(value);
                        }                        
                    }

                    cableTypes.Add(cableType);
                }

                foreach (var cable in cableTypes)
                {
                    await this.CreateCableTypeAsync(cable);
                }
            }
        }
    }
}
