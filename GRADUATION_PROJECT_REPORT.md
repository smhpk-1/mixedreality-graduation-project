# Senior Project Report: Mixed Reality Experience
## Exploring Maladaptive Daydreaming and Capitalist Alienation through VR

**Author:** Semiha PAKSOY  
**Course:** MUS441 - Senior Project I-II  
**Academic Year:** 2025-2026  
**Date:** January 1, 2026

---

## 1. Abstract

This senior project presents a Mixed Reality (MR) experience designed for the Meta Quest 3S, exploring the complex psychological phenomenon of **maladaptive daydreaming** and its relationship to **capitalist alienation**. The project leverages the immersive capabilities of Virtual Reality (VR) to create a bifurcated narrative experience that physically transports the user from a monotonous, oppressive industrial setting to a boundless, surreal playground. By juxtaposing these two distinct environments—"The Shift" and "The Colorful Playground"—the project critiques the rigidity of modern labor systems while celebrating the liberating, albeit isolating, power of human imagination. This report details the conceptual framework, technical implementation, creative process, and the specific design decisions made to translate abstract psychological theories into a tangible, interactive reality.

---

## 2. Introduction

### 2.1 Purpose & Vision
The primary purpose of this project is to utilize Virtual Reality as a medium to simulate and explore the internal experience of maladaptive daydreaming. Unlike traditional media, VR offers a sense of *presence*, allowing users to inhabit the perspective of a worker caught between the crushing weight of external societal constraints and the vivid allure of internal creative freedom.

The vision is to create a dynamic virtual environment where users interact with objects and systems using advanced hand tracking and controllers. The project aims to explore advanced user interaction paradigms, procedural content generation, and real-time feedback in a VR/MR context.

### 2.2 Motivation
Modern capitalist societies often foster feelings of alienation and monotony, particularly within industrial or repetitive work environments. For many individuals, daydreaming serves not just as a distraction, but as a critical coping mechanism—a form of psychological resistance to reclaim agency in a world that demands conformity. This project seeks to visualize this psychological escape, using VR technology to blur the lines between "reality" (the factory) and "fantasy" (the playground), making the internal struggle external and visible.

### 2.3 Problem Statement
How can immersive technology be used to represent abstract psychological concepts like alienation, dissociation, and escapism? Traditional storytelling can describe these states, but VR can induce them. This project addresses this challenge by creating a spatial narrative that physically transports the user from a restrictive, rule-bound environment to one of limitless possibility, forcing them to experience the jarring transition between these two states of being.

---

## 3. Conceptual & Theoretical Framework

### 3.1 Maladaptive Daydreaming
**Maladaptive daydreaming** is a psychological concept describing extensive fantasy activity that replaces human interaction and/or interferes with academic, interpersonal, or vocational functioning. Individuals experiencing maladaptive daydreaming often become deeply absorbed in vivid, elaborate inner worlds, sometimes as a coping mechanism for stress, trauma, or dissatisfaction with reality.

*   **Key Characteristics:** Intense, immersive, narrative-driven daydreams; difficulty controlling the urge to daydream; use of daydreaming as an escape from real-world problems.
*   **Relevance to VR:** VR serves as a technological extension of the mind’s capacity for immersive fantasy, providing a controlled, interactive space for users to explore alternate realities. The project leverages this parallel, intentionally blurring the line between healthy escapism and maladaptive detachment.

### 3.2 Capitalism, Alienation, and Escapism
Drawing from Marxist theory, the project explores the concept of **alienation**—the detachment of workers from the products of their labor, from the act of production, and from their own humanity.
*   **The Factory as Metaphor:** The industrial setting represents the capitalist machine. The user is reduced to a function (sorting cubes), governed by rigid quotas and the symbolic presence of time. Time does not operate as a gameplay mechanic, but as an atmospheric tool that reinforces pressure, surveillance, and monotony. The labor is repetitive, meaningless, and isolating.
*   **Escapism as Resistance:** The transition to the surreal world represents the mind’s attempt to escape rigid and repetitive systems. Instead of continuing a meaningless task, the user enters a space where actions are not judged by productivity, but by freedom and play.

### 3.3 Surrealism and the Subconscious
The project adopts a surrealist aesthetic to represent the inner world. This aesthetic aims to create a dreamlike and fluid experience that partially departs from the rigid logic of the real world. By deliberately stretching physical and visual expectations—through color-reactive walls, anomalous objects, and unconventional interactions—the experience reflects the fluid and ever-changing nature of the subconscious mind.

---

## 4. Project Narrative & Design

The experience is structured into two contrasting scenes, representing the duality of the protagonist's mind.

### 4.1 Scene 1: The Shift (The Reality)

**Narrative Overview**  
"The Shift" is an industrial prologue representing the monotony and alienation of the capitalist system. The player is cast as a "worker," isolated from the outside world in a dim, oppressive factory where time is tracked only by a mechanical wall clock.

**Mechanics & Interaction**
*   **The Routine:** Red and blue cubes are spawned from wall-mounted dispensers and move along a conveyor belt. The user must grab and sort them into matching bins using the Meta Quest 3S controllers.
*   **The Quota:** The user is tasked with sorting 30 cubes as part of the repetitive routine. After this phase, glowing green anomaly cubes appear. Grabbing one of these anomaly cubes triggers the transition to the second scene.

**Symbolism**
*   **Industrial Monotony:** The dark, oppressive factory represents capitalist alienation.
*   **Routine & Alienation:** The act of sorting mirrors the lack of agency in industrial labor.
*   **The Anomaly:** The glowing green cubes represent a disruption of the established system. In an environment built around red and blue cubes and their corresponding bins, the appearance of green cubes that cannot be placed into any bin reveals the system’s limitations and suggests the possibility of stepping outside its rules.

**Key Scripts**
*   `GameManager.cs`, `ObjectSpawner.cs`, `ConveyorBelt.cs`, `BinCollector.cs`, `DispenserGenerator.cs`, `FactoryScoreBoard.cs`, `WallClock.cs`, `AnomalyCube.cs`.

### 4.2 Scene 2: The Colorful Playground (The Escape)

**Narrative Overview**  
After leaving the factory environment, the player enters The Colorful Playground—a surreal space that reflects the logic of maladaptive daydreaming. This scene presents a recontextualization of familiar objects. The same cube-based interactions are reworked with new meanings and functions: cubes become colorful, playful, and capable of producing sound, allowing the user to engage with them through exploration and play rather than labor.

**Mechanics & Interaction**
*   **Creative Freedom:** The room contains 20 colorful cubes (Red, Blue, Green, Yellow, Purple) scattered across the floor.
*   **Reactive Environment:** Throwing a cube at a wall triggers the `ColorReactiveWall` script, painting the wall with that color. This allows the user to reshape their environment dynamically.
*   **Physics & Play:** Unlike the rigid sorting mechanics of Scene 1, this scene allows objects to be freely thrown and to react to surfaces through collision. There are no quotas, rules, or predefined goals guiding the interaction.

**Symbolism**
*   **20 Colorful Cubes:** Represent fragments of creativity suppressed by the factory.
*   **Reactive Walls:** Represent the malleability of reality within the imagination.
*   **Throwing vs. Sorting:** Throwing represents creative expression and release, contrasting with the careful sorting of Scene 1.
*   **No Quotas:** The absence of objectives critiques the productivity-obsessed nature of capitalism.

**Thematic Contrast**

| Scene 1: The Shift | Scene 2: The Playground |
|-------------------|------------------------|
| Dark, oppressive | Bright, liberating |
| Rules and quotas | Freedom and creativity |
| Sorting (conformity) | Throwing (expression) |
| Fixed environment | Reactive, changeable |
| Monotone colors | Vibrant spectrum |

**Key Scripts**
*   `Scene2RoomGenerator.cs`, `Scene2TwentyColoredCubesGenerator.cs`, `ColorReactiveWall.cs`, `PlaygroundCube.cs`.

---

### 4.3 Scene 3: The Platform (The Quiet Decay)

**Narrative Overview**  
The third scene relocates the daydream to an underground metro platform — a deliberately ordinary, liminal space. A subway train arrives, exchanges NPC passengers, and departs in an endless cycle, with the same passengers reappearing at their original positions each loop. Litter is scattered across the platform, and a cleaning cart follows the player. The fantasy has begun to reproduce the very structures the player escaped: waiting, routine, and voluntary labor.

**Mechanics & Interaction**
*   **The Train Cycle:** `SubwayTrainController.cs` runs the arrival/departure loop with sliding doors; `TrainPassengerDirector.cs` and `NPCTrainPassenger.cs` choreograph NPC boarding and exiting via waypoint chains.
*   **Ambient Life:** `NPCScene3Wanderer.cs` drives commuters wandering the platform with procedural, bone-driven walk animation (no Animator Controllers), grounded by a unified floor-snapping system (`NPCGrounding.cs`).
*   **Trash Cleanup:** The player may collect 20 pieces of trash into the following cart (`TrashCart.cs`); each deposit plays a quantized melody note. Completing the chore — or grabbing an anomalous cube — transitions to Scene 4.

**Symbolism**
*   **The Cyclic Train:** The loop made visible: the same passengers, the same departures, going nowhere.
*   **Voluntary Chores:** The player tidies their own daydream — escapism re-inventing labor from within.
*   **Generative Muzak:** The station itself hums a pleasant, endless score (`MetroMusicDirector.cs`) — comfort indistinguishable from sedation.

**Key Scripts**
*   `SubwayTrainController.cs`, `TrainPassengerDirector.cs`, `NPCTrainPassenger.cs`, `NPCScene3Wanderer.cs`, `NPCGrounding.cs`, `TrashCart.cs`, `MetroMusicDirector.cs`.

---

### 4.4 Scene 4: The Concert / The Loop (The Trap)

**Narrative Overview**  
The final scene delivers the daydream's promised reward: an open-air concert at night, performed by an NPC band before a dancing crowd. Near the stage floats a radial sequencer — a luminous ring of twelve slots with a sweeping playhead — surrounded by twelve glowing sound orbs. The player composes: each orb placed into a slot joins a musical loop, and with every filled slot the concert's music yields to the player's own creation. When the twelfth slot is filled and the loop plays through, the instrument reveals what it has always been: a clock. The tempo decelerates to exactly one tick per second, the playhead thins into a red second hand, numbers 1–12 surface over the filled slots, frozen hands appear at 9:00 — shift start — and the player is returned to the factory.

**Mechanics & Interaction**
*   **The Radial Sequencer:** `RadialSequencer.cs` runs a DSP-clock-scheduled 12-step loop with procedurally synthesized timbres; `SequencerSampleOrb.cs` provides grabbable, slot-snapping sound orbs with audible previews.
*   **Displacement of the Band:** Each filled slot ducks the concert stems by one twelfth (`ConcertAudioDirector.cs`); the NPC musicians (`NPCMusicianPerformer.cs`) visibly wind down and freeze as their music disappears, and the audience (`NPCAudienceMember.cs`) turns away from the stage toward the player's creation.
*   **The Reveal & Return:** `SequencerFinaleDirector.cs` decelerates the loop, collapses every placed tone into a mechanical tick, drains the ring into the Scene 1 wall-clock palette, and returns the player to `scene1` — where the factory score now plays subtly degraded (faster, detuned).

**Symbolism**
*   **Twelve Slots:** The clock face hidden in plain sight — the player's creativity was always being arranged into clock positions.
*   **Replacing the Band:** Creative agency displaces spectacle, only to discover it has built the instrument of its own return to labor-time.
*   **The Frozen Crowd:** At the reveal, band and audience freeze mid-pose — a tableau of stopped workers facing the clock the player built.
*   **The Degraded Return:** Scene 1 returns slightly faster and out of tune: the loop is the same; the loop is never the same.

**Thematic Arc**

| | Scene 1 | Scene 2 | Scene 3 | Scene 4 |
|---|---------|---------|---------|---------|
| **Space** | Factory | Playground | Metro platform | Concert |
| **Mode** | Labor | Play | Waiting | Creation |
| **The loop** | Imposed | Broken | Observed | Self-built |

**Key Scripts**
*   `RadialSequencer.cs`, `SequencerSampleOrb.cs`, `SequencerFinaleDirector.cs`, `ConcertAudioDirector.cs`, `NPCMusicianPerformer.cs`, `NPCAudienceMember.cs`.

---

## 5. Technical Implementation

### 5.1 Platform & Tools
*   **Device:** Meta Quest 3S (Android)
*   **Engine:** Unity 6 (6000.2.10f1)
*   **Frameworks:** OpenXR, XR Interaction Toolkit, Unity Input System
*   **Language:** C#
*   **Version Control:** Git

### 5.2 Key Systems & Architecture

#### 5.2.1 XR Interaction System
The project utilizes the **XR Interaction Toolkit** for robust hand tracking and controller support. Custom configurations on `XRGrabInteractable` allow for precise manipulation of cubes. Physics are tuned for realistic weight and collision response.

#### 5.2.2 Procedural Generation
To ensure replayability and a sense of scale, much of the world is generated procedurally at runtime.
*   **Factory:** `FactoryFloorGenerator.cs` and `MachineGenerator.cs` construct the industrial environment.
*   **Playground:** `Scene2RoomGenerator.cs` builds the reactive room, while `Scene2TwentyColoredCubesGenerator.cs` handles the distribution of interactive cubes.

#### 5.2.3 Dynamic Environment Logic
*   **Reactive Walls:** The `ColorReactiveWall.cs` script detects collisions and applies the material color of the colliding object to the wall mesh.
*   **Game Management:** A central `GameManager.cs` orchestrates the state flow, tracking the sorting quota and triggering scene transitions.

#### 5.2.4 Audio Immersion
*   **Spatial Audio:** Sounds are spatialized to ground the user.
*   **Procedural Audio:** Impact sounds are dynamically generated and modulated by collision velocity through `CubeCollisionSound.cs`, while `CubeGrabAudio.cs` provides audio feedback when objects are grabbed.
*   **Atmosphere:** `RuntimeAtmosphereController.cs` manages the visual atmosphere (lighting, fog) to create the oppressive factory mood.

#### 5.2.5 Optimization
*   **Lighting:** `LightingOptimizer.cs` manages real-time vs. baked lighting.
*   **Asset Management:** Efficient use of prefabs and object pooling prevents garbage collection spikes.

### 5.3 Folder Structure
*   **Assets/**: Main project assets (Materials, Prefabs, Scenes, Scripts, Resources).
*   **Library/**: Unity-generated cache.
*   **Packages/**: Unity package manifest.
*   **ProjectSettings/**: Unity project settings.

---

## 6. Development Process

### Phase 1: Project Setup & Prototyping
*   Repository initialization and XR configuration (OpenXR, XR Interaction Toolkit).
*   Prototyping core XR mechanics: grabbing, throwing, and collision detection.
*   The "sorting" mechanic was built first to establish the baseline interaction.

### Phase 2: Environment & Tools
*   Developed procedural scene generation scripts for rapid prototyping.
*   Created distinct visual identities: low-poly industrial assets for the factory, high-contrast vibrant materials for the playground.
*   Implemented custom editor tools for batch asset creation.

### Phase 3: Scripting & Logic
*   Implemented the core logic for "The Shift," connecting `BinCollector` to `GameManager` to track the 30-cube quota.
*   Developed the `AnomalyAssetGenerator` and transition logic.
*   Implemented the reactive wall system for Scene 2.

### Phase 4: Polish & Optimization
*   Performance tuning for Quest 3S (profiling, reducing draw calls).
*   Visual and audio polish: Haptic feedback, spatial audio, and baked lighting.
*   Deployed APK to device for real-world testing and iteration.

---

## 7. Outcome & Conclusion

### 7.1 Current Status
The project successfully implements the complete narrative arc. Users can experience the transition from the monotonous sorting task to the liberating creative playground. The application runs stably on the Meta Quest 3S with functional hand tracking and physics.

### 7.2 Reflection
The project demonstrates the potential of VR to convey complex psychological and sociological themes. By placing the user physically inside the metaphor, the abstract concepts of "alienation" and "freedom" become tangible, felt experiences. The contrast between the two scenes effectively communicates the relief and joy of the daydreaming state.

### 7.3 Future Work
*   **Expanded Narrative:** Further developing the narrative depth to enhance the psychological contrast between the two worlds.
*   **Enhanced Creativity:** Adding tools in the Playground scene for sculpting or gravity manipulation, further empowering the user.
*   **User Studies:** Conducting formal user testing to measure the emotional impact and interpretability of the experience.

---

## 8. References
1.  Somer, E. (2002). *Maladaptive Daydreaming: A Qualitative Inquiry*. Journal of Contemporary Psychotherapy.
2.  Marx, K. (1844). *Economic and Philosophic Manuscripts*.
3.  Breton, A. (1924). *Manifesto of Surrealism*.
4.  Unity Technologies. (2024). *Unity Documentation*.
5.  Meta. (2024). *Meta Quest Developer Documentation*.
