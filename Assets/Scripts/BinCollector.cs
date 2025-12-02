using UnityEngine;

public class BinCollector : MonoBehaviour
{
    [Header("Settings")]
    public string targetCubeName = "RedCube"; // "RedCube" or "BlueCube"
    public bool destroyOnCorrect = true;

    [Header("Feedback")]
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public Material burntMaterial; // Material to apply when wrong

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Ensure we have a trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.4f, 0.4f, 0.4f); // Approximate inner size
        }
        else if (!col.isTrigger)
        {
            // If there is a collider but it's not a trigger (e.g. the floor of the bin), 
            // we might need a separate trigger. 
            // But usually the bin object itself can be the trigger if the walls are children.
            // Let's warn the user or add a child trigger.
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore non-cubes (like hands or other props)
        if (!other.name.Contains("Cube")) return;

        if (other.name.Contains(targetCubeName))
        {
            HandleCorrect(other.gameObject);
        }
        else
        {
            HandleWrong(other.gameObject);
        }
    }

    void HandleCorrect(GameObject cube)
    {
        Debug.Log("Correct! " + cube.name + " in " + gameObject.name);
        
        if (correctSound != null) audioSource.PlayOneShot(correctSound);

        if (destroyOnCorrect)
        {
            Destroy(cube, 0.5f); // Destroy after short delay
        }
    }

    void HandleWrong(GameObject cube)
    {
        Debug.Log("WRONG! " + cube.name + " in " + gameObject.name);

        if (wrongSound != null) audioSource.PlayOneShot(wrongSound);

        // Visual Feedback: Turn it black (Burnt)
        Renderer r = cube.GetComponent<Renderer>();
        if (r != null)
        {
            if (burntMaterial != null)
            {
                r.sharedMaterial = burntMaterial;
            }
            else
            {
                r.material.color = Color.black; // Fallback
            }
        }
    }
}
