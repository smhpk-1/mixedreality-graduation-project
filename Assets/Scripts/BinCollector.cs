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

        // Generate Procedural Sounds if missing
        if (wrongSound == null) wrongSound = GenerateBuzzerSound();
        if (correctSound == null) correctSound = GenerateChimeSound();
        
        // Add Reverb for "Container" effect
        AudioReverbFilter reverb = GetComponent<AudioReverbFilter>();
        if (reverb == null)
        {
            reverb = gameObject.AddComponent<AudioReverbFilter>();
            reverb.reverbPreset = AudioReverbPreset.StoneCorridor; // Echoey industrial sound
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

        // Notify ScoreBoard
        var scoreBoard = FindFirstObjectByType<FactoryScoreBoard>();
        if (scoreBoard != null) scoreBoard.OnCorrectSort();

        if (destroyOnCorrect)
        {
            Destroy(cube, 0.5f); // Destroy after short delay
        }
    }

    void HandleWrong(GameObject cube)
    {
        Debug.Log("WRONG! " + cube.name + " in " + gameObject.name);

        if (wrongSound != null) audioSource.PlayOneShot(wrongSound);

        // Notify ScoreBoard
        var scoreBoard = FindFirstObjectByType<FactoryScoreBoard>();
        if (scoreBoard != null) scoreBoard.OnWrongSort();

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

    // --- Procedural Audio Generation ---

    private AudioClip GenerateBuzzerSound()
    {
        // Low frequency Sawtooth wave for a harsh "Wrong" buzzer
        int sampleRate = 44100;
        float frequency = 150f;
        float duration = 0.5f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate * frequency;
            // Sawtooth: 2 * (t - floor(t + 0.5))
            samples[i] = 2f * (t - Mathf.Floor(t + 0.5f));
            
            // Apply envelope (fade out)
            samples[i] *= 1f - ((float)i / sampleCount);
        }

        AudioClip clip = AudioClip.Create("ProcBuzzer", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateChimeSound()
    {
        // High frequency Sine wave for a pleasant "Correct" chime
        int sampleRate = 44100;
        float frequency = 880f; // A5
        float duration = 0.3f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate * frequency;
            samples[i] = Mathf.Sin(2 * Mathf.PI * t);
            
            // Apply envelope (fade out)
            samples[i] *= 1f - ((float)i / sampleCount);
        }

        AudioClip clip = AudioClip.Create("ProcChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
