using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Scene2TwentyColoredCubesGenerator : MonoBehaviour
{
    public Material[] cubeMaterials = new Material[5]; // 5 farklı renk için
    public string[] colorNames = { "Red", "Blue", "Green", "Yellow", "Purple" };
    private Color[] defaultColors = new Color[5] {
        new Color(1f, 0f, 0f),      // Red
        new Color(0f, 0.3f, 1f),    // Blue
        new Color(0f, 1f, 0.2f),    // Green
        new Color(1f, 1f, 0f),      // Yellow
        new Color(0.6f, 0f, 0.8f)   // Purple
    };

#if UNITY_EDITOR
    [ContextMenu("Generate 20 Colored Cubes (5x4)")]
#endif
    public void GenerateCubes()
    {
        int cubesPerColor = 4;
        float scale = 0.5f;
        float startX = -3f;
        float startZ = -2f;
        float spacing = 1.2f;

        // Eğer materyaller atanmadıysa, otomatik oluştur
        for (int i = 0; i < 5; i++)
        {
            if (cubeMaterials.Length <= i || cubeMaterials[i] == null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = defaultColors[i];
                cubeMaterials[i] = mat;
            }
        }

        int cubeIndex = 0;
        for (int colorIdx = 0; colorIdx < 5; colorIdx++)
        {
            for (int i = 0; i < cubesPerColor; i++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Cube_{colorNames[colorIdx]}_{i+1}";
                cube.transform.localScale = new Vector3(scale, scale, scale);
                cube.transform.parent = this.transform;
                // Odanın zeminine düzgün bir gridde yerleştir
                float x = startX + (cubeIndex % 5) * spacing;
                float z = startZ + (cubeIndex / 5) * spacing;
                cube.transform.position = new Vector3(x, scale / 2f, z);
                cubeIndex++;

                var renderer = cube.GetComponent<Renderer>();
                if (renderer != null && cubeMaterials.Length > colorIdx && cubeMaterials[colorIdx] != null)
                {
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                        renderer.sharedMaterial = cubeMaterials[colorIdx];
                    else
                    #endif
                        renderer.material = cubeMaterials[colorIdx];
                }
            }
        }
    }
}