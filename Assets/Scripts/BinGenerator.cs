using UnityEngine;

public class BinGenerator : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 redBinPosition = new Vector3(-0.8f, 0f, 0.5f); // Left side
    public Vector3 blueBinPosition = new Vector3(0.8f, 0f, 0.5f); // Right side
    public Vector3 binSize = new Vector3(0.5f, 0.4f, 0.5f); // Width, Height, Depth
    public float wallThickness = 0.02f;

    [ContextMenu("Generate Sorting Bins")]
    public void GenerateBins()
    {
        // Find existing root if any
        Transform existingRoot = transform.Find("SortingBins");
        if (existingRoot != null) DestroyImmediate(existingRoot.gameObject);

        GameObject root = new GameObject("SortingBins");
        root.transform.parent = transform;
        root.transform.localPosition = Vector3.zero;

        // Create Materials (Simple colors)
        Material redMat = CreateMaterial(Color.red, "RedBinMat");
        Material blueMat = CreateMaterial(Color.blue, "BlueBinMat");

        // Create Red Bin
        GameObject redBin = CreateBin(root, "RedBin", redBinPosition, binSize, redMat);
        AddCollectorLogic(redBin, "RedCube");

        // Create Blue Bin
        GameObject blueBin = CreateBin(root, "BlueBin", blueBinPosition, binSize, blueMat);
        AddCollectorLogic(blueBin, "BlueCube");
        
        Debug.Log("Bins generated with Collector Logic!");
    }

    GameObject CreateBin(GameObject parent, string name, Vector3 localPos, Vector3 size, Material mat)
    {
        GameObject bin = new GameObject(name);
        bin.transform.parent = parent.transform;
        bin.transform.localPosition = localPos;

        // Floor
        CreatePart(bin, "Floor", new Vector3(0, wallThickness / 2, 0), new Vector3(size.x, wallThickness, size.z), mat);

        // Walls
        float h = size.y;
        float t = wallThickness;

        // Front (Z+)
        CreatePart(bin, "Wall_Front", new Vector3(0, h / 2, size.z / 2 - t / 2), new Vector3(size.x, h, t), mat);
        // Back (Z-)
        CreatePart(bin, "Wall_Back", new Vector3(0, h / 2, -size.z / 2 + t / 2), new Vector3(size.x, h, t), mat);
        // Left (X-)
        CreatePart(bin, "Wall_Left", new Vector3(-size.x / 2 + t / 2, h / 2, 0), new Vector3(t, h, size.z - 2 * t), mat);
        // Right (X+)
        CreatePart(bin, "Wall_Right", new Vector3(size.x / 2 - t / 2, h / 2, 0), new Vector3(t, h, size.z - 2 * t), mat);

        return bin;
    }

    void AddCollectorLogic(GameObject bin, string targetCube)
    {
        // Add BinCollector script
        BinCollector collector = bin.AddComponent<BinCollector>();
        collector.targetCubeName = targetCube;

        // Add Trigger Collider
        BoxCollider col = bin.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0, binSize.y / 2, 0);
        col.size = new Vector3(binSize.x * 0.9f, binSize.y * 0.9f, binSize.z * 0.9f);
    }

    void CreatePart(GameObject parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.parent = parent.transform;
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = mat;
        DestroyImmediate(part.GetComponent<Collider>()); // Remove default collider
        part.AddComponent<BoxCollider>(); // Add fresh collider for physics
    }

    private Material CreateMaterial(Color color, string name)
    {
        // Try to find URP shader, fallback to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        return mat;
    }
}
