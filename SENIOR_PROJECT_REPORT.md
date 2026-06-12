# Senior Project Final Report: Mind Palace
## Exploring Maladaptive Daydreaming, Alienation, and Repetition through Interactive VR Sound

**Author:** Semiha Paksoy

**Project title:** Mind Palace

**Scene 1 title:** The Shift

**Course:** MUS 442 - Senior Project II

**Academic year:** 2025-2026

**Department:** Istanbul Bilgi University, Music Department

**Project category:** Sound Installation and Multimedia Project

**Project supervisor:** Tolga Tüzün

**Course instructors / advisor jury named in the 2025-2026 guideline:** Tolga Tüzün, Oğuz Usman, Enis Gümüş, Yiğit Özatalay, Cem Ömeroğlu, Semin Tunalı

**Date:** June 12, 2026

**Repository:** <https://github.com/smhpk-1/mixedreality-graduation-project>

---

## 1. Abstract

**Mind Palace** is a sound-centered virtual reality narrative for Meta Quest 3S that explores maladaptive daydreaming, capitalist alienation, repetition, and creative agency. The first scene is titled **The Shift**: the player begins as a factory worker sorting thirty red and blue cubes under a quota. An impossible green cube interrupts the system and opens a daydream. The player then moves through a colorful playground, a cyclic metro station, and an outdoor concert before returning to the factory.

The project began during MUS 441 as a two-scene contrast between oppressive labor and liberating play. During MUS 442, it developed into a four-scene circular narrative. The final version complicates the original idea of escape: the playground offers real creative freedom, but routine gradually returns through cleanup work, commuting cycles, musical repetition, and the hidden clock structure of a twelve-step sequencer. In the finale, the player constructs a musical loop that transforms into the factory clock and begins a subtly altered second shift.

Sound is the primary narrative material. Each scene has a different musical relationship to the player: factory labor generates quantized feedback, colored cubes become instruments, the metro station becomes a distributed generative ensemble, and the concert is gradually replaced by a loop composed by the player. The project combines interactive composition, spatial audio, procedural synthesis, DSP-clock synchronization, VR interaction, autonomous characters, and environmental storytelling.

## 2. Project Aim and Category

The project was developed in the **Sound Installation and Multimedia Project** category. Its central aim is to demonstrate how an interactive audiovisual environment can communicate an argument through embodied action and musical structure rather than through dialogue or linear exposition.

The project asks:

> How can VR and interactive sound make the relationship between alienation, escape, repetition, and creative agency physically perceptible?

The intended final experience is approximately fifteen minutes, depending on player behavior and exploration.

## 3. Motivation

Repetitive work can produce a feeling of separation from one's time, actions, and creativity. The project is especially interested in the mundane, everyday, and quotidian pressure of the work-leisure cycle: capitalism organizes labor, recovery, entertainment, and exhaustion so tightly that even leisure can begin to feel like another managed interval. In this frame, maladaptive daydreaming is not treated as a random fantasy habit, but as a pressured response to ordinary life becoming repetitive, surveilled, and emotionally depleted.

Daydreaming can become a way to recover a sense of freedom and authorship, but escape is not necessarily outside the structures that made it necessary.

VR was chosen because it makes this conflict bodily. The player does not only observe a worker sorting objects; they perform the task. They do not only hear that the fantasy is creative; they repaint walls and compose loops through physical gestures. The same interaction can therefore carry different meanings depending on the system around it.

The project uses maladaptive daydreaming as a conceptual lens, not as a clinical simulation or diagnosis. It is concerned with immersive fantasy as both relief and risk: a space of imagination that can also become closed, repetitive, and detached.

## 4. Conceptual Framework

### 4.1 Alienated Labor

The factory draws from Marx's concept of alienated labor. The player has no relationship to the purpose of the cubes and no control over the system. They are reduced to sorting correctly, and progress is measured as a quota.

The wall clock, conveyor belt, reports, quota logic, and binary bins turn time and action into external demands. Even the musical reward for correct work belongs to the factory's system.

### 4.2 Surrealism and Recontextualization

The experience uses surreal transformation by preserving familiar objects while changing their function:

- cubes move from products to toys, anomalies, and musical units;
- walls move from boundaries to canvases and then collapse;
- a metro station becomes an ensemble;
- a musical sequencer becomes a clock.

This dream logic allows one gesture to be read differently across the narrative.

### 4.3 The Ambivalence of Escape

The MUS 441 version proposed a strong contrast between the factory and the playground. The MUS 442 version extends this idea so that the fantasy develops its own routines.

The metro contains compulsory cleanup work and a visible commuter cycle. The player is positioned as a cleaner, not as someone freely choosing a helpful side activity. The concert offers the strongest creative agency, but that agency is organized into twelve repeating positions. The player's completed composition becomes the mechanism that returns them to the factory.

The final project therefore does not argue that imagination is false or useless. It argues that imagination is shaped by the reality it escapes.

## 5. Narrative and Interaction Design

## 5.1 Scene 1: The Shift

The player wakes at a workstation inside a dark industrial room. Red and blue cubes arrive on a conveyor belt and must be placed into matching bins. Correct and incorrect actions produce clearly different feedback. After thirty standard cubes have spawned, green anomaly cubes appear. Because there is no green bin, the anomaly exposes the limitation of the factory's classification system. Grabbing it loads the playground.

The interaction is intentionally simple and repetitive. Its purpose is to establish a physical memory that later scenes can transform.

## 5.2 Scene 2: The Colorful Playground

The player enters a compact dream space containing twenty colored cubes. The cube is no longer an object to classify. Each color has a synth voice, and throwing cubes at the walls repaints the room.

There is no correct color arrangement. The player creates a temporary audiovisual composition through movement. However, repeated impact also damages the room. After twenty total wall hits, the final wall and then the room collapse visually. The collapse itself is not represented by a prominent destruction sound in the current build; instead, the enclosed sound world gives way to procedural city rumble and street ambience before Scene 3.

## 5.3 Scene 3: The Platform

The player arrives at a metro station where trains and commuters repeat an autonomous cycle. NPCs wander, approach authored boarding paths, enter through sliding doors, travel away, and later return to their original positions.

Trash is scattered across the station. The player is given the role of a cleaner and must collect it using a cleanup cart that follows when needed. Each collected item adds a quantized note to the station score. After twenty pieces, the project advances to Scene 4. A transition cube also exists as an alternate route.

The metro is the narrative turning point. The fantasy is still musical and visually different from the factory, but waiting, commuting, and work have returned.

## 5.4 Scene 4: The Concert / The Loop

The concert is the apparent reward at the end of the daydream. Synchronized musical stems are distributed between stereo playback and spatial stage objects. A performing band and fourteen-person audience make the spectacle responsive: as the player's loop takes over, the band slows, the crowd turns toward the sequencer, and both freeze during the clock reveal.

The final interaction is a twelve-slot radial sequencer. The player previews and places sound orbs into the ring. Each filled slot adds a tone and reduces the concert by one twelfth. When all slots are filled, the concert has been replaced by the player's composition.

After the complete sequence plays, the ring slows to one step per second. Its colors drain, the slots become clock positions, the playhead becomes a red second hand, and fixed hands appear at 9:00. The player's tones are replaced by ticks, the orbs can no longer be removed, and the factory reloads.

The second shift is slightly faster and detuned. The loop has closed, but it is not exactly the same.

## 6. Music and Sound Design

## 6.1 Compositional Strategy

The project's musical systems use repetition as both material and subject. Ostinatos, tape loops, train cycles, stem loops, and step sequencing are not only sound-design techniques; they express the narrative.

The factory and metro systems are influenced by Brian Eno's use of the studio as a compositional tool and by incommensurate tape-loop structures in which layers repeat at different lengths and do not return to the same alignment.

## 6.2 Scene 1: Machine Recordings as Score

`FactoryMusicDirector` loads machine recordings and slices them at runtime into:

- percussive transients for the factory ostinato;
- long tonal tape-loop materials;
- steam breaths;
- spatial layers attached to factory objects.

The score runs at 76 BPM on a sample-accurate DSP grid. Correct sorts produce quantized pentatonic notes and wrong sorts produce a distinct error figure. As the quota rises, the melodic register rises. On the second shift, the tempo increases by 6 BPM and selected tape loops are slightly detuned.

## 6.3 Scene 2: Color as Timbre

Each cube color is associated with a synth recording. The loop plays while the cube is held and fades after release. Impact velocity influences response, and wall hits combine color change with musical feedback.

Audio loudness is normalized across the five source recordings to keep one color from dominating the scene.

`StreetAmbienceDirector` repairs the street's environmental playback and makes the post-collapse transition feel broader without claiming a loud wall-destruction cue. It converts unnaturally looping animal and voice clips into intermittent events, supplies procedural shop-radio and electrical sounds, and fades in city rumble once the room opens.

## 6.4 Scene 3: The Station as Ensemble

`MetroMusicDirector` runs at 58 BPM in A minor pentatonic. It creates a shared musical world from:

- rail ostinatos;
- bench tape-loop dyads;
- fluorescent hum drones;
- trash-can tick patterns;
- NPC hums, murmurs, and whispers;
- tunnel rumble;
- brake hiss;
- train-event and idle PA announcements;
- quantized trash-collection melody.

The station remains musically coherent even though its layers repeat independently. This creates a calm surface for a world built from routines.

## 6.5 Scene 4: Player Composition and Capture

`ConcertAudioDirector` starts all concert stems on one DSP timestamp. Some stems are heard as stereo layers while others come from stage objects.

The radial sequencer uses procedurally synthesized tones and a DSP-locked playhead. Placement feedback, haptics, and slot glow support musical interaction. As the player fills the sequencer, the concert and the band's movement are progressively reduced. The finale converts the musical pulse into clock time.

## 7. Technical Implementation

### 7.1 Platform and Interaction

The project is built in Unity 6 for Meta Quest 3S using OpenXR, XR Interaction Toolkit, XR Hands, and the Unity Input System. Interactions are based on grabbing, releasing, throwing, collision, trigger volumes, and haptic feedback.

### 7.2 Procedural and Tool-Based Workflow

The project uses procedural runtime systems and editor tools to build and repair scenes. Factory environments, instruments, speakers, room elements, interaction components, train doors, sequencer components, band characters, audience characters, and lighting helpers can be generated or configured through scripts.

This approach allowed rapid iteration but also created a final integration requirement: the newest Scene 4 sequencer, band, and audience systems must be installed with their editor tools and saved into `Scene 4.unity` before the final build.

### 7.3 Autonomous Metro System

The metro required a hybrid navigation approach. NPCs use NavMesh while wandering and while approaching the start of a boarding route. They then switch to explicit waypoint chains for reliable movement through train doors and interiors. The train waits for all passengers or a timeout before closing.

The final cyclic system stores each NPC's original position, hides the NPC after travel, and restores it for the next loop.

### 7.4 Quest 3S Reliability

Several issues appeared only or mainly on the device:

- XR rig position and tilt;
- missing or inconsistent trash physics;
- unstable grab behavior;
- NPC LOD disappearance;
- floating NPC appearance without realtime shadows;
- lighting differences;
- train passenger alignment.

The project addresses these with runtime physics repair, diagnostic logging, blob shadows, light-probe tooling, transform and LOD fixes, and repeated device testing.

### 7.5 Project Scale

At the final documentation pass, the current `main` workspace contains:

- four build scenes;
- 69 runtime scripts;
- 14 editor scripts;
- approximately 16,000 lines of project C#;
- multiple Resources-based sound-design libraries;
- Android APK build artifacts.

## 8. Development Process

### 8.1 MUS 441: Establishing the Core Contrast

The first semester established:

- Quest and XR project setup;
- factory environment and sorting mechanic;
- quota, anomaly, and Scene 1-to-Scene 2 transition;
- colored cubes and reactive walls;
- basic spatial and interaction sound;
- the original conceptual contrast between alienation and creative freedom.

The MUS 441 submission used **Mind Palace** as the whole project title and documented a two-scene experience. In the final MUS 442 version, **Mind Palace** remains the project title, while **The Shift** is the title of the first factory scene.

### 8.2 MUS 442: Expanding and Revising the Thesis

The second semester changed the project at both narrative and technical levels.

The playground gained destructible progression. A previous Scene 3 direction was replaced by the metro station. Scene 4 was established as a concert. Train systems, NPC behavior, trash cleanup, new transition logic, and device diagnostics were developed. In June, the factory and metro received generative musical systems, Scene 2 audio and street ambience were polished, trash deposits were made more reliable, and the concert finale was redesigned around the radial sequencer, responsive band and audience, and clock reveal.

The most important conceptual revision was the move from a binary escape story to a circular and ambivalent one.

## 9. Challenges and Solutions

### 9.1 Reliable NPC Boarding

**Problem:** NavMesh alone was unreliable for entering and exiting a moving train, and NPCs could slide, disappear, or face backward.

**Solution:** Use NavMesh only for the approach, then switch to authored waypoint chains for door and interior movement. Make the train wait for passenger completion and reset passengers cyclically.

### 9.2 Standalone Device Differences

**Problem:** Editor behavior did not always match Quest behavior.

**Solution:** Add runtime repair systems, device-visible debug logging, NPC diagnostics, blob shadows, LOD fixes, and repeated on-headset testing.

### 9.3 Making Interaction Musically Legible

**Problem:** Raw environmental recordings and uneven source clips could obscure interaction feedback.

**Solution:** Create distinct synthesized confirmation/error tones, normalize Scene 2 loop loudness, use DSP quantization, spatialize important sources, and duck competing layers.

### 9.4 Connecting the Ending to the Thesis

**Problem:** A concert could feel like a large but thematically disconnected final scene.

**Solution:** Make the player progressively replace the concert with a personal twelve-step loop, then reveal the loop as the factory clock. This makes the ending an action the player performs rather than a message delivered to them.

## 10. Outcome and Evaluation

The project now has a coherent four-scene narrative and a stronger relationship between its conceptual argument, sound design, and interaction systems. Its most successful design strategy is the transformation of repeated gestures and loops across contexts.

The final version demonstrates:

- interactive sound composition as narrative structure;
- a complete circular story rather than a simple reality/fantasy contrast;
- technically complex autonomous metro behavior;
- generative music integrated with scene objects and player actions;
- an ending in which the central argument is expressed through mechanics and sound.

No formal user study has been completed. Evaluation has primarily taken the form of iterative implementation, Editor testing, device testing, debugging, and comparison against the intended narrative flow.

## 11. Authorship and Resources

**Semiha Paksoy** conceived the project, developed its narrative and conceptual framework, designed its interactions and sound systems, implemented and integrated the Unity systems, assembled scenes, tested the Quest build, and produced the project documentation.

The project uses third-party Unity asset packages and source recordings as production resources. Their project-specific behavior, interaction logic, procedural music systems, spatialization, scene roles, and narrative integration were authored for **Mind Palace**.

## 12. First-Person Submission Note

**Because Mind Palace is designed as a first-person VR experience, a submitted video can document representative moments but cannot contain the whole work. The Bilgi Learn submission should therefore be understood as evidence and guidance for the experience, while the complete artistic form requires wearing the headset, occupying the player's position, and performing the interactions personally.**


## 13. Conclusion

The first version of the project asked whether imagination could provide freedom from monotonous labor. The final version asks a more complicated question: what happens when the structures of labor enter the imagination itself?

The project answers through a sequence of embodied transformations. Sorting becomes throwing, throwing becomes cleaning, cleaning becomes composition, and composition becomes a clock. Sound connects these transformations and makes the loop perceptible before it becomes explicit.

**Mind Palace** treats escape as real, valuable, and fragile. The player reaches creative agency, but that agency is not outside history, labor, or time. At the end, the factory returns. It sounds different because the player has heard what lies inside its rhythm.

## 14. References

1. Breton, A. (1924). *Manifesto of Surrealism*.
2. Eno, B. (1978). *Ambient 1: Music for Airports*.
3. Marx, K. (1844). *Economic and Philosophic Manuscripts of 1844*.
4. Radiohead and Epic Games. (2021). *Kid A Mnesia Exhibition*.
5. Somer, E. (2002). "Maladaptive Daydreaming: A Qualitative Inquiry." *Journal of Contemporary Psychotherapy*.
6. Unity Technologies. Unity documentation and XR Interaction Toolkit documentation.
7. Meta. Meta Quest developer documentation.
8. von Trier, L. (Director). (2000). *Dancer in the Dark* [Film]. Zentropa Entertainments.
