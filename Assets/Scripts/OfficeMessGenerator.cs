using UnityEngine;
using System.Collections.Generic;

public class OfficeMessGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform floor;
    public Transform leftWall;
    public Transform rightWall;

    [Header("Settings")]
    public int floorPaperCount = 50;
    public int wallPaperRows = 3;
    public int wallPaperCols = 4;
    public Color paperColor = new Color(0.95f, 0.95f, 0.95f);
    public Color textColor = Color.black;

    private string[] randomTexts = new string[] { 
        "MEMO", "URGENT", "TASK #42", "CONFIDENTIAL", 
        "DO NOT TOUCH", "PLAN A", "REPORT", "ERROR", 
        "404", "FIX ME", "INVOICE", "NOTICE" 
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

        // Create Material
        Material paperMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        paperMat.color = paperColor;

        // 1. Scatter on Floor (Random)
        if (floor != null)
        {
            Bounds floorBounds = floor.GetComponent<Renderer>().bounds;
            
            for (int i = 0; i < floorPaperCount; i++)
            {
                Vector3 pos = GetRandomPointInBounds(floorBounds);
                pos.y = floorBounds.max.y + 0.005f * (i % 5); // Stack slightly
                
                GameObject paper = CreatePaper(messRoot, "FloorPaper_" + i, paperMat, true);
                paper.transform.position = pos;
                
                // Random rotation on floor
                paper.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
        }

        // 2. Stick to Walls (Ordered / Nizami)
        GenerateOrderedWallPapers(leftWall, messRoot, paperMat, Vector3.right); // Left wall faces Right
        GenerateOrderedWallPapers(rightWall, messRoot, paperMat, Vector3.left); // Right wall faces Left
        
        Debug.Log("Office mess generated with text!");
    }

    void GenerateOrderedWallPapers(Transform wall, Transform root, Material mat, Vector3 normalDirection)
    {
        if (wall == null) return;

        Bounds wallBounds = wall.GetComponent<Renderer>().bounds;
        
        // Grid Settings
        float gapH = 0.4f; // Horizontal spacing
        float gapV = 0.4f; // Vertical spacing
        
        // Center the grid on the wall
        float totalWidth = (wallPaperCols - 1) * gapH;
        float totalHeight = (wallPaperRows - 1) * gapV;
        
        float startY = wallBounds.center.y + (totalHeight / 2); 
        float startZ = wallBounds.center.z - (totalWidth / 2);

        for (int row = 0; row < wallPaperRows; row++)
        {
            for (int col = 0; col < wallPaperCols; col++)
            {
                Vector3 pos = Vector3.zero;
                
                // Calculate position based on normal
                if (normalDirection.x != 0) // Side walls
                {
                    // X is fixed (surface of wall)
                    float xPos = (normalDirection.x > 0) ? wallBounds.max.x + 0.01f : wallBounds.min.x - 0.01f;
                    float yPos = startY - (row * gapV);
                    float zPos = startZ + (col * gapH);
                    
                    pos = new Vector3(xPos, yPos, zPos);
                }

                GameObject paper = CreatePaper(root, $"WallPaper_{row}_{col}", mat, true);
                paper.transform.position = pos;

                // Rotate to face the room
                if (normalDirection.x > 0) // Left Wall facing Right
                    paper.transform.rotation = Quaternion.Euler(0, 0, -90);
                else // Right Wall facing Left
                    paper.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }
    }

    Vector3 GetRandomPointInBounds(Bounds b)
    {
        return new Vector3(
            Random.Range(b.min.x * 0.8f, b.max.x * 0.8f), // Keep away from edges
            Random.Range(b.min.y * 0.8f, b.max.y * 0.8f),
            Random.Range(b.min.z * 0.8f, b.max.z * 0.8f)
        );
    }

    GameObject CreatePaper(Transform parent, string name, Material mat, bool addText)
    {
        GameObject paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        paper.name = name;
        paper.transform.parent = parent;
        
        // A4 Paper size approx: 21cm x 30cm
        paper.transform.localScale = new Vector3(0.21f, 0.001f, 0.297f);
        
        paper.GetComponent<Renderer>().sharedMaterial = mat;
        
        // Remove collider
        DestroyImmediate(paper.GetComponent<Collider>());

        if (addText)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(paper.transform, false);
            
            // Position slightly above the paper surface
            // Local Y is the thickness (0.001), so 0.5 is surface. 
            // But since we scaled Y by 0.001, 0.5 local units is tiny.
            // Wait, localPosition is relative to parent. Parent Y scale is 0.001.
            // If we put 0.6, it's 0.6 * 0.001 = 0.0006 units.
            // Actually, TextMesh is not affected by parent scale if we reset its scale?
            // No, it is.
            
            // Let's put it at Y = 0.6 (relative to parent center).
            textObj.transform.localPosition = new Vector3(0, 0.6f, 0); 
            
            // Rotate text to lie flat on the paper (X-Z plane)
            textObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            
            // Scale text up because parent Y is squashed?
            // Parent scale: (0.21, 0.001, 0.297)
            // If we set text scale (1,1,1), it will be (0.21, 0.001, 0.297) in world.
            // That squashes the text height to almost 0.
            // We need to compensate for parent's Y scale.
            
            float invScaleY = 1f / 0.001f;
            textObj.transform.localScale = new Vector3(0.1f, 0.1f * invScaleY, 0.1f); 

            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.text = randomTexts[Random.Range(0, randomTexts.Length)];
            tm.color = textColor;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 40;
            tm.characterSize = 0.1f; // Adjusted for scale
        }

        return paper;
    }
}
