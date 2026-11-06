# QuickStart

A tiny Windows startup launcher: after you sign in, it opens your selected
apps one by one with configurable delays, then exits and frees memory.

## Features

- Ordered list of programs with per-entry delay and "start minimized" option
- Enable or disable entries without deleting them
- Import and export of configuration
- Light and dark themes
- Dedicated autostart mode (`/autostart`)
- Runs once at login and closes itself — no background process
- Small single `.exe`, no installer

## Requirements

- Windows 10 or 11
- .NET Framework 4.6.2 (included with Windows 10/11)
- .NET SDK to build from source

## Build

```powershell
dotnet build -c Release
```

The output is `bin\Release\net462\QuickStart.exe`.

## Usage

1. Run `QuickStart.exe`.
2. Add programs, set the order and delays.
3. Enable autostart (the app is launched with the `/autostart` flag at login).
4. On the next sign-in QuickStart opens your apps and exits.

## Project structure

- `Program.cs` — entry point and launch logic
- `MainForm.cs` — main window
- `AppList.cs`, `StartupList.cs` — application and autostart lists
- `EditForm.cs`, `ImportForm.cs`, `Dialogs.cs` — dialogs
- `Theme.cs` — light/dark themes
- `IconUtil.cs` — icon extraction

## License

No license has been added yet.
