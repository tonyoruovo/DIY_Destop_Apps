# Windows Theme Switcher Service

The **Windows Theme Switcher Service** is a background application that automatically switches the Windows theme between light and dark modes based on a user-defined schedule. It supports configuration via command-line arguments or a settings file and ensures it runs as a single instance while adding itself to Windows Startup.

## Features

- Automatically switches between light and dark themes based on a schedule.
- Supports configuration via:
  - Command-line arguments.
  - Settings file in JSON, XML, or plain text format.
- Runs as a single instance using a global mutex.
- Automatically adds itself to Windows Startup on the first run.

## Usage

### Command-Line Options

1. **Two Arguments (Start and End Time):**
   - Pass two time strings or date-time strings.
   - Example:
     ```bash
     WinThemeSwitcherService.exe "20:00" "07:00"
     WinThemeSwitcherService.exe "2025-04-22 20:00" "2025-04-23 07:00"
     ```

2. **Single Argument (Settings File Path):**
   - Pass a path to a settings file containing the schedule.
   - Supported formats: JSON, XML, or plain text.
   - Example:
     ```bash
     WinThemeSwitcherService.exe "C:\path\to\settings.txt"
     ```

### Settings File Format

- **XML:**
  ```xml
  <LocalSettings>
      <start>20:00</start>
      <end>07:00</end>
  </LocalSettings>
  ```
- **JSON:**
```json
{
    "start": "20:00",
    "end": "07:00"
}
```
- **Plain Text:**
```
20:00 <any-whitespace> 07:00
```
Help Option
To display help information:
```bash
WinThemeSwitcherService.exe --help
```
Notes
- Times can be in `HH:mm` or `yyyy-MM-dd HH:mm` format.
- If no arguments are passed, the program tries to read `settings.txt` from the Windows app data directory.
- Only one instance of the application can run at a time.

### Installation

1. Clone the repository:
    ```bash
    git clone https://github.com/your-repo/win-theme-switcher-service.git
    ```
2. Build the project using .NET Core:
    ```bash
    dotnet build
    ```
3. Run the application
    ```bash
    dotnet run
    ```

## Project Structure
- `Program.cs`: Entry point of the application. Handles initialization, single-instance enforcement, and startup registration.
- `WinThemeSwitcherService.cs`: Contains the `Worker` class, which implements the background service logic.
- `Utils.cs`: Utility functions for registry operations and other helper methods.
- `WinThemeSwitcherService.csproj`: Project file defining dependencies and target framework.

## Requirements
- .NET Core 3.1 or later.
- Windows operating system.

## Contributing
Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License
This project is licensed under the MIT License. See the `LICENSE` file for details. 
