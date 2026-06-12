# MUS 442 Final Submission Checklist

This file translates the official 2025-2026 MUS 441/MUS 442 guidelines into a project-specific finalization checklist for **The Shift**.

## Guideline Requirements

The official guideline states that the MUS 442 submission must include:

- the final version of the senior project;
- recordings and notation, where applicable;
- a paper describing the whole senior project process;
- a final report with clear project details, including who did what;
- the required standard and duration for the selected project category.

**Selected category:** Sound Installation and Multimedia Project

**Guideline duration:** Approximately 15 minutes, if applicable.

The authoritative whole-process paper is [SENIOR_PROJECT_REPORT.md](SENIOR_PROJECT_REPORT.md).

## Markdown Knowledge Base

- [x] Final project overview and documentation map in `README.md`
- [x] Complete narrative treatment in `PROJECT_STORY.md`
- [x] Complete four-scene storyboard in `STORYBOARD.md`
- [x] Updated theoretical framework in `CONCEPTUAL_BACKGROUND.md`
- [x] Detailed implementation and development history in `PROJECT_DOCUMENTATION.md`
- [x] Final MUS 442 paper in `SENIOR_PROJECT_REPORT.md`
- [x] Concise jury-facing dossier in `GRADUATION_PROJECT_REPORT.md`
- [x] First-semester MUS 441 materials preserved in `PresentationMaterials/`
- [x] Stable Unity `.meta` files committed for every current C# script

## Final Project Verification

- [ ] Open the project in Unity `6000.2.10f1` and resolve all compile errors.
- [ ] Pull all Git LFS assets before building.
- [ ] Confirm all four root-level scenes are enabled and ordered correctly in Build Settings.
- [ ] Decide whether Scene 1 should reach the anomaly after 30 spawned cubes or 30 correct sorts.
- [ ] Verify the selected Scene 1 progression rule.
- [ ] Remove, disable, or intentionally retain the active Scene 1 direct-transition cube.
- [ ] Verify Scene 2 collapses after 20 total wall hits and loads Scene 3.
- [ ] Verify Scene 2 street ambience opens after the room collapse without repeating short voice or cat clips unnaturally.
- [ ] Remove, disable, or intentionally retain the active Scene 2 direct-transition cube.
- [ ] Verify Scene 3 trash objects can be grabbed and the cart counts only released, previously grabbed trash.
- [ ] Verify Scene 3 loads Scene 4 after 20 collected items.
- [ ] Verify the Scene 3 transition cube remains usable as an alternate route.
- [ ] Run `Tools > Scene 4 > Add Radial Sequencer` and save Scene 4.
- [ ] Run `Tools > Scene 4 > Place Band NPCs` and save Scene 4.
- [ ] Run `Tools > Scene 4 > Place Audience NPCs` and save Scene 4.
- [ ] Confirm the Scene 4 sequencer, band, and audience appear in the serialized scene.
- [ ] Verify all twelve sequencer orbs can be placed, removed, and rearranged before the reveal.
- [ ] Verify concert stems duck as slots fill.
- [ ] Verify the audience turns toward the sequencer and freezes during the clock reveal.
- [ ] Verify the clock reveal returns to `scene1`.
- [ ] Verify the second shift is faster and slightly detuned.
- [ ] Test the complete arc on Meta Quest 3S.
- [ ] Build the final signed or presentation APK.

## Audio Finalization

- [ ] Balance perceived loudness across all four scenes.
- [ ] Check that Scene 1 confirmation and error tones remain distinct inside the full factory mix.
- [ ] Check Scene 2 color sounds for equal perceived loudness and clean release fades.
- [ ] Check Scene 3 PA speech intelligibility and metro score density on the headset.
- [ ] Replace or improve Scene 4 drum sounds.
- [ ] Check concert stem synchronization and spatial source positions.
- [ ] Confirm the final clock tick is clearly audible without being uncomfortable.
- [x] Document the procedural score design as the project's notation-like musical system map (`PresentationMaterials/MUS442_Musical_System_Map.pdf`).

## Required Submission Materials Outside Markdown

The user requested this pass without generating website, PDF, or PNG outputs. The following items still need to exist for the actual course submission:

- [ ] Final playable project or APK
- [ ] Approximately 15-minute documentation or presentation recording, if required by the jury
- [ ] Representative audio recording or screen capture of the full experience
- [x] Any notation, diagram, or musical system documentation requested by the advisor (`PresentationMaterials/MUS442_Musical_System_Map.pdf` + `.docx`, diagram sources in `PresentationMaterials/diagrams/`)
- [x] Final presentation material in the format requested by the department (`PresentationMaterials/MUS442_Final_Report.pdf` + `.docx`, `PresentationMaterials/MUS442_Final_Presentation.pptx`) — pending advisor approval of the report

## Suggested Jury Demonstration Order

1. State the evolved thesis: the daydream begins as escape and ends by rebuilding the clock.
2. Demonstrate the factory task and musical feedback.
3. Show the playground's color-sound interaction and collapse.
4. Show the metro train loop and musical trash collection.
5. Show the concert sequencer replacing the band.
6. End on the clock reveal and second shift.
7. Explain the technical approach, device-specific challenges, and second-semester development.

## Authorship

The project was conceived, designed, implemented, composed, assembled, tested, and documented by **Semiha Paksoy**. Third-party Unity asset packages and audio source materials are used as production resources; their behavior, scene integration, interaction logic, generative music systems, and narrative role were authored for this project.
