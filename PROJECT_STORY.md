# Project Story: Mixed Reality Graduation Project

## Overview
This document outlines the narrative structure and interactive story progression for the Mixed Reality Graduation Project. The story unfolds across multiple scenes, each designed to immerse the user in a surreal, evolving environment where their actions drive both the narrative and the experience.

---

## Scene 1: The Shift

**The Shift** is an industrial prologue representing the monotony and alienation of the capitalist system. The player is cast as a "worker," isolated from the outside world in a dim, oppressive factory where time is tracked only by a mechanical wall clock.

### Story Beats
- **The Arrival:** The player awakens at the head of a conveyor belt in a gloomy, claustrophobic factory. The environment is filled with industrial sounds, dim lighting, and a sense of isolation.
+- **The Routine:** Red and blue cubes begin to fall from ceiling pipes (dispensers) onto the conveyor. The player's task is to quickly grab and sort these cubes into the correct color-coded bins. In total, 30 red and blue cubes are spawned and sorted.
+- **The Grind:** Reports on the walls and the relentless ticking of the wall clock reinforce the feeling of endless, repetitive labor. The cycle seems unbreakable.
+- **The Anomaly:** After the 30th cube is sorted, the machinery halts. Three glowing green "Anomaly Cubes" appear on the belt, breaking the monotony and drawing the player's attention.
+- **The Leap:** Touching any of the anomaly cubes causes the factory reality to shatter, triggering a transition to the surreal universe of Scene 2.

### Interaction
- Grab and sort cubes using hand tracking or controllers.
- Place cubes into the correct bins (red or blue).
- Interact with the anomaly cube to progress.

### Audio
- Each action (grabbing, sorting, dropping) triggers unique audio cues.
- The environment features mechanical ambience, ticking clocks, and a dramatic audio shift when the anomaly appears.

### Purpose
- Establishes the core mechanics and the oppressive, rule-driven logic of the world.
- Uses narrative and symbolism to critique capitalist alienation and foreshadow the surreal journey ahead.

---


## Scene 2: The Colorful Playground

**The Awakening**
After touching the Anomaly Cube, the player is transported from the dark factory into a bright, surreal playground—a physical manifestation of the daydreaming mind.

### Setting
- A spacious room with reactive walls made of different materials (Metal, Concrete, Wood, Glass, Stone floor)
- 20 large, colorful cubes scattered across the floor in a grid pattern
- 5 colors: Red, Blue, Green, Yellow, Purple (4 cubes each)
- Bright, ambient lighting contrasting the factory's darkness

### Objective
The player is free to explore and interact without rules or quotas. The goal is pure creative expression:
- Grab any cube using hand tracking or controllers
- Throw cubes at the surrounding walls
- Watch as walls transform to match the cube's color
- Create your own color patterns and compositions

### Interaction
- **Grab & Throw:** All 20 cubes can be grabbed and thrown with realistic physics
- **Wall Reaction:** When a cube hits a wall, the wall instantly changes to the cube's color
- **Color Persistence:** Wall colors persist until hit by another cube
- **Cube Respawn:** After hitting a wall, cubes respawn at their original position after a short delay
- **Physics:** Cubes bounce realistically off surfaces before triggering color changes

### Audio
- Each cube color produces a unique sound when colliding with surfaces
- Impact sounds are dynamically modulated by collision velocity
- Ambient soundscape reflecting the surreal, dreamlike atmosphere

### Purpose
- Represents the freedom and boundlessness of imagination
- Contrasts the rigid, rule-driven factory of Scene 1
- Allows players to "paint" their environment through play
- Symbolizes breaking free from capitalist constraints through creative expression

---

## Scene 3: The Platform

**The Platform** is the daydream's middle distance — no longer the factory, not yet the spectacle. An underground metro station where the fantasy starts to feel suspiciously ordinary.

### Setting
- An underground metro platform: benches, fluorescent lights, PA speakers, a cleaning cart
- A subway train that arrives, opens its doors, exchanges passengers, and departs in an endless cycle
- Commuter NPCs that wander the platform and board/exit the train, reappearing at their original positions each cycle

### Story Beats
- **The Arrival:** The player lands in a quieter register of the daydream — a liminal, transitional space.
- **The Chore:** Litter is scattered across the platform. The player can collect it and toss it into the cleaning cart — voluntarily performing janitorial labor inside their own fantasy. The escape has started reproducing work.
- **The Cycle:** The train returns again and again; the same passengers board, leave, and reappear. The loop is now visible in the world itself, for anyone willing to see it.
- **The Way Out:** An anomalous cube — the same impossible object that broke the factory — carries the player onward to the concert.

### Interaction
- Grab trash and deposit it into the cart (each piece plays a quantized melody note)
- Observe the autonomous train and NPC boarding cycle
- Grab the transition cube to progress

### Audio
- A generative ambient score emerges from the station's own objects — bench tape-loop pads, fluorescent hum drones, rail ostinatos, trash-can tick patterns, NPC hum/murmur voices — all locked to a single musical grid in A minor pentatonic
- PA speakers play announcements tied to train arrivals and departures

### Purpose
- Marks the daydream's quiet decay: the fantasy now contains waiting, routine, and chores
- Plants the loop motif (the train cycle) that Scene 4 will close

---

## Scene 4: The Concert / The Loop

**The Concert** is the daydream's climax and its trap. The spectacle the whole escape was promising — and the place where the player builds their own way back to the factory.

### Setting
- An open-air concert at night: stage, speaker stacks, drum kit, instruments
- Hybrid stem-based audio: some layers 2D stereo in the headset, some 3D spatial from stage objects, all DSP-clock synchronized
- Near the stage: a floating radial sequencer — a luminous ring with twelve slots and a sweeping playhead — and twelve glowing sample orbs hovering in front of it

### Story Beats
- **The Spectacle:** The player arrives inside the reward — music everywhere, no tasks, no rules.
- **The Instrument:** The floating ring invites play. The player grabs sample orbs (each humming its own synth voice) and places them into the slots. Every placed tone joins the loop, and with each filled slot the concert's music yields — the player's own loop is gradually replacing the band.
- **The Masterpiece:** With all twelve slots filled, only the player's creation plays. They have composed the scene's finale themselves.
- **The Recognition:** The loop slows. Colors drain to a white face and black rim; numbers 1–12 surface over the filled slots; the playhead thins into a red second hand; frozen hands appear at 9:00 — shift start. The sequencer always had twelve slots. It was always the clock. The player's tones collapse one by one into a mechanical tick, and the orbs can no longer be taken back.
- **The Return:** Darkness closes in around the ticking, and the player wakes at the factory workstation. The shift begins again — the machines hum slightly out of tune now, the belt runs a touch faster. The loop is the same. The loop is never the same.

### Interaction
- Grab and place sample orbs into the sequencer slots (controllers, XR Interaction Toolkit)
- Rearrange or remove orbs freely — until the reveal, when the loop stops belonging to the player
- The reveal triggers after the completed loop plays through in full

### Audio
- Concert stems duck progressively as slots fill — creative agency literally displaces the spectacle
- Sequencer tones are procedurally synthesized (six timbres in A minor pentatonic) and quantized to a DSP-clock grid
- The reveal decelerates the loop to exactly one step per second and replaces every tone with a tick — the project's central image rendered in sound

### Purpose
- Closes the narrative circle: factory → daydream → creative release → clock → factory
- Argues the project's thesis in mechanics rather than words: even escape and creativity, once made circular and productive, become another clock — and the player builds it with their own hands

---

## Audio & Story Progression
- Audio is tightly integrated with the story, evolving with the user's actions and the progression of scenes.
- Each scene features unique soundscapes and interaction-driven audio feedback, enhancing immersion and narrative flow.
- On the return to Scene 1, the factory score is subtly degraded (detuned tape loops, a slightly faster pulse) — the loop never repeats exactly.

---

*This story outline is a living document. Please provide new story elements or scene ideas as they arise, and they will be incorporated here.*
