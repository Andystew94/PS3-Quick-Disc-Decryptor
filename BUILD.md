# Building PS3 Quick Disc Decryptor

## Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or later
- Verify installation: `dotnet --version`

## Build

### Using Build Scripts

**PowerShell:**
```powershell
.\build.ps1                      # Build Release
.\build.ps1 -Configuration Debug # Build Debug
.\build.ps1 -Clean               # Clean and rebuild
.\build.ps1 -Publish             # Build and publish
```

**Command Prompt:**
```cmd
build.bat
```

### Using .NET CLI

```bash
cd "Source/PS3 Quick Disc Decryptor"
dotnet build --configuration Release
```

## Output

**Build output:** `Source/PS3 Quick Disc Decryptor/bin/Release/net6.0-windows/`

**Publish output:** `Publish/Release/` (when using `-Publish`)

## Troubleshooting

**"dotnet command not found"**
- Install .NET 6.0 SDK and restart your terminal

**"The target framework 'net6.0-windows' requires..."**
- Install .NET 6.0 SDK: https://dotnet.microsoft.com/download/dotnet/6.0

**NuGet package restore failures**
```bash
dotnet restore
dotnet nuget locals all --clear
```

## Runtime Requirements

End users need:
- [.NET 6.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Microsoft Visual C++ 2010 Runtime (x86)](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist) (for PS3Dec.exe)
- [7-Zip](https://www.7-zip.org/) (optional, for ISO extraction)

## Development

**Visual Studio 2022:**
1. Open `Source/PS3 Quick Disc Decryptor.sln`
2. Select Release or Debug configuration
3. Press F5 to run, or Ctrl+Shift+B to build

**Visual Studio Code:** Install C# and .NET Core extensions
