using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using TraysFastUpdate.Data.Repositories;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Contracts;
using TraysFastUpdate.Services.Export;

namespace TraysFastUpdate.Services.Export;

public class ExcelExportService : IExcelExportService
{
    private readonly ITraysFastUpdateDbRepository _repository;
    private readonly ICableService _cableService;

    public ExcelExportService(ITraysFastUpdateDbRepository repository, ICableService cableService)
    {
        _repository = repository;
        _cableService = cableService;
    }

    public async Task ExportTrayTableEntriesAsync()
    {
        string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        string filePath = Path.Combine(wwwrootPath, "Trays.xlsx");

        // Remove the old file
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new Worksheet(new SheetData());

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        var sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Trays" };
        sheets.Append(sheet);

        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>() ?? worksheetPart.Worksheet.AppendChild(new SheetData());

        var trays = await _repository.All<Tray>().ToListAsync();

        await CreateHeaderRowAsync(sheetData);
        await CreateDataRowsAsync(sheetData, trays);
        await CreateTableAsync(worksheetPart, trays.Count);

        workbookPart.Workbook.Save();
    }

    private static async Task CreateHeaderRowAsync(SheetData sheetData)
    {
        var headerRow = new Row() { RowIndex = 1 };
        sheetData.Append(headerRow);

        string[] headers = { "Name", "Type", "Purpose", "Width [mm]", "Height [mm]", "Length [mm]", "Cables on tray [pcs.]", "Available space [%]" };
        
        for (int i = 0; i < headers.Length; i++)
        {
            var headerCell = new Cell() { CellReference = ((char)('A' + i)).ToString() + "1" };
            headerCell.CellValue = new CellValue(headers[i]);
            headerCell.DataType = new EnumValue<CellValues>(CellValues.String);
            headerRow.Append(headerCell);
        }
    }

    private async Task CreateDataRowsAsync(SheetData sheetData, List<Tray> trays)
    {
        for (int i = 0; i < trays.Count; i++)
        {
            int cablesCount = (await _cableService.GetCablesOnTrayAsync(trays[i])).Count;
            var row = new Row() { RowIndex = (uint)(i + 2) };
            sheetData.Append(row);

            AddCellToRow(row, "A", i + 2, trays[i].Name, CellValues.String);
            AddCellToRow(row, "B", i + 2, trays[i].Type, CellValues.String);
            AddCellToRow(row, "C", i + 2, trays[i].Purpose, CellValues.String);
            AddCellToRow(row, "D", i + 2, trays[i].Width.ToString(), CellValues.Number);
            AddCellToRow(row, "E", i + 2, trays[i].Height.ToString(), CellValues.Number);
            AddCellToRow(row, "F", i + 2, trays[i].Length.ToString("F3"), CellValues.Number);
            AddCellToRow(row, "G", i + 2, cablesCount.ToString(), CellValues.Number);
            
            string spaceValue = trays[i].Purpose == "Type A (Pink color) for MV cables" 
                ? "N/A" 
                : trays[i].SpaceAvailable?.ToString("F2") ?? "N/A";
            AddCellToRow(row, "H", i + 2, spaceValue, CellValues.String);
        }
    }

    private static void AddCellToRow(Row row, string columnLetter, int rowNumber, string value, CellValues dataType)
    {
        var cell = new Cell() { CellReference = $"{columnLetter}{rowNumber}" };
        cell.CellValue = new CellValue(value);
        cell.DataType = new EnumValue<CellValues>(dataType);
        row.Append(cell);
    }

    private static async Task CreateTableAsync(WorksheetPart worksheetPart, int trayCount)
    {
        var tableDefinitionPart = worksheetPart.AddNewPart<TableDefinitionPart>();
        var table = new Table()
        {
            Id = 1,
            DisplayName = "Trays",
            Name = "Trays",
            Reference = $"A1:H{trayCount + 1}"
        };

        var autoFilter = new AutoFilter() { Reference = $"A1:H{trayCount + 1}" };
        var tableColumns = CreateTableColumns();
        var tableStyleInfo = CreateTableStyleInfo();

        table.Append(autoFilter);
        table.Append(tableColumns);
        table.Append(tableStyleInfo);

        tableDefinitionPart.Table = table;
        tableDefinitionPart.Table.Save();

        var tableParts = worksheetPart.Worksheet.GetFirstChild<TableParts>() ?? worksheetPart.Worksheet.AppendChild(new TableParts());
        tableParts.Append(new TablePart() { Id = worksheetPart.GetIdOfPart(tableDefinitionPart) });
    }

    private static TableColumns CreateTableColumns()
    {
        var tableColumns = new TableColumns() { Count = 8 };
        string[] columnNames = { "Name", "Type", "Purpose", "Width [mm]", "Height [mm]", "Length [mm]", "Cables on tray [pcs.]", "Available space [%]" };
        
        for (uint i = 0; i < columnNames.Length; i++)
        {
            tableColumns.Append(new TableColumn() { Id = i + 1, Name = columnNames[i] });
        }
        
        return tableColumns;
    }

    private static TableStyleInfo CreateTableStyleInfo()
    {
        return new TableStyleInfo()
        {
            Name = "TableStyleLight8",
            ShowFirstColumn = false,
            ShowLastColumn = false,
            ShowRowStripes = true,
            ShowColumnStripes = false
        };
    }
}