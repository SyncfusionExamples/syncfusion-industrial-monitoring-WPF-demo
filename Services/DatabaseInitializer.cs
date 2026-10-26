using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Syncfusion.IndustrialMonitoring.POC.Localization;
using Syncfusion.IndustrialMonitoring.POC.Models;

namespace Syncfusion.IndustrialMonitoring.POC.Services
{
    /// <summary>
    /// Initializes SQLite database with synthetic sensor and alarm data.
    /// Creates read-only connection strings and seeds initial data.
    /// </summary>
    public static class DatabaseInitializer
    {
        private static readonly string DbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IndustrialMonitoring",
            "monitoring.db"
        );

        // Bump this version when the seed data changes so existing databases are
        // re-created with the latest seed data (severity stored as English enum).
        private const int SchemaVersion = 4;

        public static string GetReadOnlyConnectionString()
        {
            var connectionString = $"Data Source={DbPath};Mode=ReadOnly;";
            return connectionString;
        }

        public static void Initialize()
        {
            // Create directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));

            // Check if database exists, if not create it with schema
            if (!File.Exists(DbPath))
            {
                CreateDatabase();
                return;
            }

            // Re-create the database if the seed schema version changed so the
            // Arabic seed data is used instead of the legacy English data.
            if (GetStoredVersion() < SchemaVersion)
            {
                RecreateDatabase();
            }
        }

        private static int GetStoredVersion()
        {
            try
            {
                // Remove read-only flag temporarily so we can read a version marker.
                var fileInfo = new FileInfo(DbPath);
                fileInfo.Attributes &= ~FileAttributes.ReadOnly;

                using (var connection = new SqliteConnection($"Data Source={DbPath};"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='SchemaVersion';";
                        var hasTable = Convert.ToInt64(command.ExecuteScalar()) > 0;
                        if (!hasTable) return 0;

                        command.CommandText = "SELECT Version FROM SchemaVersion;";
                        return Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private static void RecreateDatabase()
        {
            try
            {
                var fileInfo = new FileInfo(DbPath);
                fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                fileInfo.Delete();
            }
            catch
            {
                // If we can't delete it, leave it as is.
                return;
            }

            CreateDatabase();
        }

        private static void CreateDatabase()
        {
            // Create database with write access initially
            var connectionString = $"Data Source={DbPath};";
            
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // Create sensor data table
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE SensorReadings (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            SensorName TEXT NOT NULL,
                            Value REAL NOT NULL,
                            Unit TEXT NOT NULL,
                            DataQuality TEXT NOT NULL,
                            Timestamp TEXT NOT NULL
                        );
                        
                        CREATE INDEX idx_sensor_timestamp ON SensorReadings(SensorName, Timestamp DESC);
                    ";
                    command.ExecuteNonQuery();
                }

                // Create alarm events table
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE AlarmEvents (
                            EventId INTEGER PRIMARY KEY AUTOINCREMENT,
                            Timestamp TEXT NOT NULL,
                            Source TEXT NOT NULL,
                            Severity TEXT NOT NULL,
                            Message TEXT NOT NULL,
                            Status TEXT NOT NULL,
                            AcknowledgedBy TEXT,
                            AcknowledgedTime TEXT
                        );
                        
                        CREATE INDEX idx_alarm_timestamp ON AlarmEvents(Timestamp DESC);
                        CREATE INDEX idx_alarm_status ON AlarmEvents(Status);
                    ";
                    command.ExecuteNonQuery();
                }

                // Record the seed schema version so future data changes can trigger a re-seed.
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE SchemaVersion (Version INTEGER NOT NULL);
                        INSERT INTO SchemaVersion (Version) VALUES (@version);
                    ";
                    command.Parameters.AddWithValue("@version", SchemaVersion);
                    command.ExecuteNonQuery();
                }

                // Seed synthetic sensor data (last 500 readings)
                SeedSensorData(connection);

                // Seed synthetic alarm data (100 sample alarms)
                SeedAlarmData(connection);
            }

            // After creation, modify file to read-only
            MakeDatabaseReadOnly();
        }

        private static void SeedSensorData(SqliteConnection connection)
        {
            var random = new Random(42); // Fixed seed for reproducible data
            var sensorNames = new[] { ArabicMap.SensorTemperature, ArabicMap.SensorPressure, ArabicMap.SensorHumidity };
            var units = new[] { ArabicMap.ToArabic(Unit.Celsius), ArabicMap.ToArabic(Unit.Bar), ArabicMap.ToArabic(Unit.Percent) };
            var baseTime = DateTime.UtcNow.AddMinutes(-500);

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO SensorReadings (SensorName, Value, Unit, DataQuality, Timestamp)
                    VALUES (@sensorName, @value, @unit, @quality, @timestamp)
                ";

                for (int i = 0; i < 500; i++)
                {
                    int sensorIndex = i % 3;
                    double value = random.Next(0, 101) * (random.NextDouble() * 0.1 + 0.95); // Smooth variation
                    var quality = value > 90 ? ArabicMap.ToArabic(DataQuality.Bad) : (value > 70 ? ArabicMap.ToArabic(DataQuality.Stale) : ArabicMap.ToArabic(DataQuality.Good));
                    var timestamp = baseTime.AddMinutes(i).ToString("O");

                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@sensorName", sensorNames[sensorIndex]);
                    command.Parameters.AddWithValue("@value", Math.Round(value, 2));
                    command.Parameters.AddWithValue("@unit", units[sensorIndex]);
                    command.Parameters.AddWithValue("@quality", quality);
                    command.Parameters.AddWithValue("@timestamp", timestamp);

                    command.ExecuteNonQuery();
                }
            }
        }

        private static void SeedAlarmData(SqliteConnection connection)
        {
            var random = new Random(42);
            var sources = new[] { ArabicMap.SensorTemperature, ArabicMap.SensorPressure, ArabicMap.SensorHumidity };
            var severities = new[]
            {
                AlarmSeverity.Info.ToString(),
                AlarmSeverity.Warning.ToString(),
                AlarmSeverity.Critical.ToString()
            };
            var messages = ArabicMap.AlarmMessages.ToArray();

            var baseTime = DateTime.UtcNow.AddHours(-24);

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO AlarmEvents (Timestamp, Source, Severity, Message, Status, AcknowledgedBy, AcknowledgedTime)
                    VALUES (@timestamp, @source, @severity, @message, @status, @acknowledgedBy, @acknowledgedTime)
                ";

                for (int i = 0; i < 100; i++)
                {
                    var timestamp = baseTime.AddMinutes(i * 15).ToString("O");
                    var source = sources[random.Next(sources.Length)];
                    var severity = severities[random.Next(severities.Length)];
                    var message = messages[random.Next(messages.Length)];
                    var status = random.Next(0, 100) > 30 ? ArabicMap.StatusAck : ArabicMap.StatusOpen;
                    var acknowledgedBy = status == ArabicMap.StatusAck ? ArabicMap.Admin : null;
                    var acknowledgedTime = status == ArabicMap.StatusAck ? baseTime.AddMinutes(i * 15 + 5).ToString("O") : null;

                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@timestamp", timestamp);
                    command.Parameters.AddWithValue("@source", source);
                    command.Parameters.AddWithValue("@severity", severity);
                    command.Parameters.AddWithValue("@message", message);
                    command.Parameters.AddWithValue("@status", status);
                    command.Parameters.AddWithValue("@acknowledgedBy", acknowledgedBy ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@acknowledgedTime", acknowledgedTime ?? (object)DBNull.Value);

                    command.ExecuteNonQuery();
                }
            }
        }

        private static void MakeDatabaseReadOnly()
        {
            try
            {
                // Set file to read-only
                var fileInfo = new FileInfo(DbPath);
                fileInfo.Attributes = FileAttributes.ReadOnly;
            }
            catch
            {
                // If we can't set read-only, continue anyway
                // The connection string will enforce read-only mode
            }
        }
    }
}
