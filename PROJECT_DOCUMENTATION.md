# Mixed Reality Graduation Project - VR Project Documentation

## 1. Project Overview (PRD)

### **Vision**
This project is a mixed reality (MR) experience designed for the Meta Quest 3S, focusing on immersive interaction and spatial computing. The user is placed in a dynamic virtual environment where they can interact with objects and systems using hand tracking and controllers, leveraging the latest XR technologies. The project aims to explore advanced user interaction paradigms, procedural content generation, and real-time feedback in a VR/MR context.

### **Target Platform**
- **Device:** Meta Quest 3S
- **OS:** Android
- **Input:** Hand Tracking / Controllers (XR Interaction Toolkit)

### **Key Features**
1. **Immersive Environment:**
   - A detailed, interactive virtual space designed for exploration and interaction.
   - Realistic lighting, materials, and spatial audio for enhanced immersion.

2. **XR Interaction System:**
   - Full support for hand tracking and controller-based input using Unity's XR Interaction Toolkit.
   - Intuitive grab, throw, and manipulation mechanics for virtual objects.

3. **Procedural Content Generation:**
   - Runtime generation of objects, environments, or challenges to ensure replayability and variety.

4. **Physics-Based Interactions:**
   - Realistic physics for object movement, collisions, and environmental responses.

5. **Custom Editor Tools:**
   - In-editor utilities for rapid scene and asset generation, supporting fast iteration and prototyping.

6. **Performance Optimization:**
   - Efficient asset management, culling, and batching for smooth performance on standalone VR hardware.

---

## 1.1 Scene 1: The Shift

**Narrative:**
"The Shift" is the opening scene, set in a dark, oppressive factory that symbolizes capitalist monotony and alienation. The player, in the role of a worker, awakens at a conveyor belt and must sort red and blue cubes into matching bins. The endless ticking of a wall clock and reports on the walls reinforce the sense of routine and isolation. After 30 red and blue cubes are sorted, three glowing green "Anomaly Cubes" appear, breaking the cycle and triggering a transition to a surreal new world.

**Key Scripts:**
- GameManager.cs
- ObjectSpawner.cs
- ConveyorBelt.cs
- BinCollector.cs
- BinGenerator.cs
- DispenserGenerator.cs
- FactoryScoreBoard.cs
- OfficeMessGenerator.cs
- FactoryFloorGenerator.cs
- WallClock.cs
- RuntimeAtmosphereController.cs
- SimpleRoomGenerator.cs
- LightingOptimizer.cs
- CubeCollisionSound.cs
- CubeGrabAudio.cs
- AnomalyCube.cs
- AnomalyMovement.cs
- FactoryMusicDirector.cs

**Sound Design — "Studio as a Compositional Tool":**
Scene 1's score is generated at runtime from the factory's own machine recordings (`Resources/scene_1_sound_design`), following Brian Eno's studio-as-instrument philosophy:

- **Ostinato:** A percussive transient is sliced from the machine recordings and looped on a sample-accurate DSP grid (76 BPM, 16-step pattern) — the factory's mechanical heartbeat (root, low fifth, octave accents, sparse offbeat ticks).
- **Tape loops (Music for Airports technique):** Long tonal slices of the machine recordings are pitched to a drone chord and re-triggered at deliberately incommensurate periods (21.3s / 26.7s / 33.1s / 39.9s), so the layers never realign — an ever-shifting ambient texture.
- **Labor as melody:** Each correctly sorted cube plays a procedurally generated harmonic chime quantized to the next 16th of the grid, walking a minor pentatonic scale; the register rises as the 30-cube quota progresses, building musically toward the anomaly. Red and blue cubes speak a fifth apart. Wrong sorts trigger a harsh detuned-sawtooth double-buzz — confirmation and error are unmistakably distinct.
- **The anomaly:** When the GameManager leaves WorkState, the ostinato (the rhythm of labor) fades out over ~6 seconds while the tape loops (the dream) swell — the machine stops, the daydream remains.
- **The second shift:** If the player has completed the full loop (Scene 4's sequencer finale), the factory score returns subtly degraded — +6 BPM and quarter-tone detuned tape loops. The loop is the same; the loop is never the same.

The system (`FactoryMusicDirector`) bootstraps itself when scene1 loads; no scene setup is required. Raw machine ambience loops in the scene are automatically ducked so the composition reads clearly.

---

## 1.2 Scene 2: The Colorful Playground

**Narrative:**
Scene 2 transports the player to a surreal, open playground featuring 20 large, colorful cubes scattered across the floor. The cubes come in 5 vibrant colors (Red, Blue, Green, Yellow, Purple) with 4 cubes of each color. Players can freely grab and throw these cubes at the reactive walls surrounding the room. When a cube hits a wall, the wall instantly changes to match the cube's color, creating a dynamic, ever-changing environment. This scene represents freedom, creativity, and the boundless imagination of daydreaming—a stark contrast to the oppressive factory of Scene 1.

After 20 cube hits in total across all walls, the room collapses (walls, ceiling, and floor sink away, and the cubes dissolve with them), revealing a city street at night. The player walks through this street — past shop windows, a saxophone-playing street musician, talking passersby, and a stray cat — to the metro entrance that leads to Scene 3.

**Key Scripts:**
- Scene2TwentyColoredCubesGenerator.cs
- Scene2RoomGenerator.cs
- ColorReactiveWall.cs
- DestructibleWall.cs
- PlaygroundCube.cs
- MetroEntranceCityGenerator.cs
- PositionalAudioSource.cs
- StreetAmbienceDirector.cs

**Sound Design:**
- Each cube color has its own synth voice; the loop plays while held and fades out smoothly on release. Clip loudness is RMS-normalized at load so all colors sit at the same level.
- On a wall hit, the wall "absorbs" the cube's voice: the tone plays from the impact point with velocity-based pitch and decays away — the wall takes the cube's color and its sound.
- Street phase (`StreetAmbienceDirector`, self-bootstrapping): recorded one-shots (cat, talking people) play at randomized intervals instead of looping; the saxophone street musician loops as a performance; empty sources receive procedural content (shop radio, street-lamp buzz); a distant city rumble bed fades in when the room collapses, with occasional procedural car passes.

---

## 1.3 Scene 3: The Platform (Metro Station)

**Narrative:**
An underground metro platform — the daydream's quiet middle distance. A subway train arrives, opens its doors, exchanges NPC passengers, and departs in an endless cycle; the NPCs reappear at their original positions each loop. Litter is scattered across the platform, and a player-following cleaning cart lets the player voluntarily perform janitorial labor inside their own fantasy. Collecting 20 pieces of trash transitions to Scene 4. A glowing anomalous cube offers the transition as well.

**Key Systems:**
- **Train cycle:** `SubwayTrainController` (sliding doors, `staticMode` option), `TrainPassengerDirector`, `NPCTrainPassenger` — boarding/exiting via **waypoint chains, not NavMesh** (NavMesh is only used while wandering on the platform).
- **NPC wandering:** `NPCScene3Wanderer` — NavMesh wandering with procedural bone-driven walk animation (no Animator Controllers), head-look behavior, platform-edge protection.
- **NPC grounding:** `NPCGrounding` (single authoritative floor query — initial snap, NavMeshAgent baseOffset management, waypoint fine-grounding), `NPCBlobShadow` (self-bootstrapping blob shadows; the scene has no realtime shadows, so NPCs would look airborne in VR without them), `Scene3LightProbeTool` (editor menu, Tools > Scene 3 — places a light-probe grid; requires a lighting rebake).
- **Trash gameplay:** `TrashItem`, `GrabbableTrash`, `TrashCart` (player-following cart with an interior-volume deposit trigger; counts only thrown-in trash), `TrashGrabbedMarker`.
- **Diagnostics:** `NPCDiagnostics`, `VRDebugLogger` (on-device debugging).

**Key Scripts:**
- SubwayTrainController.cs
- TrainPassengerDirector.cs
- NPCTrainPassenger.cs
- NPCScene3Wanderer.cs
- NPCGrounding.cs
- NPCBlobShadow.cs
- TrashCart.cs / TrashItem.cs / GrabbableTrash.cs / TrashGrabbedMarker.cs
- MetroMusicDirector.cs
- Scene3ToScene4Cube.cs

**Sound Design — Generative Metro Score (`MetroMusicDirector`, self-bootstrapping):**
Per-object ostinatos locked to one DSP grid in A minor pentatonic: bench tape-loop pads with incommensurate periods, trash-can tick patterns, fluorescent hum drones, a rail ostinato, NPC hum/murmur/whisper voices, and tunnel rumble. PA speakers play TTS announcements (`Resources/scene_3_sound_design`) hooked to train events. Each collected trash item plays a quantized melody note via the cart's `OnTrashCollected` event.

---

## 1.4 Scene 4: The Concert / The Loop

**Narrative:**
An open-air concert at night — the daydream's climax and its trap. The player arrives inside the spectacle: stage, speaker stacks, a live band of NPC musicians, a dancing crowd. Near the stage floats a radial sequencer — a luminous ring with twelve slots and a sweeping playhead — and twelve glowing sample orbs. As the player fills the slots, their loop gradually replaces the concert; when the loop completes, the sequencer reveals itself as the Scene 1 factory clock (twelve slots — it was always a clock), the playhead becomes the red second hand, frozen hands appear at 9:00, every placed tone collapses into a mechanical tick, and the player is returned to Scene 1.

**Key Systems:**
- **Hybrid stem audio:** `ConcertAudioDirector` — some stems 2D stereo in the headset, some 3D spatial from stage objects, all DSP-clock synchronized (`PlayScheduled`). Supports progressive ducking (`SetDuckTarget`) and finale fade (`FadeOutAll`); exposes `PerformanceLevel` for visual systems.
- **Radial sequencer:** `RadialSequencer` (self-building 12-slot ring, DSP-scheduled steps, pooled AudioSources, procedural timbres — no audio assets), `SequencerSampleOrb` (grabbable floating orbs, snap-to-slot, preview hum while held), `SequencerFinaleDirector` (the clock reveal, VR-safe blackout, return to scene1).
- **NPC band:** `NPCMusicianPerformer` — bone-driven playing animation per role (drummer, guitarist, bassist, keyboardist); motion intensity follows `PerformanceLevel`, so the band winds down and freezes as the player's loop replaces the music.
- **NPC audience:** `NPCAudienceMember` — randomized per-person crowd dancing (bounce, sway, arms-up); turns toward the sequencer as it takes over; freezes at the clock reveal.
- **Stage generators:** `DrumKitGenerator`, `InstrumentGenerator`, `SaxophoneGenerator`, `StageSpeakerGenerator`.

**Editor Tools (Tools menu):**
- `Scene4SetupTool` — Tools > Setup Scene 4 for VR (XR Origin + Interaction Manager)
- `Scene4SequencerSetupTool` — Tools > Scene 4 > Add Radial Sequencer
- `Scene4BandSetupTool` — Tools > Scene 4 > Place Band NPCs
- `Scene4AudienceSetupTool` — Tools > Scene 4 > Place Audience NPCs

**Key Scripts:**
- ConcertAudioDirector.cs
- RadialSequencer.cs / SequencerSampleOrb.cs / SequencerFinaleDirector.cs
- NPCMusicianPerformer.cs / NPCAudienceMember.cs
- DrumKitGenerator.cs / InstrumentGenerator.cs / SaxophoneGenerator.cs / StageSpeakerGenerator.cs

---

## 2. Technical Specifications

### **Engine & Tools**
- **Unity Version:** Unity 6 (6000.2.10f1)
- **Render Pipeline:** Built-in Render Pipeline (Standard Shaders)
- **Scripting:** C#
- **Version Control:** Git (GitHub)

### **Key Packages**
- **XR Plugin Management:** OpenXR (Meta Quest Support)
- **XR Interaction Toolkit:** For VR rig and interactions.
- **Input System:** New Unity Input System (replacing the old Input Manager).

### **Architecture**
- **Editor Tools:** Custom Editor scripts in `Editor/` for procedural scene and asset generation.
- **Runtime Scripts:** Modular scripts for object spawning, interaction logic, and physics.
- **Self-bootstrapping audio directors:** `FactoryMusicDirector` (Scene 1), `StreetAmbienceDirector` (Scene 2), `MetroMusicDirector` (Scene 3) spawn themselves on scene load — no scene setup required.
- **Asset Organization:** Scenes live at the `Assets/` root (`scene1.unity`, `Scene 2.unity`, `Scene 3.unity`, `Scene 4.unity` — NOT in `Assets/Scenes/`); scripts in `Assets/Scripts/` with editor tooling in `Assets/Scripts/Editor/`.

---

## 3. Development Log & Steps Taken

### **Phase 1: Project Setup**
1. **Repository Initialization:**
   - Created a new Unity project and initialized a Git repository.
   - Set up `.gitignore` to exclude build and cache folders.
2. **XR Configuration:**
   - Installed OpenXR and XR Interaction Toolkit.
   - Configured XR Origin and camera rig for Quest 3S compatibility.

### **Phase 2: Environment & Tools**
3. **Scene Generation:**
   - Developed procedural scene generation scripts for rapid prototyping.
   - Implemented custom lighting and material assignment for visual consistency.
4. **Editor Utilities:**
   - Created Editor scripts for batch asset creation and placement.

### **Phase 3: Core Mechanics**
5. **Interaction System:**
   - Integrated hand tracking and controller input.
   - Developed grab, throw, and manipulation mechanics for virtual objects.
6. **Procedural Spawning:**
   - Implemented runtime object spawning with randomization and constraints.
7. **Physics Integration:**
   - Applied Unity physics for realistic object behavior and collision handling.

### **Phase 4: Polish & Optimization**
8. **Performance Tuning:**
   - Profiled and optimized scripts and assets for Quest 3S performance.
   - Reduced draw calls and optimized material usage.
9. **Visual & Audio Polish:**
   - Enhanced lighting, post-processing, and spatial audio for immersion.

### **Phase 5: Deployment & Testing**
10. **Build Configuration:**
    - Switched build target to Android.
    - Configured input system and build settings for Quest 3S.
11. **Testing:**
    - Deployed APK to device for real-world testing.
    - Collected feedback and iterated on interaction and performance.

### **Phase 6: Narrative Completion (June 2026)**
12. **Scene 3 stabilization:**
    - NPC train boarding via waypoint chains, cyclic train loop, VR diagnostics.
    - Unified NPC grounding (`NPCGrounding`) — initial floor snap, NavMeshAgent baseOffset management, waypoint fine-grounding.
    - Trash counting moved strictly to cart deposit (interior trigger, release-age guard, continuous collision).
13. **Scene 4 finale:**
    - Radial sequencer ("a clock in disguise"), NPC band and audience, concert ducking, the clock reveal, and the return to Scene 1 — closing the narrative loop.
14. **Audio completeness:**
    - Distinct confirm/error tones in Scene 1; cube audio normalization and wall-response tones in Scene 2; street ambience fixes and enrichment on the walk to the metro.

---

## 4. Current Status
- **Scenes:** All four scenes are implemented and connected in a full narrative loop (factory → playground → metro → concert → factory).
- **Build:** Android build pipeline is configured and tested on Quest 3S (`theshift1.apk` in repo root).
- **Next Steps:**
  - On-device verification of the Scene 4 finale pacing and the NPC grounding fix.
  - Run the Scene 4 placement tools (sequencer, band, audience) and rebuild lighting/probes where needed.
  - Final user testing and report polish.

---

## 5. Folder Structure Overview
- **Assets/**: Main project assets — scenes at root, plus Materials, Prefabs, Resources, Scripts (with `Scripts/Editor/` tooling), `npc_casual_set_00/` character set
- **Library/**: Unity-generated cache and build data (excluded from version control)
- **Packages/**: Unity package manifest and lock files
- **ProjectSettings/**: Unity project settings
- **UserSettings/**: User-specific settings (not shared)

---

## 6. References & Resources
- Unity Documentation: https://docs.unity3d.com/
- XR Interaction Toolkit: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit
- Meta Quest Developer: https://developer.oculus.com/

---

*This documentation will be updated as the project progresses. Please refer to this file for the latest technical and design information.*
