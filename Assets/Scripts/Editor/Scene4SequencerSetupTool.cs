using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MusicSpace.Editor
{
    /// <summary>
    /// Tools > Scene 4 > Add Radial Sequencer
    ///
    /// Adds the complete radial sequencer rig to Scene 4: the ring with
    /// RadialSequencer + SequencerFinaleDirector, and 12 grabbable sample
    /// orbs floating in front of it. Idempotent — re-running rebuilds the
    /// rig in place.
    ///
    /// The rig snaps itself to the terrain surface at runtime, so the Y
    /// placed here only needs to be roughly right.
    /// </summary>
    public static class Scene4SequencerSetupTool
    {
        private const string Scene4Path = "Assets/Scene 4.unity";
        private const string RigName = "RadialSequencerRig";

        // Player spawns at terrain center (250, ~, 250) facing +Z by default.
        // Place the rig a few meters ahead; its front (-Z) faces back at the spawn.
        private static readonly Vector3 RigPosition = new Vector3(250f, 30f, 256f);

        [MenuItem("Tools/Scene 4/Add Radial Sequencer")]
        public static void AddSequencer()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(Scene4Path, OpenSceneMode.Single);

            // Remove a previous rig so the tool can be re-run safely
            var old = GameObject.Find(RigName);
            if (old != null) Object.DestroyImmediate(old);

            // ── Rig root: sequencer ring + finale director ──────────────
            var rig = new GameObject(RigName);
            rig.transform.position = RigPosition;
            rig.transform.rotation = Quaternion.identity; // front (-Z) faces the spawn

            var sequencer = rig.AddComponent<RadialSequencer>();
            rig.AddComponent<SequencerFinaleDirector>();
            sequencer.BuildVisuals(); // visible in the editor right away

            // ── 12 sample orbs in two loose arcs between player and ring ─
            for (int i = 0; i < 12; i++)
            {
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "SampleOrb_" + i;
                orb.transform.SetParent(rig.transform, false);

                // Two rows of six, fanned in front of the ring at hand height
                int row = i / 6;
                float angle = Mathf.Lerp(-55f, 55f, (i % 6) / 5f) * Mathf.Deg2Rad;
                float dist = 1.6f + row * 0.7f;
                orb.transform.localPosition = new Vector3(
                    Mathf.Sin(angle) * dist,
                    1.0f + (i % 3) * 0.25f,
                    -Mathf.Cos(angle) * dist); // front side of the ring
                orb.transform.localScale = Vector3.one * 0.12f;

                var rb = orb.AddComponent<Rigidbody>();
                rb.mass = 0.3f;
                rb.useGravity = false;
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                var grab = orb.AddComponent<XRGrabInteractable>();
                grab.movementType = XRGrabInteractable.MovementType.VelocityTracking;
                grab.throwOnDetach = false;
                grab.forceGravityOnDetach = false;
                grab.interactionLayers = -1;

                var col = orb.GetComponent<Collider>();
                if (col != null && !grab.colliders.Contains(col))
                    grab.colliders.Add(col);

                var sample = orb.AddComponent<SequencerSampleOrb>();
                sample.orbIndex = i;
            }

            Undo.RegisterCreatedObjectUndo(rig, "Add Radial Sequencer");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Scene4Sequencer] Radial sequencer rig added at " + RigPosition);
            EditorUtility.DisplayDialog("Radial Sequencer",
                "Sequencer rig added to Scene 4.\n\n" +
                "- Ring + finale director at terrain center, 6m ahead of spawn\n" +
                "- 12 grabbable sample orbs in front of it\n" +
                "- Snaps to terrain height at runtime\n\n" +
                "Fill all 12 slots and let the loop play to trigger the clock reveal.",
                "Tamam");
        }

        [MenuItem("Tools/Scene 4/Add Radial Sequencer", validate = true)]
        public static bool ValidateAddSequencer()
        {
            return System.IO.File.Exists(
                System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), Scene4Path));
        }
    }
}
