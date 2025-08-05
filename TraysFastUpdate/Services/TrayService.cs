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
using MudBlazor;
using System.Linq;

namespace TraysFastUpdate.Services
{
    public class TrayService : ITrayService
    {
        private const double supportsWeight = 5.416;
        private const double KLDistance = 2;
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

            await TrayWeightCalculations(trayToUpdate);
            await CalculateFreePercentages(trayToUpdate);
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
            string directoryPath = Path.Combine("wwwroot", "files");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string reportType = "";

            if (tray.Purpose == "Type A (Pink color) for MV cables")
            {
                reportType = "ReportMacroTemplate_MV.docx";
            }
            else
            {
                reportType = "ReportMacroTemplate_Space.docx";
            }

            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            await ExportWordReportAsync(wwwrootPath, reportType, tray);

        }
        public async Task ExportWordReportAsync(string wwwrootPath, string templateFileName, Tray tray)
        {
            //remove old file
            string oldFilePath = Path.Combine(wwwrootPath, "files", $"TED_10004084142_001_04 - Cable tray calculations - {tray.Name}.docx");

            string templatePath = Path.Combine(wwwrootPath, templateFileName);
            string newFilePath = Path.Combine(wwwrootPath, "files", $"TED_10004084142_001_04 - Cable tray calculations - {tray.Name}.docx");

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

            var groundingCableNote = "";
            if (tray.Purpose == "Type B (Green color) for LV cables" || tray.Purpose == "Type BC (Teal color) for LV and Instrumentation and  Control cables, divided by separator")
            {
                groundingCableNote = "\r\n \tNote: Bare grounding copper cable with cross-section of 95 [mm²] with weight of 1.05 [kg/m] is included in the calculations. The cable itself will be mounted on the outside of the board of the tray and it is not included in the free space calculations.";
            }

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
        { "{GroundingCableNote}", groundingCableNote },
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
        { "{RevNo}", "03" },
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
            ReplacePlaceholdersWithImages(tray.Name, tray.Type);
        }
        public static void ReplacePlaceholdersWithImages(string trayName, string trayType)
        {
            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string wordFilePath = Path.Combine(wwwrootPath, "files", $"TED_10004084142_001_03 - Cable tray calculations - {trayName}.docx");

            // Determine image paths based on trayType
            string diagramPicPath = trayType.StartsWith("KL") ? "KL Diagram.jpg" :
                                    trayType.StartsWith("WSL") ? "WSL Diagram.jpg" : null;

            string trayPicPath = trayType.StartsWith("KL") ? "KL TrayPicture.jpg" :
                                 trayType.StartsWith("WSL") ? "WSL TrayPicture.jpg" : null;

            if (diagramPicPath == null || trayPicPath == null)
            {
                Console.WriteLine("Unsupported tray type.");
                return;
            }

            string diagramImagePath = Path.Combine(wwwrootPath, diagramPicPath);
            string trayImagePath = Path.Combine(wwwrootPath, trayPicPath);
            string fillPicture = Path.Combine(wwwrootPath, "images", $"{trayName}.jpg");

            // Open Word document
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(wordFilePath, true))
            {
                MainDocumentPart mainPart = wordDoc.MainDocumentPart;
                if (mainPart == null) return;

                // Replace placeholders with images
                ReplacePlaceholderWithImage(mainPart, "{DiagramTrayPic}", diagramImagePath);
                ReplacePlaceholderWithImage(mainPart, "{TrayPicture}", trayImagePath);
                ReplacePlaceholderWithImage(mainPart, "{FillPicture}", fillPicture);

                wordDoc.Save();
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

            if (cablesOnTray.Count == 0)
            {
                tray.CablesWeightPerMeter = 0;
                tray.CablesWeightLoad = 0;
                tray.ResultCablesWeightPerMeter = "No cables on this tray";
                tray.ResultCablesWeightLoad = "No cables on this tray";
                return;
            }

            if (tray.Purpose == "Type B (Green color) for LV cables" || tray.Purpose == "Type BC (Teal color) for LV and Instrumentation and  Control cables, divided by separator")
            {
                Cable groundingCable = new Cable
                {
                    CableType = new CableType
                    {
                        Diameter = 95,
                        Weight = 1.05
                    }
                };

                cablesOnTray.Add(groundingCable);
            }

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
            if (tray.ResultCablesWeightPerMeter == "No cables on this tray")
            {
                tray.TotalWeightLoadPerMeter = tray.TrayWeightLoadPerMeter;
                tray.TotalWeightLoad = tray.TrayOwnWeightLoad;
                tray.ResultTotalWeightLoadPerMeter = tray.ResultTrayWeightLoadPerMeter;
                tray.ResultTotalWeightLoad = tray.ResultTrayOwnWeightLoad;
                return;
            }

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
            if (tray.ResultCablesWeightPerMeter == "No cables on this tray")
            {
                tray.ResultSpaceOccupied = "N/A";
                tray.ResultSpaceAvailable = "N/A";
                tray.SpaceOccupied = 0;
                tray.SpaceAvailable = 100;
                return;
            }

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
                        (int rows, int columns) = CalculateRowsAndColumns(tray.Height - CProfileHeight, 1, sortedBundle.Value, "Power");

                        int row = 0;
                        int column = 0;

                        var sortedCables = sortedBundle.Value.OrderByDescending(x => x.CableType.Diameter).ToList();

                        if (sortedBundle.Key == "40.1-44.5" || sortedBundle.Key == "44.6 - 60")
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
                        (int rows, int columns) = CalculateRowsAndColumns(tray.Height - CProfileHeight, 1, sortedBundle.Value, "Control");

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
                else if (bundle.Key == "VFD")
                {
                    var sortedBundles = bundle.Value.OrderByDescending(x => x.Value[0].CableType.Diameter).ToList();

                    foreach (var sortedBundle in sortedBundles)
                    {
                        (int rows, int columns) = CalculateRowsAndColumns(tray.Height - CProfileHeight, 1, sortedBundle.Value, "VFD");

                        int row = 0;
                        int column = 0;

                        if (sortedBundle.Key == "30.1-42" || sortedBundle.Key == "42.1-60")
                        {
                            var groupByToLocation = sortedBundle.Value.GroupBy(x => x.ToLocation).ToList();
                            foreach (var cableGroup in groupByToLocation)
                            {
                                cableGroup.ToList().ForEach(cable =>
                                {
                                    int cableIndex = cableGroup.ToList().IndexOf(cable);
                                    if (cableIndex != 0 && cableIndex % 2 == 0 && cable.CableType.Diameter <= 45)
                                    {
                                        return;
                                    }

                                    bottomRow += cable.CableType.Diameter;
                                    bottomRow += spacing;

                                    cablesBottomRow.Add(cable);
                                });
                            }
                        }
                        else
                        {
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
            }

            var groupedByDiameterCables = cablesBottomRow.GroupBy(x => x.CableType.Diameter).ToList();

            tray.SpaceOccupied = bottomRow;
            tray.SpaceAvailable = Math.Round((double)(100 - (bottomRow / tray.Width * 100)), 2);

            if (tray.Purpose == "Type A (Pink color) for MV cables")
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
        private (int, int) CalculateRowsAndColumns(double trayHeight, int spacing, List<Cable> bundle, string purpose)
        {
            int rows = 0;
            int columns = 0;
            double diameter = bundle.Max(x => x.CableType.Diameter);

            if (purpose == "Power")
            {
                rows = Math.Min((int)Math.Floor((trayHeight) / (diameter)), 2);
                columns = (int)Math.Floor((double)bundle.Count / rows);
            }
            else if (purpose == "Control")
            {
                rows = Math.Min((int)Math.Floor((trayHeight) / (diameter)), 2);
                columns = Math.Min((int)Math.Ceiling((double)bundle.Count / rows), 20);
            }
            else if (purpose == "VFD")
            {
                rows = Math.Min((int)Math.Floor((trayHeight) / (diameter)), 2);
                columns = (int)Math.Floor((double)bundle.Count / rows);
            }

            if (bundle.Count == 2)
            {
                rows = 1;
                columns = 2;
                return (rows, columns);
            }

            if (rows > columns)
            {
                rows = (int)Math.Floor(Math.Ceiling(Math.Sqrt(bundle.Count)));
                columns = (int)Math.Floor(Math.Ceiling(Math.Sqrt(bundle.Count)));
            }

            return (rows, columns);
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
        private async Task InsertCableTableAsync(string filePath, Tray tray)
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
                        // Remove the placeholder text
                        placeholderText.Text = "";

                        // Create the table
                        DocumentFormat.OpenXml.Wordprocessing.Table table = CreateCableTable(cablesOnTray);

                        // Replace the paragraph with the table
                        body.ReplaceChild(table, parentParagraph);
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
        private static void ReplacePlaceholderWithImage(MainDocumentPart mainPart, string placeholder, string imagePath)
        {
            var paragraphs = mainPart.Document.Body.Elements<Paragraph>()
                .Where(p => p.InnerText.Contains(placeholder)).ToList();

            if (!File.Exists(imagePath) || !paragraphs.Any()) return;

            foreach (var paragraph in paragraphs)
            {
                DocumentFormat.OpenXml.Wordprocessing.Run run = paragraph.Elements<DocumentFormat.OpenXml.Wordprocessing.Run>().FirstOrDefault();
                if (run != null)
                {
                    run.RemoveAllChildren<DocumentFormat.OpenXml.Wordprocessing.Text>();
                    run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("")); // Clear text content

                    // Insert image
                    AddImageToRun(mainPart, run, imagePath);
                }
            }
        }
        private static void AddImageToRun(MainDocumentPart mainPart, DocumentFormat.OpenXml.Wordprocessing.Run run, string imagePath)
        {
            ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);
            using (FileStream stream = new FileStream(imagePath, FileMode.Open))
            {
                imagePart.FeedData(stream);
            }

            // Set custom width and height (in EMUs: 1 inch = 914400 EMUs)
            long imageWidth = 0;  // Wider image (default ~5.5 inches)
            long imageHeight = 0; // Adjusted height (~3.3 inches)

            string relationshipId = mainPart.GetIdOfPart(imagePart);

            long maxWidth = 6740000;  // Available width in EMUs
            long maxHeight = 8075981; // Available height in EMUs

            // Predefined sizes for specific images
            if (imagePath.Contains("Diagram"))
            {
                imageHeight = 3991968;
                imageWidth = 5998464;
            }
            else if (imagePath.Contains("TrayPicture"))
            {
                imageHeight = 5802096;
                imageWidth = 4645152;
            }
            else
            {
                // Read image dimensions dynamically
                if (File.Exists(imagePath))
                {
                    using (Image img = Image.FromFile(imagePath))
                    {
                        // Convert pixels to EMUs (1 pixel = 9525 EMUs)
                        long originalWidth = img.Width * 9525;
                        long originalHeight = img.Height * 9525;

                        // Calculate scaling factor to maintain aspect ratio
                        double widthRatio = (double)maxWidth / originalWidth;
                        double heightRatio = (double)maxHeight / originalHeight;
                        double scaleFactor = Math.Min(widthRatio, heightRatio);

                        // Apply scaling
                        imageWidth = (long)(originalWidth * scaleFactor);
                        imageHeight = (long)(originalHeight * scaleFactor);
                    }
                }
            }

            DocumentFormat.OpenXml.Wordprocessing.Drawing drawing = new DocumentFormat.OpenXml.Wordprocessing.Drawing(
                new wp.Inline(
                    new wp.Extent() { Cx = imageWidth, Cy = imageHeight },  // Set new image size
                    new wp.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new wp.DocProperties() { Id = 1, Name = "Inserted Image" },
                    new wp.NonVisualGraphicFrameDrawingProperties(new a.GraphicFrameLocks() { NoChangeAspect = true }),
                    new a.Graphic(
                        new a.GraphicData(
                            new pic.Picture(
                                new pic.NonVisualPictureProperties(
                                    new pic.NonVisualDrawingProperties() { Id = 0, Name = "New Image" },
                                    new pic.NonVisualPictureDrawingProperties()
                                ),
                                new pic.BlipFill(
                                    new a.Blip() { Embed = relationshipId },
                                    new a.Stretch(new a.FillRectangle())
                                ),
                                new pic.ShapeProperties(
                                    new a.Transform2D(
                                        new a.Offset() { X = 0L, Y = 0L },
                                        new a.Extents() { Cx = imageWidth, Cy = imageHeight }
                                    ),
                                    new a.PresetGeometry(new a.AdjustValueList()) { Preset = a.ShapeTypeValues.Rectangle }
                                )
                            )
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                    )
                )
            );

            run.Append(drawing);
        }
        public async Task ExportCanvasImageAsync(Excubo.Blazor.Canvas.Canvas canvas, string trayName)
        {
            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string directoryPath = Path.Combine("wwwroot", "images");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string filePath = Path.Combine(wwwrootPath, "images", $"{trayName}.jpg");
            //remove old file
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            try
            {
                var dataUrl = await canvas.ToDataURLAsync("image/jpeg", 0.8f);
                var base64 = dataUrl.Split(',')[1];
                var bytes = Convert.FromBase64String(base64);
                await File.WriteAllBytesAsync(filePath, bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public async Task ExportTrayTableEntriesAsync()
        {
            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            //Remove the old file
            if (File.Exists(Path.Combine(wwwrootPath, "Trays.xlsx")))
            {
                File.Delete(Path.Combine(wwwrootPath, "Trays.xlsx"));
            }

            SpreadsheetDocument document = SpreadsheetDocument.Create(Path.Combine(wwwrootPath, "Trays.xlsx"), SpreadsheetDocumentType.Workbook);

            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Trays" };
            sheets.Append(sheet);

            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>() ?? worksheetPart.Worksheet.AppendChild(new SheetData());

            List<Tray> trays = await _repository.All<Tray>().ToListAsync();

            Row headerRow = new Row() { RowIndex = 1 };
            sheetData.Append(headerRow);

            string[] headers = { "Name", "Type", "Purpose", "Width [mm]", "Height [mm]", "Length [mm]", "Cables on tray [pcs.]", "Available space [%]" };
            for (int i = 0; i < headers.Length; i++)
            {
                Cell headerCell = new Cell() { CellReference = ((char)('A' + i)).ToString() + "1" };
                headerCell.CellValue = new CellValue(headers[i]);
                headerCell.DataType = new EnumValue<CellValues>(CellValues.String);
                headerRow.Append(headerCell);
            }

            for (int i = 0; i < trays.Count; i++)
            {
                int cablesCount = (await _cableService.GetCablesOnTrayAsync(trays[i])).Count;

                Row row = new Row() { RowIndex = (uint)(i + 2) }; // Start from row 2
                sheetData.Append(row);

                Cell cell = new Cell() { CellReference = "A" + (i + 2) };
                cell.CellValue = new CellValue(trays[index: i].Name);
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.Append(cell);

                cell = new Cell() { CellReference = "B" + (i + 2) };
                cell.CellValue = new CellValue(trays[index: i].Type);
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.Append(cell);

                cell = new Cell() { CellReference = "C" + (i + 2) };
                cell.CellValue = new CellValue(trays[index: i].Purpose);
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.Append(cell);

                cell = new Cell() { CellReference = "D" + (i + 2) };
                cell.CellValue = new CellValue(trays[index: i].Width.ToString());
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                row.Append(cell);

                cell = new Cell() { CellReference = "E" + (i + 2) };
                cell.CellValue = new CellValue(trays[index: i].Height.ToString());
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                row.Append(cell);

                cell = new Cell() { CellReference = "F" + (i + 2) };
                cell.CellValue = new CellValue(trays[index: i].Length.ToString("F3"));
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                row.Append(cell);

                cell = new Cell() { CellReference = "G" + (i + 2) };
                cell.CellValue = new CellValue(cablesCount);
                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                row.Append(cell);


                cell = new Cell() { CellReference = "H" + (i + 2) };
                if (trays[i].Purpose == "Type A (Pink color) for MV cables")
                {
                    cell.CellValue = new CellValue("N/A");
                }
                else
                {
                    cell.CellValue = new CellValue(trays[index: i].SpaceAvailable?.ToString("F2"));
                }
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.Append(cell);
            }

            TableDefinitionPart tableDefinitionPart = worksheetPart.AddNewPart<TableDefinitionPart>();
            DocumentFormat.OpenXml.Spreadsheet.Table table = new DocumentFormat.OpenXml.Spreadsheet.Table()
            {
                Id = 1,
                DisplayName = "Trays",
                Name = "Trays",
                Reference = "A1:H" + (trays.Count + 1) // Covers header + all data rows
            };

            AutoFilter autoFilter = new AutoFilter() { Reference = "A1:G" + (trays.Count + 1) };

            TableColumns tableColumns = new TableColumns() { Count = (uint)headers.Length };
            tableColumns.Append(new TableColumn() { Id = 1, Name = "Name" });
            tableColumns.Append(new TableColumn() { Id = 2, Name = "Type" });
            tableColumns.Append(new TableColumn() { Id = 3, Name = "Purpose" });
            tableColumns.Append(new TableColumn() { Id = 4, Name = "Width [mm]" });
            tableColumns.Append(new TableColumn() { Id = 5, Name = "Height [mm]" });
            tableColumns.Append(new TableColumn() { Id = 6, Name = "Length [mm]" });
            tableColumns.Append(new TableColumn() { Id = 7, Name = "Cables on tray [pcs.]" });
            tableColumns.Append(new TableColumn() { Id = 8, Name = "Available space [%]" });

            TableStyleInfo tableStyleInfo = new TableStyleInfo()
            {
                Name = "TableStyleLight8", // Built-in Excel style
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
