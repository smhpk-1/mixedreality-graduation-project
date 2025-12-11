using UnityEngine;
using System.Collections.Generic;

public class SimpleRoomGenerator : MonoBehaviour
{
    [Header("Room Settings")]
    public float width = 16f;
    public float length = 16f;
    public float height = 7f; // Reduced height
    
    [Header("Appearance")]
    public Color wallColor = new Color(0.55f, 0.55f, 0.55f); // Cement Grey
    public Color floorColor = new Color(0.15f, 0.15f, 0.15f); // Dark Floor
    public Color ceilingColor = new Color(0.7f, 0.7f, 0.7f); // Lighter Ceiling

    [Header("Automation")]
    public OfficeMessGenerator messGenerator;

    [ContextMenu("Generate Boring Office")]
    public void GenerateRoom()
    {
        // 0. ATMOSPHERE: Find and disable the Main Sun (Directional Light)
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in allLights)
        {
            if (l.type == LightType.Directional)
            {
                l.gameObject.SetActive(false);
            }
        }

        // NEW: Darken Environment Ambient Light to make point lights visible
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.02f); // Pitch black ambient
        RenderSettings.reflectionIntensity = 0.0f; // No reflections from skybox

        // FORCE SETTINGS: Override Inspector values
        height = 7f;
        wallColor = new Color(0.55f, 0.55f, 0.55f); // Cement Grey
        ceilingColor = new Color(0.65f, 0.65f, 0.65f); // Slightly lighter than walls
        
        // FIX CONVEYOR BELT COLOR
        GameObject conveyor = GameObject.Find("conveyorbelt");
        if (conveyor != null)
        {
            Renderer[] renderers = conveyor.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                // Create a dark grey material
                Material beltMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                beltMat.color = new Color(0.2f, 0.2f, 0.2f); // Dark Grey
                beltMat.SetFloat("_Smoothness", 0.1f);
                r.sharedMaterial = beltMat;
            }
        }
        
        // 1. Cleanup: Find and destroy the old room if it exists
        GameObject existingRoom = GameObject.Find("Generated_Office_Room");
        if (existingRoom != null)
        {
            DestroyImmediate(existingRoom);
        }

        // 2. Create Parent
        GameObject root = new GameObject("Generated_Office_Room");
        root.transform.position = Vector3.zero;

        // Create Materials with UNIQUE names to force update
        string suffix = System.DateTime.Now.Ticks.ToString();
        Material wallMat = CreateMaterial(wallColor, "WallMat_Dark_" + suffix);
        Material floorMat = CreateMaterial(floorColor, "FloorMat_Dark_" + suffix);
        Material ceilingMat = CreateMaterial(ceilingColor, "CeilingMat_Dark_" + suffix);

        // Floor
        GameObject floor = CreateCube(root, "Floor", new Vector3(0, -0.05f, 0), new Vector3(width, 0.1f, length), floorMat);
        
        // Ceiling
        CreateCube(root, "Ceiling", new Vector3(0, height + 0.05f, 0), new Vector3(width, 0.1f, length), ceilingMat);

        // Walls
        float halfWidth = width / 2;
        float halfLength = length / 2;
        float halfHeight = height / 2;

        // Front (Z+)
        GameObject wallFront = CreateCube(root, "Wall_Front", new Vector3(0, halfHeight, halfLength), new Vector3(width, height, 0.1f), wallMat);
        
        // Back (Z-)
        GameObject wallBack = CreateCube(root, "Wall_Back", new Vector3(0, halfHeight, -halfLength), new Vector3(width, height, 0.1f), wallMat);

        // Left (X-)
        GameObject wallLeft = CreateCube(root, "Wall_Left", new Vector3(-halfWidth, halfHeight, 0), new Vector3(0.1f, height, length), wallMat);

        // Right (X+)
        GameObject wallRight = CreateCube(root, "Wall_Right", new Vector3(halfWidth, halfHeight, 0), new Vector3(0.1f, height, length), wallMat);
        
        // Generate Lighting
        GenerateFactoryLighting(root);

        // Automate Mess Generation
        if (messGenerator != null)
        {
            messGenerator.floor = floor.transform;
            messGenerator.walls = new List<Transform> { wallFront.transform, wallBack.transform, wallLeft.transform, wallRight.transform };
            messGenerator.GenerateMess();
        }
        else
        {
            // Try to find it if not assigned
            messGenerator = FindFirstObjectByType<OfficeMessGenerator>();
            if (messGenerator != null)
            {
                messGenerator.floor = floor.transform;
                messGenerator.walls = new List<Transform> { wallFront.transform, wallBack.transform, wallLeft.transform, wallRight.transform };
                messGenerator.GenerateMess();
            }
        }

        Debug.Log("Factory Room Generated with Lighting and Decorations!");
    }

    private void GenerateFactoryLighting(GameObject root)
    {
        GameObject lightsRoot = new GameObject("FactoryLights");
        lightsRoot.transform.parent = root.transform;
        lightsRoot.transform.localPosition = Vector3.zero;

        float lightHeight = 3.5f; // Lowered height for better visibility
        int lightsPerWall = 1; // Exactly one light per wall as requested
        float wallThickness = 0.1f;
        float halfWallThickness = wallThickness / 2f;
        
        // Side Walls (Left/Right) - Varying Z
        float spacingZ = length / (lightsPerWall + 1);
        for (int i = 1; i <= lightsPerWall; i++)
        {
            float z = -length/2 + (i * spacingZ);
            
            // Left Wall (X-)
            // Wall Center is at -width/2. Inner surface is at -width/2 + halfThickness
            Vector3 posLeft = new Vector3(-width/2 + halfWallThickness, lightHeight, z);
            CreateWallLight(lightsRoot, posLeft, Quaternion.Euler(0, 90, 0));
            
            // Right Wall (X+)
            // Wall Center is at width/2. Inner surface is at width/2 - halfThickness
            Vector3 posRight = new Vector3(width/2 - halfWallThickness, lightHeight, z);
            CreateWallLight(lightsRoot, posRight, Quaternion.Euler(0, -90, 0));
        }

        // Front/Back Walls - Varying X
        float spacingX = width / (lightsPerWall + 1);
        for (int i = 1; i <= lightsPerWall; i++)
        {
            float x = -width/2 + (i * spacingX);
            
            // Front Wall (Z+)
            // Wall Center is at length/2. Inner surface is at length/2 - halfThickness
            Vector3 posFront = new Vector3(x, lightHeight, length/2 - halfWallThickness);
            CreateWallLight(lightsRoot, posFront, Quaternion.Euler(0, 180, 0));
            
            // Back Wall (Z-)
            // Wall Center is at -length/2. Inner surface is at -length/2 + halfThickness
            Vector3 posBack = new Vector3(x, lightHeight, -length/2 + halfWallThickness);
            CreateWallLight(lightsRoot, posBack, Quaternion.Euler(0, 0, 0));
        }
    }

    private void CreateWallLight(GameObject parent, Vector3 pos, Quaternion rot)
    {
        GameObject lightObj = new GameObject("WallLight");
        lightObj.transform.parent = parent.transform;
        lightObj.transform.localPosition = pos;
        lightObj.transform.localRotation = rot;

        // 1. Base Plate (Mount) - Attached directly to wall
        GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        basePlate.name = "BasePlate";
        basePlate.transform.parent = lightObj.transform;
        // Thickness 0.02. Center at 0.01 so back face is at 0.
        basePlate.transform.localPosition = new Vector3(0, 0, 0.01f); 
        basePlate.transform.localScale = new Vector3(0.4f, 0.4f, 0.02f);
        
        Material metalMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        metalMat.color = new Color(0.3f, 0.3f, 0.35f); // Dark metal
        metalMat.SetFloat("_Smoothness", 0.2f);
        basePlate.GetComponent<Renderer>().sharedMaterial = metalMat;

        // 2. Light Fixture Body
        GameObject fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fixture.name = "FixtureBody";
        fixture.transform.parent = lightObj.transform;
        // Starts after base plate (0.02). Depth 0.15. Center at 0.02 + 0.075 = 0.095
        fixture.transform.localPosition = new Vector3(0, 0, 0.095f);
        fixture.transform.localScale = new Vector3(0.25f, 0.25f, 0.15f);
        
        // Emissive Material for the "Glass" part
        Material lightMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        lightMat.color = Color.black; // Black when off (contrast)
        lightMat.EnableKeyword("_EMISSION");
        // Multiplied by 10 for HDR intensity (Bloom effect)
        lightMat.SetColor("_EmissionColor", new Color(1.0f, 0.7f, 0.0f) * 10f); 
        fixture.GetComponent<Renderer>().sharedMaterial = lightMat;

        // 3. Light Source
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 40f; // Massive range
        l.intensity = 50.0f; // Extreme intensity to force visibility
        l.color = new Color(1.0f, 0.6f, 0.0f); // Pure Orange-Yellow
        l.shadows = LightShadows.Soft;
        l.renderMode = LightRenderMode.ForcePixel; // Force high quality rendering
        
        // Position light slightly in front of the fixture to cast light into room
        // Base(0.02) + Fixture(0.15) = 0.17. Light at 0.25 to be safe and clear.
        l.transform.localPosition = new Vector3(0, 0, 0.25f);
    }

    private GameObject CreateCube(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.parent = parent.transform;
        cube.transform.localPosition = pos;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = mat;
        return cube;
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
            mat.SetFloat("_Smoothness", 0.0f); // No shine
            mat.SetFloat("_Metallic", 0.0f);
        }
        else
        {
            mat.SetFloat("_Glossiness", 0.0f);
        }
        
        return mat;
    }
}
