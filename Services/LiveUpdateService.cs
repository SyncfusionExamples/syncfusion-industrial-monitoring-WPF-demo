using System;
using System.Collections.Generic;
using Syncfusion.IndustrialMonitoring.POC.Models;
using System.Windows.Threading;

namespace Syncfusion.IndustrialMonitoring.POC.Services
{
    public class LiveUpdateService
    {
        private DispatcherTimer _timer;
        private DataService _dataService;
        private List<Action<List<ProcessValue>>> _subscribers = new();
        private int _updateInterval = 100;
        
        public bool IsRunning { get; private set; }
        
        public int UpdateInterval
        {
            get => _updateInterval;
            set
            {
                if (_updateInterval == value) return;
                _updateInterval = value;
                
                // Setting Interval on a running DispatcherTimer restarts it with the new value
                _timer.Interval = TimeSpan.FromMilliseconds(value);
            }
        }

        public LiveUpdateService(DataService dataService)
        {
            _dataService = dataService;
            _timer = new DispatcherTimer();
            _timer.Tick += (s, e) => FireUpdate();
        }

        public void Subscribe(Action<List<ProcessValue>> callback)
        {
            _subscribers.Add(callback);
        }

        public void Unsubscribe(Action<List<ProcessValue>> callback)
        {
            _subscribers.Remove(callback);
        }

        public void Start()
        {
            if (IsRunning) return;
            _timer.Interval = TimeSpan.FromMilliseconds(UpdateInterval);
            _timer.Start();
            IsRunning = true;
        }

        public void Stop()
        {
            _timer.Stop();
            IsRunning = false;
        }

        private void FireUpdate()
        {
            var readings = new List<ProcessValue>();
            var configs = _dataService.GetSensorConfiguration();

            foreach (var config in configs)
            {
                // Use evolving live values (random walk) so the UI actually updates.
                // Historical data still comes from the read-only SQLite DB.
                readings.Add(_dataService.GetLiveSensorReading(config.Name));
            }

            foreach (var subscriber in _subscribers)
            {
                subscriber?.Invoke(readings);
            }
        }
    }
}
