using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MusicSpace.Editor
{
    /// <summary>
    /// Araç menüsü: Tools > Setup Scene 4 for VR
    /// Scene 4'e XR Origin, XR Interaction Manager ve terrain spawn noktası ekler.
    /// </summary>
    public static class Scene4SetupTool
    {
        private const string Scene4Path = "Assets/Scene 4.unity";
        private const string XROriginPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/3.2.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

        // Terrain merkezi (500x500 terrain için 250,250).
        // Y değeri runtime'da terrain yüzeyinden alınır; burada güvenli bir başlangıç yüksekliği ver.
        private static readonly Vector3 SpawnPosition = new Vector3(250f, 50f, 250f);

        [MenuItem("Tools/Setup Scene 4 for VR")]
        public static void SetupScene4()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.OpenScene(Scene4Path, OpenSceneMode.Single);

            // 1. XR Interaction Manager
            var xrManager = Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            if (xrManager == null)
            {
                var managerGO = new GameObject("XR Interaction Manager");
                managerGO.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
                Debug.Log("[Scene4Setup] XR Interaction Manager oluşturuldu.");
            }
            else
            {
                Debug.Log("[Scene4Setup] XR Interaction Manager zaten mevcut.");
            }

            // 2. XR Origin
            var existingOrigin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (existingOrigin != null)
            {
                Debug.Log("[Scene4Setup] XR Origin zaten mevcut: " + existingOrigin.name);
                PositionXROrigin(existingOrigin.transform);
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XROriginPrefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"[Scene4Setup] XR Origin prefabı bulunamadı: {XROriginPrefabPath}\n" +
                                   "Assets/Samples/XR Interaction Toolkit/ klasörünü kontrol et.");
                    EditorUtility.DisplayDialog("Hata",
                        "XR Origin prefabı bulunamadı.\n\nLütfen XR Interaction Toolkit Starter Assets'i import et.",
                        "Tamam");
                    return;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "XR Origin (XR Rig)";
                PositionXROrigin(instance.transform);
                Undo.RegisterCreatedObjectUndo(instance, "Add XR Origin to Scene 4");
                Debug.Log("[Scene4Setup] XR Origin oluşturuldu ve terrain üzerine yerleştirildi.");
            }

            // 3. TerrainSpawnPoint scriptini XR Origin'e ekle
            var xrOrigin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                var spawnScript = xrOrigin.GetComponent<TerrainSpawnPoint>();
                if (spawnScript == null)
                {
                    spawnScript = xrOrigin.gameObject.AddComponent<TerrainSpawnPoint>();
                    spawnScript.spawnX = 250f;
                    spawnScript.spawnZ = 250f;
                    spawnScript.heightOffset = 0.1f;
                    Debug.Log("[Scene4Setup] TerrainSpawnPoint scripti XR Origin'e eklendi.");
                }
            }

            // 4. Mevcut Main Camera'yı devre dışı bırak (XR Origin kendi kamerasını getirir)
            var mainCam = Camera.main;
            if (mainCam != null && mainCam.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() == null)
            {
                mainCam.gameObject.SetActive(false);
                Debug.Log("[Scene4Setup] Eski Main Camera devre dışı bırakıldı.");
            }

            // 5. Kaydet
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Scene4Setup] Scene 4 VR kurulumu tamamlandı.");

            EditorUtility.DisplayDialog("Scene 4 VR Kurulumu",
                "XR Origin ve XR Interaction Manager başarıyla eklendi.\n\n" +
                "Spawn konumu: Terrain merkezi (250, terrain yüzeyi, 250)\n\n" +
                "TerrainSpawnPoint scripti XR Origin'e eklendi — oyuncu terrain yüzeyinde başlar.",
                "Tamam");
        }

        private static void PositionXROrigin(Transform xrOrigin)
        {
            xrOrigin.position = SpawnPosition;
            xrOrigin.rotation = Quaternion.identity;
        }

        [MenuItem("Tools/Setup Scene 4 for VR", validate = true)]
        public static bool ValidateSetupScene4()
        {
            return System.IO.File.Exists(
                System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), Scene4Path));
        }
    }
}
