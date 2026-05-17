using System.Globalization;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using DataFlow.Core.Models;

namespace DataFlow.UI.Wpf.Services;

/// <summary>Parses CSV, TSV, Excel (.xlsx/.xls) locally into ChartDataDto. PDF goes via API.</summary>
public static class FileParserService
{
    public static bool CanParseLocally(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".csv" or ".tsv" or ".txt" or ".xlsx" or ".xls";
    }

    public static ChartDataDto Parse(string path, int maxRows = 10_000)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".xlsx" or ".xls" => ParseExcel(path, maxRows),
            _ => ParseCsv(path, maxRows)
        };
    }

    // ── Excel ──────────────────────────────────────────────────────
    private static ChartDataDto ParseExcel(string path, int maxRows)
    {
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheets.First();
        var used = ws.RangeUsed();
        if (used == null) return new ChartDataDto();

        var rows = used.RowsUsed().ToList();
        if (rows.Count == 0) return new ChartDataDto();

        var headers = rows[0].Cells().Select(c => c.GetString().Trim()).ToList();
        var data = new List<Dictionary<string, object?>>();

        foreach (var row in rows.Skip(1).Take(maxRows))
        {
            var cells = row.Cells().ToList();
            var dict = new Dictionary<string, object?>();
            for (var i = 0; i < headers.Count; i++)
            {
                var cell = i < cells.Count ? cells[i] : null;
                dict[headers[i]] = ParseCell(cell?.GetString() ?? "");
            }
            data.Add(dict);
        }

        return new ChartDataDto { Headers = headers, Rows = data };
    }

    // ── CSV / TSV ──────────────────────────────────────────────────
    private static ChartDataDto ParseCsv(string path, int maxRows)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var delimiter = ext == ".tsv" ? '\t' : DetectDelimiter(path);
        var lines = File.ReadLines(path, Encoding.UTF8).Take(maxRows + 1).ToList();
        if (lines.Count == 0) return new ChartDataDto();

        var headers = SplitLine(lines[0], delimiter).Select(h => h.Trim()).ToList();
        var data = new List<Dictionary<string, object?>>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cells = SplitLine(line, delimiter);
            var dict = new Dictionary<string, object?>();
            for (var i = 0; i < headers.Count; i++)
                dict[headers[i]] = ParseCell(i < cells.Count ? cells[i].Trim() : "");
            data.Add(dict);
        }

        return new ChartDataDto { Headers = headers, Rows = data };
    }

    private static char DetectDelimiter(string path)
    {
        var line = File.ReadLines(path).FirstOrDefault() ?? "";
        return line.Count(c => c == '\t') > line.Count(c => c == ',') ? '\t' : ',';
    }

    private static List<string> SplitLine(string line, char delimiter)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        foreach (var ch in line)
        {
            if (ch == '"') { inQuotes = !inQuotes; continue; }
            if (ch == delimiter && !inQuotes) { result.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(ch);
        }
        result.Add(sb.ToString());
        return result;
    }

    private static object? ParseCell(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
        return raw;
    }
}
