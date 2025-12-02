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
    public Color inkColor = new Color(0.1f, 0.1f, 0.1f); // Dark grey for text lines

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

        // Create Ink Material (for the text lines)
        Material inkMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        inkMat.color = inkColor;

        // 1. Scatter on Floor (Random)
        if (floor != null)
        {
            Bounds floorBounds = floor.GetComponent<Renderer>().bounds;
            
            for (int i = 0; i < floorPaperCount; i++)
            {
                Vector3 pos = GetRandomPointInBounds(floorBounds);
                pos.y = floorBounds.max.y + 0.01f * (i % 5); // Increased gap to fix Z-fighting flickering
                
                GameObject paper = CreatePaper(messRoot, "FloorPaper_" + i, paperMat, inkMat);
                paper.transform.position = pos;
                
                // Random rotation on floor
                paper.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
        }

        // 2. Stick to Walls (Ordered / Nizami)
        GenerateOrderedWallPapers(leftWall, messRoot, paperMat, inkMat, Vector3.right); 
        GenerateOrderedWallPapers(rightWall, messRoot, paperMat, inkMat, Vector3.left); 
        
        Debug.Log("Clean office mess generated!");
    }

    void GenerateOrderedWallPapers(Transform wall, Transform root, Material paperMat, Material inkMat, Vector3 normalDirection)
    {
        if (wall == null) return;

        Bounds wallBounds = wall.GetComponent<Renderer>().bounds;
        
        // Grid Settings
        float gapH = 0.4f; 
        float gapV = 0.4f; 
        
        float totalWidth = (wallPaperCols - 1) * gapH;
        float totalHeight = (wallPaperRows - 1) * gapV;
        
        float startY = wallBounds.center.y + (totalHeight / 2); 
        float startZ = wallBounds.center.z - (totalWidth / 2);

        for (int row = 0; row < wallPaperRows; row++)
        {
            for (int col = 0; col < wallPaperCols; col++)
            {
                Vector3 pos = Vector3.zero;
                
                if (normalDirection.x != 0) 
                {
                    float xPos = (normalDirection.x > 0) ? wallBounds.max.x + 0.02f : wallBounds.min.x - 0.02f; // Increased gap from wall
                    float yPos = startY - (row * gapV);
                    float zPos = startZ + (col * gapH);
                    
                    pos = new Vector3(xPos, yPos, zPos);
                }

                GameObject paper = CreatePaper(root, $"WallPaper_{row}_{col}", paperMat, inkMat);
                paper.transform.position = pos;

                if (normalDirection.x > 0) 
                    paper.transform.rotation = Quaternion.Euler(0, 0, -90);
                else 
                    paper.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }
    }

    Vector3 GetRandomPointInBounds(Bounds b)
    {
        return new Vector3(
            Random.Range(b.min.x * 0.8f, b.max.x * 0.8f), 
            Random.Range(b.min.y * 0.8f, b.max.y * 0.8f),
            Random.Range(b.min.z * 0.8f, b.max.z * 0.8f)
        );
    }

    GameObject CreatePaper(Transform parent, string name, Material paperMat, Material inkMat)
    {
        // Create the Paper Sheet
        GameObject paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        paper.name = name;
        paper.transform.parent = parent;
        paper.transform.localScale = new Vector3(0.21f, 0.002f, 0.297f); // Slightly thicker to avoid flickering
        paper.GetComponent<Renderer>().sharedMaterial = paperMat;
        DestroyImmediate(paper.GetComponent<Collider>());

        // Create "Scribbles" (Fake Text Lines)
        // Instead of TextMesh, we use small black cubes to look like lines of text
        int lineCount = Random.Range(3, 6);
        for (int i = 0; i < lineCount; i++)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "TextLine";
            line.transform.SetParent(paper.transform, false);
            
            // Randomize line length
            float width = Random.Range(0.5f, 0.8f);
            
            // Position lines down the page
            // Paper is 1x1x1 in local space (because parent is scaled)
            // Y is thickness. Z is height of page. X is width.
            // We want to place lines along Z.
            float zPos = 0.3f - (i * 0.15f); 
            
            line.transform.localPosition = new Vector3(0, 0.6f, zPos); // Slightly above surface
            line.transform.localScale = new Vector3(width, 0.1f, 0.05f); // Thin black strip
            
            line.GetComponent<Renderer>().sharedMaterial = inkMat;
            DestroyImmediate(line.GetComponent<Collider>());
        }

        return paper;
    }
}
