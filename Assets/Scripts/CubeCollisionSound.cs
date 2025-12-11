using UnityEngine;

public class CubeCollisionSound : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip thudSound;
    private float minVelocity = 1.0f; // Minimum velocity to play sound

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D Sound
            audioSource.minDistance = 1.0f;
            audioSource.maxDistance = 15.0f;
        }

        thudSound = GenerateThudSound();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if we hit something hard (like Floor or other objects)
        // We can check relative velocity magnitude
        if (collision.relativeVelocity.magnitude > minVelocity)
        {
            // Modulate volume based on impact speed
            float volume = Mathf.Clamp01(collision.relativeVelocity.magnitude / 10f);
            
            // Randomize pitch slightly for realism
            audioSource.pitch = Random.Range(0.8f, 1.2f);
            audioSource.PlayOneShot(thudSound, volume);
        }
    }

    private AudioClip GenerateThudSound()
    {
        // Generate a low frequency noise burst for a "thud"
        int sampleRate = 44100;
        float duration = 0.15f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random rng = new System.Random();

        for (int i = 0; i < sampleCount; i++)
        {
            // White noise
            float noise = (float)rng.NextDouble() * 2f - 1f;
            
            // Exponential decay envelope
            float t = (float)i / sampleCount;
            float envelope = Mathf.Exp(-15f * t);
            
            // Low pass filter approximation (simple moving average would be better, but let's just use lower frequency noise)
            // Actually, let's just use the noise with decay, but maybe smooth it a bit to remove high pitch
            
            samples[i] = noise * envelope;
        }
        
        // Simple Low-Pass Filter to make it duller (thud-like)
        float[] filteredSamples = new float[sampleCount];
        float lastSample = 0;
        float alpha = 0.1f; // Smoothing factor
        
        for (int i = 0; i < sampleCount; i++)
        {
            lastSample = lastSample + alpha * (samples[i] - lastSample);
            filteredSamples[i] = lastSample;
        }

        AudioClip clip = AudioClip.Create("ProcThud", sampleCount, 1, sampleRate, false);
        clip.SetData(filteredSamples, 0);
        return clip;
    }
}
