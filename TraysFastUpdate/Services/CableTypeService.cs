using DocumentFormat.OpenXml;
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
            return await _repository.All<CableType>().OrderBy(x => x.Id).ToListAsync();
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

        public async Task ExportCableTypesTableEntriesAsync()
        {
            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (File.Exists(Path.Combine(wwwrootPath, "CableTypes.xlsx")))
            {
                File.Delete(Path.Combine(wwwrootPath, "CableTypes.xlsx"));
            }

            using SpreadsheetDocument document = SpreadsheetDocument.Create(Path.Combine(wwwrootPath, "CableTypes.xlsx"), SpreadsheetDocumentType.Workbook);

            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "CableTypes" };
            sheets.Append(sheet);

            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>() ?? worksheetPart.Worksheet.AppendChild(new SheetData());

            List<CableType> cableTypes = await _repository.All<CableType>().ToListAsync();

            Row headerRow = new Row() { RowIndex = 1 };
            sheetData.Append(headerRow);

            string[] headers = { "Type", "Purpose", "Diameter [mm]", "Weight [kg/m]" };
            for (int i = 0; i < headers.Length; i++)
            {
                Cell headerCell = new Cell() { CellReference = ((char)('A' + i)).ToString() + "1" };
                headerCell.CellValue = new CellValue(headers[i]);
                headerCell.DataType = new EnumValue<CellValues>(CellValues.String);
                headerRow.Append(headerCell);
            }

            for (int i = 0; i < cableTypes.Count; i++)
            {
                var cableType = cableTypes[i];
                Row row = new Row() { RowIndex = (uint)(i + 2) };
                sheetData.Append(row);

                Cell cell = new Cell() { CellReference = "A" + (i + 2) };
                cell.CellValue = new CellValue(cableType.Type);
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.Append(cell);

                cell = new Cell() { CellReference = "B" + (i + 2) };
                cell.CellValue = new CellValue(cableType.Purpose);
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.Append(cell);

                cell = new Cell() { CellReference = "C" + (i + 2) };
                cell.CellValue = new CellValue(cableType.Diameter.ToString("F2"));
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                row.Append(cell);

                cell = new Cell() { CellReference = "D" + (i + 2) };
                cell.CellValue = new CellValue(cableType.Weight.ToString("F3"));
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                row.Append(cell);
            }

            TableDefinitionPart tableDefinitionPart = worksheetPart.AddNewPart<TableDefinitionPart>();
            DocumentFormat.OpenXml.Spreadsheet.Table table = new DocumentFormat.OpenXml.Spreadsheet.Table()
            {
                Id = 1,
                DisplayName = "CableTypes",
                Name = "CableTypes",
                Reference = "A1:D" + (cableTypes.Count + 1)
            };

            AutoFilter autoFilter = new AutoFilter() { Reference = "A1:D" + (cableTypes.Count + 1) };

            TableColumns tableColumns = new TableColumns() { Count = (uint)headers.Length };
            tableColumns.Append(new TableColumn() { Id = 1, Name = "Type" });
            tableColumns.Append(new TableColumn() { Id = 2, Name = "Purpose" });
            tableColumns.Append(new TableColumn() { Id = 3, Name = "Diameter [mm]" });
            tableColumns.Append(new TableColumn() { Id = 4, Name = "Weight [kg/m]" });

            TableStyleInfo tableStyleInfo = new TableStyleInfo()
            {
                Name = "TableStyleLight8",
                ShowFirstColumn = false,
                ShowLastColumn = false,
                ShowRowStripes = true,
                ShowColumnStripes = false
            };

            table.Append(autoFilter);
            table.Append(tableColumns);
            table.Append(tableStyleInfo);

            tableDefinitionPart.Table = table;
            tableDefinitionPart.Table.Save();

            TableParts tableParts = worksheetPart.Worksheet.GetFirstChild<TableParts>() ?? worksheetPart.Worksheet.AppendChild(new TableParts());
            tableParts.Append(new TablePart() { Id = worksheetPart.GetIdOfPart(tableDefinitionPart) });

            workbookPart.Workbook.Save();
            document.Dispose();
        }
    }
}
