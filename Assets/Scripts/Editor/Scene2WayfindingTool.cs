using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MusicSpace.Editor
{
    /// <summary>
    /// Tools > Scene 2 > Add Metro Wayfinding Signs
    ///
    /// The city street has a METRO sign only at the entrance itself (z≈48) —
    /// nothing guides the player from the collapsed room (origin) down the
    /// street. This tool plants a line of glowing metro wayfinding signs
    /// along the walk: blue emissive panel, white M + METRO text, and a
    /// built-up arrow pointing AHEAD (toward the entrance), alternating
    /// street sides every 12m.
    ///
    /// Sign roots are named "StandingSign" so StreetAmbienceDirector's
    /// wind-creak automatically attaches to them at runtime.
    ///
    /// Idempotent — re-running replaces the previous set.
    /// </summary>
    public static class Scene2WayfindingTool
    {
        private const string Scene2Path = "Assets/Scene 2.unity";
        private const string RootName = "MetroWayfinding";

        // Visual language copied from the entrance sign (MetroEntranceCityGenerator)
        private static readonly Color SignBg   = new Color(0.05f, 0.1f, 0.5f);
        private static readonly Color SignGlow = new Color(0.2f, 0.4f, 1f) * 1.2f;
        private static readonly Color PoleCol  = new Color(0.35f, 0.35f, 0.38f);

        [MenuItem("Tools/Scene 2/Add Metro Wayfinding Signs")]
        public static void AddSigns()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(Scene2Path, OpenSceneMode.Single);

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            // Street axis: room at origin → metro entrance. Found in scene if present.
            Vector3 entrance = new Vector3(0f, 0.1f, 48f);
            var entranceT = FindInScene("MetroEntrance");
            if (entranceT != null) entrance = entranceT.position;

            Vector3 ahead = entrance; ahead.y = 0f;
            ahead = ahead.sqrMagnitude < 1f ? Vector3.forward : ahead.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, ahead);

            var root = new GameObject(RootName);

            // Four signs: guidance starts right outside the collapse zone
            float[] distances = { 6f, 18f, 30f, 42f };
            for (int i = 0; i < distances.Length; i++)
            {
                float side = (i % 2 == 0) ? -3.5f : 3.5f;
                Vector3 pos = ahead * distances[i] + right * side;
                pos.y = 0f;
                BuildSign(root.transform, pos, Quaternion.LookRotation(ahead), i);
            }

            Undo.RegisterCreatedObjectUndo(root, "Add Metro Wayfinding Signs");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Metro Wayfinding",
                $"{distances.Length} wayfinding signs placed along the street " +
                "(alternating sides, every 12m, arrow pointing toward the metro).\n\n" +
                "Sign roots are named 'StandingSign' — they automatically get the\n" +
                "wind-creak sound from StreetAmbienceDirector at runtime.",
                "Tamam");
        }

        /// <summary>One standing sign: pole, glowing panel facing the walker, M + METRO + ahead-arrow.</summary>
        private static void BuildSign(Transform parent, Vector3 pos, Quaternion rot, int index)
        {
            // Root named StandingSign → StreetAmbienceDirector attaches the creak
            var sign = new GameObject("StandingSign");
            sign.transform.SetParent(parent, false);
            sign.transform.position = pos;
            sign.transform.rotation = rot;

            // Pole
            Box(sign.transform, "Pole", new Vector3(0f, 1.3f, 0f),
                new Vector3(0.08f, 2.6f, 0.08f), PoleCol, Color.black);

            // Panel — faces BACK along the street (toward approaching walkers)
            Box(sign.transform, "Panel", new Vector3(0f, 2.35f, 0f),
                new Vector3(0.85f, 0.95f, 0.06f), SignBg, SignGlow * 0.6f);

            // Text & arrow parented to the SIGN root (not the panel) — the panel's
            // non-uniform scale would skew rotated children. Front face is -Z.
            Text(sign.transform, "M_Text", new Vector3(0f, 2.52f, -0.045f), "M", 0.16f, 90);
            Text(sign.transform, "Metro_Text", new Vector3(0f, 2.24f, -0.045f), "METRO", 0.055f, 80);

            // Ahead-arrow built from boxes (reliable on any font): shaft + two head fins.
            // Points UP on the panel = "straight ahead" in standard wayfinding.
            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(sign.transform, false);
            arrow.transform.localPosition = new Vector3(0f, 2.03f, -0.045f);
            Box(arrow.transform, "Shaft", new Vector3(0f, -0.02f, 0f),
                new Vector3(0.04f, 0.17f, 0.02f), Color.white, Color.white * 0.8f);
            var finL = Box(arrow.transform, "FinL", new Vector3(-0.045f, 0.045f, 0f),
                new Vector3(0.04f, 0.12f, 0.02f), Color.white, Color.white * 0.8f);
            finL.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            var finR = Box(arrow.transform, "FinR", new Vector3(0.045f, 0.045f, 0f),
                new Vector3(0.04f, 0.12f, 0.02f), Color.white, Color.white * 0.8f);
            finR.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private static GameObject Box(Transform parent, string name, Vector3 localPos,
                                      Vector3 size, Color color, Color emission)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (emission.maxColorComponent > 0.01f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void Text(Transform parent, string name, Vector3 localPos,
                                 string text, float charSize, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // face -Z side

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.color = Color.white;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = fontSize;
            tm.characterSize = charSize;
        }

        private static Transform FindInScene(string objectName)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == objectName) return t;
            return null;
        }

        [MenuItem("Tools/Scene 2/Add Metro Wayfinding Signs", validate = true)]
        public static bool ValidateAddSigns()
        {
            return System.IO.File.Exists(
                System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), Scene2Path));
        }
    }
}
