using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Sahnedeki "Missing (Mono Script)" component'lerini bulup temizler.
/// Tools → VR Helpers → Clean Missing Scripts In Scene menüsünden çağrılır.
/// </summary>
public class CleanMissingScripts
{
    [MenuItem("Tools/VR Helpers/Clean Missing Scripts In Scene")]
    public static void CleanScene()
    {
        var allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int totalRemoved = 0;
        int touchedObjects = 0;

        foreach (var go in allGameObjects)
        {
            int countBefore = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (countBefore == 0) continue;

            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0)
            {
                Debug.Log($"[CleanMissingScripts] {go.name}: {removed} bozuk script kaldırıldı.", go);
                totalRemoved += removed;
                touchedObjects++;
            }
        }

        if (totalRemoved > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Clean Missing Scripts",
                $"{touchedObjects} obje üzerinde toplam {totalRemoved} bozuk script kaldırıldı.\n\n" +
                "Sahneyi kaydetmeyi unutma (Cmd+S).",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Clean Missing Scripts",
                "Bozuk script bulunamadı, sahne temiz!",
                "OK");
        }
    }
}
