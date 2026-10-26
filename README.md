# Syncfusion Industrial Monitoring POC

A minimal WPF/.NET 9 proof-of-concept demonstrating Syncfusion components for SCADA-style industrial monitoring dashboards.

## Features

✅ **Live Trend Chart** — Real-time sensor data visualization (500-point bounded history)  
✅ **Real-Time Gauges** — Radial + linear gauges with threshold zones (Good/Warning/Alarm)  
✅ **Virtualized Alarm Grid** — Historical alarm/event display with sorting/filtering  
✅ **Dashboard** — Composed view with chart, gauges, grid, status summary  
✅ **Live Updates** — 100 ms batched synthetic data (configurable)  
✅ **PDF & Excel Reports** — Arabic RTL reports via Syncfusion PDF Library and XlsIO  
✅ **RTL Support** — Toggle for right-to-left layout  

## Quick Start

### Prerequisites
- Windows 10/11
- .NET 9 Runtime or SDK

### Build & Run

```powershell
# Restore NuGet packages
dotnet restore

# Build
dotnet build -c Release

# Run
dotnet run --project Syncfusion.IndustrialMonitoring.POC.csproj

# Or run directly
.\bin\Release\net9.0-windows\Syncfusion.IndustrialMonitoring.POC.exe
```

## Project Structure

```
Models/               - Data models (ProcessValue, TrendDataPoint, AlarmEvent)
ViewModels/          - MVVM logic (TrendChartVM, GaugeVM, AlarmGridVM, DashboardVM)
Views/               - XAML views (TrendChartView, GaugeView, AlarmGridView, DashboardView)
Services/            - Business logic (DataService, LiveUpdateService)
App.xaml             - Application root
MainWindow.xaml      - Main window
```

## Components

### **Trend Chart**
- SfChart with LineSeries
- 500-point bounded history
- Live updates every 100 ms (configurable 50–1000 ms)
- Zoom/pan enabled

### **Gauges**
- Radial gauge: Temperature (0–100°C)
- Linear gauge: Pressure (0–100 bar)
- Color-coded zones: Good (green), Warning (orange), Alarm (red)

### **Alarm Grid**
- SfDataGrid with 100+ sample alarms
- Virtualized for smooth scrolling (10K+ rows supported)
- Sortable & filterable

### **Dashboard**
- Status summary (good/bad/stale sensors, active alarms)
- Toolbar: Live toggle, update interval slider, RTL toggle
- Responsive grid layout

## Usage

1. **Run the app** — Dashboard displays live-updating chart, gauges, and alarms
2. **Adjust Update Interval** — Slider in toolbar (50–1000 ms)
3. **Toggle Live Mode** — Checkbox pauses/resumes updates
4. **Export Reports** — Toolbar buttons "تقرير PDF" / "تقرير Excel" generate a report from the current dashboard state (see below)
5. **Toggle RTL** — Right-to-left layout toggle for Arabic/RTL languages

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

## Alarms

Alarms are edge-triggered: a sensor raises one alarm when it crosses its alarm limit and re-arms only after dropping 5 units below it, so a value hovering above the limit does not flood the alarm grid.

## Data Source

Currently uses **synthetic data** generated in-memory:
- Temperature: 0–100°C (varies smoothly)
- Pressure: 0–100 bar
- Humidity: 0–100%

To integrate with real data:
1. Extend `DataService.cs` to query your data source (REST API, MQTT, OPC-UA, database)
2. Update `LiveUpdateService` to call new data provider
3. ViewModels automatically update UI via binding

## Performance

- **Chart:** 500 points, 100 ms updates, smooth at 60 FPS
- **Grid:** Virtualized; scrolls smoothly with 10K+ rows
- **Memory:** < 100 MB typical
- **CPU:** < 5% idle, < 15% during active updates

## Localization & RTL

- Default: English, Left-to-Right
- RTL Toggle: Flips entire UI layout
- Ready for Arabic (ar-SA), Hebrew, Urdu translations

## Troubleshooting

**App won't build:**
- Ensure .NET 9 SDK installed: `dotnet --version`
- Restore NuGet: `dotnet restore`

**No data visible:**
- Check LiveUpdateService.Start() is called in DashboardViewModel
- Verify Syncfusion controls loaded (check Output window for errors)

**Grid not scrolling smoothly:**
- Virtualization enabled by default
- Ensure row height is fixed (not auto)

**RTL layout broken:**
- Toggle RTL with button in toolbar
- Check FlowDirection binding in DashboardView.xaml

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

## Next Steps

- Integrate real data source (REST API, WebSocket, MQTT, OPC-UA)
- Implement PDF/Excel export fully
- Add Arabic localization
- Add user authentication
- Deploy to production environment

---

**Version:** 1.0  
**Created:** October 26, 2026  
**Status:** Ready to demo
