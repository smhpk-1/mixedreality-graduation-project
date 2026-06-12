using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MusicSpace.Editor
{
    /// <summary>
    /// Tools > Scene 4 > Place Band NPCs
    ///
    /// Puts a band on the Scene 4 stage: one humanoid NPC per instrument
    /// (DrumKit, BassGuitar, ElectricGuitar, Synth), using the same
    /// npc_casual_set_00 characters as Scene 3. Each NPC gets an
    /// NPCMusicianPerformer (bone-driven playing animation) and an
    /// NPCBlobShadow for grounding. A stool is added for the drummer.
    ///
    /// Idempotent — re-running replaces the previous band.
    /// </summary>
    public static class Scene4BandSetupTool
    {
        private const string Scene4Path = "Assets/Scene 4.unity";
        private const string BandRootName = "StageBand";
        private const string PrefabFolder = "Assets/npc_casual_set_00/Prefabs/";

        // One distinct character per role
        private class BandMember
        {
            public string instrumentName;
            public NPCMusicianPerformer.Role role;
            public string prefabName;
            public Vector3 localOffset;   // from the instrument, in its local space
            public bool faceInstrument;   // true = look at the instrument, false = same way as it
        }

        private static readonly BandMember[] Members =
        {
            new BandMember { instrumentName = "DrumKit", role = NPCMusicianPerformer.Role.Drummer,
                             prefabName = "npc_csl_00_character_01m_01",
                             localOffset = new Vector3(0f, 0f, -0.85f), faceInstrument = true },
            new BandMember { instrumentName = "ElectricGuitar", role = NPCMusicianPerformer.Role.Guitarist,
                             prefabName = "npc_csl_00_character_02m_02",
                             localOffset = Vector3.zero, faceInstrument = false },
            new BandMember { instrumentName = "BassGuitar", role = NPCMusicianPerformer.Role.Bassist,
                             prefabName = "npc_csl_00_character_01f_02",
                             localOffset = Vector3.zero, faceInstrument = false },
            new BandMember { instrumentName = "Synth", role = NPCMusicianPerformer.Role.Keyboardist,
                             prefabName = "npc_csl_00_character_02f_01",
                             localOffset = new Vector3(0f, 0f, -0.65f), faceInstrument = true },
        };

        [MenuItem("Tools/Scene 4/Place Band NPCs")]
        public static void PlaceBand()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(Scene4Path, OpenSceneMode.Single);

            var old = GameObject.Find(BandRootName);
            if (old != null) Object.DestroyImmediate(old);

            var bandRoot = new GameObject(BandRootName);
            int placed = 0;

            foreach (var member in Members)
            {
                Transform instrument = FindInScene(member.instrumentName);
                if (instrument == null)
                {
                    Debug.LogWarning($"[Scene4Band] Instrument '{member.instrumentName}' not found — skipped.");
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabFolder + member.prefabName + ".prefab");
                if (prefab == null)
                {
                    Debug.LogWarning($"[Scene4Band] Prefab '{member.prefabName}' not found — skipped.");
                    continue;
                }

                var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                npc.name = "Band_" + member.role;
                npc.transform.SetParent(bandRoot.transform, true);

                Vector3 pos = instrument.TransformPoint(member.localOffset);
                pos.y = instrument.position.y; // stage/ground level; runtime raycast refines
                npc.transform.position = pos;
                npc.transform.rotation = member.faceInstrument
                    ? Quaternion.LookRotation(Flat(instrument.position - pos))
                    : Quaternion.Euler(0f, instrument.eulerAngles.y, 0f);

                var performer = npc.AddComponent<NPCMusicianPerformer>();
                performer.role = member.role;
                performer.instrument = instrument;

                if (npc.GetComponent<NPCBlobShadow>() == null)
                    npc.AddComponent<NPCBlobShadow>();

                // Drummer gets a stool to sit on
                if (member.role == NPCMusicianPerformer.Role.Drummer)
                {
                    var stool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stool.name = "DrumStool";
                    stool.transform.SetParent(bandRoot.transform, true);
                    stool.transform.position = pos + Vector3.up * 0.23f;
                    stool.transform.localScale = new Vector3(0.34f, 0.23f, 0.34f);
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                           ?? Shader.Find("Standard"));
                    mat.color = new Color(0.1f, 0.1f, 0.1f);
                    stool.GetComponent<Renderer>().sharedMaterial = mat;
                }

                placed++;
            }

            Undo.RegisterCreatedObjectUndo(bandRoot, "Place Band NPCs");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Stage Band",
                $"{placed}/4 band NPC placed.\n\n" +
                "- Drummer behind the kit (with stool)\n" +
                "- Guitarist/bassist holding their instruments\n" +
                "- Keyboardist behind the synth\n\n" +
                "They play in sync with the concert and wind down as the\n" +
                "player's sequencer replaces the music.\n\n" +
                "Tweak positions/holdOffset in the inspector if a rig sits oddly.",
                "Tamam");
        }

        /// <summary>Find a transform by exact name anywhere in the open scene (inactive included).</summary>
        private static Transform FindInScene(string objectName)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == objectName) return t;
            }
            return null;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 0.001f ? Vector3.forward : v.normalized;
        }

        [MenuItem("Tools/Scene 4/Place Band NPCs", validate = true)]
        public static bool ValidatePlaceBand()
        {
            return System.IO.File.Exists(
                System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), Scene4Path));
        }
    }
}
