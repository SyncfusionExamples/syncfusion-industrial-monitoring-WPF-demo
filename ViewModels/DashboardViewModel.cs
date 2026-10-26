using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Syncfusion.IndustrialMonitoring.POC.Localization;
using Syncfusion.IndustrialMonitoring.POC.Models;
using Syncfusion.IndustrialMonitoring.POC.Services;

namespace Syncfusion.IndustrialMonitoring.POC.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private DataService _dataService;
        private LiveUpdateService _liveService;
        private TrendChartViewModel _trendVM;
        private GaugeViewModel _gaugeVM;
        private AlarmGridViewModel _alarmVM;
        private readonly ReportService _reportService;
        private readonly IDialogService _dialogService;
        private bool _isDarkTheme;
        private bool _isLiveUpdating = true;
        private int _updateInterval = 100;
        private StatusSummary _statusSummary;
        private FlowDirection _flowDirection = FlowDirection.RightToLeft;

        public TrendChartViewModel TrendVM => _trendVM;
        public GaugeViewModel GaugeVM => _gaugeVM;
        public AlarmGridViewModel AlarmVM => _alarmVM;

        public StatusSummary StatusSummary
        {
            get => _statusSummary;
            set { _statusSummary = value; RaisePropertyChanged(nameof(StatusSummary)); }
        }

        public bool IsLiveUpdating
        {
            get => _isLiveUpdating;
            set
            {
                if (_isLiveUpdating == value) return;
                _isLiveUpdating = value;
                if (value)
                {
                    _liveService.UpdateInterval = _updateInterval;
                    _liveService.Start();
                }
                else
                {
                    _liveService.Stop();
                }
                _trendVM.IsLiveUpdating = value;
                RaisePropertyChanged(nameof(IsLiveUpdating));
            }
        }

        public int UpdateInterval
        {
            get => _updateInterval;
            set
            {
                if (_updateInterval == value) return;
                _updateInterval = value;
                _liveService.UpdateInterval = value;
                RaisePropertyChanged(nameof(UpdateInterval));
            }
        }

        public FlowDirection FlowDirection
        {
            get => _flowDirection;
            set { _flowDirection = value; RaisePropertyChanged(nameof(FlowDirection)); }
        }

        public ICommand ToggleRtlCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ExportPdfReportCommand { get; }
        public ICommand ExportExcelReportCommand { get; }

        public DashboardViewModel() : this(new ReportService(), new DialogService())
        {
        }

        public DashboardViewModel(ReportService reportService, IDialogService dialogService)
        {
            _reportService = reportService;
            _dialogService = dialogService;
            _dataService = new DataService();
            _liveService = new LiveUpdateService(_dataService);

            _trendVM = new TrendChartViewModel(_dataService, _liveService);
            _gaugeVM = new GaugeViewModel(_dataService, _liveService);
            _alarmVM = new AlarmGridViewModel(_dataService);

            StatusSummary = new StatusSummary { LastUpdateTime = DateTime.Now };

            // Subscribe to alarms
            _gaugeVM.AlarmRaised += (alarm) => _alarmVM.AddAlarm(alarm);

            ToggleRtlCommand = CreateCommand(ToggleRtl);
            ToggleThemeCommand = CreateCommand(() =>
            {
                _isDarkTheme = !_isDarkTheme;
                ThemeService.Apply(Application.Current.MainWindow, _isDarkTheme);
            });
            ExportPdfReportCommand = CreateCommand(() => ExportReport(
                "تصدير تقرير PDF", "ملف PDF (*.pdf)|*.pdf", "pdf", _reportService.GeneratePdfReport));
            ExportExcelReportCommand = CreateCommand(() => ExportReport(
                "تصدير تقرير Excel", "ملف Excel (*.xlsx)|*.xlsx", "xlsx", _reportService.GenerateExcelReport));

            _liveService.Start();
            UpdateStatus();

            // Update status every second
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => UpdateStatus();
            timer.Start();
        }

        private void UpdateStatus()
        {
            StatusSummary.LastUpdateTime = DateTime.Now;
            StatusSummary.ActiveAlarmsCount = _alarmVM.AlarmHistory.Count(a => a.Status == ArabicMap.StatusOpen);

            // Derived from the live readings (stale = above the warning limit, bad = above the alarm limit)
            var sensors = _gaugeVM.GetSnapshots();
            StatusSummary.GoodSensorsCount = sensors.Count(s => s.Quality == DataQuality.Good);
            StatusSummary.BadSensorsCount = sensors.Count(s => s.Quality == DataQuality.Bad);
            StatusSummary.StaleSensorsCount = sensors.Count(s => s.Quality == DataQuality.Stale);
            RaisePropertyChanged(nameof(StatusSummary));
        }

        private ReportData CreateReportSnapshot()
        {
            UpdateStatus();
            var summary = StatusSummary;
            return new ReportData
            {
                GeneratedAt = DateTime.Now,
                Sensors = _gaugeVM.GetSnapshots(),
                Summary = new StatusSummary
                {
                    GoodSensorsCount = summary.GoodSensorsCount,
                    BadSensorsCount = summary.BadSensorsCount,
                    StaleSensorsCount = summary.StaleSensorsCount,
                    ActiveAlarmsCount = summary.ActiveAlarmsCount,
                    LastUpdateTime = summary.LastUpdateTime
                },
                Trend = _trendVM.TrendData.ToList(),
                TrendConfig = _gaugeVM.TempConfig,
                Alarms = _alarmVM.AlarmHistory.ToList()
            };
        }

        private void ExportReport(string title, string filter, string extension, Action<ReportData, string> generate)
        {
            var defaultName = $"تقرير_المراقبة_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
            var path = _dialogService.ShowSaveFileDialog(title, filter, defaultName);
            if (path == null) return;

            try
            {
                generate(CreateReportSnapshot(), path);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"تعذر إنشاء التقرير: {ex.Message}", title);
                return;
            }

            if (_dialogService.Confirm("تم إنشاء التقرير بنجاح. هل تريد فتحه الآن؟", title))
                _dialogService.OpenFile(path);
        }

        private void ToggleRtl()
        {
            FlowDirection = FlowDirection == FlowDirection.LeftToRight 
                ? FlowDirection.RightToLeft 
                : FlowDirection.LeftToRight;
        }

        public void Cleanup()
        {
            _liveService.Stop();
            _trendVM.Cleanup();
            _gaugeVM.Cleanup();
        }
    }
}
