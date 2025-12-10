using UnityEngine;

public class SimpleRoomGenerator : MonoBehaviour
{
    [Header("Room Settings")]
    public float width = 16f;
    public float length = 16f;
    public float height = 10f;
    
    [Header("Appearance")]
    public Color wallColor = new Color(0.35f, 0.35f, 0.35f); // Dark Factory Grey
    public Color floorColor = new Color(0.25f, 0.25f, 0.25f); // Darker Floor
    public Color ceilingColor = new Color(0.8f, 0.8f, 0.8f); // Dim White

    [ContextMenu("Generate Boring Office")]
    public void GenerateRoom()
    {
        // Create Parent
        GameObject root = new GameObject("Generated_Office_Room");
        root.transform.position = Vector3.zero;

        // Create Materials
        Material wallMat = CreateMaterial(wallColor, "WallMat");
        Material floorMat = CreateMaterial(floorColor, "FloorMat");
        Material ceilingMat = CreateMaterial(ceilingColor, "CeilingMat");

        // Floor
        CreateCube(root, "Floor", new Vector3(0, -0.05f, 0), new Vector3(width, 0.1f, length), floorMat);
        
        // Ceiling
        CreateCube(root, "Ceiling", new Vector3(0, height + 0.05f, 0), new Vector3(width, 0.1f, length), ceilingMat);

        // Walls
        float halfWidth = width / 2;
        float halfLength = length / 2;
        float halfHeight = height / 2;

        // Front (Z+)
        CreateCube(root, "Wall_Front", new Vector3(0, halfHeight, halfLength), new Vector3(width, height, 0.1f), wallMat);
        
        // Back (Z-)
        CreateCube(root, "Wall_Back", new Vector3(0, halfHeight, -halfLength), new Vector3(width, height, 0.1f), wallMat);

        // Left (X-)
        CreateCube(root, "Wall_Left", new Vector3(-halfWidth, halfHeight, 0), new Vector3(0.1f, height, length), wallMat);

        // Right (X+)
        CreateCube(root, "Wall_Right", new Vector3(halfWidth, halfHeight, 0), new Vector3(0.1f, height, length), wallMat);
        
        Debug.Log("Boring Office Generated! You can now delete the old 'room' object.");
    }

    private void CreateCube(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.parent = parent.transform;
        cube.transform.localPosition = pos;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private Material CreateMaterial(Color color, string name)
    {
        // Try to find URP shader, fallback to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        
        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        
        // Make it rough/matte for a dirty industrial look
        if (shader.name.Contains("Universal"))
        {
            mat.SetFloat("_Smoothness", 0.15f);
        }
        else
        {
            mat.SetFloat("_Glossiness", 0.15f);
        }
        
        return mat;
    }
}
