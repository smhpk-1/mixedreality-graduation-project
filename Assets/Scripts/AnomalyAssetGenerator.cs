using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnomalyAssetGenerator : MonoBehaviour
{
    [ContextMenu("Generate Anomaly Prefab")]
    public void GenerateAndAssign()
    {
#if UNITY_EDITOR
        // 1. Create the Object in Scene
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "GreenAnomalyCube";
        cube.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f); // Slightly smaller than standard 1m cube

        // 2. Create & Assign Material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.name = "GlowingGreenMat";
        mat.color = Color.green;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.green * 3f); // Bright Glow
        
        // Save Material asset so it persists
        string matPath = "Assets/GlowingGreen.mat";
        AssetDatabase.CreateAsset(mat, matPath);
        cube.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        // 3. Add Components
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb == null) rb = cube.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // XR Grab Interactable
        XRGrabInteractable grab = cube.AddComponent<XRGrabInteractable>();
        grab.movementType = XRGrabInteractable.MovementType.VelocityTracking;
        grab.throwOnDetach = true;
        
        // Anomaly Logic Script
        cube.AddComponent<ConveyorShift.AnomalyCube>();

        // 4. Save as Prefab
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        string prefabPath = "Assets/Prefabs/GreenAnomalyCube.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cube, prefabPath);
        Debug.Log($"Prefab saved at: {prefabPath}");

        // 5. Assign to Spawner
        var spawner = FindFirstObjectByType<ConveyorShift.ObjectSpawner>();
        if (spawner != null)
        {
            // We need to use SerializedObject to modify the prefab field in Editor time properly
            SerializedObject so = new SerializedObject(spawner);
            SerializedProperty prop = so.FindProperty("greenAnomalyPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
                Debug.Log("Successfully assigned Green Anomaly Prefab to ObjectSpawner!");
            }
            else
            {
                Debug.LogError("Could not find 'greenAnomalyPrefab' field on ObjectSpawner.");
            }
        }
        else
        {
            Debug.LogError("ObjectSpawner not found in scene!");
        }

        // 6. Cleanup Scene Object
        DestroyImmediate(cube);
#else
        Debug.LogError("This tool only works in the Unity Editor.");
#endif
    }
}
