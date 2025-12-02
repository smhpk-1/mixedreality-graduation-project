using UnityEngine;
using System;

public class WallClock : MonoBehaviour
{
    [Header("Hand References")]
    public Transform hourHand;
    public Transform minuteHand;
    public Transform secondHand;

    [Header("Settings")]
    public bool smoothSeconds = false; // Set true for sweeping motion, false for ticking

    void Update()
    {
        DateTime time = DateTime.Now;

        // Calculate time values
        float seconds = smoothSeconds ? 
            (time.Second + time.Millisecond / 1000f) : time.Second;
        float minutes = time.Minute + (seconds / 60f);
        float hours = (time.Hour % 12) + (minutes / 60f);

        // Calculate angles (360 degrees / units)
        // Negative because rotation is clockwise
        float secondAngle = seconds * 6f;   // 360 / 60
        float minuteAngle = minutes * 6f;   // 360 / 60
        float hourAngle = hours * 30f;      // 360 / 12

        // Apply rotations
        if (hourHand) hourHand.localRotation = Quaternion.Euler(0, 0, -hourAngle);
        if (minuteHand) minuteHand.localRotation = Quaternion.Euler(0, 0, -minuteAngle);
        if (secondHand) secondHand.localRotation = Quaternion.Euler(0, 0, -secondAngle);
    }

    [ContextMenu("Generate Clock Visuals")]
    public void GenerateVisuals()
    {
        // Clear existing children to avoid duplicates
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        // 1. Create Face
        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        face.name = "ClockFace";
        face.transform.SetParent(transform, false);
        face.transform.localScale = new Vector3(1f, 0.05f, 1f);
        face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Face Z-axis
        
        // Color the face white
        Material faceMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        faceMat.color = Color.white;
        face.GetComponent<Renderer>().sharedMaterial = faceMat;

        // 2. Create Rim
        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "ClockRim";
        rim.transform.SetParent(transform, false);
        rim.transform.localScale = new Vector3(1.1f, 0.04f, 1.1f);
        rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        
        Material rimMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        rimMat.color = Color.black;
        rim.GetComponent<Renderer>().sharedMaterial = rimMat;

        // 3. Create Hands
        hourHand = CreateHand("HourHand", new Vector3(0.1f, 0.3f, 0.05f), Color.black, 0.1f);
        minuteHand = CreateHand("MinuteHand", new Vector3(0.08f, 0.4f, 0.05f), Color.black, 0.11f);
        secondHand = CreateHand("SecondHand", new Vector3(0.04f, 0.45f, 0.05f), Color.red, 0.12f);
        
        // 4. Create Numbers
        CreateNumbers();

        Debug.Log("Clock generated! You can now move/scale the 'WallClock' object.");
    }

    private void CreateNumbers()
    {
        float radius = 0.4f;
        for (int i = 1; i <= 12; i++)
        {
            GameObject numObj = new GameObject("Num_" + i);
            numObj.transform.SetParent(transform, false);
            
            // Calculate position (Clockwise starting from 12)
            float angle = i * 30f * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * radius;
            float y = Mathf.Cos(angle) * radius;
            
            // Z is -0.06f to sit slightly in front of the face
            numObj.transform.localPosition = new Vector3(x, y, -0.06f);
            
            TextMesh tm = numObj.AddComponent<TextMesh>();
            tm.text = i.ToString();
            tm.color = Color.black;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 60; // High font size for quality
            tm.characterSize = 0.03f; // Scale down to fit
        }
    }

    private Transform CreateHand(string name, Vector3 size, Color color, float zOffset)
    {
        // Create a pivot object (this is what rotates)
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = new Vector3(0, 0, -zOffset); // Slightly in front of face

        // Create the visual part (offset so it rotates around one end)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(pivot.transform, false);
        visual.transform.localScale = size;
        visual.transform.localPosition = new Vector3(0, size.y / 2, 0); // Shift up so pivot is at bottom

        // Color
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = color;
        visual.GetComponent<Renderer>().sharedMaterial = mat;

        return pivot.transform;
    }
}
