# Setting up on Windows

The scripts in `tools/` are written for macOS. On Windows, follow these steps by hand.
This guide is untested on Windows: exact menu names and paths may differ slightly.

## Just watching or demoing

Nothing to install. Open https://ninaxhqiu.github.io/swarmRobots_ER/ in Chrome. The
Simulated screen runs every routine, and Real robots pairs toio cubes over Web Bluetooth
(Chrome only, Bluetooth turned on).

## Editing routines

1. **Unity Hub:** download from https://unity.com/download (or `winget install Unity.UnityHub`).
2. **Licence:** open Unity Hub, sign in with a free Unity account, then
   Preferences > Licenses > Add > Get a free personal license.
3. **Unity 2021.3.26f1 with WebGL Build Support.** The project needs this exact version.
   Installs > Install Editor > Archive > download archive, find 2021.3.26f1, and tick
   **WebGL Build Support** when it asks for modules. About 8 GB.
4. **Open the project:** Unity Hub > Projects > Add > choose the `swarm` folder (the one with
   `Assets/`). The first open takes several minutes.
5. **Try routines:** press Play. It opens the simulator, and script changes in
   `Assets/Routines/` recompile in seconds. Every routine must work with 2 to 8 robots.

## Building the browser version (optional)

Only needed to see changes in Chrome before publishing. From the `swarm` folder, in PowerShell
(adjust the path if Unity was installed elsewhere):

```powershell
& "C:\Program Files\Unity\Hub\Editor\2021.3.26f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -executeMethod WebBuild.BuildSimulator -logFile build.log
cd SimPreview
python -m http.server 8002
```

Then open http://localhost:8002 in Chrome. If the build fails, search `build.log` for
`error CS`. Afterwards, `git checkout -- Assets/Unity.VisualScripting.Generated` drops files
Unity rewrites on every build.

## Publishing to GitHub Pages

`tools/publish-pages.sh` runs in Git Bash once its `UNITY=` line points at the Windows
`Unity.exe` above. It also needs the GitHub CLI (`gh`) signed in.
