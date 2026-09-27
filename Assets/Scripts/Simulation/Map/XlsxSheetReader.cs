using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;

/// <summary>
/// Reads the text of every cell of one sheet of an Excel (.xlsx) file.
///
/// An .xlsx file is a zip archive of XML files. The ones we need:
///   xl/workbook.xml             the list of sheets, by name
///   xl/_rels/workbook.xml.rels  which XML file holds each sheet
///   xl/sharedStrings.xml        the text of cells that contain words (cells only store an index into this list)
///   xl/worksheets/sheet1.xml    the cells of one sheet
/// </summary>
public static class XlsxSheetReader
{
    static readonly XNamespace SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    static readonly XNamespace DocumentRelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    static readonly XNamespace PackageRelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";

    /// <summary>
    /// Returns the text of each cell as [row, column], starting at cell A1. Empty cells are "".
    /// The grid is as big as the last row and column that contain something.
    /// </summary>
    public static string[,] ReadSheet(byte[] xlsxFileBytes, string sheetName)
    {
        using (var zip = new ZipArchive(new MemoryStream(xlsxFileBytes), ZipArchiveMode.Read))
        {
            string sheetFile = FindSheetFile(zip, sheetName);
            List<string> sharedTexts = ReadSharedTexts(zip);
            XDocument sheet = ReadXml(zip, sheetFile);

            var texts = new Dictionary<GridPosition, string>();
            int rowCount = 0;
            int columnCount = 0;

            foreach (XElement cell in sheet.Descendants(SpreadsheetNamespace + "c"))
            {
                string text = ReadCellText(cell, sharedTexts);
                if (text == "") continue;

                GridPosition position = ParseCellAddress((string)cell.Attribute("r"));
                texts[position] = text;
                rowCount = Math.Max(rowCount, position.Row + 1);
                columnCount = Math.Max(columnCount, position.Column + 1);
            }

            var grid = new string[rowCount, columnCount];
            for (int row = 0; row < rowCount; row++)
                for (int column = 0; column < columnCount; column++)
                    grid[row, column] = texts.TryGetValue(new GridPosition(row, column), out string text) ? text : "";

            return grid;
        }
    }

    static string FindSheetFile(ZipArchive zip, string sheetName)
    {
        XDocument workbook = ReadXml(zip, "xl/workbook.xml");
        string relationshipId = null;
        var sheetNames = new List<string>();
        foreach (XElement sheet in workbook.Descendants(SpreadsheetNamespace + "sheet"))
        {
            string name = (string)sheet.Attribute("name");
            sheetNames.Add(name);
            if (name == sheetName)
                relationshipId = (string)sheet.Attribute(DocumentRelationshipsNamespace + "id");
        }

        if (relationshipId == null)
            throw new CityMapException($"The Excel file has no sheet called '{sheetName}'. Its sheets are: {string.Join(", ", sheetNames)}.");

        XDocument relationships = ReadXml(zip, "xl/_rels/workbook.xml.rels");
        foreach (XElement relationship in relationships.Descendants(PackageRelationshipsNamespace + "Relationship"))
        {
            if ((string)relationship.Attribute("Id") != relationshipId) continue;

            // The target is usually relative to the xl folder ("worksheets/sheet1.xml"), sometimes absolute ("/xl/...").
            string target = (string)relationship.Attribute("Target");
            return target.StartsWith("/") ? target.Substring(1) : "xl/" + target;
        }

        throw new CityMapException($"The Excel file is damaged: sheet '{sheetName}' has no data.");
    }

    static List<string> ReadSharedTexts(ZipArchive zip)
    {
        var texts = new List<string>();
        if (zip.GetEntry("xl/sharedStrings.xml") == null) return texts; // a workbook without any words

        XDocument sharedStrings = ReadXml(zip, "xl/sharedStrings.xml");
        foreach (XElement item in sharedStrings.Root.Elements(SpreadsheetNamespace + "si"))
            texts.Add(JoinTextParts(item));
        return texts;
    }

    static string ReadCellText(XElement cell, List<string> sharedTexts)
    {
        string type = (string)cell.Attribute("t") ?? "n";

        // Text typed straight into the cell (not in the shared list).
        if (type == "inlineStr")
            return JoinTextParts(cell);

        string value = (string)cell.Element(SpreadsheetNamespace + "v") ?? "";

        // Text stored in the shared list: the cell only holds its index.
        if (type == "s")
            return sharedTexts[int.Parse(value)];

        // Numbers, true/false and formula results are stored as they are.
        return value;
    }

    /// <summary>Text can be split in several runs (for example half bold, half not); we join them.</summary>
    static string JoinTextParts(XElement element)
    {
        string text = "";
        foreach (XElement part in element.Descendants(SpreadsheetNamespace + "t"))
            text += part.Value;
        return text;
    }

    /// <summary>"G13" -> row 12, column 6.</summary>
    static GridPosition ParseCellAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            throw new CityMapException("The Excel file has a cell without an address; save it again from Excel.");

        int column = 0;
        int index = 0;
        while (index < address.Length && char.IsLetter(address[index]))
        {
            column = column * 26 + (char.ToUpperInvariant(address[index]) - 'A' + 1);
            index++;
        }
        int row = int.Parse(address.Substring(index));
        return new GridPosition(row - 1, column - 1);
    }

    static XDocument ReadXml(ZipArchive zip, string path)
    {
        ZipArchiveEntry entry = zip.GetEntry(path);
        if (entry == null)
            throw new CityMapException($"This doesn't look like an Excel .xlsx file (missing {path}).");

        using (Stream stream = entry.Open())
            return XDocument.Load(stream);
    }
}
