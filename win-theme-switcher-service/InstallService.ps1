
param(
    [string]$ServiceName = "WinThemeSwitcherService",
    [string]$AppArguments = '"18.30" "6.30"',
    [string]$StartupType = "Automatic"
)

# Build output directory
$publishDir = "$PSScriptRoot\bin\Release\netcoreapp3.1\win-x64\publish\*"
$description = "A service to switch Windows themes based on time of day."
# Define source and destination paths (MODIFY THESE PATHS)
$destinationDir = "${env:ProgramFiles(x86)}\WinThemeSwitcherService" # Update with your app name
$exePath = "$destinationDir\WinThemeSwitcherService.exe $AppArguments"

Write-Host "Arguments:- ServiceName: $ServiceName, AppArguments: $AppArguments, StartupType: $StartupType, PublishDirectory: $publishDir, Description: $description, DestinationDirectory: $destinationDir, ExecutablePath: $exePath"


# Require administrator privileges
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    Read-Host "Enter any key to exit ..."
    exit
}

# Check if source directory exists
if (-not (Test-Path $publishDir)) {
    Read-Host "Enter any key to exit ..."
    throw "Source directory not found: $publishDir"
}

# Ensure destination parent directory exists
# $parentDir = Split-Path -Path $destinationDir -Parent
$parentDir = $destinationDir
if (-not (Test-Path $parentDir)) {
    New-Item -Path $parentDir -ItemType Directory -Force | Out-Null
}

Read-Host "Enter any key to continue to move files ..."
# Move the directory (overwrite if destination exists)
Get-ChildItem -Path $publishDir -File | Move-Item -Destination $parentDir -Force -ErrorAction Stop

Read-Host "Enter any key to continue ..."
Write-Host "Successfully moved all files from '$publishDir' to '$destinationDir'"

Read-Host "Enter any key to continue service installation ..."
# Install the service (requires admin)
New-Service -Name $ServiceName -Description $description -BinaryPathName $exePath -DisplayName $ServiceName -StartupType $StartupType

Read-Host "Enter any key to start service ..."
# Start the service
Start-Service -Name $ServiceName
Read-Host "Enter any key to end ..."
