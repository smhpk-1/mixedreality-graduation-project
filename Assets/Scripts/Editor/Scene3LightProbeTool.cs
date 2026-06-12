using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Scene 3'e light probe grid'i kurar.
///
/// SORUN: Sahnede hiç LightProbeGroup yok. İstasyon baked ışıklarla aydınlatıldığı
/// için statik geometri lightmap'ten güzel görünüyor ama dinamik NPC'ler probe
/// olmadan düz ambient ile kalıyor → cihazda ortamdan kopuk, "havada" görünüyorlar.
///
/// KULLANIM: Scene 3 açıkken Tools > Scene 3 > Create Light Probe Grid.
/// Sonrasında Lighting panelinden "Generate Lighting" ile bake almak ŞART —
/// probe'lar ancak bake sonrası veri taşır.
/// </summary>
public static class Scene3LightProbeTool
{
    [MenuItem("Tools/Scene 3/Create Light Probe Grid")]
    public static void CreateProbeGrid()
    {
        // Platform alanını bank + çöp kutusu pozisyonlarından çıkar
        var anchors = new List<Vector3>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name.StartsWith("Metalbench") || t.name.StartsWith("MetalTrashCan"))
                anchors.Add(t.position);
        }

        if (anchors.Count == 0)
        {
            EditorUtility.DisplayDialog("Scene 3 Light Probes",
                "Metalbench / MetalTrashCan objesi bulunamadı — Scene 3 açık mı?", "Tamam");
            return;
        }

        Bounds area = new Bounds(anchors[0], Vector3.zero);
        foreach (var p in anchors) area.Encapsulate(p);
        area.Expand(new Vector3(6f, 0f, 6f)); // platform kenarlarına pay

        float floorY = area.min.y;
        float[] heights = { 0.25f, 1.3f, 2.6f }; // ayak / baş / tavan altı katmanları
        const float spacing = 3f;

        var positions = new List<Vector3>();
        for (float x = area.min.x; x <= area.max.x; x += spacing)
            for (float z = area.min.z; z <= area.max.z; z += spacing)
                foreach (float h in heights)
                    positions.Add(new Vector3(x, floorY + h, z));

        var existing = GameObject.Find("Scene3 LightProbes");
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var go = new GameObject("Scene3 LightProbes");
        Undo.RegisterCreatedObjectUndo(go, "Create Scene 3 Light Probe Grid");
        var group = go.AddComponent<LightProbeGroup>();
        group.probePositions = positions.ToArray(); // group origin'de → local == world

        EditorUtility.SetDirty(go);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);

        Debug.Log($"[Scene3LightProbeTool] {positions.Count} probe yerleştirildi " +
                  $"({area.size.x:F0}m x {area.size.z:F0}m alan). " +
                  "Şimdi Lighting panelinden 'Generate Lighting' ile bake al!");
    }
}
