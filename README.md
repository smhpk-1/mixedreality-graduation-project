# 🎮 Mixed Reality Graduation Project

A surreal mixed reality experience exploring themes of **maladaptive daydreaming**, **capitalist alienation**, and **creative freedom** through immersive VR gameplay on the **Meta Quest 3S**.

![Unity](https://img.shields.io/badge/Unity-6000.2.10f1-black?logo=unity)
![Platform](https://img.shields.io/badge/Platform-Meta%20Quest%203S-blue)
![License](https://img.shields.io/badge/License-Educational-green)

---

## 📖 Overview

This project is an interactive VR experience that takes players on a journey from an oppressive factory environment to a boundless, colorful playground—symbolizing the escape from monotony through imagination and creative expression.

The experience is built using **Unity 6** with the **XR Interaction Toolkit** and features:
- 🖐️ Full hand tracking support
- 🎯 Controller-based interactions
- 🏭 Procedurally generated environments
- 🎨 Physics-based gameplay mechanics
- 🔊 Immersive spatial audio

---

## 🎬 Scenes

### Scene 1: The Shift
> *An industrial prologue representing capitalist monotony and alienation*

<details>
<summary>Click to expand details</summary>

The player awakens as a "worker" in a dark, oppressive factory. Their task: sort **red and blue cubes** from a conveyor belt into matching bins. The relentless ticking of a wall clock and reports on the walls reinforce the sense of endless, repetitive labor.

**Gameplay:**
- Sort 30 cubes (red and blue) into correct bins
- Experience the monotony of industrial labor
- Discover the **Anomaly Cubes** that break the cycle

**Key Features:**
- Conveyor belt mechanics
- Color-coded bin sorting
- Atmospheric factory environment
- Dramatic transition sequence

</details>

### Scene 2: The Colorful Playground
> *A surreal space representing the unbounded imagination*

<details>
<summary>Click to expand details</summary>

After touching the Anomaly Cube, players are transported to a bright, surreal playground—a physical manifestation of the daydreaming mind.

**Gameplay:**
- Explore freely with no rules or quotas
- Grab and throw 20 colorful cubes (5 colors × 4 each)
- Watch walls transform to match cube colors
- Create your own color compositions

**Key Features:**
- Reactive walls with different materials (Metal, Wood, Glass, Concrete, Stone)
- Full physics-based throwing mechanics
- Persistent color changes
- Unique audio feedback per color and material
- Room collapse after 20 total wall hits, revealing a city street leading to the metro

</details>

### Scene 3: The Platform
> *An underground metro station — the daydream's quiet middle distance*

<details>
<summary>Click to expand details</summary>

The daydream lands somewhere suspiciously ordinary: a metro platform where a train arrives, exchanges passengers, and departs in an endless cycle. The player can tidy the platform — voluntarily doing janitorial work inside their own fantasy.

**Gameplay:**
- Watch NPC commuters wander, board, and exit the cycling train
- Collect 20 pieces of scattered trash into a player-following cleaning cart
- Each collected piece plays a quantized melody note
- Grab the anomalous cube to continue

**Key Features:**
- Autonomous train with sliding doors and waypoint-based NPC boarding
- Generative ambient score from the station's own objects (benches, lamps, rails, NPCs)
- PA announcements tied to train events
- Procedural NPC walk animation, blob shadows, unified floor grounding

</details>

### Scene 4: The Concert / The Loop
> *The daydream's climax — and its trap*

<details>
<summary>Click to expand details</summary>

An open-air concert at night: a live NPC band plays the stage, a crowd dances. Near the stage floats a radial sequencer — twelve slots, a sweeping playhead — and twelve glowing sample orbs.

**Gameplay:**
- Grab sample orbs (each hums its own synth voice) and place them into the sequencer slots
- Each filled slot makes your loop louder and the concert quieter — you are replacing the band
- Complete all twelve slots and let your loop play

**The Twist:**
The sequencer has twelve slots. It was always a clock. The loop decelerates to one tick per second, your tones collapse into a mechanical tick, numbers 1–12 surface over the slots you filled, frozen hands appear at 9:00 — shift start — and you are returned to the factory. The loop closes: factory → daydream → creation → clock → factory.

**Key Features:**
- Hybrid stem audio (2D stereo + 3D spatial, DSP-clock synced)
- DSP-scheduled 12-step radial sequencer with procedural synth timbres
- NPC musicians and audience that wind down as your loop takes over
- The clock reveal and the return to Scene 1 (which comes back subtly degraded)

</details>

---

## 🛠️ Technical Stack

| Component | Technology |
|-----------|------------|
| **Engine** | Unity 6 (6000.2.10f1) |
| **Render Pipeline** | Built-in Render Pipeline |
| **XR Framework** | OpenXR + XR Interaction Toolkit |
| **Input System** | New Unity Input System |
| **Target Device** | Meta Quest 3S |
| **Platform** | Android |

---

## 📁 Project Structure

```
Assets/
├── scene1.unity        # Scene 1: The Shift (factory)
├── Scene 2.unity       # Scene 2: The Colorful Playground (+ city street)
├── Scene 3.unity       # Scene 3: The Platform (metro)
├── Scene 4.unity       # Scene 4: The Concert / The Loop
├── Materials/          # Materials and shaders
├── Prefabs/            # Reusable prefabs
├── Resources/          # Runtime-loaded assets (sound design, blob shadow)
├── npc_casual_set_00/  # Humanoid NPC character set (Scenes 3 & 4)
├── Scripts/            # ~80 C# runtime scripts
│   ├── GameManager.cs / ConveyorBelt.cs / BinCollector.cs   (Scene 1)
│   ├── PlaygroundCube.cs / DestructibleWall.cs              (Scene 2)
│   ├── SubwayTrainController.cs / TrashCart.cs              (Scene 3)
│   ├── RadialSequencer.cs / ConcertAudioDirector.cs         (Scene 4)
│   ├── FactoryMusicDirector.cs / MetroMusicDirector.cs      (generative audio)
│   └── Editor/         # Scene setup & placement tools (Tools menu)
├── Settings/           # Project settings
└── XR/                 # XR configuration
```

> Note: scenes live at the `Assets/` root — there is no `Assets/Scenes/` folder.

---

## 🚀 Getting Started

### Prerequisites
- Unity 6 (6000.2.10f1) or later
- Meta Quest 3S device
- Android Build Support module
- OpenXR Plugin

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/mixedreality-graduation-project.git
   ```

2. **Open in Unity**
   - Launch Unity Hub
   - Add project from disk
   - Open with Unity 6

3. **Configure XR Settings**
   - Go to `Edit > Project Settings > XR Plug-in Management`
   - Enable OpenXR for Android
   - Add Meta Quest Support feature

4. **Build & Deploy**
   - Switch platform to Android (`File > Build Settings`)
   - Connect Quest 3S via USB
   - Build and Run

---

## 🎮 Controls

| Action | Hand Tracking | Controllers |
|--------|--------------|-------------|
| Grab | Pinch gesture | Grip button |
| Throw | Release pinch | Release grip |
| Move | Walk in play space | Thumbstick |

---

## 🎨 Conceptual Framework

This project explores the psychological concept of **maladaptive daydreaming** as a lens for understanding:

- **Capitalist Alienation**: The factory scene represents the monotony and lack of agency in industrial labor
- **Escapism**: VR as a technological extension of the mind's capacity for immersive fantasy
- **Creative Freedom**: The playground scene celebrates purposeless play and creative expression

> *"VR can serve as a technological extension of the mind's capacity for immersive fantasy, providing a controlled, interactive space for users to explore alternate realities."*

---

## 📚 References

- Somer, E. (2002). *Maladaptive Daydreaming: A Qualitative Inquiry*. Journal of Contemporary Psychotherapy.
- Marx, K. (1844). *Economic and Philosophic Manuscripts*.
- Breton, A. (1924). *Manifesto of Surrealism*.

---

## 📄 Documentation

- [Project Documentation](PROJECT_DOCUMENTATION.md) - Technical specifications and development log
- [Project Story](PROJECT_STORY.md) - Narrative structure and scene details
- [Conceptual Background](CONCEPTUAL_BACKGROUND.md) - Theoretical framework and symbolism

---

## 👤 Author

**Semiha PAKSOY**

📚 **MUS441 - Senior Project I-II**  
🎓 **2025-2026 Academic Year**

---

## 📝 License

This project is developed for educational purposes as part of the MUS441 Senior Project I-II course requirements.

---

<p align="center">
  <i>Breaking free from capitalist constraints through creative expression in virtual reality</i>
</p>
