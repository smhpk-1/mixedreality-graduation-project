# Graduation Project Report: Mixed Reality Experience
## Exploring Maladaptive Daydreaming and Capitalist Alienation through VR

**Author:** Semiha PAKSOY  
**Course:** MUS441 - Senior Project I-II  
**Academic Year:** 2025-2026  
**Date:** January 1, 2026

---

## 1. Abstract

This graduation project presents a Mixed Reality (MR) experience designed for the Meta Quest 3S, exploring the complex psychological phenomenon of **maladaptive daydreaming** and its relationship to **capitalist alienation**. The project leverages the immersive capabilities of Virtual Reality (VR) to create a bifurcated narrative experience that physically transports the user from a monotonous, oppressive industrial setting to a boundless, surreal playground. By juxtaposing these two distinct environments—"The Shift" and "The Colorful Playground"—the project critiques the rigidity of modern labor systems while celebrating the liberating, albeit isolating, power of human imagination. This report details the conceptual framework, technical implementation, creative process, and the specific design decisions made to translate abstract psychological theories into a tangible, interactive reality.

---

## 2. Introduction

### 2.1 Purpose
The primary purpose of this project is to utilize Virtual Reality as a medium to simulate and explore the internal experience of maladaptive daydreaming. Unlike traditional media, VR offers a sense of *presence*, allowing users to inhabit the perspective of a worker caught between the crushing weight of external societal constraints and the vivid allure of internal creative freedom.

### 2.2 Motivation
Modern capitalist societies often foster feelings of alienation and monotony, particularly within industrial or repetitive work environments. For many individuals, daydreaming serves not just as a distraction, but as a critical coping mechanism—a form of psychological resistance to reclaim agency in a world that demands conformity. This project seeks to visualize this psychological escape, using VR technology to blur the lines between "reality" (the factory) and "fantasy" (the playground), making the internal struggle external and visible.

### 2.3 Problem Statement
How can immersive technology be used to represent abstract psychological concepts like alienation, dissociation, and escapism? Traditional storytelling can describe these states, but VR can induce them. This project addresses this challenge by creating a spatial narrative that physically transports the user from a restrictive, rule-bound environment to one of limitless possibility, forcing them to experience the jarring transition between these two states of being.

---

## 3. Conceptual Framework

### 3.1 Maladaptive Daydreaming
Maladaptive daydreaming is a psychological concept describing extensive fantasy activity that replaces human interaction or interferes with daily functioning. Individuals often retreat into vivid, elaborate inner worlds to escape stress, trauma, or the mundane nature of reality.
*   **Relevance to VR:** VR acts as a technological extension of this capacity for immersive fantasy. It provides a controlled space where users can experience the "boundaryless" nature of a daydream, validating the intensity of the experience while highlighting its disconnect from the physical world.

### 3.2 Capitalism, Alienation, and Escapism
Drawing from Marxist theory, the project explores the concept of **alienation**—the detachment of workers from the products of their labor, from the act of production, and from their own humanity.
*   **The Factory as Metaphor:** The industrial setting represents the capitalist machine. The user is reduced to a function (sorting cubes), governed by quotas (30 cubes) and time (the ticking clock). The labor is repetitive, meaningless, and isolating.
*   **Escapism as Resistance:** The transition to the surreal world symbolizes the mind's refusal to be contained by these rigid structures. It is a rejection of "productive" labor in favor of "unproductive" play.

### 3.3 Surrealism and the Subconscious
The project adopts a surrealist aesthetic to depict the inner world. By suspending the laws of physics and logic—introducing reactive walls, floating anomalies, and impossible geometries—the experience mirrors the fluid, dreamlike quality of the subconscious mind. This draws inspiration from the Surrealist Manifesto, aiming to resolve the contradictory conditions of dream and reality into an absolute reality, a *surreality*.

---

## 4. Project Narrative & Design

The experience is structured into two contrasting scenes, representing the duality of the protagonist's mind.

### 4.1 Scene 1: The Shift (The Reality)
This scene establishes the baseline of oppression and monotony.
*   **Setting:** A dark, claustrophobic factory floor. The lighting is dim and industrial.
*   **Narrative:** The user plays a worker tasked with a meaningless job: sorting red and blue cubes into matching bins on a conveyor belt.
*   **Mechanics:**
    *   **The Routine:** Cubes are spawned via `DispenserGenerator` and move along a `ConveyorBelt`. The user must grab and sort them.
    *   **The Quota:** A `FactoryScoreBoard` tracks progress. The user must sort 30 cubes to trigger the next event.
    *   **The Atmosphere:** A `WallClock` ticks relentlessly, and reports on the walls reinforce a sense of surveillance.
*   **The Turning Point:** After the 30th cube, the machinery halts. The `AnomalyAssetGenerator` spawns three glowing green "Anomaly Cubes." These objects defy the factory's color palette and logic. Touching one triggers a "shattering" effect, symbolizing the dissociation from reality.

### 4.2 Scene 2: The Colorful Playground (The Escape)
This scene represents the "Awakening"—a dive into the user's vivid imagination.
*   **Setting:** A bright, expansive room generated by `Scene2RoomGenerator`. The walls are composed of various materials (Metal, Wood, Glass, Concrete) that react to touch.
*   **Narrative:** The user is free from rules, quotas, and supervisors. The goal is pure expression.
*   **Mechanics:**
    *   **Creative Freedom:** The room contains 20 colorful cubes (Red, Blue, Green, Yellow, Purple) generated by `Scene2TwentyColoredCubesGenerator`.
    *   **Reactive Environment:** Throwing a cube at a wall triggers the `ColorReactiveWall` script, painting the wall with that color. This allows the user to reshape their environment dynamically.
    *   **Physics & Play:** Unlike the rigid sorting in Scene 1, here objects bounce, roll, and interact playfully.
*   **Symbolism:** The absence of objectives critiques the productivity obsession of the factory. The user engages in "purposeless play," which, in this context, is the ultimate act of freedom.

---

## 5. Technical Implementation

### 5.1 Tools & Technologies
*   **Engine:** Unity 6 (6000.2.10f1)
*   **Platform:** Meta Quest 3S (Android)
*   **Frameworks:** OpenXR, XR Interaction Toolkit, Unity Input System
*   **Language:** C#
*   **Version Control:** Git

### 5.2 Key Systems & Architecture

#### 5.2.1 XR Interaction System
The project utilizes the **XR Interaction Toolkit** for robust hand tracking and controller support.
*   **Grabbing & Throwing:** Custom configurations on `XRGrabInteractable` allow for precise manipulation of cubes.
*   **Physics:** Rigidbody physics are tuned for realistic weight and collision response, essential for the "tactile" feel of the factory work and the playground fun.

#### 5.2.2 Procedural Generation
To ensure replayability and a sense of scale, much of the world is generated procedurally at runtime.
*   **Factory Generation:** `FactoryFloorGenerator.cs` and `MachineGenerator.cs` construct the industrial environment, placing conveyor belts and bins dynamically.
*   **Playground Generation:** `Scene2RoomGenerator.cs` builds the reactive room, while `Scene2TwentyColoredCubesGenerator.cs` handles the distribution of the interactive cubes.

#### 5.2.3 Dynamic Environment Logic
*   **Reactive Walls:** The `ColorReactiveWall.cs` script detects collisions with specific tags ("Cube") and applies the material color of the colliding object to the wall mesh, creating a persistent "painting" effect.
*   **The Anomaly:** `AnomalyCube.cs` and `AnomalyMovement.cs` handle the floating, otherworldly behavior of the transition objects, distinguishing them from the physics-bound factory objects.
*   **Game Management:** A central `GameManager.cs` orchestrates the state flow, tracking the sorting quota and triggering the scene transition.

#### 5.2.4 Audio Immersion
Audio is a critical narrative tool.
*   **Spatial Audio:** Sounds are spatialized to ground the user in the environment.
*   **Procedural Audio:** `CubeCollisionSound.cs` generates procedural impact sounds dynamically modulated by collision velocity, while `CubeGrabAudio.cs` triggers specific audio clips based on the object identity.
*   **Atmosphere:** `RuntimeAtmosphereController.cs` manages the visual atmosphere (lighting, fog) to create the oppressive factory mood.

#### 5.2.5 Optimization
Targeting the Meta Quest 3S requires strict performance management.
*   **Lighting:** `LightingOptimizer.cs` manages real-time vs. baked lighting to maintain high frame rates.
*   **Asset Management:** Efficient use of prefabs and object pooling (via `ObjectSpawner.cs`) prevents garbage collection spikes during gameplay.

---

## 6. Development Process

### 6.1 Phase 1: Prototyping & Mechanics
The initial phase focused on the core XR mechanics: grabbing, throwing, and collision detection. The "sorting" mechanic was built first to establish the baseline interaction.

### 6.2 Phase 2: Environment & Atmosphere
Distinct visual identities were developed for the two scenes. The factory used low-poly industrial assets with dark, metallic textures, while the playground utilized high-contrast, vibrant materials.

### 6.3 Phase 3: Scripting & Logic
The core logic for the "Shift" was implemented. This involved connecting the `BinCollector` logic to the `GameManager` to track the 30-cube quota and trigger the `AnomalyAssetGenerator`.

### 6.4 Phase 4: Polish & Optimization
The final phase focused on the "feel" of the experience. Haptic feedback was added to interactions, and lighting was baked to ensure smooth performance on the standalone headset.

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
