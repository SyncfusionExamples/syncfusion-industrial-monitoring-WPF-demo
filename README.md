# Syncfusion Industrial Monitoring POC

A minimal WPF/.NET 9 (MVVM) proof-of-concept showing how Syncfusion WPF Controls and the Syncfusion Document Processing Libraries cover a SCADA-style industrial monitoring dashboard. The UI is in Arabic and right-to-left by default.

## Features

| Requirement | How the POC shows it | Syncfusion component |
|---|---|---|
| Real-time trend visualization | Live temperature chart, update interval adjustable 50–1000 ms, live on/off | SfChart (`LineSeries`, `CategoryAxis`) |
| Historical trend visualization | "History" tab: 8 hours of temperature, pressure and humidity from SQLite, with zoom, pan and trackball | SfChart (`FastLineSeries`, `DateTimeAxis`, `ChartZoomPanBehavior`, `ChartTrackBallBehavior`) |
| Gauges and KPI displays | Linear gauges for temperature and pressure with good/warning/alarm ranges; bullet graphs for sensor status and active alarms | SfLinearGauge, SfBulletGraph |
| Virtualized data grid | Alarm log with sorting, filtering and Excel export | SfDataGrid |
| Dashboard-style UI | Toolbar, chart, KPI row and alarm grid in one view | ButtonAdv, WPF layout |
| Theming | Light/dark switch | SfSkinManager (Windows11Light / Windows11Dark) |
| Localization and RTL | Arabic UI, `ar-SA` culture (Hijri dates, Arabic digits), Arabic grid resources, RTL toggle | SfSkinManager, SfDataGrid localization |
| MVVM-friendly integration | View models with bindings and commands; dialogs and reports behind services | — |
| PDF and Excel reports | Arabic RTL reports generated from the current dashboard state | Syncfusion PDF Library, XlsIO |

## Quick Start

### Prerequisites
- Windows 10/11
- .NET 9 SDK

### Build & Run

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project Syncfusion.IndustrialMonitoring.POC.csproj
```

Or run the built executable directly:

```powershell
.\bin\Release\net9.0-windows10.0.19041.0\Syncfusion.IndustrialMonitoring.POC.exe
```

## Project Structure

```
Models/        - ProcessValue, TrendDataPoint, AlarmEvent, GaugeConfiguration, StatusSummary
ViewModels/    - DashboardViewModel, TrendChartViewModel, GaugeViewModel, AlarmGridViewModel
Views/         - DashboardView, TrendChartView, GaugeView, AlarmGridView
Services/      - DataService, LiveUpdateService, DatabaseInitializer, ReportService,
                 DialogService, ThemeService, LicenseKeyProvider
Localization/  - ArabicMap (Arabic names for sensors, units, statuses)
Resources/     - Arabic resources for SfDataGrid
```

## Usage

The toolbar, from right to left:

1. **مباشر (Live)** — pauses or resumes live updates of the chart and gauges
2. **Interval slider** — update interval in 50 ms steps (50–1000 ms); the current value is shown next to it
3. **تبديل الاتجاه (Toggle direction)** — switches between right-to-left and left-to-right layout
4. **فاتح / داكن (Light / dark)** — switches the theme
5. **تقرير PDF / تقرير Excel (PDF / Excel report)** — generates a report from the current dashboard state

Other actions:
- **Trend chart** — "مباشر" (Live) and "سجل تاريخي" (History) tabs. On the History tab, use the mouse wheel or drag a selection to zoom, drag to pan, double-click to reset, and hover to see values. "تصدير الصورة" (Export image) saves the live chart as PNG/JPEG/BMP.
- **Alarm grid** — click column headers to sort, use the filter icons to filter. The export button saves the grid to Excel.

## Components

### Live trend chart
- Keeps the latest 200 temperature readings; each update adds one point and drops the oldest
- Category axis, so the chart moves one point per update and the interval is visible
- Starts from where the history ends, so there is no jump between history and live data

### Historical trend chart
- Reads 8 hours of one-minute readings per sensor from the SQLite database
- Three series with a legend; zoom on the time axis, panning and trackball are built-in SfChart behaviours

### Gauges and status
- Linear gauges: temperature (0–100 °C, warning 70, alarm 90) and pressure (0–100 bar, warning 75, alarm 95)
- Humidity (warning 80, alarm 95) is shown on the History tab and in the reports
- Bullet graphs show good, bad and stale sensor counts (derived from the live readings: stale = above the warning limit, bad = above the alarm limit) and active alarms

### Alarm grid
- Loads the latest 100 alarms from the database; live alarms are added at the top (up to 1,000)
- SfDataGrid with row virtualization, sorting and filtering
- Alarms are edge-triggered: a sensor raises one alarm when it crosses its alarm limit and re-arms only after dropping 5 units below it, so a value hovering above the limit does not flood the grid

## Reports

Both reports are built by `Services/ReportService.cs` from one snapshot of the dashboard and are fully Arabic / right-to-left.

**PDF report** (Syncfusion PDF Library)
- Header band and generation time
- KPI table: current value, warning/alarm limits and status per sensor, plus sensor/alarm counts
- Temperature trend drawn as vector graphics, with warning and alarm limit lines
- Alarm log table (latest 200) that paginates with a repeating header, and page numbers

**Excel report** (XlsIO)
- `الملخص` (Summary): KPIs with status colouring
- `بيانات الاتجاه` (Trend data): trend values, conditional formatting above the limits, and a native Excel line chart
- `الإنذارات` (Alarms): full alarm log with auto-filter, frozen header and critical rows highlighted

## Data Source

- **History and alarms:** a SQLite database at `%APPDATA%\IndustrialMonitoring\monitoring.db`, created and seeded on first run and opened read-only. It is re-created when the seed schema version in `DatabaseInitializer` changes.
- **Live values:** a synthetic random walk per sensor, generated by `DataService` and pushed by `LiveUpdateService` on a `DispatcherTimer`.
- **Times:** the database stores UTC; all displayed times are local.

To use real data:
1. Extend `DataService` to query your data source (REST API, MQTT, OPC-UA, database)
2. Have `LiveUpdateService` call the new provider
3. The view models update the UI through bindings

## Localization & RTL

- The UI starts in Arabic, right-to-left. "تبديل الاتجاه" (Toggle direction) switches to left-to-right.
- Culture is `ar-SA`, so dates use the Hijri calendar and numbers use Arabic digits.
- SfDataGrid text (filter menus etc.) comes from `Resources/Syncfusion.SfGrid.WPF.ar-SA.resx`.
- UI strings are written directly in the XAML; there is no English UI yet.

## Theming

`ThemeService` switches the SfSkinManager theme between Windows11Light and Windows11Dark. The dashboard's own surface colours (page, cards, borders, text) are shared brushes in `App.xaml`, swapped together with the theme.

## Troubleshooting

**App won't build**
- Check that the .NET 9 SDK is installed: `dotnet --version`
- Restore packages: `dotnet restore`

**History tab is empty or shows old data**
- Delete `%APPDATA%\IndustrialMonitoring\monitoring.db` and restart; it is re-created and re-seeded

**Syncfusion license banner appears**
- No license key was found; see License below

## License

The Syncfusion license key is **not** stored in source. At startup `LicenseKeyProvider` looks for it in this order:

1. The `SYNCFUSION_LICENSE_KEY` environment variable
2. A `SyncfusionLicense.txt` file in the project root (git-ignored, copied to the output folder on build)

```powershell
# Option 1: environment variable (current user)
setx SYNCFUSION_LICENSE_KEY "YOUR_KEY"

# Option 2: local file
Set-Content SyncfusionLicense.txt "YOUR_KEY"
```

Without a key the controls still run but show the Syncfusion license banner.

## Possible Next Steps

- Connect a real data source (REST API, WebSocket, MQTT, OPC-UA)
- Radial gauge and numeric KPI readouts
- Load a large alarm history (e.g. 50,000 rows) to show grid virtualization at scale
- Alarm acknowledgement from the grid
- Resource-based English/Arabic switching
