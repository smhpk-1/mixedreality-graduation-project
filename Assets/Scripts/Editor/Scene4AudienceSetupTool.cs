using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace MusicSpace.Editor
{
    /// <summary>
    /// Tools > Scene 4 > Place Audience NPCs
    ///
    /// Scatters a concert crowd in front of the stage using the project's
    /// npc_casual_set_00 characters (same set as Scene 3 and the band).
    /// Each NPC gets an NPCAudienceMember (randomized groove/enthusiasm)
    /// and an NPCBlobShadow.
    ///
    /// The crowd zone is computed from the instruments' centroid, pushed
    /// toward the player spawn (terrain center 250,250) — between the
    /// stage and the player. Idempotent: re-running replaces the crowd.
    /// </summary>
    public static class Scene4AudienceSetupTool
    {
        private const string Scene4Path = "Assets/Scene 4.unity";
        private const string CrowdRootName = "ConcertAudience";
        private const string PrefabFolder = "Assets/npc_casual_set_00/Prefabs/";
        private const int CrowdSize = 36;

        // The stage platform extends ~12m from the instruments' centroid toward
        // the audience side — the crowd zone starts safely beyond that edge.
        private const float CrowdStartDistance = 17f;

        private static readonly string[] CharacterPrefabs =
        {
            "npc_csl_00_character_01f_01", "npc_csl_00_character_01f_02",
            "npc_csl_00_character_01f_03", "npc_csl_00_character_01m_01",
            "npc_csl_00_character_01m_02", "npc_csl_00_character_01m_03",
            "npc_csl_00_character_02f_01", "npc_csl_00_character_02f_02",
            "npc_csl_00_character_02f_03", "npc_csl_00_character_02m_01",
            "npc_csl_00_character_02m_02", "npc_csl_00_character_02m_03",
        };

        private static readonly string[] InstrumentNames =
            { "DrumKit", "ElectricGuitar", "BassGuitar", "Synth" };

        [MenuItem("Tools/Scene 4/Place Audience NPCs")]
        public static void PlaceAudience()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(Scene4Path, OpenSceneMode.Single);

            var old = GameObject.Find(CrowdRootName);
            if (old != null) Object.DestroyImmediate(old);

            // Stage center = instruments' centroid
            Vector3 stageCenter = Vector3.zero;
            int found = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (var n in InstrumentNames)
                    if (t.name == n) { stageCenter += t.position; found++; }
            }
            if (found == 0)
            {
                EditorUtility.DisplayDialog("Audience",
                    "No instruments found in Scene 4 (DrumKit/Guitars/Synth) — cannot place the crowd.",
                    "Tamam");
                return;
            }
            stageCenter /= found;

            // Crowd zone: IN FRONT of the stage, beyond the platform edge,
            // between the stage and the player spawn (terrain center)
            Vector3 spawn = new Vector3(250f, stageCenter.y, 250f);
            Vector3 toSpawn = spawn - stageCenter;
            toSpawn.y = 0f;
            toSpawn = toSpawn.sqrMagnitude < 1f ? Vector3.back : toSpawn.normalized;
            Vector3 crowdCenter = stageCenter + toSpawn * CrowdStartDistance;

            var crowdRoot = new GameObject(CrowdRootName);
            var rng = new System.Random(1973);

            for (int i = 0; i < CrowdSize; i++)
            {
                string prefabName = CharacterPrefabs[i % CharacterPrefabs.Length];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabFolder + prefabName + ".prefab");
                if (prefab == null) continue;

                var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                npc.name = "Audience_" + i;
                npc.transform.SetParent(crowdRoot.transform, true);

                // Loose arcs: rows of 9 fan out AWAY from the stage, jittered.
                // Front row is widest-energy; rows behind spread slightly more.
                int row = i / 9;
                float across = ((i % 9) - 4f) * 1.7f * (1f + row * 0.12f) + Jitter(rng, 0.6f);
                float back = row * 2.4f + Jitter(rng, 0.8f);
                Vector3 right = Vector3.Cross(Vector3.up, -toSpawn);
                Vector3 pos = crowdCenter + right * across + toSpawn * back;
                pos.y = stageCenter.y; // runtime raycast refines
                npc.transform.position = pos;

                var member = npc.AddComponent<NPCAudienceMember>();
                member.stageFocus = stageCenter;
                member.groove = 1.7f + (float)rng.NextDouble() * 0.9f;
                member.enthusiasm = 0.55f + (float)rng.NextDouble() * 0.45f;
                member.armsUpEvery = 10f + (float)rng.NextDouble() * 12f;

                if (npc.GetComponent<NPCBlobShadow>() == null)
                    npc.AddComponent<NPCBlobShadow>();
            }

            Undo.RegisterCreatedObjectUndo(crowdRoot, "Place Audience NPCs");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Concert Audience",
                $"{CrowdSize} audience NPC placed in front of the stage " +
                $"(crowd starts {CrowdStartDistance}m from the instruments — off the platform).\n\n" +
                "- They dance to the concert (bounce, sway, arms up)\n" +
                "- As the player's sequencer takes over, they turn toward it\n" +
                "- The clock reveal freezes them with the band",
                "Tamam");
        }

        private static float Jitter(System.Random rng, float range)
            => ((float)rng.NextDouble() * 2f - 1f) * range;

        [MenuItem("Tools/Scene 4/Place Audience NPCs", validate = true)]
        public static bool ValidatePlaceAudience()
        {
            return System.IO.File.Exists(
                System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), Scene4Path));
        }
    }
}
