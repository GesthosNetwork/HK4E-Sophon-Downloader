## HK4E Sophon Downloader

A tool to download anime game assets using their new Sophon-based download system.

Starting from version `5.6`, they transitioned to using **Sophon Chunks** for updates and discontinued distributing ZIP files.
As a result, it is no longer possible to download game assets **without using their Launcher**.
This tool aims to bypass that limitation, so you can download directly, efficiently, and without bloat.


## Features

- Full and Update download modes
- Uses official API (`getBuild`, `getUrl`, etc.)
- Language/region selector
- Built-in auto validation via real-time API
- Fast, parallel downloads (multi-threaded)
- Zero dependencies


## Requirements

- Install [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)


## Compile Instructions

To compile the project:

1. Just click `compile.bat`
2. The release output automatically will be in the `bin` folder


## How to Use

### Option 1: Interactive Menu (Recommended)

Just click Sophon.Downloader.exe
You’ll be greeted with:

```
=== Sophon Downloader ===

[1] Full Download
[2] Update Download
[0] Exit
```

Navigate with number keys, follow the prompts, and you're good.  
It will auto-detect language options and available versions from your config.


### Option 2: CLI Mode (Advanced Users)

```cmd
Sophon.Downloader.exe full   <gameId> <package> <version> <outputDir> [options]
Sophon.Downloader.exe update <gameId> <package> <fromVer> <toVer> <outputDir> [options]
```

#### Example:

```cmd
Sophon.Downloader.exe full gopR6Cufr3 game 5.7 Downloads
Sophon.Downloader.exe update gopR6Cufr3 en-us 5.6 5.7 Downloads --threads=4 --handles=128
```


### CLI Options

| Option           | Description                                 |
|------------------|---------------------------------------------|
| `--region=...`   | `OSREL` or `CNREL` (default: OSREL)         |
| `--branch=...`   | Branch override (default: main)             |
| `--launcherId=...` | Launcher ID override                      |
| `--platApp=...`  | Platform App ID override                    |
| `--threads=...`  | Number of threads (auto-limited)            |
| `--handles=...`  | Max HTTP handles (default 128)              |
| `--silent`       | Disable all console output except errors    |
| `-h`, `--help`   | Show help info                              |

> If your input is garbage, it will fall back to defaults silently.  
> You were warned.


## config.json

This file is auto-generated if not found. You can customize the default region and add more versions.

Example:

```json
{
  "Region": "OSREL",
  "Branch": "main",
  "LauncherId": "VYTpXlbWo8",
  "PlatApp": "ddxf6vlr1reo",
  "Threads": 4,
  "MaxHttpHandle": 128,
  "Silent": false,
  "Versions": {
    "full": ["5.6", "5.7", "5.8"],
    "update": [
      ["5.5", "5.6"],
      ["5.5", "5.7"],
      ["5.6", "5.7"],
      ["5.6", "5.8"],
      ["5.7", "5.8"]
    ]
  }
}
```


## Notes

- If you mess up the config, the app will silently fallback to default values.
- Garbage values like `"Silent": lmao` or `"Threads": 99999` **Silently fixed automatically.**
- Version/tag values are validated **live via the API**, not by regex.
  If your version doesn't exist, you'll get a clean `[ERROR] Failed to fetch manifest` — no crash.
- Maximum thread count = your CPU core count.


## Disclaimer

This tool is for reverse engineering & educational use only.  
Not affiliated with miHoYo, Cognosphere, or any official entity.  
Do not use this project for public distribution or commercial purposes.
