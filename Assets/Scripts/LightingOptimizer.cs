using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LightingOptimizer : MonoBehaviour
{
    [Header("Settings")]
    public Color ambientColor = new Color(0.7f, 0.75f, 0.8f); // Cool office grey
    public Color sunColor = new Color(1f, 0.95f, 0.9f); // Warm sunlight
    
    [ContextMenu("Optimize Lighting for Quest 3")]
    public void OptimizeLighting()
    {
        // 1. Global Render Settings (Cheap Ambient)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.fog = false; // Fog can be expensive on mobile VR

        // 2. Configure Lights
        // Find all lights in the scene
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        bool mainLightFound = false;

        foreach (Light l in lights)
        {
            // Reset settings first
            l.shadows = LightShadows.None;

            if (l.type == LightType.Directional)
            {
                if (!mainLightFound)
                {
                    // Main Sun (Key Light)
                    l.gameObject.name = "Main Sun";
                    l.color = sunColor;
                    l.intensity = 1.2f;
                    l.shadows = LightShadows.Hard; // Hard shadows are cheaper than Soft
                    
                    // Set to Mixed mode (requires baking for static objects, realtime for dynamic)
#if UNITY_EDITOR
                    SerializedObject so = new SerializedObject(l);
                    SerializedProperty lightmapProp = so.FindProperty("m_Lightmapping");
                    if (lightmapProp != null)
                    {
                        lightmapProp.intValue = 4; // 4 = Mixed
                        so.ApplyModifiedProperties();
                    }
#endif
                    mainLightFound = true;
                }
                else
                {
                    // Secondary directional lights (Fill Light) -> Make Baked or Disable
                    l.gameObject.name = "Fill Light (Baked)";
                    l.intensity = 0.5f;
                    l.shadows = LightShadows.None;
#if UNITY_EDITOR
                    SerializedObject so = new SerializedObject(l);
                    SerializedProperty lightmapProp = so.FindProperty("m_Lightmapping");
                    if (lightmapProp != null)
                    {
                        lightmapProp.intValue = 2; // 2 = Baked
                        so.ApplyModifiedProperties();
                    }
#endif
                }
            }
            else
            {
                // Point/Spot lights -> Always Baked for performance
                l.shadows = LightShadows.None; 
#if UNITY_EDITOR
                SerializedObject so = new SerializedObject(l);
                SerializedProperty lightmapProp = so.FindProperty("m_Lightmapping");
                if (lightmapProp != null)
                {
                    lightmapProp.intValue = 2; // 2 = Baked
                    so.ApplyModifiedProperties();
                }
#endif
            }
        }

        // 3. Add Light Probe Group 
        // Essential for dynamic objects (cubes, hands) to receive baked lighting info
        GameObject probeObj = GameObject.Find("Room_LightProbes");
        if (probeObj == null)
        {
            probeObj = new GameObject("Room_LightProbes");
            probeObj.transform.position = Vector3.zero;
        }
        
        LightProbeGroup lpg = probeObj.GetComponent<LightProbeGroup>();
        if (lpg == null) lpg = probeObj.AddComponent<LightProbeGroup>();
        
        // 4. Add Reflection Probe
        // Essential for metallic materials (dispenser, clock) to look correct
        GameObject refProbeObj = GameObject.Find("Room_ReflectionProbe");
        if (refProbeObj == null)
        {
            refProbeObj = new GameObject("Room_ReflectionProbe");
            refProbeObj.transform.position = new Vector3(0, 2, 0);
        }
        
        ReflectionProbe rp = refProbeObj.GetComponent<ReflectionProbe>();
        if (rp == null) rp = refProbeObj.AddComponent<ReflectionProbe>();
        
        rp.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
        rp.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
        rp.boxProjection = true; // Better reflections for square rooms
        rp.size = new Vector3(10, 5, 10); // Approximate room size

        Debug.Log("Lighting Optimized! IMPORTANT: Now go to Window > Rendering > Lighting and click 'Generate Lighting' to bake the lightmaps.");
    }
}
