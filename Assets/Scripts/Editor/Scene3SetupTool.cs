using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MusicSpace.Editor
{
    /// <summary>
    /// Araç menüsü: Tools > Setup Scene 3 for VR
    /// Scene 3'e XR Origin, XR Interaction Manager ve spawn noktası ekler.
    /// </summary>
    public static class Scene3SetupTool
    {
        private const string Scene3Path = "Assets/Scene 3.unity";
        private const string XROriginPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/3.2.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

        // Metro platformunun ortasına yakın bir spawn noktası.
        // Bunu sahne içinde Floor_Main objelerinin pozisyonuna göre ayarla.
        private static readonly Vector3 SpawnPosition = new Vector3(0f, 0.1f, 0f);

        [MenuItem("Tools/Setup Scene 3 for VR")]
        public static void SetupScene3()
        {
            // Mevcut sahneyi kaydet
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Scene 3'ü aç
            Scene scene = EditorSceneManager.OpenScene(Scene3Path, OpenSceneMode.Single);

            // 1. XR Interaction Manager kontrolü
            var xrManager = Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            if (xrManager == null)
            {
                var managerGO = new GameObject("XR Interaction Manager");
                managerGO.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
                Debug.Log("[Scene3Setup] XR Interaction Manager oluşturuldu.");
            }
            else
            {
                Debug.Log("[Scene3Setup] XR Interaction Manager zaten mevcut.");
            }

            // 2. XR Origin kontrolü
            var existingOrigin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (existingOrigin != null)
            {
                Debug.Log("[Scene3Setup] XR Origin zaten mevcut: " + existingOrigin.name);
                PositionXROrigin(existingOrigin.transform);
            }
            else
            {
                // Prefab'ı yükle ve sahneye ekle
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XROriginPrefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"[Scene3Setup] XR Origin prefabı bulunamadı: {XROriginPrefabPath}\n" +
                                   "Lütfen Assets/VRTemplateAssets/Prefabs/Setup/ içinden manuel olarak sürükle.");
                    return;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "XR Origin (XR Rig)";
                PositionXROrigin(instance.transform);
                Undo.RegisterCreatedObjectUndo(instance, "Add XR Origin to Scene 3");
                Debug.Log("[Scene3Setup] XR Origin oluşturuldu ve konumlandırıldı.");
            }

            // 3. Directional Light kontrolü
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun == null)
            {
                var lightGO = new GameObject("Directional Light");
                var light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                Debug.Log("[Scene3Setup] Directional Light oluşturuldu.");
            }

            // 4. Sahneyi kaydet
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Scene3Setup] Scene 3 VR kurulumu tamamlandı ve kaydedildi.");

            EditorUtility.DisplayDialog("Scene 3 VR Kurulumu",
                "XR Origin ve XR Interaction Manager başarıyla eklendi.\n\n" +
                "Spawn konumu: " + SpawnPosition + "\n\n" +
                "Not: Spawn pozisyonunu sahne içinde istediğin yere taşıyabilirsin.",
                "Tamam");
        }

        private static void PositionXROrigin(Transform xrOrigin)
        {
            xrOrigin.position = SpawnPosition;
            xrOrigin.rotation = Quaternion.identity;
            Debug.Log($"[Scene3Setup] XR Origin konumu: {SpawnPosition}");
        }

        [MenuItem("Tools/Setup Scene 3 for VR", validate = true)]
        public static bool ValidateSetupScene3()
        {
            return System.IO.File.Exists(
                System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), Scene3Path));
        }
    }
}
