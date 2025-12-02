using UnityEngine;

public class FactoryScoreBoard : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 position = new Vector3(0, 2.5f, 3.8f); // High on the back wall
    public Color textColor = Color.red; // Retro LED look

    private TextMesh textMesh;
    private int score = 0;
    private int mistakes = 0;

    // Audio Clips (Generated Procedurally)
    private AudioClip correctClip;
    private AudioClip wrongClip;
    private AudioSource audioSource;

    private void Start()
    {
        GenerateBoard();
        GenerateSounds();
        UpdateDisplay();
        
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    [ContextMenu("Generate Board")]
    public void GenerateBoard()
    {
        // Position the board
        transform.position = position;
        
        // Create Text Mesh if missing
        textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
        }

        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 100;
        textMesh.characterSize = 0.05f;
        textMesh.color = textColor;
        
        // Rotate to face room (assuming back wall is at Z+)
        // If back wall is Z+, we need to face Z- (180 deg)
        transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    void GenerateSounds()
    {
        // Generate a high pitched "Ding"
        correctClip = CreateTone(44000, 880, 0.3f); 
        
        // Generate a low pitched "Buzz"
        wrongClip = CreateTone(44000, 150, 0.5f);
    }

    public void OnCorrectSort()
    {
        score++;
        UpdateDisplay();
        if (audioSource && correctClip) audioSource.PlayOneShot(correctClip);
    }

    public void OnWrongSort()
    {
        mistakes++;
        UpdateDisplay();
        if (audioSource && wrongClip) audioSource.PlayOneShot(wrongClip);
    }

    void UpdateDisplay()
    {
        if (textMesh != null)
        {
            textMesh.text = $"PROCESSED: {score}\nERRORS: {mistakes}";
        }
    }

    // Simple Sine Wave Generator
    private AudioClip CreateTone(int sampleRate, float frequency, float duration)
    {
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t);
            
            // Decay (fade out)
            samples[i] *= 1.0f - (i / (float)sampleCount);
        }

        AudioClip clip = AudioClip.Create("Tone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
