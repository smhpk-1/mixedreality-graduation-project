using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class OfficeMessGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform floor;
    public List<Transform> walls = new List<Transform>(); // Added walls list

    [Header("Settings")]
    public int floorPaperCount = 50;
    public Color paperColor = new Color(0.95f, 0.95f, 0.95f);
    public Color inkColor = new Color(0.1f, 0.1f, 0.1f); // Dark grey for text

    private string[] reportTexts = new string[] {
        "FACTORY REPORT\n\nProduction: 120%\nEfficiency: High\nStatus: Operational",
        "SHIPPING MANIFEST\n\nItem: Crate A-113\nDest: Sector 7\nWeight: 50kg",
        "SAFETY NOTICE\n\nDays since accident: 0\nWear helmets.\nWatch your step.",
        "MEMO: LUNCH\n\nMicrowave is broken.\nPlease do not use.\n- Management",
        "INVENTORY LIST\n\nBolts: 5000\nScrews: 2000\nWrenches: 5\nHammers: 2",
        "URGENT\n\nMeeting at 14:00\nTopic: Budget Cuts\nRoom: 3B",
        "CONFIDENTIAL\n\nProject X is\nproceeding as\nplanned.",
        "REMINDER\n\nSubmit timesheets\nby Friday 5PM.\nNo exceptions."
    };

    [ContextMenu("Generate Mess")]
    public void GenerateMess()
    {
        // Create a parent for the mess if it doesn't exist
        Transform messRoot = transform.Find("Generated_Mess");
        if (messRoot != null) DestroyImmediate(messRoot.gameObject);
        
        messRoot = new GameObject("Generated_Mess").transform;
        messRoot.parent = transform;
        messRoot.localPosition = Vector3.zero;

        // Create Paper Material
        Material paperMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        paperMat.color = paperColor;

        // 1. Scatter on Floor (Random)
        if (floor != null)
        {
            Bounds floorBounds = floor.GetComponent<Renderer>().bounds;
            
            for (int i = 0; i < floorPaperCount; i++)
            {
                Vector3 pos = GetRandomPointInBounds(floorBounds);
                pos.y = floorBounds.max.y + 0.01f * (i % 5); // Increased gap to fix Z-fighting flickering
                
                GameObject paper = CreatePaper(messRoot, "FloorPaper_" + i, paperMat);
                paper.transform.position = pos;
                
                // Random rotation on floor
                paper.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
        }

        // 2. Wall Decorations
        if (walls != null && walls.Count > 0)
        {
            // Pick one wall for the Notice Board
            int boardWallIndex = Random.Range(0, walls.Count);
            
            for (int i = 0; i < walls.Count; i++)
            {
                if (walls[i] == null) continue;

                if (i == boardWallIndex)
                {
                    GenerateNoticeBoard(walls[i], messRoot);
                }
                else
                {
                    GenerateWallPapers(walls[i], messRoot, paperMat);
                }
            }
        }
        
        Debug.Log("Factory mess generated with English reports and Wall Decorations!");
    }

    void GenerateNoticeBoard(Transform wall, Transform root)
    {
        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "NoticeBoard";
        board.transform.parent = root;
        
        // Position: Center of wall, slightly offset
        Bounds b = wall.GetComponent<Renderer>().bounds;
        Vector3 center = b.center;
        
        // Determine orientation based on wall normal (approximate)
        // If wall is wide on X, normal is Z. If wide on Z, normal is X.
        bool isZWall = b.size.x > b.size.z; // Front/Back walls are wide on X
        
        Vector3 normal = Vector3.zero;
        if (isZWall)
        {
            // Check if it's Front (Z+) or Back (Z-)
            // We can check position relative to 0,0,0 or just use the bounds center
            normal = (center.z > 0) ? Vector3.back : Vector3.forward; // Point INWARDS
        }
        else
        {
            normal = (center.x > 0) ? Vector3.left : Vector3.right; // Point INWARDS
        }

        // Offset from wall
        // Wall thickness is 0.1f. Half is 0.05f.
        // We want to be on the surface: center + (normal * 0.05f) + gap
        Vector3 surfacePos = center + (normal * 0.06f);
        
        // Height Adjustment for Board
        // Center of board should be at eye level (approx 1.7m)
        // Current center.y is wall center (5m).
        surfacePos.y = 1.7f;

        board.transform.position = surfacePos;
        board.transform.rotation = Quaternion.LookRotation(normal);
        board.transform.localScale = new Vector3(3f, 2f, 0.05f); // Larger board (3m x 2m)
        
        // Material (Cork or Dark Green)
        Material boardMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        boardMat.color = new Color(0.15f, 0.25f, 0.15f); // Darker Green
        board.GetComponent<Renderer>().sharedMaterial = boardMat;

        // Add Title Text
        GameObject textObj = new GameObject("BoardTitle");
        textObj.transform.SetParent(board.transform, false);
        // Changed Z from -0.55f to 0.55f to be on the front face (Z+ faces the room)
        textObj.transform.localPosition = new Vector3(0, 0.35f, 0.55f); 
        textObj.transform.localRotation = Quaternion.Euler(0, 180, 0); // Face out
        
        // Counteract parent non-uniform scale to keep text aspect ratio correct
        // Parent: (3, 2, 0.05)
        // We want text scale to be roughly uniform in world space.
        // Let's set a base scale of 0.05.
        // X: 0.05 / 3 = 0.0166
        // Y: 0.05 / 2 = 0.025
        textObj.transform.localScale = new Vector3(0.0166f, 0.025f, 1f); 

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = "PRODUCTION SCHEDULE";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1;
        tmp.fontSizeMax = 100;
        
        // RectTransform to fit the top area
        // In local space of textObj (which is scaled down), we need large dimensions.
        // Board width in world = 3. Text scale X in world = 0.0166 * 3 = 0.05.
        // We want text width ~2.8 world units.
        // Rect Width = 2.8 / 0.05 = 56.
        RectTransform rtTitle = textObj.GetComponent<RectTransform>();
        rtTitle.sizeDelta = new Vector2(50f, 10f);
        
        // Add List Text
        GameObject listObj = new GameObject("BoardList");
        listObj.transform.SetParent(board.transform, false);
        // Changed Z from -0.55f to 0.55f
        listObj.transform.localPosition = new Vector3(0, -0.1f, 0.55f); 
        listObj.transform.localRotation = Quaternion.Euler(0, 180, 0);
        listObj.transform.localScale = new Vector3(0.0166f, 0.025f, 1f); // Same scale fix

        TextMeshPro tmpList = listObj.AddComponent<TextMeshPro>();
        tmpList.text = "- Check Conveyor Belts\n- Inspect Sorting Mechanism\n- Report Anomalies\n- Clean Workstation\n- Safety Drill at 15:00";
        tmpList.alignment = TextAlignmentOptions.TopLeft;
        tmpList.color = Color.white;
        tmpList.enableAutoSizing = true;
        tmpList.fontSizeMin = 1;
        tmpList.fontSizeMax = 80;
        
        RectTransform rtList = listObj.GetComponent<RectTransform>();
        rtList.sizeDelta = new Vector2(45f, 30f); // Large area for list
    }

    void GenerateWallPapers(Transform wall, Transform root, Material paperMat)
    {
        Bounds b = wall.GetComponent<Renderer>().bounds;
        bool isZWall = b.size.x > b.size.z;
        
        Vector3 normal = Vector3.zero;
        if (isZWall) normal = (b.center.z > 0) ? Vector3.back : Vector3.forward;
        else normal = (b.center.x > 0) ? Vector3.left : Vector3.right;

        // Random Grid
        int rows = Random.Range(2, 4);
        int cols = Random.Range(3, 6);
        
        float width = isZWall ? b.size.x : b.size.z;
        // float height = b.size.y; // Unused now
        
        // Human Eye Level Logic
        // Average eye level is around 1.6m - 1.7m.
        // We want papers to be around this height, maybe slightly spread.
        // Let's center the grid around Y = 1.8m
        float centerHeight = 1.8f;
        float gridHeight = 1.5f; // Total height of the paper area
        
        float startX = -width / 3f;
        float startY = centerHeight + (gridHeight / 2f); // Start from top of the grid area
        float gapX = width / (cols + 1);
        float gapY = gridHeight / (rows + 1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                // Skip some randomly
                if (Random.value > 0.7f) continue;

                GameObject paper = CreatePaper(root, $"WallPaper_{r}_{c}", paperMat);
                
                // Position relative to wall center
                Vector3 localPos = Vector3.zero;
                
                // Calculate Y position relative to wall center
                // Wall center Y is at height/2 (e.g., 5m if height is 10m).
                // We want absolute Y to be (startY - r*gapY).
                // So local Y = (AbsoluteY) - (WallCenterY)
                float absoluteY = startY - (r * gapY);
                float localY = absoluteY - b.center.y;

                if (isZWall)
                {
                    localPos = new Vector3(startX + (c * gapX), localY, 0);
                    // Add randomness
                    localPos += new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0);
                }
                else
                {
                    // For X walls, we move along Z
                    localPos = new Vector3(0, localY, startX + (c * gapX));
                     localPos += new Vector3(0, Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
                }

                // Fix Wall Alignment
                // Wall thickness is 0.1f. Half is 0.05f.
                // We want to be SAFE from Z-fighting and being inside.
                // Let's use a HUGE gap to test: 0.2f (20cm from surface)
                
                Vector3 surfacePos = b.center + (normal * 0.2f); 
                
                // We only use X and Z from surfacePos, and add our calculated local Y offset to center Y
                // Actually localPos already contains the offset from center.
                // So:
                paper.transform.position = surfacePos + localPos;
                
                // Reset Y to be correct absolute height (override the center Y calculation to be safe)
                Vector3 finalPos = paper.transform.position;
                finalPos.y = absoluteY;
                paper.transform.position = finalPos;

                paper.transform.rotation = Quaternion.LookRotation(normal);
                
                // Rotate 90 deg to face out properly (Paper creates flat on Y, we need it flat on Z/X)
                // CreatePaper makes it flat on Y (lying down).
                // LookRotation(normal) makes Z point to normal.
                // We want Paper's UP (Y) to point to normal.
                // So we rotate -90 on X relative to that?
                paper.transform.rotation = Quaternion.LookRotation(normal) * Quaternion.Euler(-90, 0, 0);
                
                // Add slight random tilt
                paper.transform.Rotate(0, 0, Random.Range(-5f, 5f));
            }
        }
    }

    // Removed GenerateOrderedWallPapers

    Vector3 GetRandomPointInBounds(Bounds b)
    {
        return new Vector3(
            Random.Range(b.min.x * 0.8f, b.max.x * 0.8f), 
            Random.Range(b.min.y * 0.8f, b.max.y * 0.8f),
            Random.Range(b.min.z * 0.8f, b.max.z * 0.8f)
        );
    }

    GameObject CreatePaper(Transform parent, string name, Material paperMat)
    {
        // Create the Paper Sheet
        GameObject paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        paper.name = name;
        paper.transform.parent = parent;
        // Increased size by 1.5x to make them more visible (approx A3 size)
        paper.transform.localScale = new Vector3(0.315f, 0.002f, 0.445f); 
        paper.GetComponent<Renderer>().sharedMaterial = paperMat;
        DestroyImmediate(paper.GetComponent<Collider>());

        // Create Text using TextMeshPro
        GameObject textObj = new GameObject("PaperText");
        textObj.transform.SetParent(paper.transform, false);
        
        // Position slightly above the paper surface
        // Paper Y scale is 0.002. Local Y=0.5 is surface.
        // We set it to 5.0 to be safely above (1cm gap in world space)
        textObj.transform.localPosition = new Vector3(0, 5.0f, 0); 
        
        // Rotate to lie flat
        textObj.transform.localRotation = Quaternion.Euler(90, 0, 0);

        // Fix Aspect Ratio Distortion
        // Parent X scale is 0.315, Z scale is 0.445.
        // Text X aligns with Parent X. Text Y aligns with Parent Z.
        // We scale Text X up so that Global Scale X matches Global Scale Y.
        // Factor = 0.445 / 0.315 = ~1.41
        textObj.transform.localScale = new Vector3(1.41f, 1f, 1f);

        // Add TextMeshPro
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        
        // Setup Text
        tmp.text = reportTexts[Random.Range(0, reportTexts.Length)];
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        
        // Auto Size for best fit
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = 10f;
        
        // Adjust RectTransform to fit paper
        // We want the text box to cover the paper surface with some padding.
        // Global Width Target = PaperWidth * 0.8 = 0.315 * 0.8 = 0.252
        // Global Height Target = PaperHeight * 0.8 = 0.445 * 0.8 = 0.356
        // Global Scale X = 0.315 * 1.41 = 0.444
        // Global Scale Y = 0.445 * 1 = 0.445
        // Rect Width = Target / Scale = 0.252 / 0.444 = ~0.57
        // Rect Height = Target / Scale = 0.356 / 0.445 = ~0.8
        
        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0.57f, 0.8f);

        // Try to load default font
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) tmp.font = font;

        return paper;
    }
}
