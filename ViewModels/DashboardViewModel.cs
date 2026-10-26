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

        public DashboardViewModel()
        {
            _dataService = new DataService();
            _liveService = new LiveUpdateService(_dataService);

            _trendVM = new TrendChartViewModel(_dataService, _liveService);
            _gaugeVM = new GaugeViewModel(_dataService, _liveService);
            _alarmVM = new AlarmGridViewModel(_dataService);

            StatusSummary = new StatusSummary { LastUpdateTime = DateTime.UtcNow };

            // Subscribe to alarms
            _gaugeVM.AlarmRaised += (alarm) => _alarmVM.AddAlarm(alarm);

            ToggleRtlCommand = CreateCommand(ToggleRtl);

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
            StatusSummary.GoodSensorsCount = 2;
            StatusSummary.BadSensorsCount = 0;
            StatusSummary.StaleSensorsCount = 1;
            RaisePropertyChanged(nameof(StatusSummary));
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
