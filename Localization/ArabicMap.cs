using System;
using System.Collections.Generic;
using System.Linq;
using Syncfusion.IndustrialMonitoring.POC.Models;

namespace Syncfusion.IndustrialMonitoring.POC.Localization
{
    /// <summary>
    /// Central Arabic localization map. Provides bidirectional translation
    /// between the canonical (English) internal keys and the Arabic display
    /// / database strings, so that the database can be seeded entirely in
    /// Arabic while the application logic keeps working against the enums.
    /// </summary>
    public static class ArabicMap
    {
        // ----- Status values (stored verbatim in the database) -----
        public const string StatusOpen = "مفتوح";
        public const string StatusAck = "مُقر";
        public const string Admin = "المشرف";

        // ----- Arabic sensor name constants (canonical internal names) -----
        public const string SensorTemperature = "درجة الحرارة";
        public const string SensorPressure = "الضغط";
        public const string SensorHumidity = "الرطوبة";

        // ----- Sensor names (legacy English key -> Arabic) -----
        public static readonly IReadOnlyDictionary<string, string> SensorNames =
            new Dictionary<string, string>
            {
                ["Temperature"] = SensorTemperature,
                ["Pressure"] = SensorPressure,
                ["Humidity"] = SensorHumidity
            };

        // ----- Units -----
        public static readonly IReadOnlyDictionary<Unit, string> Units =
            new Dictionary<Unit, string>
            {
                [Unit.Celsius] = "درجة مئوية",
                [Unit.Bar] = "بار",
                [Unit.Percent] = "نسبة مئوية",
                [Unit.Liter] = "لتر"
            };

        // ----- Data quality -----
        public static readonly IReadOnlyDictionary<DataQuality, string> DataQualities =
            new Dictionary<DataQuality, string>
            {
                [DataQuality.Good] = "جيد",
                [DataQuality.Bad] = "سيئ",
                [DataQuality.Stale] = "قديم"
            };

        // ----- Alarm severity -----
        public static readonly IReadOnlyDictionary<AlarmSeverity, string> Severities =
            new Dictionary<AlarmSeverity, string>
            {
                [AlarmSeverity.Info] = "معلومات",
                [AlarmSeverity.Warning] = "تحذير",
                [AlarmSeverity.Critical] = "حرج"
            };

        // ----- Alarm messages (seeded) -----
        public static readonly IReadOnlyList<string> AlarmMessages =
            new List<string>
            {
                "تجاوزت قراءة المستشعر حد التحذير",
                "تجاوزت قراءة المستشعر حد الإنذار",
                "عادت قراءة المستشعر إلى الوضع الطبيعي",
                "تم اكتشاف انتهاء مهلة الاتصال",
                "تم اكتشاف انحراف في المعايرة"
            };

        // ----- Forward mapping -----
        public static string ToArabic(string englishSensorName) =>
            SensorNames.TryGetValue(englishSensorName, out var ar) ? ar : englishSensorName;

        public static string ToArabic(Unit unit) =>
            Units.TryGetValue(unit, out var ar) ? ar : unit.ToString();

        public static string ToArabic(DataQuality quality) =>
            DataQualities.TryGetValue(quality, out var ar) ? ar : quality.ToString();

        public static string ToArabic(AlarmSeverity severity) =>
            Severities.TryGetValue(severity, out var ar) ? ar : severity.ToString();

        // ----- Reverse mapping (Arabic -> enum) -----
        public static Unit UnitFromArabic(string arabic)
        {
            foreach (var kv in Units)
                if (kv.Value == arabic) return kv.Key;
            return default;
        }

        public static DataQuality DataQualityFromArabic(string arabic)
        {
            foreach (var kv in DataQualities)
                if (kv.Value == arabic) return kv.Key;
            return DataQuality.Good;
        }

        public static AlarmSeverity SeverityFromArabic(string arabic)
        {
            foreach (var kv in Severities)
                if (kv.Value == arabic) return kv.Key;
            return AlarmSeverity.Info;
        }

        // ----- Database-safe parsing (supports legacy English values too) -----
        public static Unit UnitFromDb(string value)
        {
            if (string.IsNullOrEmpty(value)) return default;
            if (Enum.TryParse<Unit>(value, true, out var unit)) return unit;
            return UnitFromArabic(value);
        }

        public static DataQuality DataQualityFromDb(string value)
        {
            if (string.IsNullOrEmpty(value)) return DataQuality.Good;
            if (Enum.TryParse<DataQuality>(value, true, out var quality)) return quality;
            return DataQualityFromArabic(value);
        }

        public static AlarmSeverity SeverityFromDb(string value)
        {
            if (string.IsNullOrEmpty(value)) return AlarmSeverity.Info;
            if (Enum.TryParse<AlarmSeverity>(value, true, out var severity)) return severity;
            return SeverityFromArabic(value);
        }
    }
}
