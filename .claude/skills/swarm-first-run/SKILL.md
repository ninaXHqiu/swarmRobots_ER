---
name: swarm-first-run
description: First-time setup for the swarm toio project - installs Unity, gets the licence activated, builds the routine simulator and opens it at localhost. Use when someone has just pulled or cloned this repo, asks how to set up or run the project, or wants to see the simulation / run routines.
---

# Swarm project: first run

Goal: take someone from a fresh clone to watching routines in the simulator at
http://localhost:8002, with as little effort on their side as possible. macOS only
(the scripts use Homebrew). Run commands from the project root (the folder with `Assets/`).

## Steps

1. **Check prerequisites.** macOS, Homebrew (`brew --version`), Google Chrome, about 12 GB
   free disk. If Homebrew is missing, point them to https://brew.sh and stop until it is installed.

2. **Install Unity** (skip if `~/Unity/Editors/2021.3.26f1/Unity.app` already exists):
   run `tools/first-time-setup.sh` in the background - it downloads ~8 GB and can take
   15-30 minutes. Tell them it is running and that nothing is needed from them meanwhile.

3. **Licence - the one step they must do themselves.** Unity will not run without it, and you
   cannot sign in for them. Ask them to open Unity Hub, sign in with a Unity account (free),
   then Preferences > Licenses > Add > Get a free personal license. Wait for them to confirm.

4. **Build and open the simulator:** run `tools/run-simulator.sh` in a terminal tab they can
   see (it keeps a local server running until Ctrl-C). The first build takes ~10 minutes;
   later runs only rebuild when scripts changed. It opens http://localhost:8002 in Chrome.
   If it fails, the script prints the compile errors or the licence message - fix and rerun.

5. **Explain the two screens:**
   - **Simulator** (opens first): simulated robots on the 3D mat. **Robots - / +** sets 2 to 8
     robots; one button per routine (the running one turns blue); **Stop** ends it.
   - **Real robots** (button at the top) opens the Control screen: turn toio cubes on, press
     **+ Add robot** once per robot (each opens Chrome's pairing popup), then **Connect**. Its
     Routines card runs the same routines on the real robots; Samira's interactions (magnet,
     circle, remote...) are there too. **Simulator** (top right) goes back.

## Day-to-day after the first run

- Routines live in `Assets/Routines/`, one C# file each. To make one, copy
  `StudyFlowExample.cs`, rename the class and `Name`, and change the steps. Shared helpers are
  in `SwarmRoutine.cs` (`Swarm.MoveTo`, `Swarm.Follow`, `Swarm.Wait`, ...) and `Shapes.cs`.
  Every routine must work with 2 to 8 robots.
- Fastest loop for editing routines: open the project in Unity Hub (Add > this folder) and
  press Play - it always opens the simulator, and script changes recompile in seconds.
  `tools/run-simulator.sh` is for the browser version (it rebuilds when scripts changed).
- Real robots in the browser need Chrome (Web Bluetooth). Background/hidden tabs pause the
  simulation - keep the tab visible.

## Removing Unity later

```bash
brew uninstall --cask unity-hub
rm -rf ~/Unity ~/Library/Unity ~/Library/Application\ Support/Unity* ~/Library/Application\ Support/UnityHub ~/Library/Caches/com.unity3d.* ~/Library/Preferences/com.unity3d.* ~/Library/Logs/Unity
```
The project's `Library/` folder (~1.3 GB, rebuilt automatically) can also be deleted.
