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
    public class CableService : ICableService
    {
        private readonly ITraysFastUpdateDbRepository _repository;

        public CableService(ITraysFastUpdateDbRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateCableAsync(Cable cable)
        {
            bool cableExists = await _repository.All<Cable>().AnyAsync(c => c.Tag == cable.Tag && c.CableType == cable.CableType && c.FromLocation == cable.FromLocation && cable.ToLocation == cable.ToLocation);
            if (cableExists)
            {
                Cable cableToUpdate = new Cable
                {
                    Id = await _repository.All<Cable>().Where(c => c.Tag == cable.Tag && c.CableType == cable.CableType && c.FromLocation == cable.FromLocation && cable.ToLocation == cable.ToLocation).Select(c => c.Id).FirstOrDefaultAsync(),
                    Tag = cable.Tag,
                    FromLocation = cable.FromLocation,
                    ToLocation = cable.ToLocation,
                    Routing = cable.Routing,
                    CableTypeId = cable.CableTypeId
                };

                await UpdateCableAsync(cableToUpdate);
                return;
            }

            var cableType = await _repository.All<CableType>().FirstOrDefaultAsync(ct => ct.Id == cable.CableTypeId);
            if (cableType == null)
            {
                throw new InvalidOperationException($"Cable {cable.Tag} with {cable.CableTypeId} not found.");
            }
            cable.CableType = cableType;

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
            return await _repository.All<Cable>().OrderBy(x => x.Id).ToListAsync();
        }

        public async Task UpdateCableAsync(Cable cable)
        {
            var cableToUpdate = await _repository.All<Cable>().FirstOrDefaultAsync(c => c.Id == cable.Id);
            if (cableToUpdate == null)
            {
                return;
            }
            cableToUpdate.Tag = cable.Tag;
            cableToUpdate.FromLocation = cable.FromLocation;
            cableToUpdate.ToLocation = cable.ToLocation;
            cableToUpdate.Routing = cable.Routing;
            cableToUpdate.CableTypeId = cable.CableTypeId;

            var cableType = await _repository.All<CableType>().FirstOrDefaultAsync(ct => ct.Id == cable.CableTypeId);
            if (cableType == null)
            {
                throw new InvalidOperationException($"CableType with ID {cable.CableTypeId} not found.");
            }
            cableToUpdate.CableType = cableType;

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
                            cable.CableTypeId = ctValue.Id;
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
                .Where(c => c.Routing != null && c.Routing.Split('/')
                    .Any(segment => string.Equals(segment, tray.Name, StringComparison.OrdinalIgnoreCase)))  // Case-insensitive comparison
                .ToList();

            //if (filteredCables.Count == 0)
            //{
            //    throw new InvalidOperationException($"No cables found on tray {tray.Name}.");
            //}

            return filteredCables;
        }

        public async Task<Dictionary<string, Dictionary<string, List<Cable>>>> GetCablesBundlesOnTrayAsync(Tray tray)
        {
            Dictionary<string, Dictionary<string, List<Cable>>> result = [];

            var cables = await GetCablesOnTrayAsync(tray);

            foreach (var cable in cables)
            {
                var cableBundle = DetermenCableDiameterGroup(cable.CableType.Diameter);

                if (!result.ContainsKey(cable.CableType.Purpose))
                {
                    result.Add(cable.CableType.Purpose, new Dictionary<string, List<Cable>>());
                }
                if (!result[cable.CableType.Purpose].ContainsKey(cableBundle))
                {
                    result[cable.CableType.Purpose].Add(cableBundle, new List<Cable>());
                }
                result[cable.CableType.Purpose][cableBundle].Add(cable);

            }

            return result;
        }

        private static string DetermenCableDiameterGroup(double diameter)
        {
            if (diameter <= 8)
            {
                return "0-8";
            }
            else if (diameter <= 15)
            {
                return "8.1-15";
            }
            else if (diameter <= 21)
            {
                return "15.1-21";
            }
            else if (diameter <= 30)
            {
                return "21.1-30";
            }
            else if (diameter <= 40)
            {
                return "30.1-40";
            }
            else if (diameter <= 44.5)
            {
                return "40.1-44.5";
            }
            else if (diameter <= 60)
            {
                return "44.6-60";
            }
            else
            {
                return "60+";
            }  
        }

        public async Task ExportCablesTableEntriesAsync()
        {
            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (File.Exists(Path.Combine(wwwrootPath, "Cables.xlsx")))
            {
                File.Delete(Path.Combine(wwwrootPath, "Cables.xlsx"));
            }

            SpreadsheetDocument document = SpreadsheetDocument.Create(Path.Combine(wwwrootPath, "Cables.xlsx"), SpreadsheetDocumentType.Workbook);

            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Cables" };
            sheets.Append(sheet);

            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>() ?? worksheetPart.Worksheet.AppendChild(new SheetData());

            List<Cable> cables = await _repository.All<Cable>().Include(c => c.CableType).ToListAsync();

            Row headerRow = new Row() { RowIndex = 1 };
            sheetData.Append(headerRow);

            string[] headers = { "Tag", "Type", "Purpose", "Diameter [mm]", "Weight [kg/m]", "From Location", "To Location", "Routing" };
            for (int i = 0; i < headers.Length; i++)
            {
                Cell headerCell = new Cell() { CellReference = ((char)('A' + i)).ToString() + "1" };
                headerCell.CellValue = new CellValue(headers[i]);
                headerCell.DataType = new EnumValue<CellValues>(CellValues.String);
                headerRow.Append(headerCell);
            }

            for (int i = 0; i < cables.Count; i++)
            {
                var cable = cables[i];
                Row row = new Row() { RowIndex = (uint)(i + 2) };
                sheetData.Append(row);

                Cell cell = new Cell() { CellReference = "A" + (i + 2) };
                cell.CellValue = new CellValue(cable.Tag);
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.Append(cell);

                cell = new Cell() { CellReference = "B" + (i + 2) };
                cell.CellValue = new CellValue(cable.CableType?.Type ?? "");
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.Append(cell);

                cell = new Cell() { CellReference = "C" + (i + 2) };
                cell.CellValue = new CellValue(cable.CableType?.Purpose ?? "");
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.Append(cell);

                cell = new Cell() { CellReference = "D" + (i + 2) };
                cell.CellValue = new CellValue(cable.CableType?.Diameter.ToString("F2") ?? "0.00");
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                row.Append(cell);

                cell = new Cell() { CellReference = "E" + (i + 2) };
                cell.CellValue = new CellValue(cable.CableType?.Weight.ToString("F3") ?? "0.000");
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                row.Append(cell);

                cell = new Cell() { CellReference = "F" + (i + 2) };
                cell.CellValue = new CellValue(cable.FromLocation ?? "");
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.Append(cell);

                cell = new Cell() { CellReference = "G" + (i + 2) };
                cell.CellValue = new CellValue(cable.ToLocation ?? "");
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.Append(cell);

                cell = new Cell() { CellReference = "H" + (i + 2) };
                cell.CellValue = new CellValue(cable.Routing ?? "");
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.Append(cell);
            }

            TableDefinitionPart tableDefinitionPart = worksheetPart.AddNewPart<TableDefinitionPart>();
            DocumentFormat.OpenXml.Spreadsheet.Table table = new DocumentFormat.OpenXml.Spreadsheet.Table()
            {
                Id = 1,
                DisplayName = "Cables",
                Name = "Cables",
                Reference = "A1:H" + (cables.Count + 1)
            };

            AutoFilter autoFilter = new AutoFilter() { Reference = "A1:H" + (cables.Count + 1) };

            TableColumns tableColumns = new TableColumns() { Count = (uint)headers.Length };
            tableColumns.Append(new TableColumn() { Id = 1, Name = "Tag" });
            tableColumns.Append(new TableColumn() { Id = 2, Name = "Type" });
            tableColumns.Append(new TableColumn() { Id = 3, Name = "Purpose" });
            tableColumns.Append(new TableColumn() { Id = 4, Name = "Diameter [mm]" });
            tableColumns.Append(new TableColumn() { Id = 5, Name = "Weight [kg/m]" });
            tableColumns.Append(new TableColumn() { Id = 6, Name = "From Location" });
            tableColumns.Append(new TableColumn() { Id = 7, Name = "To Location" });
            tableColumns.Append(new TableColumn() { Id = 8, Name = "Routing" });

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
