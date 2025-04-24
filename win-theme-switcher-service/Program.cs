using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
// using System.Configuration.Install;

using WinThemeSwitcherService;
/// <summary>
/// The entry point for the Windows Theme Switcher Service application.
/// This class initializes the service, ensures it runs as a single instance, and adds it to Windows Startup.
/// </summary>
/// <remarks>
/// The service switches the Windows theme between light and dark modes based on a user-defined schedule.
/// It supports configuration via command-line arguments or a settings file.
/// </remarks>
class Program
{
    /// <summary>
    /// The name of the global mutex used to ensure that only one instance of the application runs at a time.
    /// </summary>
    private const string MUTEX_NAME = "Global\\ThemeSwitcherMutex";
    /// <summary>
    /// The registry key path used to add the application to Windows Startup.
    /// </summary>
    private const string REG_WIN_STARTUP_KEY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";
    static void Main(string[] args)
    {
        if(args.Length > 0 && (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase) || args[0].Equals("-h", StringComparison.OrdinalIgnoreCase)))
        {
            ShowHelp();
            return;
        }
        using var mutex = new Mutex(true, MUTEX_NAME, out bool createdNew);
        if (!createdNew)
        {
            Console.WriteLine("Another instance is already running!\n\nPress any key to exit");
            Console.Read();
            return;
        }

        try
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            EnsureAddedToStartup(exePath);
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n\nPress any key to exit");
            Console.Read();
        }


    }
    /// <summary>
    /// Configures and creates the host builder for the application.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the application.</param>
    /// <returns>An <see cref="IHostBuilder"/> configured with the application's services.</returns>
    static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureLogging((hostContext, logging) =>
            {
                // Clear default providers if needed
                logging.ClearProviders();

                // Add built-in providers
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventLog(settings =>
                {
                    settings.SourceName = Utils.ID;
                    settings.LogName = "Application";
                });
            })
            .ConfigureServices((hostContext, services) => services.AddHostedService<Worker>());
    }
    /// <summary>
    /// Ensures that the application is added to Windows Startup by creating or updating the necessary registry entry.
    /// </summary>
    /// <param name="exePath">The file path to the application's executable.</param>
    private static void EnsureAddedToStartup(string exePath/*, string[] args*/)
    {
        /*var services = ServiceController.GetServices();
        if(services != null && services.Length > 0)
        {
            var service = Array.Find(services, s => s.ServiceName.Equals(Utils.ID));
            if(service == null)
            {
                // Add this service to windows services
                if (Environment.UserInteractive)
                {

                    if (args.Length > 0)
                    {
                        switch (args[0])
                        {
                            case "-install":
                                {
                                    ManagedInstallerClasss.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                                    break;
                                }
                            case "-uninstall":
                                {
                                    ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                                    break;
                                }
                        }
                    }
                }
                else
                {
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[] { new MyService() };
                    ServiceBase.Run(ServicesToRun);
                }
            }
        }*/
        try
        {
            using var key = Utils.GetWritableRegistryKey(REG_WIN_STARTUP_KEY_PATH);
            Utils.SetRegistryValueIfNull(key, exePath, exePath);
            if(key == null) {
                var dir = Directory.CreateDirectory(Utils.BASE_DIR);
                dir.Create();
            }
        }
        catch (Exception ex)
        {
            Utils.fileLogger.LogSync($"Could not add/read {exePath} to startup. See below", LogLevel.Error);
            Utils.fileLogger.LogSync(ex, LogLevel.Error);
            Console.Read();
        }
    }
    /// <summary>
    /// Displays help information about the application's usage, options, and modes.
    /// </summary>
    static void ShowHelp()
    {
        Console.WriteLine(@"
        Windows-Theme-Switcher-Service Help

        Usage:
        WinThemeSwitcherService.exe [start-time] [end-time]
        WinThemeSwitcherService.exe [settings-file-path]

        Description:
        This background service automatically switches Windows light/dark mode based on the specified times or settings.

        Options:
        --help, -h           Show this help information.

        Modes:
        1. Two arguments (Start and End time):
            Pass two time strings or date-time strings.
            Example:
            WinThemeSwitcherService.exe ""20:00"" ""07:00""
            WinThemeSwitcherService.exe ""2025-04-22 20:00"" ""2025-04-23 07:00""

        2. Single argument (Settings file path):
            Pass a path to a settings file containing the times.
            This file may be in XML, JSON or plaintext format
            Example:
            WinThemeSwitcherService.exe ""C:\path\to\settings.txt""
            
            Format of settings file:

            (XML)
            <LocalSettings>
                <start>20:00</start>
                <end>7:00</end>
            </LocalSettings>
            
            (JSON)
            {
                ""start"": ""20:00"",
                ""end"": ""07:00""
            }

            (plaintext)
            20:00 <any-whitespace> 07:00

        Notes:
        - Times can be in ""HH:mm"" or ""yyyy-MM-dd HH:mm"" format.
        - If no arguments are passed, the program tries to read ""settings.txt"" in the windows app data directory.
        - The service auto-adds itself to Windows Startup on first run.
        - Only one instance can run at a time.
    ");
    }
}