using UnityEditor;
using UnityEngine;

/// <summary>
/// Sahnedeki çöp objelerinden Missing Script kaldırır,
/// GrabbableTrash ekler, Rigidbody'yi kinematic yapar.
/// Tools → Trash → Cleanup & Setup
/// </summary>
public static class TrashCleanup
{
    private static readonly string[] TrashNames =
    {
        "Prefab_WaterBottle",
        "Prefab_CoffeeCup",
        "Prefab_BotteCap",
        "Prefab_SodaBottle",
        "Prefab_SodaCan",
        "Prefab_SodaCup",
        "Mesh_WatterBottle",
        "Mesh_WaterBottle",
    };

    [MenuItem("Tools/Trash/Cleanup & Setup")]
    public static void CleanupAndSetup()
    {
        int missingRemoved = 0;
        int setupCount     = 0;

        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (!IsTrash(go.name)) continue;

            // ── Missing Script bileşenlerini kaldır ─────────────────────────
            SerializedObject so = new SerializedObject(go);
            SerializedProperty components = so.FindProperty("m_Component");

            for (int i = components.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty element = components.GetArrayElementAtIndex(i);
                Object obj = element
                    .FindPropertyRelative("component").objectReferenceValue;

                if (obj == null)
                {
                    Undo.RecordObject(go, "Remove Missing Script");
                    components.DeleteArrayElementAtIndex(i);
                    missingRemoved++;
                }
            }
            so.ApplyModifiedProperties();

            // ── Rigidbody kinematic yap ──────────────────────────────────────
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Undo.RecordObject(rb, "Set Kinematic");
                rb.isKinematic = true;
                rb.useGravity  = false;
            }

            // ── GrabbableTrash ekle (yoksa) ──────────────────────────────────
            if (go.GetComponent<GrabbableTrash>() == null)
                Undo.AddComponent<GrabbableTrash>(go);

            setupCount++;
        }

        EditorUtility.DisplayDialog(
            "Temizlik Tamamlandı",
            $"{missingRemoved} Missing Script kaldırıldı.\n{setupCount} çöp objesi GrabbableTrash ile kuruldu.",
            "Tamam");
    }

    private static bool IsTrash(string name)
    {
        foreach (string n in TrashNames)
            if (name.Contains(n)) return true;
        return false;
    }
}
