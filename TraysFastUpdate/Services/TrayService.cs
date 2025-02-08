using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
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

            tray.ResultSpaceAvailable = "N/A";
            tray.ResultSpaceOccupied = "N/A";

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

            if (tray.Length / 1000 / distance + 1 < 0.2)
            {
                supportsCount = (int)Math.Floor(tray.Length / 1000 / distance + 1);
            }
            else
            {
                supportsCount = (int)Math.Ceiling(tray.Length / 1000 / distance + 1);
            }

            totalWeight = supportsCount * supportsWeight;

            tray.SupportsCount = supportsCount;
            tray.SupportsWeightLoadPerMeter = Math.Round((totalWeight / tray.Length) * 1000, 3);
            tray.SupportsTotalWeight = Math.Round(totalWeight, 3);

            var supportsCountSb = new StringBuilder();
            supportsCountSb.Append($"({Math.Round(tray.Length / 1000, 3)} * 1000) / {distance} ≈ {Math.Round(tray.Length / 1000 / distance + 1, 3)} = {supportsCount} [pcs.]");
            tray.ResultSupportsCount = supportsCountSb.ToString();

            var supportsWeightLoadPerMeterSb = new StringBuilder();
            supportsWeightLoadPerMeterSb.Append($"{Math.Round(totalWeight, 3)} / ({Math.Round(tray.Length, 3)} * 1000) = {tray.SupportsWeightLoadPerMeter} [kg/m]");
            tray.ResultSupportsWeightLoadPerMeter = supportsWeightLoadPerMeterSb.ToString();

            var supportsTotalWeightSb = new StringBuilder();
            supportsTotalWeightSb.Append($"{supportsCount} * {supportsWeight} = {Math.Round(totalWeight, 3)} [kg]");
            tray.ResultSupportsTotalWeight = supportsTotalWeightSb.ToString();

            await _repository.SaveChangesAsync();
        }
        private async Task CalculateTrayOwnWeight(Tray tray)
        {
            tray.TrayWeightLoadPerMeter = Math.Round((double)(tray.Weight + tray.SupportsWeightLoadPerMeter), 3);
            tray.TrayOwnWeightLoad = Math.Round((double)(tray.TrayWeightLoadPerMeter * tray.Length / 1000), 3);

            var trayWeightLoadPerMeterSb = new StringBuilder();
            trayWeightLoadPerMeterSb.AppendLine($"{Math.Round(tray.Weight, 3)} + {Math.Round((double)tray.SupportsWeightLoadPerMeter, 3)} = {Math.Round((double)tray.TrayWeightLoadPerMeter, 3)} [kg/m]");
            tray.ResultTrayWeightLoadPerMeter = trayWeightLoadPerMeterSb.ToString();

            var trayOwnWeightLoadSb = new StringBuilder();
            trayOwnWeightLoadSb.AppendLine($"{Math.Round((double)tray.TrayWeightLoadPerMeter, 3)} * ({Math.Round(tray.Length, 3)} / 1000) = {Math.Round((double)tray.TrayOwnWeightLoad, 3)} [kg]");
            tray.ResultTrayOwnWeightLoad = trayOwnWeightLoadSb.ToString();

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

            var cablesWeightPerMeterSb = new StringBuilder();
            for (int i = 0; i < cablesOnTray.Count - 1; i++)
            {
                cablesWeightPerMeterSb.Append($"{cablesOnTray[i].CableType.Weight} + ");
            }
            cablesWeightPerMeterSb.Append($"{cablesOnTray[cablesOnTray.Count - 1].CableType.Weight} = {Math.Round(cablesWeight, 3)} [kg/m]");
            tray.ResultCablesWeightPerMeter = cablesWeightPerMeterSb.ToString();

            var cablesWeightLoadSb = new StringBuilder();
            cablesWeightLoadSb.Append($"{Math.Round(cablesWeightPerMeter, 3)} * ({Math.Round(tray.Length, 3)} / 1000) = {Math.Round((double)tray.CablesWeightLoad, 3)} [kg]");
            tray.ResultCablesWeightLoad = cablesWeightLoadSb.ToString();

            await _repository.SaveChangesAsync();
        }
        private async Task CalculateTrayTotalWeight(Tray tray)
        {
            tray.TotalWeightLoadPerMeter = Math.Round((double)(tray.TrayWeightLoadPerMeter + tray.CablesWeightPerMeter), 3);
            tray.TotalWeightLoad = Math.Round((double)(tray.TrayOwnWeightLoad + tray.CablesWeightLoad), 3);

            var totalWeightLoadPerMeterSb = new StringBuilder();
            totalWeightLoadPerMeterSb.Append($"{Math.Round((double)tray.TrayWeightLoadPerMeter, 3)} + {Math.Round((double)tray.CablesWeightPerMeter, 3)} = {Math.Round((double)tray.TotalWeightLoadPerMeter, 3)} [kg/m]");
            tray.ResultTotalWeightLoadPerMeter = totalWeightLoadPerMeterSb.ToString();

            var totalWeightLoadSb = new StringBuilder();
            totalWeightLoadSb.Append($"{Math.Round((double)tray.TrayOwnWeightLoad, 3)} + {Math.Round((double)tray.CablesWeightLoad, 3)} = {Math.Round((double)tray.TotalWeightLoad, 3)} [kg]");
            tray.ResultTotalWeightLoad = totalWeightLoadSb.ToString();

            await _repository.SaveChangesAsync();
        }
        private async Task CalculateFreePercentages(Tray tray)
        {
            var bundles = await _cableService.GetCablesBundlesOnTrayAsync(tray);

            double bottomRow = 0;

            List<Cable> cablesBottomRow = new List<Cable>();

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
                                if (cableIndex != 0 && cableIndex % 2 == 0 && cable.CableType.Diameter <= 45)
                                {
                                    continue;
                                }

                                bottomRow += cable.CableType.Diameter;
                                bottomRow += spacing;

                                cablesBottomRow.Add(cable);
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

                                    cablesBottomRow.Add(cable);
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

                                cablesBottomRow.Add(cable);
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

            var groupedByDiameterCables = cablesBottomRow.GroupBy(x => x.CableType.Diameter).ToList();

            tray.SpaceOccupied = bottomRow;
            tray.SpaceAvailable = Math.Round((double)(100 - (bottomRow / tray.Width * 100)), 2);

            if (tray.Purpose == "MV")
            {
                tray.ResultSpaceOccupied = "N/A";
                tray.ResultSpaceAvailable = "N/A";
                return;
            }

            var spaceOccupiedSb = new StringBuilder();
            for (int i = 0; i < groupedByDiameterCables.Count - 1; i++)
            {
                spaceOccupiedSb.Append($"({groupedByDiameterCables[i].Key} * {groupedByDiameterCables[i].Count()}) + {spacing} * {groupedByDiameterCables[i].Count()} + ");
            }
            spaceOccupiedSb.Append($"({groupedByDiameterCables[groupedByDiameterCables.Count - 1].Key} * {groupedByDiameterCables[groupedByDiameterCables.Count - 1].Count()}) + {spacing} * {groupedByDiameterCables[groupedByDiameterCables.Count - 1].Count()} = {Math.Round(bottomRow, 3)} [mm]");
            tray.ResultSpaceOccupied = spaceOccupiedSb.ToString();

            var spaceAvailableSb = new StringBuilder();
            spaceAvailableSb.Append($"100 - ({Math.Round(bottomRow, 3)} / {Math.Round(tray.Width, 3)} * 100) = {Math.Round((double)tray.SpaceAvailable, 2)} [%]");
            tray.ResultSpaceAvailable = spaceAvailableSb.ToString();
        }
        private (int, int) calculateRowsAndColumns(double trayHeight, int spacing, List<Cable> bundle, string purpose)
        {
            int rows = 0;
            int columns = 0;
            double diameter = bundle.Max(x => x.CableType.Diameter);

            if (purpose == "Power")
            {
                rows = Math.Min((int)Math.Floor((trayHeight) / (diameter + spacing)), 3);
                columns = (int)Math.Floor((double)bundle.Count / rows);
            }
            else if (purpose == "Control")
            {
                rows = Math.Min((int)Math.Floor((trayHeight) / (diameter + spacing)), 7);
                columns = Math.Min((int)Math.Ceiling((double)bundle.Count / rows), 20);
            }

            if (rows > columns)
            {
                rows = (int)Math.Floor(Math.Ceiling(Math.Sqrt(bundle.Count)));
                columns = (int)Math.Floor(Math.Ceiling(Math.Sqrt(bundle.Count)));
            }

            return (rows, columns);
        }

        public async Task ExportToFileAsync(Tray tray)
        {
            string directoryPath = Path.Combine("wwwroot", "files", $"{tray.Name}");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string filePath = Path.Combine(directoryPath, $"{tray.Name}.xlsx");

            using MemoryStream memoryStream = new MemoryStream();
            using SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());
            Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Trays" };
            sheets.Append(sheet);
            SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
            Row headerRow = new Row();
            headerRow.Append(
                new Cell() { CellValue = new CellValue("Name"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Type"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Purpose"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Width"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Height"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Length"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Weight"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Supports Count"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Supports Total Weight"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Supports Weight Load Per Meter"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Tray Weight Load Per Meter"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Tray Own Weight Load"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Cables Weight Per Meter"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Cables Weight Load"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Total Weight Load Per Meter"), DataType = CellValues.String },
                new Cell()
                {
                    CellValue = new CellValue("Total Weight Load"),
                    DataType = CellValues.String
                },
                new Cell() { CellValue = new CellValue("Space Occupied"), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue("Space Available"), DataType = CellValues.String }
                );
            sheetData.AppendChild(headerRow);

            Row dataRow = new Row();

            dataRow.Append(
                new Cell() { CellValue = new CellValue(tray.Name), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.Type), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.Purpose), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.Width.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.Height.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.Length.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.Weight.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.SupportsCount.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.SupportsTotalWeight.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.SupportsWeightLoadPerMeter.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.TrayWeightLoadPerMeter.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.TrayOwnWeightLoad.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.CablesWeightPerMeter.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.CablesWeightLoad.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.TotalWeightLoadPerMeter.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.TotalWeightLoad.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.SpaceOccupied.ToString()), DataType = CellValues.String },
                new Cell() { CellValue = new CellValue(tray.SpaceAvailable.ToString()), DataType = CellValues.String }
                );

            sheetData.AppendChild(dataRow);

            workbookPart.Workbook.Save();
            document.Dispose();

            memoryStream.Position = 0;

            await using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await memoryStream.CopyToAsync(fileStream);
            memoryStream.Close();

            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            await ExportWordReportAsync(wwwrootPath, "ReportMacroTemplate_Space.docx", tray);

        }

        public async Task ExportWordReportAsync(string wwwrootPath, string templateFileName, Tray tray)
        {
            //remove old file
            string oldFilePath = Path.Combine(wwwrootPath, "files", $"{tray.Name}", $"{tray.Name}.docx");

            string templatePath = Path.Combine(wwwrootPath, templateFileName);
            string newFilePath = Path.Combine(wwwrootPath, "files", $"{tray.Name}", $"{tray.Name}.docx");

            double distance = 0;
            if (tray.Type.StartsWith("KL"))
            {
                distance = KLDistance;
            }
            else if (tray.Type.StartsWith("WSL"))
            {
                distance = WSLDistance;
            }

            // Copy template and rename it
            File.Copy(templatePath, newFilePath, true);

            var replacements = new Dictionary<string, string>
    {
        { "{TrayName}", tray.Name },
        { "{TrayType}", tray.Type },
        { "{TrayPurpose}", tray.Purpose },
        { "{TrayHeight}", tray.Height.ToString("F0", CultureInfo.InvariantCulture) },
        { "{TrayWidth}", tray.Width.ToString("F0", CultureInfo.InvariantCulture) },
        { "{TrayLength}", tray.Length.ToString("F2", CultureInfo.InvariantCulture) },
        { "{TrayWeight}", tray.Weight.ToString("F3", CultureInfo.InvariantCulture) },
        { "{SupportsCount}", tray.ResultSupportsCount},
        { "{Distance}", distance.ToString("F1", CultureInfo.InvariantCulture) },
        { "{SupportWeight}", "5.416" },
        { "{SuppTotalWeight}", tray.ResultSupportsTotalWeight},
        { "{SuppWeightPerMeter}", tray.ResultSupportsWeightLoadPerMeter},
        { "{TrayLoadPerMeter}", tray.ResultTrayWeightLoadPerMeter},
        { "{TrayWeightCalcs}", tray.ResultTrayOwnWeightLoad},
        { "{CablesWeightPerMeter}", tray.ResultCablesWeightPerMeter},
        { "{CablesWeightCalculations}", tray.ResultCablesWeightLoad},
        { "{TotalPerPoint}", tray.ResultTotalWeightLoadPerMeter},
        { "{TotalCalc}", tray.ResultTotalWeightLoad},
        { "{TrayHeightFormula}", (tray.Height - 15).ToString("F0", CultureInfo.InvariantCulture)},
        { "{DiametersSum}", tray.ResultSpaceOccupied},
        { "{FreeSpace}", tray.ResultSpaceAvailable},
        { "{DocNo}", "10004084142" },
        { "{DocType}", "TED" },
        { "{DocPart}", "001" },
        { "{RevNo}", "02" },
        { "{TodayDate}", DateTime.Now.ToString("dd-MM-yyyy") } // Today's date
    };

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(newFilePath, true))
            {
                var mainPart = wordDoc.MainDocumentPart;

                // Replace text in the document body
                ReplaceTextInElement(mainPart.Document.Body, replacements);

                // Replace text in all footers
                if (mainPart.FooterParts != null)
                {
                    foreach (var footer in mainPart.FooterParts)
                    {
                        ReplaceTextInElement(footer.Footer, replacements);
                    }
                }

                wordDoc.MainDocumentPart.Document.Save();

            }
            await InsertCableTableAsync(newFilePath, tray);
        }

        private void ReplaceTextInElement(OpenXmlElement element, Dictionary<string, string> replacements)
        {
            if (element == null) return;

            foreach (var paragraph in element.Descendants<Paragraph>())
            {
                string paragraphText = string.Join("", paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));

                // Replace placeholders
                bool modified = false;
                foreach (var key in replacements.Keys)
                {
                    if (paragraphText.Contains(key))
                    {
                        paragraphText = paragraphText.Replace(key, replacements[key]);
                        modified = true;
                    }
                }

                if (modified)
                {
                    // Clear all existing text elements
                    foreach (var text in paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
                    {
                        text.Text = string.Empty;
                    }

                    // Create a new Text element with the full updated content
                    var run = paragraph.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.Run>() ?? paragraph.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                    var textElement = run.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.Text>() ?? run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text());
                    textElement.Text = paragraphText;
                }
            }
        }

        public async Task InsertCableTableAsync(string filePath, Tray tray)
        {
            List<Cable> cablesOnTray = await _cableService.GetCablesOnTrayAsync(tray);

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, true))
            {
                var mainPart = wordDoc.MainDocumentPart;
                var body = mainPart.Document.Body;

                // Locate the placeholder
                var placeholderText = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
                    .FirstOrDefault(t => t.Text.Trim() == "{CablesTable}");

                if (placeholderText != null)
                {
                    var parentParagraph = placeholderText.Ancestors<Paragraph>().FirstOrDefault();
                    if (parentParagraph != null)
                    {
                        placeholderText.Text = placeholderText.Text.Replace("{CablesTable}", ""); // Remove placeholder

                        // Create the table
                        DocumentFormat.OpenXml.Wordprocessing.Table table = CreateCableTable(cablesOnTray);

                        // Insert table **AFTER** the placeholder paragraph
                        body.InsertAfter(table, parentParagraph);
                    }
                }

                wordDoc.MainDocumentPart.Document.Save();
            }
        }

        private DocumentFormat.OpenXml.Wordprocessing.Table CreateCableTable(List<Cable> cablesOnTray)
        {
            DocumentFormat.OpenXml.Wordprocessing.Table table = new DocumentFormat.OpenXml.Wordprocessing.Table();

            // Define table properties (border, width, alignment)
            TableProperties tblProps = new TableProperties(
                new TableBorders(
                    new DocumentFormat.OpenXml.Wordprocessing.TopBorder() { Val = BorderValues.Single, Size = 8 },
                    new DocumentFormat.OpenXml.Wordprocessing.BottomBorder() { Val = BorderValues.Single, Size = 8 },                                   
                    new DocumentFormat.OpenXml.Wordprocessing.LeftBorder() { Val = BorderValues.Single, Size = 8 },
                    new DocumentFormat.OpenXml.Wordprocessing.RightBorder() { Val = BorderValues.Single, Size = 8 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4 }
                )
            );
            table.AppendChild(tblProps);

            string[] headers = { "No.", "Cable name", "Cable type", "Cable diameter [mm]", "Cable weight [kg/m]" };
            int[] columnWidths = { 1000, 3000, 5000, 2000, 2000 }; // Wider "Cable type" column

            // Add header row
            TableRow headerRow = new TableRow();
            for (int i = 0; i < headers.Length; i++)
            {
                TableCell cell = CreateTableCell(headers[i], true, columnWidths[i]); // Pass width
                headerRow.Append(cell);
            }
            table.Append(headerRow);

            // Add data rows
            int index = 1;
            foreach (var cable in cablesOnTray)
            {
                TableRow row = new TableRow();

                row.Append(CreateTableCell(index.ToString(), false, columnWidths[0])); // Auto-incrementing No.
                row.Append(CreateTableCell(cable.Tag, false, columnWidths[1])); // Cable name
                row.Append(CreateTableCell(cable.CableType?.Type ?? "N/A", false, columnWidths[2])); // Wider Cable type
                row.Append(CreateTableCell(cable.CableType?.Diameter.ToString("F1", CultureInfo.InvariantCulture) ?? "0.0", false, columnWidths[3])); // Cable diameter
                row.Append(CreateTableCell(cable.CableType?.Weight.ToString("F3", CultureInfo.InvariantCulture) ?? "0.000", false, columnWidths[4])); // Cable weight


                table.Append(row);
                index++;
            }

            return table;
        }

        private TableCell CreateTableCell(string text, bool bold, int width)
        {
            DocumentFormat.OpenXml.Wordprocessing.RunProperties runProperties = new DocumentFormat.OpenXml.Wordprocessing.RunProperties();
            if (bold)
            {
                runProperties.Append(new DocumentFormat.OpenXml.Wordprocessing.Bold());
            }

            return new TableCell(
                new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width.ToString() }),
                new Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(runProperties, new DocumentFormat.OpenXml.Wordprocessing.Text(text)))
            );
        }



    }
}
