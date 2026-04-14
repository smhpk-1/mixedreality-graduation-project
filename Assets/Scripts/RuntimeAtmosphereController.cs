using UnityEngine;
using UnityEngine.SceneManagement;

public class RuntimeAtmosphereController : MonoBehaviour
{
    [Header("Factory Atmosphere Settings")]
    public Color ambientColor = new Color(0.02f, 0.02f, 0.02f); // Pitch Black
    public bool disableSun = true;

    [Header("Scene Filter")]
    [Tooltip("Only apply atmosphere in this scene. Leave empty to apply in any scene.")]
    public string targetSceneName = "Scene 3";

    private void Start()
    {
        if (!string.IsNullOrEmpty(targetSceneName) &&
            SceneManager.GetActiveScene().name != targetSceneName)
        {
            return;
        }
        ApplyAtmosphere();
    }

    [ContextMenu("Apply Atmosphere Now")]
    public void ApplyAtmosphere()
    {
        // 1. Kill the Sun
        if (disableSun)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    l.gameObject.SetActive(false);
                    Debug.Log("RuntimeAtmosphere: Sun disabled.");
                }
            }
        }

        // 2. Kill Ambient Light (The "Grey Fog" killer)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.reflectionIntensity = 0.2f; // Low reflections
        RenderSettings.fog = false;

        Debug.Log("RuntimeAtmosphere: Dark Factory settings applied.");
    }
}
