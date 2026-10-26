using System;
using System.Collections.Generic;
using Syncfusion.IndustrialMonitoring.POC.Localization;
using Syncfusion.IndustrialMonitoring.POC.ViewModels;

namespace Syncfusion.IndustrialMonitoring.POC.Models
{
    public enum DataQuality { Good, Bad, Stale }
    public enum AlarmSeverity { Info, Warning, Critical }
    public enum Unit { Celsius, Bar, Percent, Liter }

    public class ProcessValue
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
        public Unit Unit { get; set; }
        public DateTime Timestamp { get; set; }
        public DataQuality Quality { get; set; } = DataQuality.Good;
    }

    public class TrendDataPoint : ProcessValue
    {
        public bool IsOutlier { get; set; }
    }

    public class AlarmEvent
    {
        public int EventId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }
        public AlarmSeverity Severity { get; set; }
        public string Message { get; set; }
        public string Status { get; set; } = "Open";
        public string AcknowledgedBy { get; set; }
        public DateTime? AcknowledgedTime { get; set; }
    }

    public class GaugeConfiguration
    {
        public string Name { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double WarningThreshold { get; set; }
        public double AlarmThreshold { get; set; }
        public Unit Unit { get; set; }
    }

    public class StatusSummary : ViewModelBase
    {
        private int _goodSensorsCount;
        private int _badSensorsCount;
        private int _staleSensorsCount;
        private int _activeAlarmsCount;
        private DateTime _lastUpdateTime;

        public int GoodSensorsCount
        {
            get => _goodSensorsCount;
            set { _goodSensorsCount = value; RaisePropertyChanged(nameof(GoodSensorsCount)); }
        }

        public int BadSensorsCount
        {
            get => _badSensorsCount;
            set { _badSensorsCount = value; RaisePropertyChanged(nameof(BadSensorsCount)); }
        }

        public int StaleSensorsCount
        {
            get => _staleSensorsCount;
            set { _staleSensorsCount = value; RaisePropertyChanged(nameof(StaleSensorsCount)); }
        }

        public int ActiveAlarmsCount
        {
            get => _activeAlarmsCount;
            set { _activeAlarmsCount = value; RaisePropertyChanged(nameof(ActiveAlarmsCount)); }
        }

        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set { _lastUpdateTime = value; RaisePropertyChanged(nameof(LastUpdateTime)); }
        }
    }
}
