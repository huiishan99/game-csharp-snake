# Windows Testing Guide

Use this checklist when validating the game on Windows before sharing a build.

## Visual Studio

1. Install Visual Studio 2022 with the `.NET desktop development` workload.
2. Open `SnakeGame.sln`.
3. Set `SnakeGame` as the startup project.
4. Build the solution in `Release` mode.
5. Run `SnakeGame.Tests` once to verify the rule checks.
6. Start `SnakeGame` and complete the smoke test below.

## VS Code Or Terminal

VS Code can run the project if Windows has MSBuild for .NET Framework installed. The easiest route is Visual Studio 2022 Build Tools or full Visual Studio 2022.

Open a Developer PowerShell or Developer Command Prompt in the repository root:

```powershell
msbuild SnakeGame.sln /p:Configuration=Release /m
.\SnakeGame.Tests\bin\Release\SnakeGame.Tests.exe
.\bin\Release\SnakeGame.exe
```

If `msbuild` is not found, open the terminal from Visual Studio's Developer PowerShell entry or install the Visual Studio Build Tools.

## GitHub Actions

The `Windows Build` workflow builds the Release solution, runs `SnakeGame.Tests`, writes a build summary, and uploads a versioned zip artifact.

1. Push the branch to GitHub.
2. Open the repository's `Actions` tab.
3. Choose `Windows Build`.
4. Use `Run workflow` for a manual build, or wait for the push build.
5. Open the finished run and download the `SnakeGame-Windows-Release-<version>-<sha>` artifact.
6. Unzip it and check `BUILD_INFO.txt` before running the executable.

## Smoke Test

1. Start a run with default settings.
2. Confirm the start countdown appears before the snake moves.
3. Move with arrow keys and `WASD`.
4. Pause and resume with `Space`, then confirm the resume countdown appears.
5. Restart from the game-over overlay with `Enter`.
6. Change menu speed with `-`, `+`, Left, and Right.
7. Choose each `Mode` preset and confirm speed, wall, progressive, and obstacle options update.
8. Choose each `Board` preset and confirm the window resizes before the run starts.
9. Choose each `Theme` preset and confirm the board, HUD, snake, food, and menu controls recolor.
10. Type a `Challenge` seed, restart with the same seed, and confirm obstacle/food placement is repeatable.
11. Finish a scoring run and confirm the mode leaderboard updates on the start panel.
12. Turn off `Wrap walls` and confirm the board shows a red border and wall collision ends the run.
13. Turn on `Progressive speed` and confirm the HUD shows a `+` next to speed.
14. Turn on `Obstacles` and confirm blocked cells appear and collision ends the run.
15. Toggle `Sound` and confirm start, eat, pause/resume, and finish sounds respect the setting.
16. Start a run and confirm the window cannot be resized until the game returns to the restart panel.
17. Confirm the start panel covers all controls at the minimum window size and normal Windows scaling.
18. Confirm the start panel preview snake animates smoothly and the eat feedback is visible without feeling distracting.
19. Close and reopen the app to confirm speed, mode, board, theme, challenge, leaderboard, sound, and best-score settings are restored.


## Automated Windows UI smoke and screenshots

CI also compiles `tools/WindowsSmoke.cs` as a separate verification executable next to the Release app. It first launches `SnakeGame.exe` normally, verifies its window and clean shutdown, and captures its menu. It then loads the production `Form1` from that assembly to check presets, board sizes, live countdown/movement timers, scored gameplay, pause/resume, collisions, seed replay, and disk-backed settings reload. The harness uses its existing key and timer handlers for deterministic gameplay; it does not inject scores, snake positions or artwork.

The `SnakeGame-Runtime-<sha>` artifact contains actual `CopyFromScreen` PNGs, `smoke-results.txt`, and source/run metadata. Review the image pixels as well as the automated result: a successful build alone does not prove that controls fit or that a capture shows the expected window.

To run the same smoke check on an interactive Windows desktop, after building Release:

```powershell
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
& $csc /nologo /target:exe /out:bin\Release\WindowsSmoke.exe /reference:bin\Release\SnakeGame.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Configuration.dll tools\WindowsSmoke.cs
.\bin\Release\WindowsSmoke.exe artifacts\runtime
```

Use a clean Windows test profile: the harness assumes first-launch defaults for the standalone app and writes sample scores/preferences for its integration checks. Do not run it against personal settings you want to retain. It needs a visible unlocked desktop for screenshots. The manual smoke checklist above still covers real keyboard delivery, audio, play feel, window interactions and different Windows scaling configurations.
