using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Sahnedeki tüm NPCIdlePose component'lerini siler.
/// Tools → VR Helpers → Remove All NPCIdlePose menüsünden çağrılır.
/// NPCScene3Wanderer zaten procedural walk yapıyor, NPCIdlePose gereksiz.
/// </summary>
public class RemoveAllNPCIdlePose
{
    [MenuItem("Tools/VR Helpers/Remove All NPCIdlePose")]
    public static void RemoveAll()
    {
        var allIdlePoses = Object.FindObjectsByType<NPCIdlePose>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var ip in allIdlePoses)
        {
            if (ip == null) continue;
            Undo.DestroyObjectImmediate(ip);
            count++;
        }

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Remove NPCIdlePose",
                $"{count} adet NPCIdlePose component'i kaldırıldı.\n\n" +
                "Sahneyi kaydetmeyi unutma (Cmd+S).",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Remove NPCIdlePose",
                "Sahnede NPCIdlePose component'i bulunamadı.",
                "OK");
        }
    }
}
