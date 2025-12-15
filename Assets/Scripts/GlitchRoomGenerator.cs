using UnityEngine;

public class GlitchRoomGenerator : MonoBehaviour
{
    [Header("Room Settings")]
    public float width = 10f;
    public float length = 10f;
    public float height = 4f;
    public float wallThickness = 0.2f;

    [Header("Appearance")]
    public Color roomColor = new Color(0.2f, 0.2f, 0.2f); // Dark Grey

    private void Start()
    {
        GenerateRoom();
    }

    [ContextMenu("Generate Room")]
    public void GenerateRoom()
    {
        // Cleanup
        Transform existing = transform.Find("RoomGeometry");
        if (existing != null) DestroyImmediate(existing.gameObject);

        GameObject root = new GameObject("RoomGeometry");
        root.transform.parent = transform;
        root.transform.localPosition = Vector3.zero;

        // Create Material
        Material roomMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        roomMat.color = roomColor;
        roomMat.SetFloat("_Smoothness", 0.2f); // Matte finish

        float halfWidth = width / 2;
        float halfLength = length / 2;
        float halfHeight = height / 2;

        // 1. Floor
        CreateWall(root, "Floor", 
            new Vector3(0, -wallThickness/2, 0), 
            new Vector3(width, wallThickness, length), 
            roomMat);

        // 2. Ceiling
        CreateWall(root, "Ceiling", 
            new Vector3(0, height + wallThickness/2, 0), 
            new Vector3(width, wallThickness, length), 
            roomMat);

        // 3. Walls
        // Front (+Z)
        CreateWall(root, "Wall_Front", 
            new Vector3(0, halfHeight, halfLength + wallThickness/2), 
            new Vector3(width, height, wallThickness), 
            roomMat);

        // Back (-Z)
        CreateWall(root, "Wall_Back", 
            new Vector3(0, halfHeight, -halfLength - wallThickness/2), 
            new Vector3(width, height, wallThickness), 
            roomMat);

        // Left (-X)
        CreateWall(root, "Wall_Left", 
            new Vector3(-halfWidth - wallThickness/2, halfHeight, 0), 
            new Vector3(wallThickness, height, length), 
            roomMat);

        // Right (+X)
        CreateWall(root, "Wall_Right", 
            new Vector3(halfWidth + wallThickness/2, halfHeight, 0), 
            new Vector3(wallThickness, height, length), 
            roomMat);
            
        // Add a light inside so we can see the corners
        GameObject lightObj = new GameObject("RoomLight");
        lightObj.transform.parent = root.transform;
        lightObj.transform.localPosition = new Vector3(0, height - 0.5f, 0);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = Mathf.Max(width, length) * 1.5f;
        light.intensity = 1.0f;
        light.color = Color.white;
    }

    private void CreateWall(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.parent = parent.transform;
        wall.transform.localPosition = pos;
        wall.transform.localScale = size;
        
        Renderer r = wall.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }
}
