---
name: project-overview
description: High-level summary of The Shift VR project — scenes, tech stack, repo structure
metadata:
  type: project
---

**The Shift** — VR narrative experience for Meta Quest 3S (Unity 6 / 6000.2.10f1, XR Interaction Toolkit, OpenXR, Android target, hand tracking + controllers). Theme: maladaptive daydreaming and escape from capitalist monotony. Graduation project.

4 scenes, all at `Assets/` root (NOT `Assets/Scenes/`):
- **scene1.unity** — Factory: sort 30 red/blue cubes from conveyor belt → Anomaly Cube triggers Scene 2. `GameManager`, `ConveyorBelt`, `BinCollector`, `ObjectSpawner`, `AnomalyCube`. Generative Eno-style score: `FactoryMusicDirector`.
- **Scene 2.unity** — Playground: 20 grabbable colored cubes; throw at walls → repaint + sound; 10 hits collapses wall → Scene 3. `ColorReactiveWall`, `DestructibleWall`.
- **Scene 3.unity** — Metro station: subway train, NPCs wandering/boarding via waypoint chains (NOT NavMesh for boarding), trash cleanup gameplay, transition to Scene 4. Key: `SubwayTrainController`, `NPCScene3Wanderer`, `NPCTrainPassenger`, `TrainPassengerDirector`, `MetroMusicDirector`, `NPCBlobShadow` (blob shadows — no realtime shadows on Quest). Editor tool: `Scene3LightProbeTool` (Tools > Scene 3).
- **Scene 4.unity** — Concert: stem-based hybrid audio, `ConcertAudioDirector`.

**Code**: `Assets/Scripts/` (~73 .cs files). Comments/tooltips in Turkish — match that style.
**Editor tools**: `Assets/Scripts/Editor/` — Scene3SetupTool, Scene4SetupTool, TrainDoorBuilder, CleanMissingScripts, FixTrashItemsForVR, etc. Scenes built heavily via procedural/editor generators.
**APK**: `theshift1.apk` + IL2CPP symbols in repo root.
**Remote**: https://github.com/smhpk-1/mixedreality-graduation-project

**Why:** Graduation project — narrative art piece, not a game product.
**How to apply:** Prioritize Quest performance (no realtime shadows, blob shadows for NPCs, LOD awareness). Code comments go in Turkish.
