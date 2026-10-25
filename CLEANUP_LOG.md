# Cleanup log

Record of what was removed from Samira's original project, what each item was, and how to get it back.

Everything listed here still exists in git history at commit `e295ff9` ("Add Samira's Unity project as-is"). To restore any item:

```bash
git checkout e295ff9 -- "<path>"
```

## Removed

### Unity / editor generated files
Recreated automatically by Unity or VS Code when the project is opened. Now listed in `.gitignore`.

| Item | What it was |
|---|---|
| `*.csproj` (36 files), `First Toio.sln` | C# project files Unity generates for code editors |
| `obj/` | Compiler intermediate output |
| `Logs/` | Unity editor logs from Samira's machine |
| `UserSettings/` | Per-user editor preferences (window layout etc.) |
| `mono_crash.0.0.json` | Crash dump from a past Unity editor crash (Mono runtime, arm64). No project value |
| `.DS_Store` | macOS Finder metadata |

### Old Mac app builds
Xcode projects exported by Unity's macOS build. Native desktop builds, unrelated to the web app. Their 123 MB data file was already excluded from git (over GitHub's 100 MB limit), so these builds were incomplete in the repo anyway.

| Item | What it was |
|---|---|
| `ToioMove/` | Xcode project `ToioMove.xcodeproj` + app bundle. Likely a build of the ToioMove demo (`Assets/ToioMoveDemo.cs`) |
| `Xcode_Toio/` | Xcode project `Xcode_Toio.xcodeproj` + app bundle. Same layout, a general test build |

## Removed in the second cleanup (2026-10-24)

Everything below is still in git history at commit `20d000d`. To restore any item:

```bash
git checkout 20d000d -- "<path>"
```

### GitHub Pages leftovers
Added for a GitHub Pages site that was later dropped (the project runs locally). Nothing used them.

| Item | What it was |
|---|---|
| `index.html` | Pages landing page linking to `SwarmControl/` and Samira's original build |
| `.nojekyll` | Empty file telling GitHub Pages not to run Jekyll |
| `SwarmControl/` | Stale web build of the Control app from 2026-10-23, before the routines existed. The local web build is now `SimPreview/` (made by `tools/run-simulator.sh`, not in git) |
| `WebBuild.Build()` in `Assets/Editor/WebBuild.cs` | The build command that wrote `SwarmControl/`. `WebBuild.BuildSimulator()` stays |

### Old WebGL builds (previously kept for reference)
About 380 MB. Each folder was a complete web app (`index.html`, `Build/`, `TemplateData/`). The source scenes and scripts are still in `Assets/`, so any of them can be rebuilt. The "likely source" column is inferred from names and dates, not confirmed.

| Folder | Built | Likely source in `Assets/` | Notes |
|---|---|---|---|
| `firstWeb/` | 2023-06-05 | early test scene | First WebGL test |
| `SampleCircling/` | 2023-06-05 | toio SDK `Samples/Sample_Circling` | SDK sample built for web |
| `TestUI/` | 2023-07-20 | UI test scene | Connection / UI experiment |
| `AppIt2/` | 2023-08-02 | `oneCubeApp.cs` | Single-robot app, iteration 2 |
| `SampleUITwoCubes/` | 2023-08-02 | toio SDK UI sample | Two-cube UI sample |
| `OneCubeAppIt1/` | 2023-08-16 | `oneCubeApp.cs`, `OneCubeApp.unity` | Single-robot fidget app. Live at samirapul.github.io/ToioOneCubeAppIt1 (referenced in the lab doc) |
| `SwarmFidgetWebApp/` | 2023-10-08 | `SingleCubeInteractions.cs` | First multi-robot fidget app (C1–C5 connect) |
| `SwarmFidgetWebAppColours/` | 2023-10-11 | `SingleCubeInteractions.cs` | Same with LED colours. Likely the one live as SwarmFidgetAppColoursDemo (lab doc) |
| `SwarmFidgetCircleInteractionsWebApp/` | 2023-10-23 | `circleInteractions.cs` | 6-robot circle interactions. Live as SwarmFidgetCircleInteractions (lab doc) |
| `SwarmFidgetWebAppColoursSmall/` | 2023-10-23 | `SingleCubeInteractions.cs` | Smaller-layout variant |
| `SwarmFidgetWebAppColoursSmall2/` | 2023-10-26 | `SingleCubeInteractions.cs` | Latest variant of the multi-robot app |
