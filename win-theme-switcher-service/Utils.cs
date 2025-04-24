using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

/// <summary>
/// Sets of functions that have utility uses across this project
/// </summary>
static class Utils
{
    public static readonly WinThemeSwitcherService.LocalFileLogger fileLogger = new WinThemeSwitcherService.LocalFileLogger("logs.txt");
    public enum SerializerType
    {
        JSON,
        XML,
        YAML,
        CSV,
        INI,
        TOML,
        BSON,
        MessagePack,
        ProtocolBuffers,
        Avro,
        Parquet,
        MsgPack
    }
    public enum ArrayMatcher
    {
        All,
        None,
        Any,
        One,
        Some
    }
    /// <summary>
    /// A generic id for this app. This will be used in constructing various other ids including directories and registry entries
    /// </summary>
    public const string ID = "WinThemeSwitcherService";
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
    /// Remove a registry entry
    /// </summary>
    /// <param name="writableKey"></param>
    /// <param name="valueName"></param>
    public static void RemoveRegistryValue(RegistryKey writableKey, string valueName)
    {
        if(writableKey.GetValue(valueName) != null)
        {
            writableKey.DeleteValue(valueName);
            writableKey.DeleteSubKey(valueName);
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
                    fileLogger.LogSync($"Unable to retrieve registry key: \"{REG_WIN_THEME_KEY_PATH}\" while trying to {tempStr} dark mode", LogLevel.Error);
                    fileLogger.LogSync("Toggle failed", LogLevel.Error);
                    return;
                }

                SetRegistryValue(key, "AppsUseLightTheme", darkMode ? 0 : 1, RegistryValueKind.DWord);
                fileLogger.LogSync($"\"AppsUseLightTheme\" successfully set to a value of {(darkMode ? '0' : '1')}");
                SetRegistryValue(key, "SystemUsesLightTheme", darkMode ? 0 : 1, RegistryValueKind.DWord);
                fileLogger.LogSync($"\"SystemUsesLightTheme\" successfully set to a value of {(darkMode ? '0' : '1')}");
        }
        catch(Exception e)
        {
            string tempStr = darkMode ? "light to dark" : "dark to light";
            fileLogger.LogSync($"Unable to switch mode from {tempStr}. See details below", LogLevel.Error);
            fileLogger.LogSync(e.ToString(), LogLevel.Error);
            return;
        }
        string temp = darkMode ? "light to dark" : "dark to light";
        fileLogger.LogSync($"Switch from {temp} successful", LogLevel.Error);
    }
    public static R[] Map<T, R>(T[] array, Func<T, int, T[], R> mapper)
    {
        var result = new R[array.Length];
        for(int i = 0; i < array.Length; i++)
        {
            result[i] = mapper(array[i], i, array);
        }
        return result;
    }
    public static T Find<T>(T[] array, Func<T, int, T[], bool> predicate)
    {
        for(int i = 0; i < array.Length; i++)
        {
            if(predicate(array[i], i, array)) return array[i];
        }
#pragma warning disable CS8603 // Possible null reference return.
        return default;
#pragma warning restore CS8603 // Possible null reference return.
    }
    public static T[] Filter<T>(T[] array, Func<T, int, T[], bool> filter)
    {
        var result = new T[array.Length];
        var tracker = -1;
        for(int iteration = 0; iteration < array.Length; iteration++)
        {
            if(filter(array[iteration], iteration, array)) result[++tracker] = array[iteration];
        }
        return result[0..(tracker + 1)];
    }
#pragma warning disable CS8601 // Possible null reference assignment.
    public static R Reduce<T, R>(T[] array, Func<R, T, int, T[], R> reducer, R initial = default)
#pragma warning restore CS8601 // Possible null reference assignment.
    {
        var accumulated = initial;
        for(int iteration = 0; iteration < array.Length; iteration++)
        {
            accumulated = reducer(accumulated, array[iteration], iteration, array);
        }
        return accumulated;
    }
    public static bool Match<T>(T[] array, T operand, ArrayMatcher type, Func<T, T, int, T[], bool> comparator)
    {
        switch(type)
        {
            default:
            case ArrayMatcher.Some:
            case ArrayMatcher.Any:
                return Any(array, operand, comparator);
            case ArrayMatcher.All:
                return All(array, operand, comparator);
            case ArrayMatcher.One:
                return One(array, operand, comparator);
            case ArrayMatcher.None:
                return None(array, operand, comparator);
        }
    }
    private static bool All<T>(T[] array, T operand, Func<T, T, int, T[], bool> comparator)
    {
        return Frequency(array, operand, comparator) == array.Length;
    }
    private static bool Any<T>(T[] array, T operand, Func<T, T, int, T[], bool> comparator)
    {
        return Frequency(array, operand, comparator) > 0;
    }
    private static bool None<T>(T[] array, T operand, Func<T, T, int, T[], bool> comparator)
    {
        return Frequency(array, operand, comparator) == 0;
    }
    private static bool One<T>(T[] array, T operand, Func<T, T, int, T[], bool> comparator)
    {
        return Frequency(array, operand, comparator) == 1;
    }
    public static int Frequency<T>(T[] array, T operand, Func<T, T, int, T[], bool> comparator)
    {
        var frequency = 0;
        for(int iteration = 0; iteration < array.Length; iteration++)
        {
            if(comparator(operand, array[iteration], iteration, array)) frequency++;
        }
        return frequency;
    }
    public static int FindIndex<T>(T[] array, Func<T, int, T[], bool> predicate)
    {
        for(int i = 0; i < array.Length; i++)
        {
            if(predicate(array[i], i, array)) return i;
        }
        return -1;
    }
    public static string ToString<T>(T[] array, SerializerType type)
    {
        #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        return ToString(array, type, null);
        #pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
    public static string ToString<T>(T[] array, SerializerType type, Func<T, int, T[], string> mapper)
    {
        switch(type)
        {
            case SerializerType.JSON:
            {
                return ToJSON(array, mapper);
            };
            case SerializerType.XML:
            {
                return ToXML(array, mapper);
            };
            default:
            case SerializerType.CSV:
            {
                return ToCSV(array, mapper);
            };
        }
    }
    private static string ToJSON<T>(T[] array, Func<T, int, T[], string>? mapper)
    {
        if(mapper == null)
        {
            return JsonSerializer.Serialize(array, array.GetType());
        }
        return JsonSerializer.Serialize(Map(array, mapper), array.GetType());
    }
    private static string ToXML<T>(T[] array, Func<T, int, T[], string>? mapper)
    {
        var cache = new StringWriter();
        var xmlWriter = XmlWriter.Create(cache);
        if(mapper == null)
        {
            new XmlSerializer(array.GetType()).Serialize(xmlWriter, array);
        }
        else
        {
            new XmlSerializer(array.GetType()).Serialize(xmlWriter, Map(array, mapper));
        }
        return cache.ToString();
    }
    /// <summary>
    /// Serializes an array of T into a CSV string, one row per element.
    /// </summary>
    /// <typeparam name="T">Type of the items in the array.</typeparam>
    /// <param name="array">The array to serialize.</param>
    /// <param name="mapper">
    /// Optional: maps each element to a raw string value. 
    /// If null, element.ToString() is used (or empty if null).
    /// </param>
    /// <returns>A CSV text with one quoted field per line.</returns>
    private static string ToCSV<T>(T[] array, Func<T, int, T[], string>? mapper)
    {
        if (array == null || array.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();

        for (int i = 0; i < array.Length; i++)
        {
            // get raw field value
            var raw = mapper != null
                ? mapper(array[i], i, array)
                : array[i]?.ToString() ?? string.Empty;

            // escape & quote it
            sb.Append(EscapeAndQuote(raw));
            sb.Append(',');

            // append newline except after last
            if (i < array.Length - 1)
                sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Doubles any quotes (single or double) and then wraps the whole thing in double-quotes.
    /// </summary>
    private static string EscapeAndQuote(string field)
    {
        // 1) escape double-quotes by doubling them
        // 2) escape single-quotes by doubling them (per your spec)
        var escaped = field.Replace("\"", "\"\"");

        // always wrap in double-quotes so commas/newlines stay inside the field
        return $"\"{escaped}\"";
    }
    
}