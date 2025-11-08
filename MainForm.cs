using System;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace WindowsEventLogReporter
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private async void BtnPrint_Click(object? sender, EventArgs e)
        {
            try
            {
                btnPrint.Enabled = false;
                btnPrint.Text = "Working...";

                string channel = comboLogSource.SelectedItem?.ToString() ?? "Application";
                string levelName = comboLevel.SelectedItem?.ToString() ?? "Error";
                string rangeName = comboRange.SelectedItem?.ToString() ?? "Last 24 hours";

                int? level = MapLevel(levelName);
                long? millis = MapRangeToMillis(rangeName);
                string xPath = BuildXPath(level, millis);

                var query = new EventLogQuery(channel, PathType.LogName, xPath)
                {
                    ReverseDirection = true
                };

                using var reader = new EventLogReader(query);

                string reportDir = AppDomain.CurrentDomain.BaseDirectory;
                string fileName = $"WindowsLogs_{channel}_{Sanitize(levelName)}_{Sanitize(rangeName)}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                string fullPath = Path.Combine(reportDir, fileName);

                int count = 0;
                var html = new StringBuilder();
                WriteHtmlHeader(html, channel, levelName, rangeName);

                for (EventRecord? rec = reader.ReadEvent(); rec != null; rec = reader.ReadEvent())
                {
                    count++;
                    string id = rec.Id.ToString(CultureInfo.InvariantCulture);
                    string time = rec.TimeCreated?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                    string provider = rec.ProviderName ?? "N/A";
                    string task = (rec.TaskDisplayName ?? rec.Task.ToString()) ?? "N/A";
                    string keywords = rec.KeywordsDisplayNames != null ? string.Join(", ", rec.KeywordsDisplayNames) : "N/A";
                    string levelText = LevelToString(rec.Level);
                    string message = SafeMessage(rec);

                    html.AppendLine("<section class=\"record\">");
                    html.AppendLine($"  <div><span class=\"lbl\">Date/Time:</span> {Encode(time)}</div>");
                    html.AppendLine($"  <div><span class=\"lbl\">Event ID:</span> {Encode(id)}</div>");
                    html.AppendLine($"  <div><span class=\"lbl\">Provider:</span> {Encode(provider)}</div>");
                    html.AppendLine($"  <div><span class=\"lbl\">Task:</span> {Encode(task)}</div>");
                    html.AppendLine($"  <div><span class=\"lbl\">Keywords:</span> {Encode(keywords)}</div>");
                    html.AppendLine($"  <div><span class=\"lbl\">Level:</span> {FormatLevelBold(levelText)}</div>");
                    html.AppendLine("  <details>");
                    html.AppendLine("    <summary>Message</summary>");
                    html.AppendLine($"    <pre>{Encode(message)}</pre>");
                    html.AppendLine("  </details>");
                    html.AppendLine("</section>");
                }

                WriteHtmlFooter(html, count);
                await File.WriteAllTextAsync(fullPath, html.ToString(), Encoding.UTF8);

                MessageBox.Show(
                    count == 0
                        ? $"No matching events found.\nReport saved anyway:\n{fullPath}"
                        : $"Report saved:\n{fullPath}",
                    "Done",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (EventLogNotFoundException ex)
            {
                MessageBox.Show($"Failed to query logs.\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Access denied reading Windows Logs. Run as Administrator.\n\n" + ex.Message,
                    "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error:\n{ex}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnPrint.Enabled = true;
                btnPrint.Text = "Print";
            }
        }

        // Helper methods

        private static int? MapLevel(string levelName) => levelName switch
        {
            "Critical" => 1,
            "Error" => 2,
            "Warning" => 3,
            _ => null
        };

        private static long? MapRangeToMillis(string rangeName) => rangeName switch
        {
            "Anytime" => null,
            "Last hour" => TimeSpan.FromHours(1).TotalMillisecondsAsLong(),
            "Last 24 hours" => TimeSpan.FromHours(24).TotalMillisecondsAsLong(),
            "Last 7 days" => TimeSpan.FromDays(7).TotalMillisecondsAsLong(),
            _ => null
        };

        private static string BuildXPath(int? level, long? millis)
        {
            var conditions = new StringBuilder();

            if (level.HasValue)
            {
                if (conditions.Length > 0) conditions.Append(" and ");
                conditions.Append($"Level={level.Value}");
            }

            if (millis.HasValue)
            {
                if (conditions.Length > 0) conditions.Append(" and ");
                conditions.Append($"TimeCreated[timediff(@SystemTime) <= {millis.Value}]");
            }

            string inner = conditions.Length == 0 ? "System" : $"System[{conditions}]";
            return $"*[ {inner} ]";
        }

        private static string SafeMessage(EventRecord rec)
        {
            try { return rec.FormatDescription() ?? "(no message)"; }
            catch { return "(message not available)"; }
        }

        private static string Encode(string s) => WebUtility.HtmlEncode(s);

        private static string FormatLevelBold(string? level) =>
            level?.ToLowerInvariant() switch
            {
                "error" => "<strong style='color:red;'>Error</strong>",
                "warning" => "<strong style='color:orange;'>Warning</strong>",
                "critical" => "<strong style='color:crimson;'>Critical</strong>",
                _ => Encode(level ?? "N/A")
            };

        private static string LevelToString(byte? level) => level switch
        {
            1 => "Critical",
            2 => "Error",
            3 => "Warning",
            4 => "Information",
            5 => "Verbose",
            _ => "Unknown"
        };

        private static string Sanitize(string s)
        {
            foreach (var ch in Path.GetInvalidFileNameChars())
                s = s.Replace(ch, '_');
            return s.Replace(' ', '_');
        }

        private static void WriteHtmlHeader(StringBuilder sb, string channel, string level, string range)
        {
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
            sb.AppendLine("<title>Windows Event Log Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;}");
            sb.AppendLine(".record{border:1px solid #ccc;border-radius:8px;padding:10px;margin:10px 0;}");
            sb.AppendLine(".lbl{font-weight:600;}");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine($"<h1>Windows Event Log Report</h1>");
            sb.AppendLine($"<p><b>Source:</b> {channel} | <b>Level:</b> {level} | <b>Range:</b> {range}</p>");
            sb.AppendLine("<hr>");
        }

        private static void WriteHtmlFooter(StringBuilder sb, int count)
        {
            sb.AppendLine("<hr>");
            sb.AppendLine($"<p><b>Total matching events:</b> {count}</p>");
            sb.AppendLine("<footer style='text-align:center;margin-top:40px;font-style:italic;'>");
            sb.AppendLine("Application Design by Noe Tovar-MBA 2025  |  Visit me at <a href='https://noetovar.com'>noetovar.com</a>");
            sb.AppendLine("</footer>");
            sb.AppendLine("</body></html>");
        }
    }

    internal static class TimeSpanExtensions
    {
        public static long TotalMillisecondsAsLong(this TimeSpan ts) =>
            (long)Math.Round(ts.TotalMilliseconds, MidpointRounding.AwayFromZero);
    }
}
