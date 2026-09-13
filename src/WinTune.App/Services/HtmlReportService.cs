using System.Net;
using System.Text;

namespace WinTune.App.Services;

public sealed record FindingRow(string ModuleName, string Severity, string Title, string Description);

public sealed class HtmlReportService
{
    private const string HtmlHead = """
        <!DOCTYPE html>
        <html lang="es"><head><meta charset="utf-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>WinTune Pro &mdash; Reporte</title>
        <style>
        :root{color-scheme:light dark}
        body{font-family:Segoe UI,Arial,sans-serif;margin:0;padding:24px;background:#fff;color:#111}
        @media(prefers-color-scheme:dark){
          body{background:#121212;color:#e5e5e5}
          th{background:#222}
          th,td{border-color:#444}
        }
        header{border-bottom:2px solid #ccc;padding-bottom:12px;margin-bottom:16px}
        h1{margin:0;font-size:22px}
        .meta{margin-top:8px;font-size:14px}
        .badge{display:inline-block;padding:4px 12px;border-radius:999px;background:#16a34a;color:#fff;font-weight:700}
        table{width:100%;border-collapse:collapse;font-size:14px}
        th,td{border:1px solid #999;padding:8px;text-align:left;vertical-align:top}
        th{background:#eee}
        </style></head><body>
        """;

    public string BuildReport(string scanDate, IReadOnlyList<FindingRow> findings, int healthScore)
    {
        findings ??= [];
        var sb = new StringBuilder();
        sb.Append(HtmlHead);
        sb.Append("<header><h1>WinTune Pro &mdash; Reporte de An&aacute;lisis</h1>");
        sb.Append($"<div class=\"meta\">Fecha: {WebUtility.HtmlEncode(scanDate)} &nbsp;");
        sb.Append($"<span class=\"badge\">Salud: {healthScore}/100</span></div></header>");
        sb.Append("<table><thead><tr><th>M&oacute;dulo</th><th>Severidad</th><th>T&iacute;tulo</th><th>Descripci&oacute;n</th></tr></thead><tbody>");

        foreach (var f in findings)
        {
            var mod   = WebUtility.HtmlEncode(f.ModuleName ?? "");
            var sev   = WebUtility.HtmlEncode(f.Severity ?? "");
            var title = WebUtility.HtmlEncode(f.Title ?? "");
            var desc  = WebUtility.HtmlEncode(f.Description ?? "");
            var (bg, fg) = SeverityStyle(f.Severity);
            sb.Append($"<tr><td>{mod}</td>");
            sb.Append($"<td style=\"background:{bg};color:{fg}\">{sev}</td>");
            sb.Append($"<td>{title}</td><td>{desc}</td></tr>\n");
        }

        sb.Append("</tbody></table></body></html>");
        return sb.ToString();
    }

    private static (string bg, string fg) SeverityStyle(string? sev) => sev switch
    {
        "Critical" => ("#dc2626", "white"),
        "High"     => ("#ea580c", "white"),
        "Medium"   => ("#ca8a04", "black"),
        "Low"      => ("#0284c7", "white"),
        _          => ("transparent", "inherit"),
    };
}
