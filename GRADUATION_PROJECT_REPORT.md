# Graduation Project Report: Mixed Reality Experience
## Exploring Maladaptive Daydreaming and Capitalist Alienation through VR

**Author:** Semiha PAKSOY  
**Course:** MUS441 - Senior Project I-II  
**Academic Year:** 2025-2026  
**Date:** January 1, 2026

---

## 1. Abstract

This graduation project presents a Mixed Reality (MR) experience designed for the Meta Quest 3S, exploring the psychological phenomenon of **maladaptive daydreaming** and its relationship to **capitalist alienation**. The project leverages the immersive capabilities of Virtual Reality (VR) to create a narrative-driven experience that transitions from a monotonous, oppressive industrial setting to a boundless, surreal playground. By juxtaposing these two distinct environments, the project critiques the rigidity of modern labor systems while celebrating the liberating power of human imagination. This report details the conceptual framework, technical implementation, and creative process behind the development of this interactive experience.

---

## 2. Introduction

### 2.1 Purpose
The primary purpose of this project is to use VR as a medium to simulate and explore the internal experience of maladaptive daydreaming. It aims to provide users with a tangible representation of the contrast between external societal constraints and internal creative freedom.

### 2.2 Motivation
Modern capitalist societies often foster feelings of alienation and monotony, particularly within industrial or repetitive work environments. For many, daydreaming serves as a coping mechanism—a form of resistance to reclaim agency. This project seeks to visualize this psychological escape, using VR technology to blur the lines between "reality" (the factory) and "fantasy" (the playground).

### 2.3 Problem Statement
How can immersive technology be used to represent abstract psychological concepts like alienation and escapism? This project addresses this by creating a spatial narrative that physically transports the user from a restrictive environment to one of limitless possibility.

---

## 3. Conceptual Background

### 3.1 Maladaptive Daydreaming
Maladaptive daydreaming is a psychological concept describing extensive fantasy activity that replaces human interaction or interferes with daily functioning. Individuals often retreat into vivid, elaborate inner worlds to escape stress or trauma.
*   **Relevance to VR:** VR acts as a technological extension of this capacity for immersive fantasy, providing a controlled space where users can experience the "boundaryless" nature of a daydream.

### 3.2 Capitalism, Alienation, and Escapism
Drawing from Marxist theory, the project explores the concept of **alienation**—the detachment of workers from the products of their labor and from their own humanity.
*   **The Factory:** Represents the capitalist machine, where the user is reduced to a function (sorting cubes), governed by quotas and time.
*   **Escapism:** The transition to the surreal world symbolizes the mind's refusal to be contained by these rigid structures.

### 3.3 Surrealism
The project adopts a surrealist aesthetic to depict the inner world. By suspending the laws of physics and logic (e.g., reactive walls, floating anomalies), the experience mirrors the fluid, dreamlike quality of the subconscious mind.

---

## 4. Project Description

### 4.1 Overview
The experience is divided into two contrasting scenes, each with distinct visual styles, mechanics, and audio landscapes.

### 4.2 Scene 1: The Shift (The Reality)
*   **Setting:** A dark, oppressive factory.
*   **Narrative:** The user plays a worker tasked with sorting red and blue cubes into matching bins on a conveyor belt.
*   **Atmosphere:** Dominated by mechanical sounds, a ticking clock, and dim lighting. Reports on the walls reinforce a sense of surveillance and routine.
*   **The Turning Point:** After sorting 30 cubes, the routine is disrupted by the appearance of glowing green "Anomaly Cubes." Touching one shatters the factory simulation.

### 4.3 Scene 2: The Colorful Playground (The Escape)
*   **Setting:** A bright, expansive room with reactive walls made of various materials (Metal, Wood, Glass, Concrete).
*   **Narrative:** The user is free from rules and quotas. The goal is pure expression.
*   **Mechanics:** The room contains 20 colorful cubes (Red, Blue, Green, Yellow, Purple). Throwing a cube at a wall paints the wall with that color, allowing the user to reshape their environment.
*   **Symbolism:** The absence of objectives critiques the productivity obsession of the factory, celebrating purposeless play.

---

## 5. Technical Implementation

### 5.1 Tools & Technologies
*   **Engine:** Unity 6 (6000.2.10f1)
*   **Platform:** Meta Quest 3S (Android)
*   **Frameworks:** OpenXR, XR Interaction Toolkit, Unity Input System
*   **Language:** C#

### 5.2 Key Features & Mechanics
1.  **XR Interaction System:**
    *   Full hand tracking and controller support for immersive object manipulation (grabbing, throwing).
    *   Physics-based interactions for realistic object collisions and movement.

2.  **Procedural Generation:**
    *   **Scene 1:** Procedural spawning of cubes on the conveyor belt (`ObjectSpawner.cs`, `ConveyorBelt.cs`).
    *   **Scene 2:** Runtime generation of the playground environment and cube placement (`Scene2TwentyColoredCubesGenerator.cs`, `Scene2RoomGenerator.cs`).

3.  **Dynamic Environment:**
    *   **Reactive Walls:** Custom scripts (`ColorReactiveWall.cs`) allow walls to change color and material properties upon collision.
    *   **Audio System:** Spatial audio with unique sound cues for different materials and interactions (`CubeCollisionSound.cs`).

4.  **Optimization:**
    *   Efficient asset management and batching to ensure smooth performance on standalone VR hardware.

### 5.3 Development Process
The development followed an iterative approach:
1.  **Prototyping:** Establishing core XR mechanics (grabbing, throwing) and scene flow.
2.  **Environment Design:** Building the contrasting visual identities of the Factory and the Playground.
3.  **Scripting:** Implementing game logic, procedural generation, and interaction systems.
4.  **Polish:** Enhancing lighting, audio, and haptic feedback for immersion.
5.  **Testing:** Deploying builds to the Meta Quest 3S for performance and user experience validation.

---

## 6. Outcome & Conclusion

### 6.1 Current Status
The project successfully implements the core narrative arc and interactive mechanics. Users can experience the transition from the monotonous sorting task to the liberating creative playground. The application runs stably on the target hardware with functional hand tracking and physics.

### 6.2 Reflection
The project demonstrates the potential of VR to convey complex psychological and sociological themes. By placing the user physically inside the metaphor, the abstract concepts of "alienation" and "freedom" become tangible, felt experiences.

### 6.3 Future Work
*   **Expanded Narrative:** Introducing more scenes to further explore the depths of the daydreaming world.
*   **Enhanced Interactions:** Adding more complex tools for creativity in the Playground scene (e.g., sculpting, gravity manipulation).
*   **User Testing:** Conducting formal user studies to measure the emotional impact and interpretability of the experience.

---

## 7. References
1.  Somer, E. (2002). *Maladaptive Daydreaming: A Qualitative Inquiry*. Journal of Contemporary Psychotherapy.
2.  Marx, K. (1844). *Economic and Philosophic Manuscripts*.
3.  Breton, A. (1924). *Manifesto of Surrealism*.
4.  Unity Technologies. (2024). *Unity Documentation*.
5.  Meta. (2024). *Meta Quest Developer Documentation*.
