using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class OfficeMessGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform floor;
    // public Transform leftWall; // Removed
    // public Transform rightWall; // Removed

    [Header("Settings")]
    public int floorPaperCount = 50;
    // public int wallPaperRows = 3; // Removed
    // public int wallPaperCols = 4; // Removed
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

        // Wall generation removed as requested.
        
        Debug.Log("Clean office mess generated with English reports!");
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
        textObj.transform.localPosition = new Vector3(0, 0.6f, 0); 
        
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
        tmp.enableWordWrapping = true;
        
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
