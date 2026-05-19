using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools → Scene2 → Add Transition Cube to Scene 3
/// Scene 2'yi açar, grab edilince Scene 3'e geçen bir küp ekler.
/// </summary>
public static class Scene2TransitionCubeSetup
{
    private const string Scene2Path = "Assets/Scene 2.unity";

    [MenuItem("Tools/Scene2/Add Transition Cube to Scene 3")]
    public static void AddTransitionCube()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(Scene2Path, OpenSceneMode.Single);

        // Var mı kontrol et
        SceneTransitionCube existing = Object.FindFirstObjectByType<SceneTransitionCube>();
        if (existing != null)
        {
            Debug.Log("[Scene2Setup] SceneTransitionCube zaten mevcut: " + existing.gameObject.name);
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            EditorUtility.DisplayDialog("Zaten Var",
                "Scene 2'de zaten bir SceneTransitionCube mevcut.\nInspector'dan targetScene = \"Scene 3\" ayarını kontrol et.",
                "Tamam");
            return;
        }

        // Küp oluştur
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "TransitionCube_ToScene3";
        cube.transform.position = new Vector3(0f, 1.5f, 0f);
        cube.transform.localScale  = new Vector3(0.3f, 0.3f, 0.3f);

        SceneTransitionCube comp = cube.AddComponent<SceneTransitionCube>();
        comp.targetScene    = "Scene 3";
        comp.cubeColor      = new Color(0.1f, 0.6f, 1f, 1f); // Mavi — metro temasına uygun
        comp.glowIntensity  = 0.4f;
        comp.floatAmplitude = 0.05f;
        comp.floatSpeed     = 2f;
        comp.rotateSpeed    = 30f;

        Undo.RegisterCreatedObjectUndo(cube, "Add TransitionCube_ToScene3");
        Selection.activeGameObject = cube;
        EditorGUIUtility.PingObject(cube);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[Scene2Setup] TransitionCube_ToScene3 oluşturuldu ve Scene 2 kaydedildi.");
        EditorUtility.DisplayDialog("Tamamlandı",
            "TransitionCube_ToScene3 oluşturuldu.\n\nKüp grab edilince Scene 3 yüklenir.\nKonumunu Hierarchy'den ayarlayabilirsin.",
            "Tamam");
    }
}
