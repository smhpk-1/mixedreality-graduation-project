# The Shift

**The Shift** is a sound-centered virtual reality narrative for Meta Quest 3S. It explores maladaptive daydreaming, capitalist alienation, routine, and creative agency through a circular journey across four interactive scenes.

The project began under the working title **Mind Palace** as a two-scene contrast between an oppressive factory and a liberating playground. During MUS 442 Senior Project II, it developed into a more ambivalent story: the daydream first offers freedom, then gradually reproduces chores, repetition, spectacle, and time. In the finale, the musical loop created by the player reveals itself as the factory clock and returns the player to the beginning.

## Project Identity

- **Author:** Semiha Paksoy
- **Course:** MUS 442 - Senior Project II
- **Academic year:** 2025-2026
- **Category:** Sound Installation and Multimedia Project
- **Platform:** Meta Quest 3S, Android
- **Engine:** Unity 6 (`6000.2.10f1`)
- **XR stack:** OpenXR, XR Interaction Toolkit, XR Hands, Unity Input System
- **Render pipeline:** Universal Render Pipeline
- **Primary build:** `theshift1.apk`
- **Repository:** <https://github.com/smhpk-1/mixedreality-graduation-project>

## Core Idea

The player begins as a worker sorting cubes under a quota. An impossible cube interrupts the system and opens a daydream. The same basic gestures are then transformed across the experience:

| Scene | Player action | Meaning |
|---|---|---|
| Factory | Sort cubes correctly | Labor, conformity, measured output |
| Playground | Throw cubes to paint and sound the room | Release, play, creative agency |
| Metro | Collect trash while watching commuters loop | Routine returning inside fantasy |
| Concert | Build a musical loop that becomes a clock | Creativity captured by repetition |

The experience does not present daydreaming as simply good or bad. It treats escape as necessary and imaginative, while asking what happens when escape becomes another closed loop.

## Scene Flow

All four scenes are enabled in build settings and live at the root of `Assets/`.

### 1. `Assets/scene1.unity` - The Shift

The player sorts a run of 30 red and blue cubes from a conveyor belt into matching bins. Correct and incorrect sorting actions produce distinct quantized musical feedback. After the spawner produces the 30th standard cube, green anomaly cubes appear; touching one opens the daydream.

The factory's generative score is built at runtime from machine recordings. A mechanical ostinato, spatial tape-loop layers, steam breaths, and the player's sorting notes share one DSP-clock grid.

### 2. `Assets/Scene 2.unity` - The Colorful Playground

Twenty colored cubes replace the factory's restricted red/blue system. Each color has a synth voice. Grabbing and throwing cubes creates sound and repaints reactive walls. After 20 total wall hits, the room collapses and the player enters the metro.

The scene reframes the cube from a product to be classified into an instrument for play. When the room collapses, repaired street sources, procedural city rumble, and passing-car sounds open the enclosed playground into the city.

### 3. `Assets/Scene 3.unity` - The Platform

A subway train arrives, opens its sliding doors, exchanges passengers, departs, and returns. NPCs wander using NavMesh, then use authored waypoint chains to board and exit the train. They reappear at their original positions so the commuter cycle can repeat.

The player can collect 20 pieces of trash with a following cleanup cart. Its generated interior trigger accepts only previously grabbed, released trash and uses continuous collision detection to make deposits reliable. Each collected item adds a quantized note to the metro score. A transition cube also exists as an alternate progression route.

### 4. `Assets/Scene 4.unity` - The Concert / The Loop

The player enters an outdoor concert with synchronized stereo and spatial stems, a performing band, and a responsive audience. The final interaction is a twelve-slot radial sequencer. The player places sound orbs into the ring, gradually replacing the concert with their own loop as the band slows and the crowd turns toward the player.

When the loop is complete, the sequencer slows, turns into a clock face at 9:00, and replaces the player's tones with mechanical ticking. The experience returns to the factory for a subtly faster and detuned second shift.

## Sound as Structure

Sound is not an added layer; it carries the narrative.

- **Scene 1:** labor becomes a quantized melody inside a machine-derived generative score.
- **Scene 2:** color becomes timbre and physical play becomes performance.
- **Scene 3:** station objects, commuters, announcements, trains, and collected trash form one spatial composition.
- **Scene 4:** the player's loop competes with the concert, then becomes the clock that closes the story.

The factory and metro systems use sample-accurate DSP scheduling, pentatonic pitch constraints, procedural synthesis, spatial sources, and deliberately incommensurate loop periods inspired by Brian Eno's tape-loop methods.

## Technical Highlights

- Physics-based grab, throw, collision, sorting, cleanup, and placement interactions
- Controller and hand-tracking support through the XR Interaction Toolkit
- Procedural runtime generators and editor setup tools
- DSP-clock synchronized generative and stem-based audio systems
- Autonomous train, sliding doors, commuter wandering, boarding, exiting, and cyclic reset
- Quest-focused fixes for physics, rig alignment, NPC LOD behavior, lighting, and blob shadows
- Haptic placement feedback and progressive audio ducking in the concert finale
- Responsive concert band and audience behavior linked to the finale
- Four-scene Android build flow with a complete circular narrative

## Important Finalization Note

The Scene 4 radial sequencer, performing band, and audience systems are implemented in runtime scripts and editor setup tools. They must be installed into and verified in the serialized Scene 4 using `Tools > Scene 4 > Add Radial Sequencer`, `Place Band NPCs`, and `Place Audience NPCs` before the final APK is considered presentation-ready.

The serialized Scene 1 and Scene 2 files also contain active direct-transition cubes that bypass their intended tasks. These should be deliberately kept as presentation shortcuts or disabled for the final narrative build.

The remaining final polish items are tracked in [PROJECT_DOCUMENTATION.md](PROJECT_DOCUMENTATION.md) and [MUS442_SUBMISSION_CHECKLIST.md](MUS442_SUBMISSION_CHECKLIST.md).

## Documentation Map

- [SENIOR_PROJECT_REPORT.md](SENIOR_PROJECT_REPORT.md) - authoritative MUS 442 final paper
- [GRADUATION_PROJECT_REPORT.md](GRADUATION_PROJECT_REPORT.md) - concise final submission dossier
- [PROJECT_DOCUMENTATION.md](PROJECT_DOCUMENTATION.md) - technical design, implementation, and development history
- [PROJECT_STORY.md](PROJECT_STORY.md) - complete narrative treatment
- [STORYBOARD.md](STORYBOARD.md) - scene-by-scene player experience
- [CONCEPTUAL_BACKGROUND.md](CONCEPTUAL_BACKGROUND.md) - theoretical framework and symbolism
- [MUS442_SUBMISSION_CHECKLIST.md](MUS442_SUBMISSION_CHECKLIST.md) - final delivery and presentation checklist
- `PresentationMaterials/` - archived MUS 441 first-semester submission materials

## Opening the Project

1. Install Unity `6000.2.10f1` with Android Build Support.
2. Clone the repository and pull Git LFS assets.
3. Open the project from Unity Hub.
4. Confirm OpenXR is enabled for Android and Meta Quest support is active.
5. Confirm the four root-level scene files are enabled in Build Settings.
6. Run the Scene 4 setup tools and verify the full sequence in the Unity Editor.
7. Build and test on Meta Quest 3S.

## References

- Somer, E. (2002). "Maladaptive Daydreaming: A Qualitative Inquiry."
- Marx, K. (1844). *Economic and Philosophic Manuscripts of 1844*.
- Breton, A. (1924). *Manifesto of Surrealism*.
- Eno, B. (1978). *Ambient 1: Music for Airports*.
- Radiohead and Epic Games. (2021). *Kid A Mnesia Exhibition*.

---

The player escapes the clock, composes a loop, and discovers that the loop was the clock.
