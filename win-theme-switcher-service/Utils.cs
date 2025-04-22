using System;
using Microsoft.Win32;

/// <summary>
/// Sets of functions that have utility uses across this project
/// </summary>
static class Utils
{
    /// <summary>
    /// A generic id for this app. This will be used in constructing various other ids including directories and registry entries
    /// </summary>
    public const string ID = "Windows-Theme-Switcher-Service";
    /// <summary>
    /// The base directory for storing/persisting app data and related info
    /// </summary>
    public static readonly string BASE_DIR = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\{ID}";
    /// <summary>
    /// The windows identifier for registry value holding windows theme config
    /// </summary>
    private const string REG_WIN_THEME_KEY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    /// <summary>
    /// All possible white spaces for delimiting latin strings
    /// </summary>.
    /// <remarks>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.char.iswhitespace?view=net-9.0#system-char-iswhitespace(system-string-system-int32)">See this doc under the 'Remarks' section for details</see>
    /// </remarks>
    public static readonly char[] WHITESPACE = new char[] {
        '\u0020',
        '\u00A0',
        '\u1680',
        '\u2000',
        '\u2001',
        '\u2002',
        '\u2003',
        '\u2004',
        '\u2005',
        '\u2006',
        '\u2007',
        '\u2008',
        '\u2009',
        '\u200A',
        '\u202F',
        '\u205F',
        '\u3000',
        '\u2028',
        '\u2029',
        '\u0009',
        '\u000A',
        '\u000B',
        '\u000C',
        '\u000D',
        '\u0085',
    };
    /// <summary>
    /// Writes a value using the given registry key
    /// </summary>
    /// <param name="writableKey">a registry key that can be used to write to the registry using <c>writableKey.SetValue()</c></param>
    /// <param name="valueName">The name of the value to be written to the registry</param>
    /// <param name="value">The value to be written to the registry</param>
    /// <param name="valueKind">The type of the value to be written to the registry</param>
    public static void SetRegistryValue(RegistryKey writableKey, string valueName, object value, RegistryValueKind valueKind = RegistryValueKind.String)
    {
        if (writableKey.GetValue(valueName, 1) != value)
            writableKey.SetValue(valueName, value, valueKind);
    }
    /// <summary>
    /// The same as <see cref="SetRegistryValue"/> with the difference being that this will only set write to the registry if the given <c>valueName</c> is empty
    /// </summary>
    /// <param name="writableKey">a registry key that can be used to write to the registry using <c>writableKey.SetValue()</c></param>
    /// <param name="valueName">The name of the value to be written to the registry</param>
    /// <param name="value">The value to be written to the registry</param>
    /// <param name="valueKind">The type of the value to be written to the registry</param>
    public static void SetRegistryValueIfNull(RegistryKey writableKey, string valueName, object value, RegistryValueKind valueKind = RegistryValueKind.String)
    {
        if(writableKey.GetValue(valueName) != null)
        {
            SetRegistryValue(writableKey, valueName, value, valueKind);
        }
    }
    /// <summary>
    /// Calls <c>Registry.CurrentUser.OpenSubKey</c> with <c>writable</c> set to <c>true</c>
    /// </summary>
    /// <param name="name">The name of windows identifier for this key</param>
    /// <returns>A <c>RegistryKey</c> object for the given key</returns>
    public static RegistryKey GetWritableRegistryKey(string name)
    {
        return Registry.CurrentUser.OpenSubKey(name, true)!;
    }
    /// <summary>
    /// Sets the system and app color mode to a light (if <c>false</c> is passed as an argument) or dark (if <c>true</c> is passed asn an argument) mode
    /// </summary>
    /// <param name="darkMode">The mode to switch the system to</param>
    public static void SetColorMode(bool darkMode)
    {
        try
        {
                using var key = GetWritableRegistryKey(REG_WIN_THEME_KEY_PATH);

                if (key == null)
                {
                    string tempStr = darkMode ? "switch to" : "switch from";
                    Console.Error.WriteLine($"Unable to retrieve registry key: \"{REG_WIN_THEME_KEY_PATH}\" while trying to {tempStr} dark mode");
                    Console.Error.WriteLine("Toggle failed");
                    return;
                }

                SetRegistryValue(key, "AppsUseLightTheme", darkMode ? 0 : 1, RegistryValueKind.DWord);
                Console.WriteLine($"\"AppsUseLightTheme\" successfully set to a value of {(darkMode ? '0' : '1')}");
                SetRegistryValue(key, "SystemUsesLightTheme", darkMode ? 0 : 1, RegistryValueKind.DWord);
                Console.WriteLine($"\"SystemUsesLightTheme\" successfully set to a value of {(darkMode ? '0' : '1')}");
        }
        catch(Exception e)
        {
            string tempStr = darkMode ? "light to dark" : "dark to light";
            Console.Error.WriteLine($"Unable to switch mode from {tempStr}. See details below");
            Console.Error.WriteLine(e.ToString());
            return;
        }
        string temp = darkMode ? "light to dark" : "dark to light";
        Console.WriteLine($"Switch from {temp} successful");
    }
}