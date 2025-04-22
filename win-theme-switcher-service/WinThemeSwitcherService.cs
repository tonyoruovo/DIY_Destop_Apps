using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WinThemeSwitcherService
{
    /// <summary>
    /// A background service that automatically switches the Windows theme between light and dark modes
    /// based on a user-defined schedule. The schedule can be provided via command-line arguments or a settings file.
    /// </summary>
    /// <remarks>
    /// The service runs continuously, checking the current time against the defined schedule and toggling
    /// the theme accordingly. It supports JSON, XML, and plain text formats for the settings file.
    /// <example>
    /// Command-line usage examples:
    /// <code language="bash">
    /// theme-service.exe "20:00" "07:00" // Start and end times as arguments
    /// theme-service.exe "C:\path\to\settings.txt" // Path to a settings file
    /// </code>
    /// </example>
    /// </remarks>
    public class Worker : BackgroundService
    {
        /// <summary>
        /// The default name of the settings file used to store or retrieve the theme switching schedule.
        /// </summary>
        private const string SETTINGS_FILE_NAME = "settings.txt";
        /// <summary>
        /// Logger instance used to log messages and events for the Worker service.
        /// </summary>
        private readonly ILogger<Worker> _logger;
        /// <summary>
        /// Stores (in-memory) the user-defined schedule for switching between light and dark themes.
        /// </summary>
        private readonly LocalSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker"/> class.
        /// Configures the theme switching schedule based on command-line arguments or a settings file.
        /// </summary>
        /// <param name="logger">An <see cref="ILogger{Worker}"/> instance for logging messages and events.</param>
        /// <exception cref="FormatException">Thrown when the provided arguments or settings file have an invalid format.</exception>
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            var args = Environment.GetCommandLineArgs();
            if(args == null)
            {
                _logger.LogCritical("Something went wrong. You might need to restart your computer. Use --help to see usage hints");
                Environment.Exit(0);
            }
            else if(args.Length == 1)
            {
                _logger.LogWarning(new EventId(int.MinValue, $"WARN-{Utils.ID}"), "A single argument was found. This will be consumed as a file path to the args needed");
                if(!TryLoadSettingsFile(args[0], out settings, _logger))
                {
                    ThrowFormatEx();
                    return;
                }
            }
            else if(args.Length == 2)
            {
                if(!TimeSpan.TryParse(args[0], out var start) || !TimeSpan.TryParse(args[1], out var end))
                {
                    ThrowFormatEx();
                    return;
                }
                settings = new LocalSettings {
                    start = start,
                    end = end
                };
            }
            else
            {
                if(!TryLoadSettingsFile($"{Utils.BASE_DIR}\\{SETTINGS_FILE_NAME}", out settings, _logger))
                {
                    ThrowFormatEx();
                    return;
                }
            }
        }
        /// <summary>
        /// Executes the background service logic to periodically check the current time and switch
        /// the Windows theme between light and dark modes based on the user-defined schedule.
        /// </summary>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> that is triggered when the service is stopping.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
           while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now.TimeOfDay;
                bool shouldBeDark = IsDarkPeriod(settings, now);

                Utils.SetColorMode(shouldBeDark);

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            SaveSettings(settings);
        }
        /// <summary>
        /// Determines whether the current time falls within the dark mode period based on the user-defined schedule.
        /// </summary>
        /// <param name="settings">The <see cref="LocalSettings"/> containing the start and end times for dark mode.</param>
        /// <param name="currentTime">The current time to evaluate.</param>
        /// <returns><c>true</c> if the current time is within the dark mode period; otherwise, <c>false</c>.</returns>
        private bool IsDarkPeriod(LocalSettings settings, TimeSpan currentTime)
        {
            if (settings.start < settings.end)
                return currentTime >= settings.start && currentTime < settings.end;
            
            return currentTime >= settings.start || currentTime < settings.end;
        }
        /// <summary>
        /// Attempts to load the theme switching schedule from a specified settings file.
        /// Supports JSON, XML, and plain text formats.
        /// </summary>
        /// <param name="path">The file path to the settings file.</param>
        /// <param name="settings">The <see cref="LocalSettings"/> object to populate with the loaded schedule.</param>
        /// <param name="_logger">An <see cref="ILogger{Worker}"/> instance for logging errors and warnings.</param>
        /// <returns><c>true</c> if the settings file was successfully loaded; otherwise, <c>false</c>.</returns>
        private static bool TryLoadSettingsFile(string path, out LocalSettings settings, ILogger<Worker> _logger)
        {
            if(File.Exists(path))
            {
                _logger.LogError("File does not exist");
                settings = new LocalSettings();
                return false;
            }
            try
            {
                settings = ParseJSON(path);
                return true;
            }
            catch (Exception e0)
            {
                _logger.LogWarning(new EventId(int.MinValue, $"WARN-{Utils.ID}"), e0, "File is not in JSON format. Switching to XML parsing ...");
                try
                {
                    settings = ParseXML(path);
                    return true;
                }
                catch
                {
                    _logger.LogWarning(new EventId(int.MinValue, $"WARN-{Utils.ID}"), e0, "File is not in XML format. Switching to plain text parsing ...");
                    try
                    {
                        settings = ParsePlainText(path);
                        return true;
                    }
                    catch (Exception e)
                    {
                        settings = new LocalSettings();
                        Console.Error.WriteLine("No arguments provided and settings file is missing/invalid");
                        Console.Error.WriteLine(e);
                        return false;
                    }
                }
            }
        }
        /// <summary>
        /// Parses a JSON file to extract the theme switching schedule.
        /// </summary>
        /// <param name="path">The file path to the JSON settings file.</param>
        /// <returns>A <see cref="LocalSettings"/> object populated with the schedule from the JSON file.</returns>
        /// <exception cref="JsonException">Thrown when the JSON file is invalid or cannot be deserialized.</exception>
        private static LocalSettings ParseJSON(string path)
        {
            return JsonSerializer.Deserialize<LocalSettings>(path);
        }
        /// <summary>
        /// Parses an XML file to extract the theme switching schedule.
        /// </summary>
        /// <param name="path">The file path to the XML settings file.</param>
        /// <returns>A <see cref="LocalSettings"/> object populated with the schedule from the XML file.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the XML file is invalid or cannot be deserialized.</exception>
        private static LocalSettings ParseXML(string path)
        {
            return (LocalSettings)new XmlSerializer(typeof(LocalSettings)).Deserialize(new StringReader(path));
        }
        /// <summary>
        /// Parses a plain text file to extract the theme switching schedule.
        /// </summary>
        /// <param name="path">The file path to the plain text settings file.</param>
        /// <returns>A <see cref="LocalSettings"/> object populated with the schedule from the plain text file.</returns>
        /// <exception cref="FormatException">Thrown when the plain text file does not contain exactly two valid time values.</exception>
        private static LocalSettings ParsePlainText(string path)
        {
            var txt = File.ReadAllText(path);
            var args = txt.Split(Utils.WHITESPACE, 2);
            if(args.Length != 2) throw new FormatException("arguments must be exactly 2");
                return new LocalSettings {
                    // start = TimeSpan.ParseExact(args[0], null, CultureInfo.InvariantCulture),
                    start = TimeSpan.ParseExact(args[0], "G", new CultureInfo("en-US")),
                    end = TimeSpan.ParseExact(args[1], "G", CultureInfo.InvariantCulture),
                };
        }
        /// <summary>
        /// Saves the user-defined theme switching schedule to a settings file in JSON format.
        /// </summary>
        /// <param name="settings">The <see cref="LocalSettings"/> object containing the schedule to be saved.</param>
        private static void SaveSettings(LocalSettings settings)
        {
                string json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SETTINGS_FILE_NAME, json);
        }
        /// <summary>
        /// Throws a <see cref="FormatException"/> with a detailed error message indicating invalid time format in arguments.
        /// </summary>
        /// <exception cref="FormatException">Always thrown to indicate invalid time format in arguments.</exception>
        private static void ThrowFormatEx() {
            throw new FormatException(
                new StringBuilder()
                .Append("Invalid time format in arguments.\n")
                .Append("Use --help to see usage hints")
                .ToString()
            );
        }
    }
    /// <summary>
    /// Represents the user-defined schedule for switching between light and dark themes.
    /// </summary>
    /// <remarks>
    /// Contains the start and end times for the dark mode period.
    /// </remarks>
    public struct LocalSettings
    {
        /// <summary>
        /// The start time for the dark mode period.
        /// </summary>
        public TimeSpan start;
        /// <summary>
        /// The end time for the dark mode period.
        /// </summary>
        public TimeSpan end;
    }
}

