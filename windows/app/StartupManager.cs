using System;
using System.Reflection;
using Microsoft.Win32;

// Manages per-user Windows login auto-start (no UAC elevation required).
internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "KeycatDesktop";

    public static bool IsEnabled()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
            {
                if (key == null) return false;
                string value = key.GetValue(ValueName) as string;
                return !string.IsNullOrEmpty(value);
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool SetEnabled(bool enable)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
            {
                if (key == null) return false;
                if (enable)
                {
                    string exe = Assembly.GetExecutingAssembly().Location;
                    if (string.IsNullOrEmpty(exe)) return false;
                    key.SetValue(ValueName, "\"" + exe + "\"", RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(ValueName, false);
                }
                return true;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }
}
