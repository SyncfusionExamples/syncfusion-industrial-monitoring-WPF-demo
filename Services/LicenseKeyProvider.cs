using System;
using System.IO;

namespace Syncfusion.IndustrialMonitoring.POC.Services
{
    /// <summary>
    /// Resolves the Syncfusion license key without keeping it in source control.
    /// Lookup order: SYNCFUSION_LICENSE_KEY environment variable, then a
    /// git-ignored SyncfusionLicense.txt file next to the executable.
    /// </summary>
    public static class LicenseKeyProvider
    {
        public const string EnvironmentVariableName = "SYNCFUSION_LICENSE_KEY";
        public const string LicenseFileName = "SyncfusionLicense.txt";

        public static string? GetLicenseKey()
        {
            var key = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (!string.IsNullOrWhiteSpace(key))
                return key.Trim();

            var path = Path.Combine(AppContext.BaseDirectory, LicenseFileName);
            if (File.Exists(path))
            {
                key = File.ReadAllText(path).Trim();
                if (!string.IsNullOrWhiteSpace(key))
                    return key;
            }

            // No key: Syncfusion controls run but show the license warning banner.
            return null;
        }
    }
}
