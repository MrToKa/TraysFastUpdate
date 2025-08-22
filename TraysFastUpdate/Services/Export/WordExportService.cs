using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System.Globalization;
using TraysFastUpdate.Common.Constants;
using TraysFastUpdate.Models;
using TraysFastUpdate.Services.Contracts;
using TraysFastUpdate.Services.Export;

// Namespace aliases to resolve conflicts
using W = DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace TraysFastUpdate.Services.Export;

public class WordExportService : IWordExportService
{
    private readonly ICableService _cableService;

    public WordExportService(ICableService cableService)
    {
        _cableService = cableService;
    }

    public async Task ExportWordReportAsync(string wwwrootPath, string templateFileName, Tray tray)
    {
        string templatePath = System.IO.Path.Combine(wwwrootPath, templateFileName);
        string newFilePath = System.IO.Path.Combine(wwwrootPath, "files", $"TED_10004084142_001_04 - Cable tray calculations - {tray.Name}.docx");

        double distance = GetDistanceByTrayType(tray.Type);

        // Copy template and rename it
        File.Copy(templatePath, newFilePath, true);

        var replacements = CreateReplacementsDictionary(tray, distance);

        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(newFilePath, true))
        {
            var mainPart = wordDoc.MainDocumentPart;
            if (mainPart == null) return;

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
        await ReplacePlaceholdersWithImagesAsync(tray.Name, tray.Type);
    }

    private static double GetDistanceByTrayType(string trayType)
    {
        return trayType.StartsWith(TrayConstants.TrayTypes.KL) 
            ? TrayConstants.KLDistance 
            : TrayConstants.WSLDistance;
    }

    private static string GetGroundingCableNote(string trayPurpose)
    {
        return trayPurpose == TrayConstants.TrayPurposes.TypeB || 
               trayPurpose == TrayConstants.TrayPurposes.TypeBC
            ? "\r\n \tNote: Bare grounding copper cable with cross-section of 95 [mm²] with weight of 1.05 [kg/m] is included in the calculations. The cable itself will be mounted on the outside of the board of the tray and it is not included in the free space calculations."
            : "";
    }

    private static Dictionary<string, string> CreateReplacementsDictionary(Tray tray, double distance)
    {
        return new Dictionary<string, string>
        {
            { "{TrayName}", tray.Name },
            { "{TrayType}", tray.Type },
            { "{TrayPurpose}", tray.Purpose },
            { "{TrayHeight}", tray.Height.ToString("F0", CultureInfo.InvariantCulture) },
            { "{TrayWidth}", tray.Width.ToString("F0", CultureInfo.InvariantCulture) },
            { "{TrayLength}", tray.Length.ToString("F2", CultureInfo.InvariantCulture) },
            { "{TrayWeight}", tray.Weight.ToString("F3", CultureInfo.InvariantCulture) },
            { "{SupportsCount}", tray.ResultSupportsCount ?? ""},
            { "{Distance}", distance.ToString("F1", CultureInfo.InvariantCulture) },
            { "{SupportWeight}", TrayConstants.SupportsWeight.ToString("F3") },
            { "{GroundingCableNote}", GetGroundingCableNote(tray.Purpose) },
            { "{SuppTotalWeight}", tray.ResultSupportsTotalWeight ?? ""},
            { "{SuppWeightPerMeter}", tray.ResultSupportsWeightLoadPerMeter ?? ""},
            { "{TrayLoadPerMeter}", tray.ResultTrayWeightLoadPerMeter ?? ""},
            { "{TrayWeightCalcs}", tray.ResultTrayOwnWeightLoad ?? ""},
            { "{CablesWeightPerMeter}", tray.ResultCablesWeightPerMeter ?? ""},
            { "{CablesWeightCalculations}", tray.ResultCablesWeightLoad ?? ""},
            { "{TotalPerPoint}", tray.ResultTotalWeightLoadPerMeter ?? ""},
            { "{TotalCalc}", tray.ResultTotalWeightLoad ?? ""},
            { "{TrayHeightFormula}", (tray.Height - 15).ToString("F0", CultureInfo.InvariantCulture)},
            { "{DiametersSum}", tray.ResultSpaceOccupied ?? ""},
            { "{FreeSpace}", tray.ResultSpaceAvailable ?? ""},
            { "{DocNo}", "10004084142" },
            { "{DocType}", "TED" },
            { "{DocPart}", "001" },
            { "{RevNo}", "04" },
            { "{TodayDate}", DateTime.Now.ToString("dd-MM-yyyy") }
        };
    }

    private void ReplaceTextInElement(OpenXmlElement element, Dictionary<string, string> replacements)
    {
        if (element == null) return;

        foreach (var paragraph in element.Descendants<W.Paragraph>())
        {
            string paragraphText = string.Join("", paragraph.Descendants<W.Text>().Select(t => t.Text));

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
                foreach (var text in paragraph.Descendants<W.Text>())
                {
                    text.Text = string.Empty;
                }

                // Create a new Text element with the full updated content
                var run = paragraph.GetFirstChild<W.Run>() ?? paragraph.AppendChild(new W.Run());
                var textElement = run.GetFirstChild<W.Text>() ?? run.AppendChild(new W.Text());
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
            if (mainPart == null) return;
            
            var body = mainPart.Document.Body;
            if (body == null) return;

            // Locate the placeholder
            var placeholderText = body.Descendants<W.Text>()
                .FirstOrDefault(t => t.Text.Trim() == "{CablesTable}");

            if (placeholderText != null)
            {
                var parentParagraph = placeholderText.Ancestors<W.Paragraph>().FirstOrDefault();
                if (parentParagraph != null)
                {
                    // Remove the placeholder text
                    placeholderText.Text = "";

                    // Create the table
                    W.Table table = CreateCableTable(cablesOnTray);

                    // Replace the paragraph with the table
                    body.ReplaceChild(table, parentParagraph);
                }
            }

            wordDoc.MainDocumentPart.Document.Save();
        }
    }

    private W.Table CreateCableTable(List<Cable> cablesOnTray)
    {
        W.Table table = new W.Table();

        // Define table properties (border, width, alignment)
        W.TableProperties tblProps = new W.TableProperties(
            new W.TableBorders(
                new W.TopBorder() { Val = W.BorderValues.Single, Size = 8 },
                new W.BottomBorder() { Val = W.BorderValues.Single, Size = 8 },
                new W.LeftBorder() { Val = W.BorderValues.Single, Size = 8 },
                new W.RightBorder() { Val = W.BorderValues.Single, Size = 8 },
                new W.InsideHorizontalBorder() { Val = W.BorderValues.Single, Size = 4 },
                new W.InsideVerticalBorder() { Val = W.BorderValues.Single, Size = 4 }
            )
        );
        table.AppendChild(tblProps);

        string[] headers = { "No.", "Cable name", "Cable type", "Cable diameter [mm]", "Cable weight [kg/m]" };
        int[] columnWidths = { 1000, 3000, 5000, 2000, 2000 };

        // Add header row
        W.TableRow headerRow = new W.TableRow();
        for (int i = 0; i < headers.Length; i++)
        {
            W.TableCell cell = CreateTableCell(headers[i], true, columnWidths[i]);
            headerRow.Append(cell);
        }
        table.Append(headerRow);

        // Add data rows
        int index = 1;
        foreach (var cable in cablesOnTray)
        {
            W.TableRow row = new W.TableRow();

            row.Append(CreateTableCell(index.ToString(), false, columnWidths[0]));
            row.Append(CreateTableCell(cable.Tag, false, columnWidths[1]));
            row.Append(CreateTableCell(cable.CableType?.Type ?? "N/A", false, columnWidths[2]));
            row.Append(CreateTableCell(cable.CableType?.Diameter.ToString("F1", CultureInfo.InvariantCulture) ?? "0.0", false, columnWidths[3]));
            row.Append(CreateTableCell(cable.CableType?.Weight.ToString("F3", CultureInfo.InvariantCulture) ?? "0.000", false, columnWidths[4]));

            table.Append(row);
            index++;
        }

        return table;
    }

    private W.TableCell CreateTableCell(string text, bool bold, int width)
    {
        W.RunProperties runProperties = new W.RunProperties();
        if (bold)
        {
            runProperties.Append(new W.Bold());
        }

        return new W.TableCell(
            new W.TableCellProperties(new W.TableCellWidth { Type = W.TableWidthUnitValues.Dxa, Width = width.ToString() }),
            new W.Paragraph(new W.Run(runProperties, new W.Text(text)))
        );
    }

    private static async Task ReplacePlaceholdersWithImagesAsync(string trayName, string trayType)
    {
        await Task.Run(() =>
        {
            string wwwrootPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string wordFilePath = System.IO.Path.Combine(wwwrootPath, "files", $"TED_10004084142_001_04 - Cable tray calculations - {trayName}.docx");

            // Determine image paths based on trayType
            string? diagramPicPath = trayType.StartsWith("KL") ? "KL Diagram.jpg" :
                                    trayType.StartsWith("WSL") ? "WSL Diagram.jpg" : null;

            string? trayPicPath = trayType.StartsWith("KL") ? "KL TrayPicture.jpg" :
                                 trayType.StartsWith("WSL") ? "WSL TrayPicture.jpg" : null;

            if (diagramPicPath == null || trayPicPath == null)
            {
                Console.WriteLine("Unsupported tray type for image replacement.");
                return;
            }

            string diagramImagePath = System.IO.Path.Combine(wwwrootPath, diagramPicPath);
            string trayImagePath = System.IO.Path.Combine(wwwrootPath, trayPicPath);
            string fillPicture = System.IO.Path.Combine(wwwrootPath, "images", $"{trayName}.jpg");

            if (!File.Exists(wordFilePath))
            {
                Console.WriteLine($"Word file not found: {wordFilePath}");
                return;
            }

            // Open Word document
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(wordFilePath, true))
            {
                MainDocumentPart? mainPart = wordDoc.MainDocumentPart;
                if (mainPart == null) return;

                // Replace placeholders with images
                ReplacePlaceholderWithImage(mainPart, "{DiagramTrayPic}", diagramImagePath);
                ReplacePlaceholderWithImage(mainPart, "{TrayPicture}", trayImagePath);
                ReplacePlaceholderWithImage(mainPart, "{FillPicture}", fillPicture);

                wordDoc.Save();
            }
        });
    }

    private static void ReplacePlaceholderWithImage(MainDocumentPart mainPart, string placeholder, string imagePath)
    {
        var paragraphs = mainPart.Document.Body?.Elements<W.Paragraph>()
            .Where(p => p.InnerText.Contains(placeholder)).ToList();

        if (paragraphs == null || !paragraphs.Any() || !File.Exists(imagePath)) return;

        foreach (var paragraph in paragraphs)
        {
            W.Run? run = paragraph.Elements<W.Run>().FirstOrDefault();
            if (run != null)
            {
                run.RemoveAllChildren<W.Text>();
                run.AppendChild(new W.Text(""));

                // Insert image
                AddImageToRun(mainPart, run, imagePath);
            }
        }
    }

    private static void AddImageToRun(MainDocumentPart mainPart, W.Run run, string imagePath)
    {
        try
        {
            ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);
            using (FileStream stream = new FileStream(imagePath, FileMode.Open))
            {
                imagePart.FeedData(stream);
            }

            long imageWidth = 0;
            long imageHeight = 0;
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
                    using (System.Drawing.Image img = System.Drawing.Image.FromFile(imagePath))
                    {
                        long originalWidth = img.Width * 9525;
                        long originalHeight = img.Height * 9525;

                        double widthRatio = (double)maxWidth / originalWidth;
                        double heightRatio = (double)maxHeight / originalHeight;
                        double scaleFactor = Math.Min(widthRatio, heightRatio);

                        imageWidth = (long)(originalWidth * scaleFactor);
                        imageHeight = (long)(originalHeight * scaleFactor);
                    }
                }
            }

            W.Drawing drawing = new W.Drawing(
                new DW.Inline(
                    new DW.Extent() { Cx = imageWidth, Cy = imageHeight },
                    new DW.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties() { Id = 1, Name = "Inserted Image" },
                    new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks() { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties() { Id = 0, Name = "New Image" },
                                    new PIC.NonVisualPictureDrawingProperties()
                                ),
                                new PIC.BlipFill(
                                    new A.Blip() { Embed = relationshipId },
                                    new A.Stretch(new A.FillRectangle())
                                ),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset() { X = 0L, Y = 0L },
                                        new A.Extents() { Cx = imageWidth, Cy = imageHeight }
                                    ),
                                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
                                )
                            )
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                    )
                )
            );

            run.Append(drawing);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding image to Word document: {ex.Message}");
        }
    }
}