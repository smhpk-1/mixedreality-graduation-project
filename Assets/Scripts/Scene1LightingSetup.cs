using UnityEngine;

public class Scene1LightingSetup : MonoBehaviour
{
    [Header("Ambient Settings")]
    public Color ambientColor = new Color(0.5f, 0.5f, 0.55f);
    
    [Header("Directional Light")]
    public Color sunColor = new Color(0.95f, 0.9f, 0.85f);
    public float sunIntensity = 1.2f;
    public Vector3 sunRotation = new Vector3(50f, -30f, 0f);

    private void Start()
    {
        ApplyLighting();
    }

    [ContextMenu("Apply Scene 1 Lighting")]
    public void ApplyLighting()
    {
        // 1. Set ambient to a visible industrial level
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.reflectionIntensity = 1f;

        // 2. Re-enable any disabled directional lights
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        bool hasSun = false;
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.gameObject.SetActive(true);
                l.intensity = sunIntensity;
                l.color = sunColor;
                hasSun = true;
            }
        }

        // 3. If no directional light exists, create one
        if (!hasSun)
        {
            GameObject sunObj = new GameObject("Scene1_Sun");
            sunObj.transform.rotation = Quaternion.Euler(sunRotation);
            Light sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = sunColor;
            sun.intensity = sunIntensity;
            sun.shadows = LightShadows.Hard;
        }

        Debug.Log("Scene1LightingSetup: Lighting applied.");
    }
}
