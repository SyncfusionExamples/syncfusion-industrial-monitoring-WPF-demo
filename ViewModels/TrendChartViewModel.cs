using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Syncfusion.IndustrialMonitoring.POC.Localization;
using Syncfusion.IndustrialMonitoring.POC.Models;
using Syncfusion.IndustrialMonitoring.POC.Services;

namespace Syncfusion.IndustrialMonitoring.POC.ViewModels
{
    public class TrendChartViewModel : ViewModelBase
    {
        private ObservableCollection<TrendDataPoint> _trendData;
        private bool _isLiveUpdating = true;
        private int _maxPoints = 500;
        private DataService _dataService;
        private LiveUpdateService _liveService;

        public ObservableCollection<TrendDataPoint> TrendData
        {
            get => _trendData;
            set { _trendData = value; RaisePropertyChanged(nameof(TrendData)); }
        }

        public bool IsLiveUpdating
        {
            get => _isLiveUpdating;
            set { _isLiveUpdating = value; RaisePropertyChanged(nameof(IsLiveUpdating)); }
        }

        public int MaxPoints
        {
            get => _maxPoints;
            set { _maxPoints = value; RaisePropertyChanged(nameof(MaxPoints)); }
        }

        public TrendChartViewModel(DataService dataService, LiveUpdateService liveService)
        {
            _dataService = dataService;
            _liveService = liveService;
            TrendData = new ObservableCollection<TrendDataPoint>();

            // Initialize with synthetic data
            var trends = _dataService.GenerateSyntheticTrend(200);
            foreach (var t in trends)
                TrendData.Add(t);

            // Subscribe to live updates
            _liveService.Subscribe(OnLiveUpdate);
        }

        private void OnLiveUpdate(List<ProcessValue> readings)
        {
            if (!IsLiveUpdating) return;

            var tempReading = readings.FirstOrDefault(r => r.Name == ArabicMap.SensorTemperature);
            if (tempReading != null)
            {
                var point = new TrendDataPoint
                {
                    Id = TrendData.Count,
                    Name = tempReading.Name,
                    Value = tempReading.Value,
                    Unit = tempReading.Unit,
                    Timestamp = tempReading.Timestamp,
                    Quality = tempReading.Quality
                };

                TrendData.Add(point);

                if (TrendData.Count > MaxPoints)
                    TrendData.RemoveAt(0);
            }
        }

        public void Cleanup()
        {
            _liveService.Unsubscribe(OnLiveUpdate);
        }
    }
}
