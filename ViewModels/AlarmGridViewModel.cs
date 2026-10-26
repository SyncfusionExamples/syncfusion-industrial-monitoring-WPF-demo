using System;
using System.Collections.ObjectModel;
using Syncfusion.IndustrialMonitoring.POC.Localization;
using Syncfusion.IndustrialMonitoring.POC.Models;
using Syncfusion.IndustrialMonitoring.POC.Services;

namespace Syncfusion.IndustrialMonitoring.POC.ViewModels
{
    public class AlarmGridViewModel : ViewModelBase
    {
        private ObservableCollection<AlarmEvent> _alarmHistory;
        private int _maxAlarms = 1000;

        public ObservableCollection<AlarmEvent> AlarmHistory
        {
            get => _alarmHistory;
            set { _alarmHistory = value; RaisePropertyChanged(nameof(AlarmHistory)); }
        }

        public AlarmGridViewModel(DataService dataService)
        {
            AlarmHistory = new ObservableCollection<AlarmEvent>();

            // Load sample alarms
            var alarms = dataService.GetSampleAlarms(100);
            foreach (var alarm in alarms)
                AlarmHistory.Add(alarm);
        }

        public void AddAlarm(AlarmEvent alarm)
        {
            if (alarm == null) return;

            AlarmHistory.Insert(0, alarm); // Add to top

            if (AlarmHistory.Count > _maxAlarms)
                AlarmHistory.RemoveAt(AlarmHistory.Count - 1);
        }

        public void AcknowledgeAlarm(AlarmEvent alarm)
        {
            if (alarm == null) return;
            alarm.Status = ArabicMap.StatusAck;
            alarm.AcknowledgedBy = ArabicMap.Admin;
            alarm.AcknowledgedTime = DateTime.UtcNow;
            RaisePropertyChanged(nameof(AlarmHistory));
        }

        public void ClearHistory()
        {
            AlarmHistory.Clear();
        }
    }
}
