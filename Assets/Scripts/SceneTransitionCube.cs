using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach this script to any GameObject (e.g. a Cube) in Scene 1.
/// When the player grabs it, transitions to Scene 2.
/// The script will automatically set up XR grab interaction, purple color, and floating animation.
/// </summary>
public class SceneTransitionCube : MonoBehaviour
{
    [Header("Scene Transition")]
    public string targetScene = "Scene 2";
    
    [Header("Visual")]
    public Color cubeColor = new Color(0.6f, 0.2f, 0.9f, 1f); // Purple
    public float glowIntensity = 0.3f;
    public float floatAmplitude = 0.05f;
    public float floatSpeed = 2f;
    public float rotateSpeed = 30f;
    
    private Vector3 basePosition;
    private bool isGrabbed = false;
    
    private void Start()
    {
        basePosition = transform.position;
        
        // Apply purple material with glow
        SetupVisuals();
        
        // Setup XR Grab Interactable if not present
        SetupGrab();
        
        Debug.Log($"[SceneTransitionCube] Ready at {transform.position}. Grab to go to {targetScene}");
    }
    
    private void SetupVisuals()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;
        
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = cubeColor;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", cubeColor);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", cubeColor);
        
        // Emission glow
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", cubeColor * glowIntensity);
        
        renderer.material = mat;
    }
    
    private void SetupGrab()
    {
        // Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.mass = 0.5f;
        
        // Collider
        Collider col = GetComponent<Collider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();
        
        // XR Grab Interactable
        XRGrabInteractable grab = GetComponent<XRGrabInteractable>();
        if (grab == null) grab = gameObject.AddComponent<XRGrabInteractable>();
        grab.movementType = XRGrabInteractable.MovementType.Instantaneous;
        grab.throwOnDetach = false;
        grab.interactionLayers = -1;
        
        if (!grab.colliders.Contains(col))
        {
            grab.colliders.Add(col);
        }
        
        // Listen for grab
        grab.selectEntered.AddListener(OnCubeGrabbed);
    }
    
    private void Update()
    {
        if (isGrabbed) return;
        
        // Floating animation
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = basePosition + Vector3.up * yOffset;
        
        // Slow rotation
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }
    
    private void OnCubeGrabbed(SelectEnterEventArgs args)
    {
        if (isGrabbed) return;
        isGrabbed = true;
        
        Debug.Log($"[SceneTransitionCube] Grabbed! Loading {targetScene}...");
        StartCoroutine(TransitionAfterDelay(0.5f));
    }
    
    private System.Collections.IEnumerator TransitionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(targetScene);
    }
}
