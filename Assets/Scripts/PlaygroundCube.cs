using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

namespace MusicSpace
{
    /// <summary>
    /// A colored cube for Scene 2 that can be grabbed and thrown at reactive walls.
    /// When it collides with a ColorReactiveWall, the wall changes to match this cube's color.
    /// After collision, the cube respawns at its original position.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlaygroundCube : MonoBehaviour
    {
        [Header("Identity")]
        public Color cubeColor = Color.white;
        
        [Header("Spawn Settings")]
        public Vector3 spawnPosition;
        public float respawnDelay = 1.5f;
        
        [Header("Audio")]
        public AudioClip collisionSound;
        [Range(0f, 1f)] public float collisionVolume = 0.8f;
        
        [Header("Physics")]
        public float minVelocityForWallChange = 0.5f; // Lowered threshold for easier triggering

        private XRGrabInteractable grabInteractable;
        private Rigidbody rb;
        private AudioSource audioSource;
        private bool isGrabbed = false;
        private bool hasHitWall = false;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();
            
            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 15f;
            
            // Store spawn position if not set
            if (spawnPosition == Vector3.zero)
            {
                spawnPosition = transform.position;
            }
            
            // Generate collision sound if not assigned
            if (collisionSound == null)
            {
                collisionSound = GenerateCollisionSound();
            }
            
            Debug.Log($"PlaygroundCube initialized: {gameObject.name}, color: {cubeColor}");
        }

        private void OnEnable()
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }

        private void OnDisable()
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            hasHitWall = false;
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            isGrabbed = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Allow collision detection even while grabbed for better responsiveness
            if (hasHitWall) return;
            
            float velocity = collision.relativeVelocity.magnitude;
            
            Debug.Log($"PlaygroundCube {gameObject.name} hit {collision.gameObject.name}, velocity: {velocity}");
            
            // Check if we hit a reactive wall
            ColorReactiveWall wall = collision.gameObject.GetComponent<ColorReactiveWall>();
            if (wall != null)
            {
                Debug.Log($"PlaygroundCube {gameObject.name}: Found ColorReactiveWall on {collision.gameObject.name}");
                
                if (velocity >= minVelocityForWallChange)
                {
                    hasHitWall = true;
                    
                    Debug.Log($"PlaygroundCube {gameObject.name}: Changing wall color to {cubeColor}");
                    
                    // Change wall color to match this cube (permanent until next hit)
                    wall.ChangeColorInstant(cubeColor, 0f);
                    
                    // Play collision sound with pitch based on velocity
                    PlayCollisionSound(velocity);
                    
                    // Respawn cube after delay
                    StartCoroutine(RespawnAfterDelay());
                }
                else
                {
                    Debug.Log($"PlaygroundCube {gameObject.name}: Velocity too low ({velocity} < {minVelocityForWallChange})");
                }
            }
            else if (velocity > 0.3f)
            {
                // Play softer sound for non-wall collisions (floor, other cubes)
                PlayCollisionSound(velocity * 0.3f);
            }
        }

        private void PlayCollisionSound(float velocity)
        {
            if (collisionSound == null || audioSource == null) return;
            
            // Adjust pitch based on velocity (faster = higher pitch)
            float normalizedVelocity = Mathf.Clamp01(velocity / 10f);
            audioSource.pitch = Mathf.Lerp(0.8f, 1.3f, normalizedVelocity);
            audioSource.volume = collisionVolume * Mathf.Lerp(0.5f, 1f, normalizedVelocity);
            audioSource.PlayOneShot(collisionSound);
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            ResetCube();
        }

        /// <summary>
        /// Reset cube to its original spawn position and state.
        /// </summary>
        public void ResetCube()
        {
            // Reset position and rotation
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;
            
            // Reset physics
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Reset state
            hasHitWall = false;
            isGrabbed = false;
        }

        /// <summary>
        /// Generate a procedural collision sound based on cube color.
        /// Different colors produce slightly different tones.
        /// </summary>
        private AudioClip GenerateCollisionSound()
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            
            // Base frequency varies by color hue
            float hue, sat, val;
            Color.RGBToHSV(cubeColor, out hue, out sat, out val);
            float baseFreq = 220f + hue * 440f; // Range: 220Hz - 660Hz
            
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                
                // Combine two sine waves for richer sound
                float wave1 = Mathf.Sin(2 * Mathf.PI * baseFreq * t);
                float wave2 = Mathf.Sin(2 * Mathf.PI * baseFreq * 1.5f * t) * 0.5f;
                
                samples[i] = (wave1 + wave2) / 1.5f;
                
                // Apply quick decay envelope
                float envelope = 1f - ((float)i / sampleCount);
                envelope = envelope * envelope; // Exponential decay
                samples[i] *= envelope;
            }
            
            AudioClip clip = AudioClip.Create("CubeCollision_" + gameObject.name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw line to spawn position
            Gizmos.color = cubeColor;
            Gizmos.DrawLine(transform.position, spawnPosition);
            Gizmos.DrawWireSphere(spawnPosition, 0.05f);
        }
#endif
    }
}
