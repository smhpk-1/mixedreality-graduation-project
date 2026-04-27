using UnityEngine;

namespace MusicSpace
{
    /// <summary>
    /// One-time setup helper. Add this component to the existing "Elevator" GameObject in the scene.
    /// Right-click the component header → "Setup Call Button".
    /// After running, remove this component — it is no longer needed.
    /// </summary>
    public class ElevatorButtonSetup : MonoBehaviour
    {
        [ContextMenu("Setup Call Button")]
        private void SetupCallButton()
        {
            // ── 1. Remove old proximity trigger (auto-open on approach) ──────────
            Transform proxTriggerObj = transform.Find("ElevatorProximityTrigger");
            if (proxTriggerObj != null)
            {
                ElevatorProximityTrigger proxScript = proxTriggerObj.GetComponent<ElevatorProximityTrigger>();
                if (proxScript != null)
                    DestroyImmediate(proxScript);
                // Keep the GameObject but disable its collider so it no longer fires
                BoxCollider bc = proxTriggerObj.GetComponent<BoxCollider>();
                if (bc != null)
                    bc.enabled = false;
                Debug.Log("[ElevatorButtonSetup] ElevatorProximityTrigger disabled.");
            }
            else
            {
                Debug.Log("[ElevatorButtonSetup] No ElevatorProximityTrigger found — skipping removal.");
            }

            // ── 2. Skip if call button already exists ────────────────────────────
            if (transform.Find("ElevatorCallButton") != null)
            {
                Debug.LogWarning("[ElevatorButtonSetup] ElevatorCallButton already exists. Remove it first if you want to recreate it.");
                return;
            }

            // ── 3. Determine elevator width from LeftDoor scale ─────────────────
            float elevWidth = 2f; // fallback
            Transform leftDoor = transform.Find("LeftDoor");
            if (leftDoor != null)
                elevWidth = leftDoor.localScale.x * 2f;

            Color buttonColor = new Color(0.8f, 0.7f, 0.1f);

            // ── 4. Button panel (mount plate) ────────────────────────────────────
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "CallButtonPanel";
            panel.transform.parent = transform;
            // Position: right of door opening, front face of elevator, eye level
            panel.transform.localPosition = new Vector3(elevWidth / 2f + 0.08f, 1.2f, -0.02f);
            panel.transform.localRotation = Quaternion.identity;
            panel.transform.localScale = new Vector3(0.14f, 0.22f, 0.04f);
            ApplyMaterial(panel, new Color(0.25f, 0.25f, 0.27f), 0.5f, 0.6f);

            // ── 5. Button cylinder ───────────────────────────────────────────────
            GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btn.name = "ElevatorCallButton";
            btn.transform.parent = transform;
            // Stick out in front of the panel toward the player (−Z in elevator local space)
            btn.transform.localPosition = new Vector3(elevWidth / 2f + 0.08f, 1.2f, -0.07f);
            btn.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            btn.transform.localScale = new Vector3(0.07f, 0.025f, 0.07f);
            ApplyMaterial(btn, buttonColor, 0f, 0.3f, buttonColor, 2.5f);
            btn.AddComponent<ElevatorCallButton>();

            Debug.Log("[ElevatorButtonSetup] Call button created. You can now remove this ElevatorButtonSetup component.");

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        private static void ApplyMaterial(GameObject obj, Color color, float metallic = 0f, float smoothness = 0.3f, Color? emissionColor = null, float emissionIntensity = 0f)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r == null) return;
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (emissionColor.HasValue && emissionIntensity > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor.Value * emissionIntensity);
            }
            r.sharedMaterial = mat;
        }
    }
}
