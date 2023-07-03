﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Widget.WidgetConfiguration;

namespace Widget
{
    internal class WidgetConfiguration
    {
        // Widget constant
        public const string widgetMutex = "Widget_VirusTotal";

        // Paths
        private static readonly string widgetConfigFileName = "config.json";
        private static readonly string widgetConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Widget_VirusTotal");
        private static readonly string widgetConfigPath = Path.Combine(widgetConfigDirectory, widgetConfigFileName);

        public class WidgetSettings
        {
            public string? VirusTotalApiKey { get; set; }
            public bool LicenseAgreementAccepted { get; set; }

            // Default settings
            private static readonly WidgetSettings DefaultSettings = new WidgetSettings
            {
                VirusTotalApiKey = null,
                LicenseAgreementAccepted = false
            };


            public WidgetSettings LoadSettingsFromConfigFile()
            {
                if (!EnsureConfigFileExists())
                    throw new Exception("Config file error");

                // Read config file
                string jsonString = File.ReadAllText(widgetConfigPath);
                if (string.IsNullOrEmpty(jsonString))
                {
#if DEBUG
                    Debug.WriteLine("WidgetSettings jsonString is empty");
#endif
                    InitializeConfigFileWithDefaultSettings();
                    jsonString = File.ReadAllText(widgetConfigPath);
                }

#if DEBUG
                Debug.WriteLine($"WidgetSettings jsonString: {jsonString}");
#endif
                WidgetSettings widgetSettings = JsonSerializer.Deserialize<WidgetSettings>(jsonString);

                // Decrypt sensitive user data
                if (!string.IsNullOrEmpty(widgetSettings?.VirusTotalApiKey))
                {
                    byte[] encryptedKey = Convert.FromBase64String(widgetSettings.VirusTotalApiKey);
                    byte[] decryptedKey = DataProtector.UnprotectData(encryptedKey);
                    widgetSettings.VirusTotalApiKey = Encoding.UTF8.GetString(decryptedKey);
                }

                return widgetSettings;
            }

            public void SaveUserData(string jsonData)
            {
                // Ensure the config file exists
                if (!EnsureConfigFileExists())
                    throw new Exception("Config file error");

                // Encrypt sensitive user data
                WidgetSettings widgetSettings = JsonSerializer.Deserialize<WidgetSettings>(jsonData);
                if (!string.IsNullOrEmpty(widgetSettings?.VirusTotalApiKey))
                {
                    byte[] encryptedKey = DataProtector.ProtectData(Encoding.UTF8.GetBytes(widgetSettings.VirusTotalApiKey));
                    widgetSettings.VirusTotalApiKey = Convert.ToBase64String(encryptedKey);
                }

                // Serialize the WidgetSettings instance to JSON with indentation
                string jsonString = JsonSerializer.Serialize(widgetSettings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(widgetConfigPath, jsonString);
            }


            public bool EnsureConfigFileExists()
            {
                if (!DoesConfigFileExist(widgetConfigPath))
                {
                    if (!CreateConfigFile(widgetConfigPath))
                    {
                        return false;
                    }
                }
                return true;
            }

            public bool DoesConfigFileExist(string filePath)
            {
                return File.Exists(filePath);
            }


            public bool CreateConfigFile(string filePath)
            {
                try
                {
                    // Need to create the directory first
                    // Then the file itself, using the using statement to ensure it's possible to read/write to the file
                    Directory.CreateDirectory(widgetConfigDirectory);
                    using (FileStream fileStream = File.Create(filePath))
                    {

                    }

                    return DoesConfigFileExist(filePath);
                }
                catch (Exception E)
                {
#if DEBUG
                    Debug.WriteLine($"CreateConfigFile: {E.ToString()}");
#endif
                    return false;
                }
            }

            private bool InitializeConfigFileWithDefaultSettings()
            {
                try
                {
                    JsonSerializerOptions jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    string jsonString = JsonSerializer.Serialize(DefaultSettings, jsonOptions);
                    File.WriteAllText(widgetConfigPath, jsonString);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
