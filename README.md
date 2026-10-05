# Aim Trainer Prototype (Godot 4 + C#)

A minimal, fully playable first-person aim trainer inspired by Aimlabs.
No external assets required - everything is built from Godot primitives.

## How to run

1. Install **Godot 4.x (.NET edition)** (the build with C# support).
2. Open Godot -> **Import** -> select this folder's `project.godot`.
3. Press **F5** (Play). The first launch builds the C# assembly automatically.
   If it complains about the assembly, run `dotnet build` once in this folder,
   or use Project -> Tools -> C# -> Build Solution in the editor.
4. The game starts directly into a 60-second **Flick Practice** round.

## Controls

| Key / Mouse      | Action                              |
|------------------|-------------------------------------|
| Mouse            | Aim (camera look)                   |
| Left Mouse       | Shoot (hitscan raycast)             |
| W A S D          | Move                                |
| ESC              | Release / re-capture mouse cursor   |
| R                | Restart round                       |

## Project structure

```
res://
  scenes/     Main.tscn (game), Target.tscn (shootable target)
  scripts/    GameSettings.cs        - all tunable defaults
              PlayerController.cs    - FPS camera, WASD, hitscan shooting
              AimTrainerManager.cs   - round flow, score/hits/misses/accuracy
              TargetSpawner.cs       - pooled targets, forward-arc spawn logic
              Target.cs              - Area3D bullseye target + material factory
              TrainingMode.cs        - mode abstraction + FlickPracticeMode
              HUD.cs                 - stats, timer, crosshair, results panel
              SoundGenerator.cs      - procedural placeholder shoot "pop"
  ui/         (reserved for future themes/menus)
  materials/  RoomTheme.tres (sky/environment)
  audio/      (drop your own shoot/hit WAV files here)
```

## Tuning

Every knob lives either as an `[Export]` on the matching node in the editor
(inspect `Player`, `TargetSpawner`, `AimTrainerManager`, `HUD`) or as a default
in `scripts/GameSettings.cs`:

* `MouseSensitivity` (PlayerController) - radians per mouse pixel
* `RoundDuration` (AimTrainerManager) - seconds
* `ScorePerHit` / `PenaltyPerMiss` (AimTrainerManager)
* `TargetRadius` (TargetSpawner) - target size
* `MinDistance` / `MaxDistance` (TargetSpawner) - spawn distance range
* `MaxActiveTargets` (TargetSpawner) - 1 = Flick Practice
* `MoveSpeed` (Target) - reserved, 0 = static targets
* Crosshair color/size (HUD exports)

## Adding future modes

1. Create a class extending `TrainingMode` (see `FlickPracticeMode`).
2. Override `OnRoundStart` / `OnUpdate` / `OnTargetHit` with the new rules.
3. In `AimTrainerManager._Ready()` swap which mode instance is assigned.

The spawner, HUD, scoring and input systems are already mode-agnostic.
