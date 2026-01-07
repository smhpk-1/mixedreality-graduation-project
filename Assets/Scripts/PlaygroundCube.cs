using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

namespace MusicSpace
{
    /// <summary>
    /// A colored cube for Scene 2 that can be grabbed and thrown at reactive walls.
    /// When grabbed, plays a looping sound based on the cube's color.
    /// When it collides with a ColorReactiveWall, the wall changes to match this cube's color.
    /// Audio is loaded automatically based on cube color.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlaygroundCube : MonoBehaviour
    {
        [Header("Identity")]
        public Color cubeColor = Color.white;
        public string colorName = ""; // red, blue, green, yellow, purple
        
        [Header("Spawn Settings")]
        public Vector3 spawnPosition;
        public float respawnDelay = 1.5f;
        
        [Header("Audio Settings")]
        public AudioClip loopSound; // Auto-loaded based on colorName - plays while grabbed
        [Range(0f, 1f)] public float loopVolume = 0.7f;
        [Range(0f, 1f)] public float collisionVolume = 1.0f;
        
        [Header("Physics-Based Audio")]
        [Tooltip("Minimum pitch when velocity is low")]
        public float minPitch = 0.8f;
        [Tooltip("Maximum pitch when velocity is high")]
        public float maxPitch = 1.3f;
        [Tooltip("Velocity at which max pitch is reached")]
        public float maxVelocityForPitch = 12f;
        [Tooltip("Minimum volume multiplier for collision sounds")]
        public float minVolumeMultiplier = 0.4f;
        
        [Header("Physics")]
        public float minVelocityForWallChange = 0.5f;
        public float minVelocityForSound = 0.2f;

        private XRGrabInteractable grabInteractable;
        private Rigidbody rb;
        private AudioSource loopAudioSource;    // For looping grab sound
        private AudioSource collisionAudioSource; // For collision one-shots
        private bool isGrabbed = false;
        private bool hasHitWall = false;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();
            
            // Setup loop audio source (for grab looping sound)
            loopAudioSource = GetComponent<AudioSource>();
            if (loopAudioSource == null)
            {
                loopAudioSource = gameObject.AddComponent<AudioSource>();
            }
            loopAudioSource.playOnAwake = false;
            loopAudioSource.loop = true; // IMPORTANT: Loop while grabbed
            loopAudioSource.spatialBlend = 1f; // 3D sound
            loopAudioSource.rolloffMode = AudioRolloffMode.Linear;
            loopAudioSource.minDistance = 1f;
            loopAudioSource.maxDistance = 15f;
            loopAudioSource.volume = loopVolume;
            
            // Setup collision audio source (for one-shot collision sounds)
            collisionAudioSource = gameObject.AddComponent<AudioSource>();
            collisionAudioSource.playOnAwake = false;
            collisionAudioSource.loop = false;
            collisionAudioSource.spatialBlend = 1f;
            collisionAudioSource.rolloffMode = AudioRolloffMode.Linear;
            collisionAudioSource.minDistance = 1f;
            collisionAudioSource.maxDistance = 20f;
            
            // Store spawn position if not set
            if (spawnPosition == Vector3.zero)
            {
                spawnPosition = transform.position;
            }
            
            // Auto-detect color name from object name if not set
            if (string.IsNullOrEmpty(colorName))
            {
                colorName = DetectColorFromName();
            }
            
            // Auto-load sound based on color
            if (loopSound == null && !string.IsNullOrEmpty(colorName))
            {
                LoadSoundForColor();
            }
        }
        
        /// <summary>
        /// Detect color name from GameObject name (e.g., "Cube_Red_1" -> "red")
        /// </summary>
        private string DetectColorFromName()
        {
            string objName = gameObject.name.ToLower();
            
            if (objName.Contains("red")) return "red";
            if (objName.Contains("blue")) return "blue";
            if (objName.Contains("green")) return "green";
            if (objName.Contains("yellow")) return "yellow";
            if (objName.Contains("purple")) return "purple";
            
            return "";
        }
        
        /// <summary>
        /// Load audio clip from Resources based on color name
        /// Falls back to a similar color if exact match not found
        /// </summary>
        private void LoadSoundForColor()
        {
            string colorLower = colorName.ToLower();
            string path = "scene_2_sound_design/" + colorLower;
            loopSound = Resources.Load<AudioClip>(path);
            
            // Fallback for yellow (no dedicated yellow sound - use green or red)
            if (loopSound == null && colorLower == "yellow")
            {
                loopSound = Resources.Load<AudioClip>("scene_2_sound_design/green");
                if (loopSound == null)
                {
                    loopSound = Resources.Load<AudioClip>("scene_2_sound_design/red");
                }
            }
            
            if (loopSound == null)
            {
                Debug.LogWarning($"PlaygroundCube {gameObject.name}: Could not load sound from Resources/{path}");
            }
            else
            {
                // Assign the loaded sound to the loop audio source
                loopAudioSource.clip = loopSound;
            }
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
            
            // Start playing the looping sound when grabbed
            StartLoopSound();
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            isGrabbed = false;
            
            // Stop the looping sound when released
            StopLoopSound();
        }
        
        /// <summary>
        /// Start playing the looping sound assigned to this cube's color
        /// </summary>
        private void StartLoopSound()
        {
            if (loopAudioSource != null && loopSound != null)
            {
                loopAudioSource.clip = loopSound;
                loopAudioSource.volume = loopVolume;
                loopAudioSource.pitch = 1f;
                loopAudioSource.Play();
            }
        }
        
        /// <summary>
        /// Stop the looping sound
        /// </summary>
        private void StopLoopSound()
        {
            if (loopAudioSource != null && loopAudioSource.isPlaying)
            {
                loopAudioSource.Stop();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Skip if already hit wall (prevent multiple triggers)
            if (hasHitWall) return;
            
            float velocity = collision.relativeVelocity.magnitude;
            string hitObjectName = collision.gameObject.name;
            
            // Check if we hit a reactive wall - search on object AND parents
            ColorReactiveWall wall = collision.gameObject.GetComponent<ColorReactiveWall>();
            if (wall == null)
            {
                wall = collision.gameObject.GetComponentInParent<ColorReactiveWall>();
            }
            
            // Check if this is a wall by name (Wall_Front, Wall_Back, etc.)
            bool isWallByName = hitObjectName.StartsWith("Wall_");
            
            if (wall != null)
            {
                if (velocity >= minVelocityForWallChange)
                {
                    hasHitWall = true;
                    
                    // Stop the loop sound when hitting wall
                    StopLoopSound();
                    
                    // Change wall color to match this cube (permanent until next hit)
                    wall.ChangeColorInstant(cubeColor, 0f);
                    
                    // Play collision sound with full intensity for wall hits
                    PlayCollisionSound(velocity, 1f);
                    
                    // Respawn cube after delay
                    StartCoroutine(RespawnAfterDelay());
                }
            }
            else if (isWallByName)
            {
                // Wall doesn't have ColorReactiveWall - add it dynamically
                wall = collision.gameObject.AddComponent<ColorReactiveWall>();
                
                if (velocity >= minVelocityForWallChange)
                {
                    hasHitWall = true;
                    
                    // Stop the loop sound when hitting wall
                    StopLoopSound();
                    
                    wall.ChangeColorInstant(cubeColor, 0f);
                    PlayCollisionSound(velocity, 1f);
                    StartCoroutine(RespawnAfterDelay());
                }
            }
            else if (velocity > minVelocityForSound)
            {
                // Play sound for non-wall collisions (floor, other cubes) with reduced intensity
                PlayCollisionSound(velocity, 0.5f);
            }
        }

        /// <summary>
        /// Play collision sound with physics-based parameter manipulation
        /// </summary>
        /// <param name="velocity">Impact velocity</param>
        /// <param name="volumeScale">Additional volume scaling (0-1)</param>
        private void PlayCollisionSound(float velocity, float volumeScale = 1f)
        {
            if (loopSound == null || collisionAudioSource == null) return;
            
            // Normalize velocity for parameter calculation
            float normalizedVelocity = Mathf.Clamp01(velocity / maxVelocityForPitch);
            
            // Physics-based pitch: faster impact = higher pitch
            float pitch = Mathf.Lerp(minPitch, maxPitch, normalizedVelocity);
            
            // Physics-based volume: harder impact = louder
            float volumeMultiplier = Mathf.Lerp(minVolumeMultiplier, 1f, normalizedVelocity);
            float finalVolume = collisionVolume * volumeMultiplier * volumeScale;
            
            // Apply parameters
            collisionAudioSource.pitch = pitch;
            collisionAudioSource.volume = finalVolume;
            
            // Play the sound as one-shot (use the same clip as the loop)
            collisionAudioSource.PlayOneShot(loopSound);
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
            // Stop any playing sounds
            StopLoopSound();
            
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
