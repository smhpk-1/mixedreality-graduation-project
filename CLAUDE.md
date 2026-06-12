# The Shift — Mixed Reality Graduation Project

VR narrative experience for **Meta Quest 3S**, built in **Unity 6 (6000.2.10f1)** with the **XR Interaction Toolkit** (OpenXR, Android target, hand tracking + controllers). Theme: maladaptive daydreaming and escape from capitalist monotony.

## Scenes (all in build settings, at `Assets/` root — NOT `Assets/Scenes/`)

1. **`Assets/scene1.unity` — The Shift (factory)**: Player sorts 30 red/blue cubes from a conveyor belt into color-matched bins. After the 30th cube, green Anomaly Cubes appear; touching one transitions to Scene 2. Key scripts: `GameManager`, `ConveyorBelt`, `BinCollector`, `ObjectSpawner`, `AnomalyCube`. Generative score: `FactoryMusicDirector` (self-bootstrapping, Eno-style — ostinato + incommensurate tape loops sliced at runtime from `Resources/scene_1_sound_design` machine recordings; bin sorts play quantized pentatonic notes).
2. **`Assets/Scene 2.unity` — The Colorful Playground**: 20 grabbable colored cubes; throwing them at walls repaints the walls (`ColorReactiveWall`), each color has a synth tone. 20 total hits across all walls collapse the room (`DestructibleWall`) → transition to Scene 3. `StreetAmbienceDirector` self-bootstraps, repairs street-source playback, and fades in procedural city ambience after the collapse.
3. **`Assets/Scene 3.unity` — Metro station**: Subway train arrives/departs with sliding doors (`SubwayTrainController`, has a `staticMode`). NPCs wander the platform (`NPCScene3Wanderer`) and board/exit the train via **waypoint chains, not NavMesh** (`NPCTrainPassenger`, `TrainPassengerDirector`) — NavMesh is only used while wandering. Cyclic train loop: NPCs reappear at original positions. Also trash-cleanup gameplay (`TrashItem`, `GrabbableTrash`, `TrashCart`) and a transition cube to Scene 4. The cart generates an interior trigger and accepts only previously grabbed trash after it has been released. Generative score: `MetroMusicDirector` (self-bootstrapping, Eno-style — per-object ostinatos locked to one DSP grid in A minor pentatonic: bench tape-loop pads with incommensurate periods, trash-can tick patterns, fluorescent hum drones, rail ostinato, NPC hum/murmur/whisper voices, tunnel rumble; PA speakers play TTS announcements from `Resources/scene_3_sound_design` hooked to train events; trash collection plays quantized melody notes). NPC grounding on Quest: `NPCBlobShadow` (self-bootstrapping blob shadows — scene has no realtime shadows, so NPCs look airborne in VR without them; material at `Resources/NPCBlobShadow.mat`) + `Scene3LightProbeTool` editor menu (Tools > Scene 3) to place a probe grid (needs a lighting rebake). **Physics**: the Subway Vol2 pack has NO colliders in any prefab; `Scene3PhysicsBootstrap` (self-bootstrapping) adds box colliders to walls/floors/furniture, carving `NavMeshObstacle`s (so NPCs can't path through walls), and normalizes trash items (grab+rigidbody on root only, full colliders, `TrashGrabVRConfig` settings — fixes cap-only grabs, in-hand jitter, mid-air freeze on release). NPC LODGroups are forced to LOD0 by the wanderer (the pack's low-LOD meshes are mis-bound and Android's lodBias 0.7 showed them constantly).
4. **`Assets/Scene 4.unity` — Concert / The Loop**: Stem-based hybrid audio — some stems 2D stereo, some 3D spatial from scene objects, all DSP-clock synced (`ConcertAudioDirector`). Generated drum kits / instruments / stage speakers. Radial sequencer clock finale, performing band, and fourteen-person responsive audience are implemented in scripts/editor tools but still need to be installed, saved, and verified in the serialized scene.

## Code layout

- **`Assets/Scripts/`** — 69 runtime scripts (~16k total project C# lines including editor tools). Comments and tooltips are mostly **Turkish**; match that style when editing.
- **`Assets/Scripts/Editor/`** — editor tooling: `Scene3SetupTool`, `Scene4SetupTool`, `TrainDoorBuilder`, `CleanMissingScripts`, `FixTrashItemsForVR`, `TrashCleanup`, etc. Scenes are built heavily via procedural/editor generators rather than hand-placed assets.

## Docs

- `README.md`, `PROJECT_DOCUMENTATION.md`, `PROJECT_STORY.md`, `STORYBOARD.md`, `CONCEPTUAL_BACKGROUND.md` — complete final four-scene knowledge base.
- `SENIOR_PROJECT_REPORT.md` — authoritative MUS 442 final paper.
- `GRADUATION_PROJECT_REPORT.md` — concise final submission dossier.
- `MUS442_SUBMISSION_CHECKLIST.md` — final integration and delivery checklist.

## Notes

- Built APK `theshift1.apk` and IL2CPP symbols folder sit in the repo root.
- Remote: https://github.com/smhpk-1/mixedreality-graduation-project
- Recent work (June 2026): Scene 3 NPC train boarding fixes, cyclic train loop, VR diagnostics (`NPCDiagnostics`, `VRDebugLogger`).
