using System;
using System.Collections.Generic;
using Syncfusion.IndustrialMonitoring.POC.Localization;
using Syncfusion.IndustrialMonitoring.POC.Models;
using Syncfusion.IndustrialMonitoring.POC.Services;

namespace Syncfusion.IndustrialMonitoring.POC.ViewModels
{
    public class GaugeViewModel : ViewModelBase
    {
        private double _temperatureValue = 45;
        private double _pressureValue = 50;
        private double _humidityValue = 60;
        private GaugeConfiguration _tempConfig;
        private GaugeConfiguration _pressureConfig;
        private GaugeConfiguration _humidityConfig;
        private LiveUpdateService _liveService;

        public event Action<AlarmEvent> AlarmRaised;

        public double TemperatureValue
        {
            get => _temperatureValue;
            set { _temperatureValue = value; RaisePropertyChanged(nameof(TemperatureValue)); }
        }

        public double PressureValue
        {
            get => _pressureValue;
            set { _pressureValue = value; RaisePropertyChanged(nameof(PressureValue)); }
        }

        public double HumidityValue
        {
            get => _humidityValue;
            set { _humidityValue = value; RaisePropertyChanged(nameof(HumidityValue)); }
        }

        public GaugeConfiguration TempConfig => _tempConfig;
        public GaugeConfiguration PressureConfig => _pressureConfig;
        public GaugeConfiguration HumidityConfig => _humidityConfig;

        public GaugeViewModel(DataService dataService, LiveUpdateService liveService)
        {
            _liveService = liveService;
            var configs = dataService.GetSensorConfiguration();
            _tempConfig = configs[0];
            _pressureConfig = configs[1];
            _humidityConfig = configs[2];

            _liveService.Subscribe(OnLiveUpdate);
        }

        private void OnLiveUpdate(List<ProcessValue> readings)
        {
            foreach (var reading in readings)
            {
                if (reading.Name == ArabicMap.SensorTemperature)
                    TemperatureValue = reading.Value;
                else if (reading.Name == ArabicMap.SensorPressure)
                    PressureValue = reading.Value;
                else if (reading.Name == ArabicMap.SensorHumidity)
                    HumidityValue = reading.Value;

                CheckThresholds(reading);
            }
        }

        private void CheckThresholds(ProcessValue reading)
        {
            var config = reading.Name switch
            {
                var n when n == ArabicMap.SensorTemperature => _tempConfig,
                var n when n == ArabicMap.SensorPressure => _pressureConfig,
                var n when n == ArabicMap.SensorHumidity => _humidityConfig,
                _ => null
            };

            if (config != null && reading.Value > config.AlarmThreshold)
            {
                AlarmRaised?.Invoke(new AlarmEvent
                {
                    EventId = DateTime.Now.Millisecond,
                    Timestamp = DateTime.UtcNow,
                    Source = reading.Name,
                    Severity = AlarmSeverity.Critical,
                    Message = $"تجاوز {reading.Name} حد الإنذار: {reading.Value:F1}",
                    Status = ArabicMap.StatusOpen
                });
            }
        }

        public void Cleanup()
        {
            _liveService.Unsubscribe(OnLiveUpdate);
        }
    }
}
