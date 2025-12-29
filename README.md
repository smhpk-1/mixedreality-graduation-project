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
├── Materials/          # Materials and shaders
├── Prefabs/           # Reusable prefabs
├── Resources/         # Runtime-loaded assets
├── Scenes/            # Unity scene files
├── Scripts/           # C# game scripts
│   ├── GameManager.cs
│   ├── ObjectSpawner.cs
│   ├── ConveyorBelt.cs
│   ├── BinCollector.cs
│   ├── ColorReactiveWall.cs
│   ├── PlaygroundCube.cs
│   ├── AnomalyCube.cs
│   └── ...
├── Settings/          # Project settings
└── XR/                # XR configuration
```

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
