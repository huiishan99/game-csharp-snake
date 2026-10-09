# Runtime screenshot provenance

These PNGs are unedited client-area desktop captures of the production Windows Forms app. They are not AI images, mockups, or screenshots of a web recreation.

- Captured: **2026-10-09 UTC**, GitHub-hosted `windows-latest`
- App source/build commit: [`57a8b99f495503ae554204af4755b7a98d1022b4`](https://github.com/huiishan99/game-csharp-snake/commit/57a8b99f495503ae554204af4755b7a98d1022b4)
- Passing Windows build and runtime run: [37881084539](https://github.com/huiishan99/game-csharp-snake/actions/runs/37881084539)
- Runtime artifact: `SnakeGame-Runtime-57a8b99f495503ae554204af4755b7a98d1022b4` (artifact ID `11594199086`)
- Release configuration: C# / Windows Forms / .NET Framework 4.7.2, Standard 44×30 board, **704×524** captured client area
- Method: `tools/WindowsSmoke.cs` loads `Form1` from the Release `SnakeGame.exe`, shows the actual window, and uses `Graphics.CopyFromScreen`. It reads state to route the snake but does not set snake positions, scores or food. Gameplay uses the production key and timer handlers. A live timer step and a normal standalone executable launch/close are checked separately.

| Repository file | Original artifact file | What it shows |
| --- | --- | --- |
| `menu.png` | `01-menu-classic.png` | First-launch Arcade setup in Classic theme, with all controls visible |
| `maze-menu.png` | `02-menu-neon-maze.png` | Maze setup, Neon theme, challenge `SHAN-VERSE` |
| `gameplay.png` | `03-gameplay-neon-maze.png` | Maze/Neon gameplay after earning 120 points by eating 12 food items |
| `handheld.png` | `06-menu-handheld.png` | Handheld-themed restart menu and locally saved Maze score |

The selected images above were visually inspected. The harness's initial separate-process capture (`00-standalone-menu.png`) was not selected: it captured before that window finished painting. The capture helper was subsequently hardened to wait for foreground ownership and a painted frame; this did not alter gameplay or the selected images.

## Verification results

The linked run passed the Release build, all **22 engine/rule checks**, **35 UI/runtime assertions**, and Windows package creation. See the preserved [runtime assertion output](smoke-results.txt).

Covered: standalone executable startup/shutdown, startup window dimensions, menu bounds, all five mode presets and three board sizes, countdown and live movement timers, real scoring/growth through arrow/WASD handlers, pause/resume, collision and restart, window-size locking/unlocking, seed replay, speed controls, and disk reload of settings, best score and leaderboard.

Visual review found and fixed a genuine first-launch issue: the designer's font scaling could shrink the window and clip the bottom of the menu. Applying the saved board dimensions during form initialization restores the intended size. Regression assertions now cover startup size and menu containment.

Not covered: human play feel, operating-system keyboard delivery, audible sound quality, other Windows DPI/display setups, long-running sessions, or the final full-board win through the UI (the engine checks cover that rule). The settings check reloads disk-backed settings and reconstructs the form in the harness process; it does not claim standalone cross-process preference persistence was tested.

## SHA-256

```text
0a2bb0c37d217198c56f73777888598dba0a54d5f71b0c69f0b837361f2aadb6  gameplay.png
2e275ece2a7a6ffd8bdcfee44935ef403b88d422d9c370d298c526efe2929279  handheld.png
9a997ece830f92d241cc78e9920d937063b84003de6f3a09ebc835fa9a82d1d1  maze-menu.png
e6667e00ba415ccad1fd39bc2a4a809317b3508fed7cce45ed7ca920d9e75ab8  menu.png
```
