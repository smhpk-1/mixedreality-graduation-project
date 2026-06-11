## 1.2 Scene 2: The Colorful Playground

**Narrative:**
Scene 2 transports the player to a surreal, open playground featuring 20 large, colorful cubes scattered across the floor. The cubes come in 5 vibrant colors (Red, Blue, Green, Yellow, Purple) with 4 cubes of each color. Players can freely grab and throw these cubes at the reactive walls surrounding the room. When a cube hits a wall, the wall instantly changes to match the cube's color, creating a dynamic, ever-changing environment. This scene represents freedom, creativity, and the boundless imagination of daydreaming—a stark contrast to the oppressive factory of Scene 1.

**Key Scripts:**
- Scene2TwentyColoredCubesGenerator.cs
- Scene2RoomGenerator.cs
- ColorReactiveWall.cs
- PlaygroundCube.cs
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
- **Labor as melody:** Each correctly sorted cube plays a machine-derived note quantized to the next 16th of the grid, walking a minor pentatonic scale; the register rises as the 30-cube quota progresses, building musically toward the anomaly. Red and blue cubes speak a fifth apart. Wrong sorts trigger a dissonant (minor-second) but rhythmically quantized cluster.
- **The anomaly:** When the GameManager leaves WorkState, the ostinato (the rhythm of labor) fades out over ~6 seconds while the tape loops (the dream) swell — the machine stops, the daydream remains.

The system (`FactoryMusicDirector`) bootstraps itself when scene1 loads; no scene setup is required. Raw machine ambience loops in the scene are automatically ducked so the composition reads clearly.

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
- **Asset Organization:** Structured `Assets/` folder with subfolders for Materials, Prefabs, Scenes, Scripts, and Resources.

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

---

## 4. Current Status
- **Scene:** Core environment and interaction systems are functional in Editor.
- **Build:** Android build pipeline is configured and tested on Quest 3S.
- **Next Steps:**
  - Expand procedural content and interaction variety.
  - Further optimize for performance and user experience.
  - Prepare for user testing and feedback collection.

---

## 5. Folder Structure Overview
- **Assets/**: Main project assets (Materials, Prefabs, Scenes, Scripts, etc.)
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
