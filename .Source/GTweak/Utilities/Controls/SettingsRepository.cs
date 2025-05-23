﻿using GTweak.Utilities.Helpers;
using GTweak.Utilities.Helpers.Managers;
using GTweak.Windows;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GTweak.Utilities.Controls
{
    internal struct StoragePaths
    {
        internal static string Config = string.Empty;
        internal static string FolderLocation => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "GTweak");
        internal static string SystemDisk => Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
        internal static string HostsFile => Path.Combine(Environment.SystemDirectory, @"drivers\etc\hosts");
        internal static string PowFile => Path.Combine(FolderLocation, "UltimatePerformance.pow");
        internal static string IconBlank => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Blank.ico");
        internal static string LogFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GTweak_Error.log");
        internal static string RegistryLocation => @"HKEY_CURRENT_USER\Software\GTweak";
    }

    internal sealed class SettingsRepository
    {
        [DllImport("winmm.dll")]
        internal static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        internal static readonly string[] AvailableLangs = { "en", "fr", "it", "ko", "ru", "uk" };
        internal static readonly string[] AvailableThemes = { "Dark", "Light", "Cobalt", "Dark amethyst", "Cold Blue", "System" };

        internal static int PID = 0;
        internal static string currentRelease = (Assembly.GetEntryAssembly() ?? throw new InvalidOperationException()).GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split(' ').Last().Trim();
        internal static readonly string currentName = AppDomain.CurrentDomain.FriendlyName;
        internal static readonly string currentLocation = Assembly.GetExecutingAssembly().Location;

        private static readonly Dictionary<string, object> _defaultSettings = new Dictionary<string, object>
        {
            ["Notification"] = true,
            ["Update"] = true,
            ["TopMost"] = false,
            ["Sound"] = true,
            ["Volume"] = 50,
            ["Language"] = App.GettingSystemLanguage,
            ["Theme"] = "Dark",
            ["HiddenIP"] = true
        };

        private static readonly Dictionary<string, object> _cachedSettings = new Dictionary<string, object>(_defaultSettings);

        internal static bool IsViewNotification { get => (bool)_cachedSettings["Notification"]; set => ChangingParameters(value, "Notification"); }
        internal static bool IsUpdateCheckRequired { get => (bool)_cachedSettings["Update"]; set => ChangingParameters(value, "Update"); }
        internal static bool IsTopMost { get => (bool)_cachedSettings["TopMost"]; set => ChangingParameters(value, "TopMost"); }
        internal static bool IsPlayingSound { get => (bool)_cachedSettings["Sound"]; set => ChangingParameters(value, "Sound"); }
        internal static int Volume { get => (int)_cachedSettings["Volume"]; set => ChangingParameters(value, "Volume"); }
        internal static string Language { get => (string)_cachedSettings["Language"]; set => ChangingParameters(value, "Language"); }
        internal static string Theme { get => (string)_cachedSettings["Theme"]; set => ChangingParameters(value, "Theme"); }
        internal static bool IsHiddenIpAddress { get => (bool)_cachedSettings["HiddenIP"]; set => ChangingParameters(value, "HiddenIP"); }

        private static void ChangingParameters<T>(T value, string key)
        {
            _cachedSettings[key] = value;
            RegistryHelp.Write(Registry.CurrentUser, @"Software\GTweak", key, value.ToString(), RegistryValueKind.String);
        }

        internal static void СheckingParameters()
        {
            bool isRegistryEmpty = false;

            foreach (string key in _defaultSettings.Keys)
            {
                if (RegistryHelp.ValueExists(StoragePaths.RegistryLocation, key))
                {
                    isRegistryEmpty = true;
                    break;
                }
            }

            if (isRegistryEmpty)
            {
                foreach (var subkey in _defaultSettings)
                    ChangingParameters(subkey.Value, subkey.Key);
            }
            else
            {
                foreach (var kv in _defaultSettings)
                {
                    _cachedSettings[kv.Key] = kv.Value is bool defaultBool ? RegistryHelp.GetValue(StoragePaths.RegistryLocation, kv.Key, defaultBool) :
                        kv.Value is int defaultInt ? RegistryHelp.GetValue(StoragePaths.RegistryLocation, kv.Key, defaultInt) :
                        kv.Value is string defaultString ? RegistryHelp.GetValue(StoragePaths.RegistryLocation, kv.Key, defaultString) : kv.Value;
                }
            }
        }

        internal static void SaveFileConfig()
        {
            if (INIManager.IsAllTempDictionaryEmpty)
                new ViewNotification().Show("", "info", "export_warning_notification");
            else
            {
                VistaSaveFileDialog vistaSaveFileDialog = new VistaSaveFileDialog
                {
                    FileName = "Config GTweak",
                    Filter = "(*.INI)|*.INI",
                    RestoreDirectory = true
                };

                if (vistaSaveFileDialog.ShowDialog() != true)
                    return;

                try
                {
                    StoragePaths.Config = Path.Combine(Path.GetDirectoryName(vistaSaveFileDialog.FileName), Path.GetFileNameWithoutExtension(vistaSaveFileDialog.FileName) + ".ini");

                    if (File.Exists(StoragePaths.Config))
                        File.Delete(StoragePaths.Config);

                    INIManager iniManager = new INIManager(StoragePaths.Config);
                    iniManager.Write("GTweak", "Author", "Greedeks");
                    iniManager.Write("GTweak", "Release", currentRelease);
                    iniManager.WriteAll(INIManager.SectionConf, INIManager.TempTweaksConf);
                    iniManager.WriteAll(INIManager.SectionIntf, INIManager.TempTweaksIntf);
                    iniManager.WriteAll(INIManager.SectionSvc, INIManager.TempTweaksSvc);
                    iniManager.WriteAll(INIManager.SectionSys, INIManager.TempTweaksSys);
                }
                catch (Exception ex) { ErrorLogging.LogDebug(ex); }
            }
        }

        internal static void OpenFileConfig()
        {
            VistaOpenFileDialog vistaOpenFileDialog = new VistaOpenFileDialog
            {
                Filter = "(*.INI)|*.INI",
                RestoreDirectory = true,
            };

            if (vistaOpenFileDialog.ShowDialog() == false)
                return;

            StoragePaths.Config = vistaOpenFileDialog.FileName;
            INIManager iniManager = new INIManager(StoragePaths.Config);

            if (iniManager.GetKeysOrValue("GTweak", false).Contains("Greedeks") && iniManager.GetKeysOrValue("GTweak").Contains("Release"))
            {
                if (File.ReadLines(StoragePaths.Config).Any(line => line.Contains("TglButton")) || File.ReadLines(StoragePaths.Config).Any(line => line.Contains("Slider")))
                    new ImportWindow(Path.GetFileName(vistaOpenFileDialog.FileName)).ShowDialog();
                else
                    new ViewNotification().Show("", "info", "empty_import_notification");
            }
            else
                new ViewNotification().Show("", "info", "warn_import_notification");
        }

        internal static void SelfRemoval()
        {
            try
            {
                RegistryHelp.DeleteFolderTree(Registry.CurrentUser, @"Software\GTweak");
                RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\Microsoft\Tracing\GTweak_RASAPI32");
                RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\Microsoft\Tracing\GTweak_RASMANCS");

                CommandExecutor.RunCommand($"/c taskkill /f /im \"{currentName}\" & choice /c y /n /d y /t 3 & del \"{currentLocation}\" & " +
                    @$"rd /s /q ""{StoragePaths.FolderLocation}"" & rd /s /q ""{Environment.SystemDirectory}\config\systemprofile\AppData\Local\GTweak""");
            }
            catch (Exception ex) { ErrorLogging.LogDebug(ex); }
        }

        internal static void SelfReboot() => CommandExecutor.RunCommand($"/c taskkill /f /im \"{currentName}\" & choice /c y /n /d y /t 1 & start \"\" \"{currentLocation}\"");
    }
}
