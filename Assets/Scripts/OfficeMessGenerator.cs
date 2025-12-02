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
    public int wallPaperCount = 10;
    public Color paperColor = new Color(0.95f, 0.95f, 0.95f);

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

        // 1. Scatter on Floor
        if (floor != null)
        {
            Vector3 floorSize = floor.localScale; // Assuming standard cube scaled
            // Unity Plane is 10x10, Cube is 1x1. Assuming Cube based room generator.
            // Adjust bounds based on the object type.
            Bounds floorBounds = floor.GetComponent<Renderer>().bounds;
            
            for (int i = 0; i < floorPaperCount; i++)
            {
                Vector3 pos = GetRandomPointInBounds(floorBounds);
                pos.y = floorBounds.max.y + 0.005f * (i % 5); // Stack slightly to avoid z-fighting
                
                GameObject paper = CreatePaper(messRoot, "FloorPaper_" + i, paperMat);
                paper.transform.position = pos;
                
                // Random rotation on floor
                paper.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
        }

        // 2. Stick to Walls
        GenerateWallPapers(leftWall, messRoot, paperMat, Vector3.right); // Left wall faces Right (usually)
        GenerateWallPapers(rightWall, messRoot, paperMat, Vector3.left); // Right wall faces Left
        
        Debug.Log("Office mess generated!");
    }

    void GenerateWallPapers(Transform wall, Transform root, Material mat, Vector3 normalDirection)
    {
        if (wall == null) return;

        Bounds wallBounds = wall.GetComponent<Renderer>().bounds;

        for (int i = 0; i < wallPaperCount; i++)
        {
            Vector3 pos = GetRandomPointInBounds(wallBounds);
            
            // Pull out slightly from the wall based on normal
            // We need to find which axis is the "thickness" to avoid putting paper inside the wall
            // But simpler is to just use the bounds surface.
            
            // For a simple box room, we can approximate.
            // If normal is (1,0,0), we want max X.
            if (normalDirection.x > 0) pos.x = wallBounds.max.x + 0.01f;
            else if (normalDirection.x < 0) pos.x = wallBounds.min.x - 0.01f;
            
            GameObject paper = CreatePaper(root, "WallPaper_" + i, mat);
            paper.transform.position = pos;

            // Rotate to face the room
            if (normalDirection.x != 0)
                paper.transform.rotation = Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, 90, 0); // Vertical paper
            
            // Add slight random tilt
            paper.transform.Rotate(Vector3.forward, Random.Range(-5f, 5f));
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

    GameObject CreatePaper(Transform parent, string name, Material mat)
    {
        GameObject paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        paper.name = name;
        paper.transform.parent = parent;
        
        // A4 Paper size approx: 21cm x 30cm
        // Very thin cube
        paper.transform.localScale = new Vector3(0.21f, 0.001f, 0.297f);
        
        paper.GetComponent<Renderer>().sharedMaterial = mat;
        
        // Remove collider so they don't interfere with physics/walking
        DestroyImmediate(paper.GetComponent<Collider>());

        return paper;
    }
}
