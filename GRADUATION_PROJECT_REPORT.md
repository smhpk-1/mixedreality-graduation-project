# Mind Palace - MUS 442 Final Submission Dossier

**Author:** Semiha Paksoy

**Project title:** Mind Palace

**Scene 1 title:** The Shift

**Course:** MUS 442 - Senior Project II

**Category:** Sound Installation and Multimedia Project

**Platform:** Meta Quest 3S

**Project supervisor:** Tolga Tüzün

**Date:** June 12, 2026

## Project in One Sentence

A factory worker escapes into a musical daydream, only to build a creative loop that reveals itself as the clock of the shift they escaped.

## Final Thesis

The project began as a contrast between monotonous labor and liberating imagination. Its final form is more ambivalent: capitalism turns everyday life into a managed work-leisure cycle, and the pressure of that mundane, quotidian repetition pushes the protagonist toward maladaptive daydreaming. Imagination creates real agency and relief, but the routines, rewards, and time structures of reality gradually return inside the fantasy.

## Four-Scene Arc

| Scene | Core interaction | Sound role | Narrative role |
|---|---|---|---|
| The Shift | Sort 30 cubes | Labor creates quantized notes inside a machine score | Establish alienation and measured work |
| Colorful Playground | Throw colored sound cubes at walls | Color becomes timbre and gesture becomes performance | Offer creative release |
| The Platform | Clean trash while trains and commuters loop | Station objects and compulsory cleanup form a generative score | Let routine re-enter the daydream |
| Concert / The Loop | Fill a 12-step radial sequencer | Player loop replaces concert, then becomes ticking | Reveal creative repetition as the clock |

## What Changed During MUS 442

- Expanded the project from two scenes to a four-scene circular narrative.
- Added destructible Scene 2 progression.
- Replaced the former Scene 3 direction with a metro station.
- Built autonomous train, sliding-door, NPC wandering, boarding, exiting, and cyclic reset systems.
- Added VR trash cleanup and a following collection cart.
- Added the concert scene, synchronized hybrid stem playback, generated instruments, and stage systems.
- Added generative factory and metro scores tied to player actions and scene objects.
- Added Quest-specific physics, LOD, lighting, shadow, rig, and diagnostic fixes.
- Added procedural street ambience, reliable trash-deposit detection, and a responsive concert audience.
- Designed the radial sequencer clock reveal and second-shift ending.

## Musical Contribution

Sound is the project's main narrative system.

- The factory turns machine recordings and sorted cubes into a DSP-synchronized composition.
- The playground turns colored cubes into free physical instruments.
- The metro turns architecture, NPCs, trains, announcements, and trash collection into one spatial generative ensemble.
- The concert lets the player replace a precomposed spectacle with a personal loop, then transforms that loop into clock time.

**Because Mind Palace is a first-person VR project, video documentation can show the structure but cannot fully substitute for the headset experience. The work has to be experienced personally through the player's embodied position and actions.**

## Technical Contribution

- Unity 6, OpenXR, XR Interaction Toolkit, XR Hands, Android
- Four build scenes
- 69 runtime and 14 editor C# scripts on current `main`
- Approximately 16,000 lines of project C#
- Physics-based VR interaction
- DSP-clock scheduling and spatial audio
- Procedural synthesis and runtime sample slicing
- Hybrid NavMesh and waypoint-chain NPC movement
- Tool-driven scene generation and repair

## Key Presentation Moments

1. Correct and incorrect sorting feedback in the factory.
2. Green anomaly cube transition.
3. Colored cube wall painting and visual room collapse.
4. Metro train arrival, NPC boarding, trash melody, and commuter reset.
5. Sequencer placement and concert ducking.
6. Clock reveal and return to the faster, detuned second shift.



## Documentation Set

- [SENIOR_PROJECT_REPORT.md](SENIOR_PROJECT_REPORT.md) - full final report and process paper
- [PROJECT_DOCUMENTATION.md](PROJECT_DOCUMENTATION.md) - technical specification and development history
- [PROJECT_STORY.md](PROJECT_STORY.md) - narrative treatment
- [STORYBOARD.md](STORYBOARD.md) - final player journey
- [CONCEPTUAL_BACKGROUND.md](CONCEPTUAL_BACKGROUND.md) - conceptual framework
- [README.md](README.md) - public project overview

## Authorship

The project was conceived, designed, implemented, composed, assembled, tested, and documented by **Semiha Paksoy**. Third-party assets are used as production resources; the project-specific systems, interaction design, sound behavior, scene logic, and narrative integration were created for **Mind Palace**.
