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

        // Create Materials (Dirty/Industrial colors)
        // Darker, less saturated Red
        Material redMat = CreateMaterial(new Color(0.6f, 0.1f, 0.1f), "RedBinMat", 0.1f); 
        // Darker, less saturated Blue
        Material blueMat = CreateMaterial(new Color(0.1f, 0.1f, 0.6f), "BlueBinMat", 0.1f);
        // Dark floor for contrast
        Material darkMat = CreateMaterial(new Color(0.1f, 0.1f, 0.1f), "BinFloorMat", 0.0f); 
        // Rusty Metal for Frame
        Material frameMat = CreateMaterial(new Color(0.25f, 0.2f, 0.2f), "BinFrameMat", 0.2f);

        // Create Red Bin
        GameObject redBin = CreateBin(root, "RedBin", redBinPosition, binSize, redMat, darkMat, frameMat);
        AddCollectorLogic(redBin, "RedCube");

        // Create Blue Bin
        GameObject blueBin = CreateBin(root, "BlueBin", blueBinPosition, binSize, blueMat, darkMat, frameMat);
        AddCollectorLogic(blueBin, "BlueCube");
        
        Debug.Log("Bins generated with Collector Logic!");
    }

    GameObject CreateBin(GameObject parent, string name, Vector3 localPos, Vector3 size, Material wallMat, Material floorMat, Material frameMat)
    {
        GameObject bin = new GameObject(name);
        bin.transform.parent = parent.transform;
        bin.transform.localPosition = localPos;

        // Floor - Use Dark Material for better depth perception
        CreatePart(bin, "Floor", new Vector3(0, wallThickness / 2, 0), new Vector3(size.x, wallThickness, size.z), floorMat);

        // Walls
        float h = size.y;
        float t = wallThickness;

        // Front (Z+)
        CreatePart(bin, "Wall_Front", new Vector3(0, h / 2, size.z / 2 - t / 2), new Vector3(size.x, h, t), wallMat);
        // Back (Z-)
        CreatePart(bin, "Wall_Back", new Vector3(0, h / 2, -size.z / 2 + t / 2), new Vector3(size.x, h, t), wallMat);
        // Left (X-)
        CreatePart(bin, "Wall_Left", new Vector3(-size.x / 2 + t / 2, h / 2, 0), new Vector3(t, h, size.z - 2 * t), wallMat);
        // Right (X+)
        CreatePart(bin, "Wall_Right", new Vector3(size.x / 2 - t / 2, h / 2, 0), new Vector3(t, h, size.z - 2 * t), wallMat);

        // Frame (Corners and Rim)
        CreateFrame(bin, size, t, frameMat);

        return bin;
    }

    void CreateFrame(GameObject parent, Vector3 size, float t, Material mat)
    {
        float h = size.y;
        float frameT = t * 1.5f; // Slightly thicker than walls

        // 4 Corner Pillars
        // FL (Front-Left)
        CreatePart(parent, "Frame_FL", new Vector3(-size.x/2 + frameT/2, h/2, size.z/2 - frameT/2), new Vector3(frameT, h, frameT), mat);
        // FR (Front-Right)
        CreatePart(parent, "Frame_FR", new Vector3(size.x/2 - frameT/2, h/2, size.z/2 - frameT/2), new Vector3(frameT, h, frameT), mat);
        // BL (Back-Left)
        CreatePart(parent, "Frame_BL", new Vector3(-size.x/2 + frameT/2, h/2, -size.z/2 + frameT/2), new Vector3(frameT, h, frameT), mat);
        // BR (Back-Right)
        CreatePart(parent, "Frame_BR", new Vector3(size.x/2 - frameT/2, h/2, -size.z/2 + frameT/2), new Vector3(frameT, h, frameT), mat);

        // Top Rim
        // Front
        CreatePart(parent, "Rim_Front", new Vector3(0, h - frameT/2, size.z/2 - frameT/2), new Vector3(size.x, frameT, frameT), mat);
        // Back
        CreatePart(parent, "Rim_Back", new Vector3(0, h - frameT/2, -size.z/2 + frameT/2), new Vector3(size.x, frameT, frameT), mat);
        // Left
        CreatePart(parent, "Rim_Left", new Vector3(-size.x/2 + frameT/2, h - frameT/2, 0), new Vector3(frameT, frameT, size.z - 2*frameT), mat);
        // Right
        CreatePart(parent, "Rim_Right", new Vector3(size.x/2 - frameT/2, h - frameT/2, 0), new Vector3(frameT, frameT, size.z - 2*frameT), mat);
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

    private Material CreateMaterial(Color color, string name, float smoothness = 0.3f)
    {
        // Try to find URP shader, fallback to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        
        // Add depth cues via material properties
        if (shader.name.Contains("Universal"))
        {
            mat.SetFloat("_Smoothness", smoothness); // Adjustable smoothness
            mat.SetFloat("_Metallic", 0.1f);   // Slight metallic for industrial look
        }
        else
        {
            mat.SetFloat("_Glossiness", smoothness);
        }
        
        return mat;
    }
}
