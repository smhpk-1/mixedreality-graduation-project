# The Shift - Project Documentation

## 1. Document Purpose

This is the authoritative technical knowledge base for the final MUS 442 version of **The Shift**. It describes the implemented four-scene experience, the systems added during the second semester, the current integration state, and the remaining presentation-readiness checks.

## 2. Product Definition

### 2.1 Vision

Create a sound-centered VR narrative in which the player experiences a progression from imposed labor to apparent creative freedom, then recognizes how repetition and measured time return inside the fantasy.

### 2.2 Target

| Area | Current configuration |
|---|---|
| Device | Meta Quest 3S |
| Platform | Android, ARM64 |
| Engine | Unity 6 `6000.2.10f1` |
| Render pipeline | Universal Render Pipeline `17.2.0` |
| XR runtime | OpenXR `1.15.1` |
| Interaction | XR Interaction Toolkit `3.2.2` |
| Hand support | XR Hands `1.7.0` |
| Input | Unity Input System `1.14.2` |
| Navigation | AI Navigation `2.0.12` |
| Source language | C# |
| Version control | Git and Git LFS |

### 2.3 Final Experience

The build contains four root-level Unity scenes in this order:

1. `Assets/scene1.unity`
2. `Assets/Scene 2.unity`
3. `Assets/Scene 3.unity`
4. `Assets/Scene 4.unity`

The intended complete flow is:

`factory quota -> anomaly -> playground collapse -> metro cleanup/transition -> concert sequencer -> clock reveal -> second shift`

## 3. Design Principles

### 3.1 Reuse the Gesture, Change the Meaning

Grabbing and moving simple objects remains understandable across the entire project, but its meaning develops:

- sort for the system;
- throw for play;
- collect as voluntary labor;
- place to compose;
- discover that composition has become time.

### 3.2 Make Sound Functional

Audio communicates state, rewards interaction, connects objects, and carries the narrative. Each scene has a distinct musical logic, but repeated pitch and timing ideas connect the scenes.

### 3.3 Build for Standalone VR

The project favors simple meshes, baked or non-realtime lighting, pooled audio sources, transform-driven NPC animation, procedural tools, and targeted device fixes over expensive realtime effects.

## 4. Scene Systems

## 4.1 Scene 1 - The Shift

### Player Flow

1. `ObjectSpawner` creates red and blue cubes.
2. `ConveyorBelt` moves them toward the player.
3. `BinCollector` checks whether a cube matches the bin.
4. `ObjectSpawner` counts thirty spawned standard cubes.
5. Green `AnomalyCube` objects appear after the thirtieth standard cube is spawned, independent of the correct-sort score.
6. Grabbing an anomaly loads Scene 2.

The serialized scene also contains an active `PurpleTransitionSceneCube` that can load Scene 2 immediately. It currently functions as a shortcut and bypasses the intended work phase.

### Main Runtime Scripts

- `GameManager`
- `ObjectSpawner`
- `ConveyorBelt`
- `BinCollector`
- `BinGenerator`
- `DispenserGenerator`
- `FactoryFloorGenerator`
- `MachineGenerator`
- `OfficeMessGenerator`
- `FactoryScoreBoard`
- `WallClock`
- `AnomalyCube`
- `FactoryMusicDirector`

### Factory Music System

`FactoryMusicDirector` bootstraps automatically when `scene1` loads. It:

- loads recordings from `Resources/scene_1_sound_design`;
- slices machine recordings into percussion, steam, and tonal tape material;
- schedules a 76 BPM, sixteen-step ostinato on the Unity DSP clock;
- retriggers tape layers at deliberately incommensurate periods;
- spatializes layers from conveyor and machine objects;
- creates quantized confirmation notes for correct sorting;
- creates a distinct quantized buzz for incorrect sorting;
- raises the melodic register as the player approaches the quota;
- supports a `SecondShift` state that adds 6 BPM and slight detuning after the finale.

## 4.2 Scene 2 - The Colorful Playground

### Player Flow

1. The player enters a room containing twenty colored cubes.
2. `PlaygroundCube` plays a color-specific synth loop while held.
3. Throwing a cube into a reactive wall changes the wall's color.
4. `DestructibleWall` counts hits across all walls.
5. After twenty total wall hits, the room collapses and Scene 3 loads.

The serialized scene also contains an active `TransitionCube_ToScene3` that bypasses the collapse sequence.

### Main Runtime Scripts

- `Scene2RoomGenerator`
- `Scene2TwentyColoredCubesGenerator`
- `PlaygroundCube`
- `ColorReactiveWall`
- `DestructibleWall`
- `SceneTransitionCube`
- `SaxophoneGenerator`
- `AmbientAudioSource`
- `StreetAmbienceDirector`

### Audio and Interaction Details

- Five color recordings are loaded from `Resources/scene_2_sound_design`.
- Cube loop loudness is normalized by measuring RMS and applying a bounded gain correction.
- Releasing a cube fades its held loop instead of stopping abruptly.
- Wall-response tones fade separately from the held loop.
- Respawning cubes do not restart their audio unexpectedly.
- Wall damage is shared across the room rather than tracked per wall.
- `StreetAmbienceDirector` self-bootstraps, changes short cat and voice recordings from unnatural loops into intermittent events, keeps the street saxophone performing, and fills previously silent shop and lamp sources with procedural sound.
- When the room ceiling deactivates during collapse, a city-rumble bed fades in and procedural car passes begin, carrying the player from the enclosed playground toward the metro.

## 4.3 Scene 3 - The Platform

### Player Flow

The scene combines two systems that operate at the same time:

- an autonomous metro and commuter loop;
- a player-driven trash cleanup task.

Collecting twenty trash items with the cart loads Scene 4. A grabbable transition cube also provides a direct alternate route.

### Train and Passenger Architecture

`SubwayTrainController` controls:

- arrival from a start point;
- stopping at the platform;
- sliding door animation;
- a minimum open-door duration;
- optional waiting until all passengers have boarded;
- departure and return for another cycle;
- a `staticMode` for a stationary train.

`NPCScene3Wanderer` handles normal platform wandering with NavMesh. When boarding begins, `NPCTrainPassenger`:

1. uses NavMesh to approach the first authored waypoint;
2. disables NavMesh;
3. follows a waypoint chain through the door and into the train;
4. parents itself to the train during travel;
5. exits through another waypoint chain or disappears with the train;
6. returns to its original position for the next cycle.

`TrainPassengerDirector` coordinates passenger groups, boarding points, train waiting, exit behavior, and cyclic reset.

### Trash Interaction

`Scene3PhysicsBootstrap`, `TrashGrabVRConfig`, `TrashItem`, `GrabbableTrash`, and `TrashGrabbedMarker` normalize found trash assets into usable VR objects.

`TrashCart`:

- follows the player only after a distance threshold is crossed;
- stays at ground height and avoids responding to head rotation;
- creates an interior basket trigger from the cart's rendered bounds at runtime;
- accepts only trash that has previously been grabbed, is no longer held, and has been released for at least 0.15 seconds;
- uses continuous dynamic collision detection on normalized trash objects to reduce missed fast throws;
- counts collected items;
- loads Scene 4 after twenty accepted items.

### Metro Music System

`MetroMusicDirector` bootstraps automatically when Scene 3 loads. It uses a shared 58 BPM DSP grid in A minor pentatonic and treats the station as an ensemble:

- rails play a low ostinato;
- benches trigger dyads at incommensurate periods;
- fluorescent lights produce chord drones;
- trash cans play sparse tick patterns;
- NPCs hum, murmur, or whisper from their moving bodies;
- tunnel ends produce a rumble bed;
- train events trigger brake hiss and PA announcements;
- trash collection creates a quantized rising melody and completion arpeggio.

### Quest-Specific Scene 3 Support

- `NPCBlobShadow` creates inexpensive grounding shadows because realtime shadows are not used.
- `Scene3LightProbeTool` places a probe grid for baked lighting.
- `NPCDiagnostics` and `VRDebugLogger` support device-only debugging.
- `Scene3PhysicsBootstrap` repairs missing physics and grab configuration at runtime.

## 4.4 Scene 4 - The Concert / The Loop

### Current Serialized Scene

`Assets/Scene 4.unity` currently contains the concert environment and `ConcertAudioDirector`. The radial sequencer, performing band, and audience are implemented in scripts and editor tools but are not yet serialized into the scene on `main`.

### Concert Audio

`ConcertAudioDirector`:

- creates one runtime `AudioSource` per stem;
- supports both headset-centered 2D stereo and object-sourced 3D spatial modes;
- starts every stem on one DSP-clock timestamp;
- exposes a duck target used by the finale;
- exposes a performance level that can drive band animation;
- fades all stems during the clock reveal.

### Radial Sequencer Finale

The final system is implemented by:

- `RadialSequencer`
- `SequencerSampleOrb`
- `SequencerFinaleDirector`
- `Editor/Scene4SequencerSetupTool`

The sequencer:

- has twelve slots arranged as a disguised clock face;
- previews each orb's synthesized tone while held;
- snaps released orbs into nearby empty slots;
- provides placement haptics and slot highlights;
- schedules tones and the rotating playhead on the DSP clock;
- reduces concert level by one twelfth per filled slot;
- waits for two full completed loops;
- slows to one step per second;
- replaces tones with ticking;
- reveals numbers, clock hands, and a 9:00 shift-start time;
- blackens the view and reloads Scene 1.

### Performing Band

The band system is implemented by:

- `NPCMusicianPerformer`
- `Editor/Scene4BandSetupTool`

Band roles include drummer, guitarist, bassist, and keyboardist. Their bone-driven performance intensity follows `ConcertAudioDirector.PerformanceLevel`, so the musicians slow and stop as the player's sequence replaces the concert.

### Audience

The audience system is implemented by:

- `NPCAudienceMember`
- `Editor/Scene4AudienceSetupTool`

The setup tool places fourteen varied audience NPCs between the player and stage. They dance independently during the concert, turn toward the player's sequencer as it replaces the band, and freeze when the clock reveal takes over.

### Scene 4 Integration Requirement

Before the final build:

1. Run `Tools > Scene 4 > Add Radial Sequencer`.
2. Run `Tools > Scene 4 > Place Band NPCs`.
3. Run `Tools > Scene 4 > Place Audience NPCs`.
4. Save `Assets/Scene 4.unity`.
5. Verify the complete finale in the Editor and on Quest.

## 5. Architecture

### 5.1 Runtime and Editor Separation

Runtime behavior lives in `Assets/Scripts/`. Scene-construction and repair utilities live in `Assets/Scripts/Editor/`.

At the final documentation pass on June 12, 2026, the current `main` workspace contains:

- 69 runtime C# scripts;
- 14 editor C# scripts;
- approximately 16,000 lines of project C# code.

Every current C# script has a committed Unity `.meta` file so its GUID remains stable across machines.

### 5.2 Self-Bootstrapping Systems

The following systems install themselves after the relevant scene loads:

- `FactoryMusicDirector`
- `StreetAmbienceDirector`
- `MetroMusicDirector`
- `NPCBlobShadow`
- `Scene3PhysicsBootstrap`

This reduces manual scene setup and repairs scene-wide behavior at runtime.

### 5.3 Editor Tooling

Important editor utilities include:

- `Scene3SetupTool`
- `Scene3LightProbeTool`
- `TrainDoorBuilder`
- `FixTrashItemsForVR`
- `TrashCleanup`
- `Scene4SetupTool`
- `Scene4SequencerSetupTool`
- `Scene4BandSetupTool`
- `Scene4AudienceSetupTool`
- `CleanMissingScripts`

The project uses these tools to convert imported assets and large scenes into project-specific interactive systems.

### 5.4 Scene Transitions

| From | Trigger | Destination |
|---|---|---|
| Scene 1 | Grab anomaly cube after thirty standard cubes spawn | Scene 2 |
| Scene 1 | Grab active direct-transition cube | Scene 2 |
| Scene 2 | Reach twenty total wall hits and complete collapse | Scene 3 |
| Scene 2 | Grab active direct-transition cube | Scene 3 |
| Scene 3 | Collect twenty trash items | Scene 4 |
| Scene 3 | Grab transition cube | Scene 4 |
| Scene 4 | Complete sequencer and clock reveal | Scene 1 |

## 6. Second-Semester Development

### March 2026 - Extending Beyond the Playground

- Added destructible reactive walls and room-collapse progression.
- Added Scene 3 to build settings.
- Began changing the project from a two-scene contrast into a multi-scene arc.

### April 2026 - Replacing Scene 3 and Establishing Scene 4

- Replaced the previous Scene 3 direction with a metro station.
- Added metro entrance/city generation and Scene 3 VR setup.
- Added train start, stop, exit, and sliding-door systems.
- Added Scene 4 environment setup, stage speakers, instruments, and concert audio.

### May 2026 - Making the Metro Alive

- Added NPC wandering.
- Added passenger boarding and exiting.
- Replaced unreliable boarding navigation with authored waypoint chains.
- Added train waiting and passenger cycle resets.
- Added trash cleanup interaction and Scene 3-to-Scene 4 progression.

### June 2026 - Sound, Device Reliability, and Finale

- Added cyclic commuter behavior and VR diagnostics.
- Added the generative factory and metro music systems.
- Added Quest-specific NPC shadows, physics repair, LOD fixes, and rig corrections.
- Added Scene 2 audio normalization and shared wall-hit collapse.
- Added Scene 2 street-audio repair and a procedural city transition after the room collapses.
- Added the radial sequencer clock finale and second-shift return.
- Added band performance behavior linked to concert ducking.
- Added a fourteen-person audience that turns toward the player's loop and freezes at the clock reveal.
- Improved trash deposits with a generated cart-interior trigger, release-state checks, and continuous collision detection.

## 7. Current Status

### Implemented

- Four scenes enabled in build settings
- Complete narrative logic from Scene 1 through Scene 3
- Factory interaction and generative score
- Playground color/sound interaction and collapse
- Metro train, doors, commuters, cleanup, transition, and generative score
- Concert stem system
- Scene 4 sequencer finale code and setup tool
- Scene 4 performing band code and setup tool
- Scene 4 audience code and setup tool
- Quest-targeted repair and diagnostic systems
- APK build artifacts in the repository root

### Required Before Final Presentation

- Serialize and verify the Scene 4 sequencer, band, and audience in `Scene 4.unity`.
- Decide whether the active Scene 1 and Scene 2 direct-transition cubes should be removed, disabled, or intentionally kept as presentation shortcuts.
- Decide whether Scene 1 progression should remain spawn-count based or be tied to the correct-sort score to match the narrative idea of a quota.
- Run the complete experience on Meta Quest 3S after the final Scene 4 setup.
- Complete a cross-scene loudness and mix pass.
- Improve the Scene 4 drum sound.
- Confirm NPC behavior remains reliable throughout a long device session.
- Record the required final documentation/presentation material.

## 8. Known Limitations

- Scene 4's newest systems are tool-installed rather than present in the saved scene on `main`.
- Active direct-transition cubes currently allow Scene 1 and Scene 2 gameplay to be skipped.
- Scene 1 anomaly timing is based on spawned cubes rather than completed correct sorts.
- The project has not documented a formal user study.
- Imported assets and generated scene content increase repository and build size.
- NPC behavior depends on scene-specific waypoints and can require visual verification after scene edits.
- Standalone VR behavior can differ from Editor behavior, especially for physics, LOD, lighting, and XR rig transforms.
- The final audio master pass remains open.

## 9. References and Resources

- Unity documentation: <https://docs.unity3d.com/>
- XR Interaction Toolkit: <https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit>
- Meta Quest developer documentation: <https://developers.meta.com/horizon/>
- Project repository: <https://github.com/smhpk-1/mixedreality-graduation-project>
