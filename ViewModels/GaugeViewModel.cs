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
        private const double AlarmDeadband = 5.0;
        private readonly HashSet<string> _sensorsInAlarm = new();
        private int _nextEventId = 100000;

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

            if (config == null) return;

            // Edge-triggered with hysteresis: raise one alarm when the value crosses
            // the threshold, and re-arm only after it falls below threshold - deadband.
            // Without this a value hovering above the limit raised an alarm every tick.
            if (_sensorsInAlarm.Contains(reading.Name))
            {
                if (reading.Value < config.AlarmThreshold - AlarmDeadband)
                    _sensorsInAlarm.Remove(reading.Name);
                return;
            }

            if (reading.Value > config.AlarmThreshold)
            {
                _sensorsInAlarm.Add(reading.Name);
                AlarmRaised?.Invoke(new AlarmEvent
                {
                    EventId = ++_nextEventId,
                    Timestamp = DateTime.UtcNow,
                    Source = reading.Name,
                    Severity = AlarmSeverity.Critical,
                    Message = $"تجاوز {reading.Name} حد الإنذار: {reading.Value:F1}",
                    Status = ArabicMap.StatusOpen
                });
            }
        }

        /// <summary>Current value of every sensor together with its limits.</summary>
        public IReadOnlyList<SensorSnapshot> GetSnapshots() => new[]
        {
            new SensorSnapshot { Config = _tempConfig, Value = TemperatureValue },
            new SensorSnapshot { Config = _pressureConfig, Value = PressureValue },
            new SensorSnapshot { Config = _humidityConfig, Value = HumidityValue }
        };

        public void Cleanup()
        {
            _liveService.Unsubscribe(OnLiveUpdate);
        }
    }
}
