using UnityEngine;
using System.Collections.Generic;

public class FloatingMessController : MonoBehaviour
{
    [Header("Settings")]
    public float floatSpeed = 0.5f;
    public float rotationSpeed = 10f;
    public float riseAmount = 1.5f; // How high they go
    public float noiseScale = 0.5f; // For random movement

    private bool isFloating = false;
    private List<Transform> papers = new List<Transform>();
    private List<Vector3> initialPositions = new List<Vector3>();
    private List<Vector3> randomOffsets = new List<Vector3>();

    public void StartFloating()
    {
        // Feature removed as per request. Papers will remain grounded.
        isFloating = false;
    }

    void Update()
    {
        // Feature removed.
    }
}
