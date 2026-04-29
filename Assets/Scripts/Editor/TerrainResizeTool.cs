using UnityEditor;
using UnityEngine;

public class TerrainResizeTool : EditorWindow
{
    private float newWidth = 250f;
    private float newLength = 250f;
    private float newHeight = 100f;
    private int newHeightmapResolution = 257;

    [MenuItem("Tools/VR Terrain Resize")]
    public static void ShowWindow()
    {
        GetWindow<TerrainResizeTool>("VR Terrain Resize");
    }

    private void OnGUI()
    {
        GUILayout.Label("Terrain Boyutu Küçült (VR Performans)", EditorStyles.boldLabel);
        GUILayout.Space(5);

        Terrain terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain == null)
        {
            EditorGUILayout.HelpBox("Sahnede aktif bir Terrain bulunamadı.", MessageType.Warning);
            return;
        }

        TerrainData data = terrain.terrainData;
        EditorGUILayout.HelpBox(
            $"Mevcut: {data.size.x}x{data.size.z}m  |  Heightmap: {data.heightmapResolution}",
            MessageType.Info);

        GUILayout.Space(10);
        GUILayout.Label("Yeni Boyutlar:", EditorStyles.boldLabel);

        newWidth = EditorGUILayout.FloatField("Genişlik (m)", newWidth);
        newLength = EditorGUILayout.FloatField("Uzunluk (m)", newLength);
        newHeight = EditorGUILayout.FloatField("Max Yükseklik (m)", newHeight);

        GUILayout.Space(5);
        GUILayout.Label("Heightmap Çözünürlüğü (2^n + 1):", EditorStyles.boldLabel);
        newHeightmapResolution = EditorGUILayout.IntPopup(newHeightmapResolution,
            new string[] { "65 (düşük)", "129 (orta-düşük)", "257 (orta)", "513 (yüksek)" },
            new int[] { 65, 129, 257, 513 });

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "VR için önerilen: 250x250m, Heightmap 129 veya 257",
            MessageType.None);

        GUILayout.Space(5);

        if (GUILayout.Button("Terrain'i Yeniden Boyutlandır", GUILayout.Height(40)))
        {
            ResizeTerrain(terrain, newWidth, newLength, newHeight, newHeightmapResolution);
        }
    }

    private static void ResizeTerrain(Terrain terrain, float width, float length, float height, int hmRes)
    {
        TerrainData data = terrain.terrainData;

        Undo.RegisterCompleteObjectUndo(data, "Terrain Resize");

        if (data.heightmapResolution != hmRes)
            data.heightmapResolution = hmRes;

        data.size = new Vector3(width, height, length);

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        Debug.Log($"[TerrainResizeTool] Terrain yeniden boyutlandırıldı: {width}x{length}m, Yükseklik: {height}m, Heightmap: {hmRes}");
    }
}
