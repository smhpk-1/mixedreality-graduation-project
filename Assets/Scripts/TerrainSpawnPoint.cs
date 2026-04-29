using UnityEngine;

/// <summary>
/// Attach this to the XR Origin (or Main Camera parent) in Scene 4.
/// Snaps the player to the terrain surface on scene load.
/// </summary>
public class TerrainSpawnPoint : MonoBehaviour
{
    [Tooltip("Terrain'in üstünde duracağı X,Z noktası (terrain merkezi önerilir)")]
    public float spawnX = 250f;
    public float spawnZ = 250f;

    [Tooltip("Terrain yüzeyine ek yükseklik (ayaklar batmasın)")]
    public float heightOffset = 1.8f;

    private void Start()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogWarning("[TerrainSpawnPoint] Aktif terrain bulunamadı!");
            return;
        }

        float terrainY = terrain.SampleHeight(new Vector3(spawnX, 0f, spawnZ));
        Vector3 spawnPos = new Vector3(spawnX, terrainY + heightOffset, spawnZ);
        transform.position = spawnPos;

        Debug.Log($"[TerrainSpawnPoint] Oyuncu yerleştirildi: {spawnPos}");
    }
}
