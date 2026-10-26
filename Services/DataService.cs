using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Syncfusion.IndustrialMonitoring.POC.Localization;
using Syncfusion.IndustrialMonitoring.POC.Models;

namespace Syncfusion.IndustrialMonitoring.POC.Services
{
    /// <summary>
    /// Data service that reads from SQLite database in read-only mode.
    /// Provides sensor readings and alarm events with timestamps, units, and quality levels.
    /// </summary>
    public class DataService
    {
        private static Random _random = new Random();
        private readonly string _readOnlyConnectionString;
        private readonly Dictionary<string, double> _lastValues = new Dictionary<string, double>();
        private readonly Dictionary<string, DateTime> _lastTimestamps = new Dictionary<string, DateTime>();

        public DataService()
        {
            // Initialize database on first use
            DatabaseInitializer.Initialize();
            _readOnlyConnectionString = DatabaseInitializer.GetReadOnlyConnectionString();
        }

        /// <summary>
        /// Generates an evolving live value (random walk) for real-time updates.
        /// Historical data still comes from the read-only SQLite DB; this method
        /// simulates the "live" sensor stream on top of that.
        /// </summary>
        public ProcessValue GetLiveSensorReading(string sensorName)
        {
            var config = GetSensorConfiguration().FirstOrDefault(c => c.Name == sensorName);
            if (config == null) return null;

            // Start from the last known value (or the DB value on first call)
            if (!_lastValues.TryGetValue(sensorName, out double current))
            {
                current = GetLatestSensorReading(sensorName)?.Value
                          ?? (config.MinValue + config.MaxValue) / 2;
            }

            // Random walk: drift a small amount around the current value
            double drift = (_random.NextDouble() - 0.5) * 6.0; // ±3 units per tick
            double next = Math.Clamp(current + drift, config.MinValue, config.MaxValue);

            _lastValues[sensorName] = next;
            _lastTimestamps[sensorName] = DateTime.UtcNow;

            var quality = next > config.AlarmThreshold ? DataQuality.Bad
                        : next > config.WarningThreshold ? DataQuality.Stale
                        : DataQuality.Good;

            return new ProcessValue
            {
                Id = sensorName.GetHashCode(),
                Name = sensorName,
                Value = Math.Round(next, 2),
                Unit = config.Unit,
                Timestamp = DateTime.UtcNow,
                Quality = quality
            };
        }

        public List<GaugeConfiguration> GetSensorConfiguration()
        {
            return new List<GaugeConfiguration>
            {
                new GaugeConfiguration { Name = ArabicMap.SensorTemperature, MinValue = 0, MaxValue = 100, WarningThreshold = 70, AlarmThreshold = 90, Unit = Unit.Celsius },
                new GaugeConfiguration { Name = ArabicMap.SensorPressure, MinValue = 0, MaxValue = 100, WarningThreshold = 75, AlarmThreshold = 95, Unit = Unit.Bar },
                new GaugeConfiguration { Name = ArabicMap.SensorHumidity, MinValue = 0, MaxValue = 100, WarningThreshold = 80, AlarmThreshold = 95, Unit = Unit.Percent }
            };
        }

        public ProcessValue GetLatestSensorReading(string sensorName)
        {
            try
            {
                using (var connection = new SqliteConnection(_readOnlyConnectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT SensorName, Value, Unit, DataQuality, Timestamp
                            FROM SensorReadings
                            WHERE SensorName = @sensorName
                            ORDER BY Timestamp DESC
                            LIMIT 1
                        ";
                        command.Parameters.AddWithValue("@sensorName", sensorName);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ProcessValue
                                {
                                    Id = sensorName.GetHashCode(),
                                    Name = reader.GetString(0),
                                    Value = reader.GetDouble(1),
                                    Unit = ArabicMap.UnitFromDb(reader.GetString(2)),
                                    Timestamp = DateTime.Parse(reader.GetString(4)),
                                    Quality = ArabicMap.DataQualityFromDb(reader.GetString(3))
                                };
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback to synthetic data if database fails
                var config = GetSensorConfiguration().FirstOrDefault(c => c.Name == sensorName);
                if (config == null) return null;
                double value = _random.Next((int)config.MinValue, (int)config.MaxValue);
                return new ProcessValue
                {
                    Id = sensorName.GetHashCode(),
                    Name = sensorName,
                    Value = value,
                    Unit = config.Unit,
                    Timestamp = DateTime.UtcNow,
                    Quality = value > config.AlarmThreshold ? DataQuality.Bad : (value > config.WarningThreshold ? DataQuality.Stale : DataQuality.Good)
                };
            }

            return null;
        }

        public List<AlarmEvent> GetSampleAlarms(int count = 100)
        {
            var alarms = new List<AlarmEvent>();

            try
            {
                using (var connection = new SqliteConnection(_readOnlyConnectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT EventId, Timestamp, Source, Severity, Message, Status, AcknowledgedBy, AcknowledgedTime
                            FROM AlarmEvents
                            ORDER BY Timestamp DESC
                            LIMIT @count
                        ";
                        command.Parameters.AddWithValue("@count", count);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                alarms.Add(new AlarmEvent
                                {
                                    EventId = reader.GetInt32(0),
                                    Timestamp = DateTime.Parse(reader.GetString(1)),
                                    Source = reader.GetString(2),
                                    Severity = ArabicMap.SeverityFromDb(reader.GetString(3)),
                                    Message = reader.GetString(4),
                                    Status = reader.GetString(5),
                                    AcknowledgedBy = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    AcknowledgedTime = reader.IsDBNull(7) ? null : (DateTime?)DateTime.Parse(reader.GetString(7))
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Empty list if database fails (will be populated by live updates)
            }

            return alarms;
        }

        public List<TrendDataPoint> GenerateSyntheticTrend(int count = 500)
        {
            var trends = new List<TrendDataPoint>(count);

            // Build a smooth, strictly forward-moving time series in memory so the
            // trend chart always renders a proper sequential line (no flat/backward
            // segments) regardless of the database state.
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddSeconds(-count * 2);

            // Start temperature around the middle of the operating range (0-100).
            double value = 55.0;
            double velocity = 0.0; // rate of change, gives smooth momentum

            for (int i = 0; i < count; i++)
            {
                // Deterministic per-tick random using a fixed seed so the initial
                // series is reproducible while still looking organic.
                double noise = (_random.NextDouble() - 0.5);

                // Momentum-based random walk: value follows a smooth trend with
                // gentle acceleration/deceleration instead of jumping around.
                velocity = Math.Clamp(velocity * 0.9 + noise * 1.6, -2.0, 2.0);
                value = Math.Clamp(value + velocity, 10.0, 90.0);

                // Quality classification based on the temperature value.
                var quality = value > 80.0 ? DataQuality.Bad
                            : value > 65.0 ? DataQuality.Stale
                            : DataQuality.Good;

                trends.Add(new TrendDataPoint
                {
                    Id = i,
                    Name = ArabicMap.SensorTemperature,
                    Value = Math.Round(value, 2),
                    Unit = Unit.Celsius,
                    Timestamp = startTime.AddSeconds(i * 2), // strictly increasing
                    Quality = quality,
                    IsOutlier = false
                });
            }

            // Continue the live random walk from where the history ends so the
            // trend line has no jump between historical and live data.
            _lastValues[ArabicMap.SensorTemperature] = Math.Round(value, 2);

            return trends;
        }
    }
}
