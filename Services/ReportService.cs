using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Syncfusion.IndustrialMonitoring.POC.Localization;
using Syncfusion.IndustrialMonitoring.POC.Models;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.XlsIO;
using Color = System.Drawing.Color;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;

namespace Syncfusion.IndustrialMonitoring.POC.Services
{
    /// <summary>
    /// Point-in-time snapshot of the dashboard that both report formats are built from.
    /// </summary>
    public class ReportData
    {
        public DateTime GeneratedAt { get; init; }
        public IReadOnlyList<SensorSnapshot> Sensors { get; init; } = Array.Empty<SensorSnapshot>();
        public StatusSummary Summary { get; init; } = new();
        public IReadOnlyList<TrendDataPoint> Trend { get; init; } = Array.Empty<TrendDataPoint>();
        public GaugeConfiguration TrendConfig { get; init; } = new();
        public IReadOnlyList<AlarmEvent> Alarms { get; init; } = Array.Empty<AlarmEvent>();
    }

    public class SensorSnapshot
    {
        public GaugeConfiguration Config { get; init; } = new();
        public double Value { get; init; }

        public DataQuality Quality =>
            Value > Config.AlarmThreshold ? DataQuality.Bad
            : Value > Config.WarningThreshold ? DataQuality.Stale
            : DataQuality.Good;
    }

    /// <summary>
    /// Generates Arabic (RTL) monitoring reports with the Syncfusion PDF Library
    /// and the Syncfusion Excel Library (XlsIO).
    /// </summary>
    public class ReportService
    {
        private const string ReportTitle = "تقرير المراقبة الصناعية";
        private const int MaxPdfAlarms = 200;
        private const float FooterHeight = 28;

        private static readonly CultureInfo Culture = CultureInfo.CurrentCulture;

        #region PDF

        public void GeneratePdfReport(ReportData data, string filePath)
        {
            using var regularFontStream = OpenSystemFont("arial.ttf", "tahoma.ttf");
            using var boldFontStream = OpenSystemFont("arialbd.ttf", "tahomabd.ttf");

            // Arabic text needs a Unicode TrueType font with Arabic glyphs plus RTL string formats.
            var titleFont = new PdfTrueTypeFont(boldFontStream, 18);
            var headingFont = new PdfTrueTypeFont(boldFontStream, 13);
            var bodyFont = new PdfTrueTypeFont(regularFontStream, 10);
            var smallFont = new PdfTrueTypeFont(regularFontStream, 8);

            var rtl = new PdfStringFormat
            {
                TextDirection = PdfTextDirection.RightToLeft,
                Alignment = PdfTextAlignment.Right,
                LineAlignment = PdfVerticalAlignment.Middle
            };

            using var document = new PdfDocument();
            document.PageSettings.Size = PdfPageSize.A4;
            document.PageSettings.Margins.All = 36;
            document.DocumentInformation.Title = ReportTitle;

            var page = document.Pages.Add();
            var g = page.Graphics;
            float width = page.GetClientSize().Width;
            float y = 0;

            // Header band
            g.DrawRectangle(new PdfSolidBrush(new PdfColor(47, 79, 79)), new RectangleF(0, y, width, 44));
            g.DrawString(ReportTitle, titleFont, PdfBrushes.White, new RectangleF(10, y, width - 20, 44), rtl);
            y += 50;
            g.DrawString($"تاريخ الإنشاء: {data.GeneratedAt.ToString("G", Culture)}", bodyFont, PdfBrushes.DimGray,
                new RectangleF(0, y, width, 16), rtl);
            y += 26;

            // KPI table
            y = DrawHeading(g, "مؤشرات الأداء الرئيسية", headingFont, rtl, width, y);
            var kpiGrid = CreateRtlGrid(bodyFont, rtl, new[] { "الحالة", "حد الإنذار", "حد التحذير", "القيمة الحالية", "المستشعر" });
            foreach (var s in data.Sensors)
            {
                var row = kpiGrid.Rows.Add();
                row.Cells[0].Value = ArabicMap.ToArabic(s.Quality);
                row.Cells[1].Value = s.Config.AlarmThreshold.ToString("N0", Culture);
                row.Cells[2].Value = s.Config.WarningThreshold.ToString("N0", Culture);
                row.Cells[3].Value = $"{s.Value.ToString("N1", Culture)} {ArabicMap.ToArabic(s.Config.Unit)}";
                row.Cells[4].Value = s.Config.Name;
                row.Cells[0].Style.BackgroundBrush = new PdfSolidBrush(QualityColor(s.Quality));
            }
            var kpiResult = kpiGrid.Draw(page, new RectangleF(0, y, width, 0));
            y = kpiResult.Bounds.Bottom + 8;

            var summary = data.Summary;
            g.DrawString(
                $"مستشعرات جيدة: {summary.GoodSensorsCount}   •   مستشعرات سيئة: {summary.BadSensorsCount}   •   " +
                $"مستشعرات قديمة: {summary.StaleSensorsCount}   •   إنذارات نشطة: {summary.ActiveAlarmsCount}",
                bodyFont, PdfBrushes.Black, new RectangleF(0, y, width, 16), rtl);
            y += 30;

            // Trend chart, drawn as vector graphics from the trend data
            y = DrawHeading(g, $"اتجاه {data.TrendConfig.Name}", headingFont, rtl, width, y);
            y = DrawTrendChart(g, data, smallFont, rtl, new RectangleF(0, y, width, 220)) + 20;

            // Alarm table (paginates automatically, header repeats on every page)
            y = DrawHeading(g, "سجل الإنذارات", headingFont, rtl, width, y);
            var alarmGrid = CreateRtlGrid(bodyFont, rtl, new[] { "الحالة", "الرسالة", "الخطورة", "المستشعر", "الوقت" });
            alarmGrid.RepeatHeader = true;
            alarmGrid.Columns[0].Width = 55;
            alarmGrid.Columns[2].Width = 55;
            alarmGrid.Columns[3].Width = 75;
            alarmGrid.Columns[4].Width = 115;
            foreach (var alarm in data.Alarms.Take(MaxPdfAlarms))
            {
                var row = alarmGrid.Rows.Add();
                row.Cells[0].Value = alarm.Status;
                row.Cells[1].Value = alarm.Message;
                row.Cells[2].Value = ArabicMap.ToArabic(alarm.Severity);
                row.Cells[3].Value = alarm.Source;
                row.Cells[4].Value = alarm.Timestamp.ToString("g", Culture);
                if (alarm.Severity == AlarmSeverity.Critical)
                    row.Cells[2].Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(255, 205, 210));
            }
            // Keep the bottom of every page free for the page-number footer
            float pageHeight = page.GetClientSize().Height;
            alarmGrid.Draw(page, new RectangleF(0, y, width, pageHeight - FooterHeight - y),
                new PdfGridLayoutFormat
                {
                    Layout = PdfLayoutType.Paginate,
                    Break = PdfLayoutBreakType.FitPage,
                    PaginateBounds = new RectangleF(0, 0, width, pageHeight - FooterHeight)
                });

            if (data.Alarms.Count > MaxPdfAlarms)
            {
                var last = document.Pages[document.Pages.Count - 1];
                last.Graphics.DrawString(
                    $"تم عرض أحدث {MaxPdfAlarms} إنذار من أصل {data.Alarms.Count}. التفاصيل الكاملة في تقرير Excel.",
                    smallFont, PdfBrushes.DimGray,
                    new RectangleF(0, last.GetClientSize().Height - FooterHeight, width, 12), rtl);
            }

            // Page numbers in the footer of every page
            for (int i = 0; i < document.Pages.Count; i++)
            {
                var p = document.Pages[i];
                var size = p.GetClientSize();
                p.Graphics.DrawString($"صفحة {i + 1} من {document.Pages.Count}", smallFont, PdfBrushes.Gray,
                    new RectangleF(0, size.Height - 12, size.Width, 12),
                    new PdfStringFormat { TextDirection = PdfTextDirection.RightToLeft, Alignment = PdfTextAlignment.Left });
            }

            using var output = File.Create(filePath);
            document.Save(output);
        }

        private static float DrawHeading(PdfGraphics g, string text, PdfFont font, PdfStringFormat rtl, float width, float y)
        {
            g.DrawString(text, font, new PdfSolidBrush(new PdfColor(47, 79, 79)), new RectangleF(0, y, width, 20), rtl);
            g.DrawLine(new PdfPen(new PdfColor(200, 200, 200), 0.75f), 0, y + 22, width, y + 22);
            return y + 28;
        }

        private static PdfGrid CreateRtlGrid(PdfFont font, PdfStringFormat rtl, string[] headers)
        {
            // PdfGrid lays columns out left-to-right, so callers list headers leftmost
            // first, i.e. in reverse Arabic reading order (the last one is read first).
            var grid = new PdfGrid();
            grid.Columns.Add(headers.Length);
            grid.Headers.Add(1);
            var header = grid.Headers[0];
            for (int i = 0; i < headers.Length; i++)
                header.Cells[i].Value = headers[i];

            var cellFormat = new PdfStringFormat
            {
                TextDirection = PdfTextDirection.RightToLeft,
                Alignment = PdfTextAlignment.Right,
                LineAlignment = PdfVerticalAlignment.Middle,
                WordWrap = PdfWordWrapType.Word
            };

            grid.Style.Font = font;
            grid.Style.CellPadding = new PdfPaddings(4, 4, 3, 3);
            foreach (PdfGridColumn column in grid.Columns)
                column.Format = cellFormat;

            header.Style.Font = font;
            header.Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(47, 79, 79));
            header.Style.TextBrush = PdfBrushes.White;
            return grid;
        }

        private static float DrawTrendChart(PdfGraphics g, ReportData data, PdfFont font, PdfStringFormat rtl, RectangleF bounds)
        {
            var config = data.TrendConfig;
            var points = data.Trend;
            const float axisLabelWidth = 28;
            const float axisLabelHeight = 14;

            // Plot area; the value axis sits on the right to match the RTL dashboard
            var plot = new RectangleF(bounds.X, bounds.Y, bounds.Width - axisLabelWidth, bounds.Height - axisLabelHeight);
            g.DrawRectangle(new PdfPen(new PdfColor(224, 224, 224), 0.75f), plot);

            double min = config.MinValue, max = config.MaxValue;
            float ToY(double v) => plot.Bottom - (float)((v - min) / (max - min)) * plot.Height;

            var gridPen = new PdfPen(new PdfColor(238, 238, 238), 0.5f);
            for (double v = min; v <= max; v += (max - min) / 5)
            {
                float gy = ToY(v);
                g.DrawLine(gridPen, plot.Left, gy, plot.Right, gy);
                g.DrawString(v.ToString("N0", Culture), font, PdfBrushes.Gray,
                    new RectangleF(plot.Right + 2, gy - 6, axisLabelWidth - 2, 12),
                    new PdfStringFormat { Alignment = PdfTextAlignment.Left });
            }

            DrawThreshold(g, plot, ToY(config.WarningThreshold), new PdfColor(239, 108, 0));
            DrawThreshold(g, plot, ToY(config.AlarmThreshold), new PdfColor(198, 40, 40));

            if (points.Count > 1)
            {
                // Newest point on the left, mirroring the dashboard's RTL chart
                float step = plot.Width / (points.Count - 1);
                var linePen = new PdfPen(new PdfColor(0, 102, 204), 1.2f);
                var previous = new PointF(plot.Right, ToY(points[0].Value));
                for (int i = 1; i < points.Count; i++)
                {
                    var current = new PointF(plot.Right - i * step, ToY(points[i].Value));
                    g.DrawLine(linePen, previous, current);
                    previous = current;
                }

                var labelY = plot.Bottom + 2;
                g.DrawString(points[0].Timestamp.ToString("T", Culture), font, PdfBrushes.Gray,
                    new RectangleF(plot.Right - 120, labelY, 120, 12), rtl);
                g.DrawString(points[^1].Timestamp.ToString("T", Culture), font, PdfBrushes.Gray,
                    new RectangleF(plot.Left, labelY, 120, 12),
                    new PdfStringFormat { TextDirection = PdfTextDirection.RightToLeft, Alignment = PdfTextAlignment.Left });
            }

            return bounds.Bottom;
        }

        private static void DrawThreshold(PdfGraphics g, RectangleF plot, float y, PdfColor color)
        {
            var pen = new PdfPen(color, 0.75f) { DashStyle = PdfDashStyle.Dash };
            g.DrawLine(pen, plot.Left, y, plot.Right, y);
        }

        private static PdfColor QualityColor(DataQuality quality) => quality switch
        {
            DataQuality.Bad => new PdfColor(255, 205, 210),
            DataQuality.Stale => new PdfColor(255, 224, 178),
            _ => new PdfColor(200, 230, 201)
        };

        private static Stream OpenSystemFont(params string[] candidates)
        {
            var fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            foreach (var name in candidates)
            {
                var path = Path.Combine(fontsFolder, name);
                if (File.Exists(path))
                    return File.OpenRead(path);
            }
            throw new FileNotFoundException($"No Arabic-capable font found ({string.Join(", ", candidates)}).");
        }

        #endregion

        #region Excel

        public void GenerateExcelReport(ReportData data, string filePath)
        {
            using var engine = new ExcelEngine();
            var application = engine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            var workbook = application.Workbooks.Create(3);
            BuildSummarySheet(workbook.Worksheets[0], data);
            BuildTrendSheet(workbook.Worksheets[1], data);
            BuildAlarmSheet(workbook.Worksheets[2], data);
            workbook.ActiveSheetIndex = 0;

            using var output = File.Create(filePath);
            workbook.SaveAs(output);
        }

        private static void BuildSummarySheet(IWorksheet sheet, ReportData data)
        {
            sheet.Name = "الملخص";
            sheet.IsRightToLeft = true;

            sheet.Range["A1"].Text = ReportTitle;
            sheet.Range["A1:E1"].Merge();
            sheet.Range["A1"].CellStyle.Font.Bold = true;
            sheet.Range["A1"].CellStyle.Font.Size = 16;
            sheet.Range["A1"].CellStyle.Font.RGBColor = Color.White;
            sheet.Range["A1:E1"].CellStyle.Color = Color.FromArgb(47, 79, 79);

            sheet.Range["A2"].Text = "تاريخ الإنشاء";
            sheet.Range["B2"].DateTime = data.GeneratedAt;
            sheet.Range["B2"].NumberFormat = "yyyy-mm-dd hh:mm:ss";

            string[] headers = { "المستشعر", "القيمة الحالية", "الوحدة", "حد التحذير", "حد الإنذار", "الحالة" };
            WriteHeaderRow(sheet, 4, headers);

            int row = 5;
            foreach (var s in data.Sensors)
            {
                sheet.Range[row, 1].Text = s.Config.Name;
                sheet.Range[row, 2].Number = Math.Round(s.Value, 1);
                sheet.Range[row, 2].NumberFormat = "0.0";
                sheet.Range[row, 3].Text = ArabicMap.ToArabic(s.Config.Unit);
                sheet.Range[row, 4].Number = s.Config.WarningThreshold;
                sheet.Range[row, 5].Number = s.Config.AlarmThreshold;
                sheet.Range[row, 6].Text = ArabicMap.ToArabic(s.Quality);
                sheet.Range[row, 6].CellStyle.Color = s.Quality switch
                {
                    DataQuality.Bad => Color.FromArgb(255, 205, 210),
                    DataQuality.Stale => Color.FromArgb(255, 224, 178),
                    _ => Color.FromArgb(200, 230, 201)
                };
                row++;
            }

            row++;
            var summary = data.Summary;
            WriteKeyValue(sheet, row++, "مستشعرات جيدة", summary.GoodSensorsCount);
            WriteKeyValue(sheet, row++, "مستشعرات سيئة", summary.BadSensorsCount);
            WriteKeyValue(sheet, row++, "مستشعرات قديمة", summary.StaleSensorsCount);
            WriteKeyValue(sheet, row++, "إنذارات نشطة", summary.ActiveAlarmsCount);
            WriteKeyValue(sheet, row++, "إجمالي الإنذارات في السجل", data.Alarms.Count);

            sheet.UsedRange.AutofitColumns();
            sheet.Range["A1"].RowHeight = 28;
        }

        private static void BuildTrendSheet(IWorksheet sheet, ReportData data)
        {
            sheet.Name = "بيانات الاتجاه";
            sheet.IsRightToLeft = true;

            WriteHeaderRow(sheet, 1, new[] { "الوقت", data.TrendConfig.Name, "الجودة" });
            int row = 2;
            // Trend data is oldest-first, so the Excel chart reads in time order
            foreach (var p in data.Trend)
            {
                sheet.Range[row, 1].DateTime = p.Timestamp;
                sheet.Range[row, 1].NumberFormat = "yyyy-mm-dd hh:mm:ss";
                sheet.Range[row, 2].Number = p.Value;
                sheet.Range[row, 2].NumberFormat = "0.00";
                sheet.Range[row, 3].Text = ArabicMap.ToArabic(p.Quality);
                row++;
            }
            int lastRow = row - 1;
            sheet.Range["A2"].FreezePanes();
            sheet.UsedRange.AutofitColumns();

            if (lastRow < 2) return;

            // Highlight values above the warning and alarm thresholds
            var values = sheet.Range[2, 2, lastRow, 2];
            AddGreaterThanFormat(values, data.TrendConfig.AlarmThreshold, Color.FromArgb(255, 205, 210));
            AddGreaterThanFormat(values, data.TrendConfig.WarningThreshold, Color.FromArgb(255, 224, 178));

            // Native Excel line chart over the trend data
            var chart = sheet.Charts.Add();
            chart.ChartType = ExcelChartType.Line;
            chart.DataRange = sheet.Range[1, 2, lastRow, 2];
            chart.IsSeriesInRows = false;
            chart.Series[0].CategoryLabels = sheet.Range[2, 1, lastRow, 1];
            chart.ChartTitle = $"اتجاه {data.TrendConfig.Name}";
            chart.HasLegend = false;
            chart.PrimaryValueAxis.MinimumValue = data.TrendConfig.MinValue;
            chart.PrimaryValueAxis.MaximumValue = data.TrendConfig.MaxValue;
            chart.PrimaryCategoryAxis.NumberFormat = "hh:mm:ss";
            chart.TopRow = 2;
            chart.LeftColumn = 5;
            chart.BottomRow = 24;
            chart.RightColumn = 16;
        }

        private static void BuildAlarmSheet(IWorksheet sheet, ReportData data)
        {
            sheet.Name = "الإنذارات";
            sheet.IsRightToLeft = true;

            WriteHeaderRow(sheet, 1, new[] { "المعرف", "الوقت", "المستشعر", "الخطورة", "الرسالة", "الحالة", "أقر بواسطة", "وقت الإقرار" });
            int row = 2;
            foreach (var a in data.Alarms)
            {
                sheet.Range[row, 1].Number = a.EventId;
                sheet.Range[row, 2].DateTime = a.Timestamp;
                sheet.Range[row, 2].NumberFormat = "yyyy-mm-dd hh:mm:ss";
                sheet.Range[row, 3].Text = a.Source;
                sheet.Range[row, 4].Text = ArabicMap.ToArabic(a.Severity);
                sheet.Range[row, 5].Text = a.Message;
                sheet.Range[row, 6].Text = a.Status;
                sheet.Range[row, 7].Text = a.AcknowledgedBy ?? string.Empty;
                if (a.AcknowledgedTime is DateTime ackTime)
                {
                    sheet.Range[row, 8].DateTime = ackTime;
                    sheet.Range[row, 8].NumberFormat = "yyyy-mm-dd hh:mm:ss";
                }
                if (a.Severity == AlarmSeverity.Critical)
                    sheet.Range[row, 1, row, 8].CellStyle.Color = Color.FromArgb(255, 235, 238);
                row++;
            }

            int lastRow = Math.Max(row - 1, 1);
            sheet.AutoFilters.FilterRange = sheet.Range[1, 1, lastRow, 8];
            sheet.Range["A2"].FreezePanes();
            sheet.UsedRange.AutofitColumns();
        }

        private static void WriteHeaderRow(IWorksheet sheet, int row, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
                sheet.Range[row, i + 1].Text = headers[i];

            var range = sheet.Range[row, 1, row, headers.Length];
            range.CellStyle.Font.Bold = true;
            range.CellStyle.Font.RGBColor = Color.White;
            range.CellStyle.Color = Color.FromArgb(47, 79, 79);
        }

        private static void WriteKeyValue(IWorksheet sheet, int row, string label, int value)
        {
            sheet.Range[row, 1].Text = label;
            sheet.Range[row, 1].CellStyle.Font.Bold = true;
            sheet.Range[row, 2].Number = value;
        }

        private static void AddGreaterThanFormat(IRange range, double threshold, Color color)
        {
            var format = range.ConditionalFormats.AddCondition();
            format.FormatType = ExcelCFType.CellValue;
            format.Operator = ExcelComparisonOperator.Greater;
            format.FirstFormula = threshold.ToString(CultureInfo.InvariantCulture);
            format.BackColorRGB = color;
            format.StopIfTrue = true;
        }

        #endregion
    }
}
