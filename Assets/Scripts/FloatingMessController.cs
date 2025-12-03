using UnityEngine;
using System.Collections.Generic;

public class FloatingMessController : MonoBehaviour
{
    [Header("Settings")]
    public float floatSpeed = 0.5f;
    public float rotationSpeed = 10f;
    public float riseAmount = 1.5f; // How high they go
    public float noiseScale = 0.5f; // For random movement

    private bool isFloating = false;
    private List<Transform> papers = new List<Transform>();
    private List<Vector3> initialPositions = new List<Vector3>();
    private List<Vector3> randomOffsets = new List<Vector3>();

    public void StartFloating()
    {
        if (isFloating) return;

        // Find the papers
        GameObject messRoot = GameObject.Find("Generated_Mess");
        if (messRoot == null)
        {
            Debug.LogWarning("Generated_Mess not found! Make sure OfficeMessGenerator has run.");
            return;
        }

        papers.Clear();
        initialPositions.Clear();
        randomOffsets.Clear();

        foreach (Transform child in messRoot.transform)
        {
            // Only affect floor papers, maybe? Or all? 
            // Let's affect all for a massive effect, or filter by name if needed.
            // User said "yerdeki o nizami kağıtlar... havada süzülmeye başlasın"
            // Actually user said "yerdeki o nizami kağıtlar" but previously "yerdeki" were random and "duvardaki" were nizami.
            // Let's float everything for maximum effect.
            
            papers.Add(child);
            initialPositions.Add(child.position);
            randomOffsets.Add(new Vector3(Random.value, Random.value, Random.value) * 10f);
        }

        isFloating = true;
        Debug.Log("Daydream started: Papers are floating...");
    }

    void Update()
    {
        if (!isFloating) return;

        float time = Time.time;

        for (int i = 0; i < papers.Count; i++)
        {
            Transform paper = papers[i];
            Vector3 startPos = initialPositions[i];
            Vector3 offset = randomOffsets[i];

            // Calculate new position
            // Rise up slowly
            float rise = Mathf.Sin((time + offset.x) * floatSpeed) * 0.2f + (riseAmount * Mathf.Min(1, (Time.timeSinceLevelLoad % 100) / 5)); 
            // Actually we want them to lift off and stay floating.
            // Let's just use a Perlin noise field for organic movement.
            
            float noiseX = Mathf.PerlinNoise(time * floatSpeed + offset.x, offset.y) - 0.5f;
            float noiseY = Mathf.PerlinNoise(time * floatSpeed + offset.y, offset.z); // 0 to 1
            float noiseZ = Mathf.PerlinNoise(time * floatSpeed + offset.z, offset.x) - 0.5f;

            Vector3 targetPos = startPos + new Vector3(noiseX, noiseY * riseAmount, noiseZ);
            
            // Smoothly move there
            paper.position = Vector3.Lerp(paper.position, targetPos, Time.deltaTime * floatSpeed);

            // Rotate gently
            paper.Rotate(Vector3.up, rotationSpeed * Time.deltaTime * (noiseX + 0.5f));
            paper.Rotate(Vector3.right, rotationSpeed * Time.deltaTime * (noiseZ + 0.5f));
        }
    }
}
