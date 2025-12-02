using UnityEngine;

public class DispenserGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform spawnPoint; // The point where cubes appear
    public Material machineMaterial; // Material for the dispenser

    [Header("Settings")]
    public Vector3 size = new Vector3(0.6f, 1.0f, 0.6f); // Size of the dispenser box
    public float wallThickness = 0.05f;
    public float pipeHeight = 4.0f; // Height of the pipe extending to ceiling

    [ContextMenu("Generate Dispenser")]
    public void GenerateDispenser()
    {
        if (spawnPoint == null)
        {
            // Try to find the ObjectSpawner's spawn point automatically
            var spawner = FindObjectOfType<ConveyorShift.ObjectSpawner>();
            if (spawner != null)
            {
                spawnPoint = spawner.transform;
            }
            else
            {
                Debug.LogError("No Spawn Point or ObjectSpawner found!");
                return;
            }
        }

        // Create Parent
        string dispenserName = "Cube_Dispenser";
        Transform existing = spawnPoint.parent ? spawnPoint.parent.Find(dispenserName) : transform.Find(dispenserName);
        if (existing != null) DestroyImmediate(existing.gameObject);

        GameObject dispenser = new GameObject(dispenserName);
        dispenser.transform.position = spawnPoint.position + Vector3.up * (size.y / 2); // Center it above the spawn point
        dispenser.transform.parent = spawnPoint.parent ?? transform;

        // Create Material if missing
        if (machineMaterial == null)
        {
            machineMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            machineMaterial.color = new Color(0.3f, 0.3f, 0.35f); // Dark metallic grey
        }

        // Generate Walls
        CreateWall(dispenser.transform, new Vector3(0, 0, size.z/2), new Vector3(size.x, size.y, wallThickness)); // Front
        CreateWall(dispenser.transform, new Vector3(0, 0, -size.z/2), new Vector3(size.x, size.y, wallThickness)); // Back
        CreateWall(dispenser.transform, new Vector3(-size.x/2, 0, 0), new Vector3(wallThickness, size.y, size.z)); // Left
        CreateWall(dispenser.transform, new Vector3(size.x/2, 0, 0), new Vector3(wallThickness, size.y, size.z)); // Right
        
        // Generate Top Cap
        CreateWall(dispenser.transform, new Vector3(0, size.y/2, 0), new Vector3(size.x + wallThickness, wallThickness, size.z + wallThickness)); // Top

        // Generate Pipe to Ceiling
        CreatePipe(dispenser.transform, new Vector3(0, size.y/2 + pipeHeight/2, 0), pipeHeight);

        Debug.Log("Dispenser generated around " + spawnPoint.name);
    }

    void CreateWall(Transform parent, Vector3 localPos, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = localPos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = machineMaterial;
        DestroyImmediate(wall.GetComponent<Collider>()); 
    }

    void CreatePipe(Transform parent, Vector3 localPos, float height)
    {
        GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipe.name = "CeilingPipe";
        pipe.transform.SetParent(parent, false);
        pipe.transform.localPosition = localPos;
        // Cylinder default height is 2. Scale Y = height / 2.
        // Width is slightly smaller than the box
        pipe.transform.localScale = new Vector3(size.x * 0.7f, height / 2, size.z * 0.7f); 
        pipe.GetComponent<Renderer>().sharedMaterial = machineMaterial;
        DestroyImmediate(pipe.GetComponent<Collider>());
    }
}
