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
- Highly suitable for users who want to downgrade game versions for purposes such as data mining, asset management, or private server usage


## Requirements

- Install <i>[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)</i>


## Compile Instructions

To compile the project:

1. Just click `compile.bat`
2. The release output automatically will be in the `bin` folder


## How to Use

### Option 1: Interactive Menu (Recommended)

Just click <i>**Sophon.Downloader.exe**</i>  
You’ll be greeted with:

```
=== Sophon Downloader ===

[1] Full Download
[2] Update Download
[0] Exit
```

Navigate with number keys, follow the prompts, and you're good.  
It will auto-detect language options and available versions from your config.


### Option 2: CLI Usage (Advanced Users)

```cmd
Sophon.Downloader.exe full   <gameId> <package> <version> <outputDir> [options]
Sophon.Downloader.exe update <gameId> <package> <fromVer> <toVer> <outputDir> [options]
```

#### Example:

#### CMD
```bat
Sophon.Downloader.exe full hk4e game 6.5 Downloads\GenshinImpact_6.5.0
Sophon.Downloader.exe update hk4e zh-cn 6.4 6.5 Downloads\audio_zh-cn_6.4.0_6.5.0 --main --CNREL --threads=8 --handles=128
```

#### PowerShell
```powershell
./Sophon.Downloader.exe full hk4e game 6.5 Downloads\Yuanshen_6.5.0 --main --CNREL
./Sophon.Downloader.exe update hk4e game 6.5 6.6 Downloads\game_6.5.0_6.6.0 --predownload --CNREL
```


### CLI Options

| Option             | Description                                 |
|--------------------|---------------------------------------------|
| `--region=...`     | `OSREL` or `CNREL` (default: OSREL)         |
| `--branch=...`     | `main` or `predownload` (default: main)     |
| `--launcherId=...` | Launcher ID override                        |
| `--platApp=...`    | Platform App ID override                    |
| `--threads=...`    | Number of threads (auto-limited)            |
| `--handles=...`    | Max HTTP handles (default 128)              |
| `--silent`         | Disable all console output except errors    |
| `-h`, `--help`     | Show help info                              |


## config.json

This file is auto-generated if not found. You can customize the default region and add more versions.

Example:

```json
{
  "Region": "OSREL",
  "Branch": "main",
  "LauncherId": "VYTpXlbWo8",
  "PlatApp": "ddxf6vlr1reo",
  "Password": "bDL4JUHL625x",
  "Threads": 4,
  "MaxHttpHandle": 128,
  "Silent": false,
  "Versions": {
    "full": ["6.0", "6.1", "6.2", "6.3", "6.4", "6.5"],
    "update": [
      ["6.0", "6.1"],
      ["6.1", "6.2"],
      ["6.2", "6.3"],
      ["6.3", "6.4"],
      ["6.4", "6.5"]
    ]
  }
}

```

## Download Speed Optimization

Download speed depends on both the downloader configuration and the user's network quality.

If you experience slow speeds, try reducing parallel connections:

```json
"Threads": 2,
"MaxHttpHandle": 8
```

Lower values often provide more stable performance, especially on unstable or high-latency networks.

For detailed explanation and advanced tuning, see:  
👉 [Download Speed Optimization Guide](docs/download-speed-optimization.md)


## Notes

- Invalid config values automatically fallback to safe defaults.
- Incorrect types or excessive values are silently corrected automatically.
- Version/tag values are validated **live via the API**, not by regex.
- If your version doesn't exist, you'll get a clean `[ERROR] Failed to fetch manifest` — no crash.
- Maximum thread count = your CPU core count.


## Disclaimer

This tool is for reverse engineering & educational use only.  
Not affiliated with miHoYo, Cognosphere, or any official entity.  
Do not use this project for public distribution or commercial purposes.


## Credits

This project is based on [SophonDownloader](https://github.com/Escartem/SophonDownloader)  
This project also includes compiled libraries and assets derived from [Hi3Helper.Sophon](https://github.com/CollapseLauncher/Hi3Helper.Sophon)  
Major fixes, modifications, and continued development were carried out by [GesthosNetwork](https://github.com/GesthosNetwork)  
