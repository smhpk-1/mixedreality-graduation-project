using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

namespace MusicSpace
{
    /// <summary>
    /// A colored cube for Scene 2 that can be grabbed and thrown at reactive walls.
    /// When it collides with a ColorReactiveWall, the wall changes to match this cube's color.
    /// Audio is loaded automatically based on cube color and manipulated by physics.
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
        public AudioClip collisionSound; // Auto-loaded based on colorName
        [Range(0f, 1f)] public float baseVolume = 0.8f;
        
        [Header("Physics-Based Audio")]
        [Tooltip("Minimum pitch when velocity is low")]
        public float minPitch = 0.7f;
        [Tooltip("Maximum pitch when velocity is high")]
        public float maxPitch = 1.4f;
        [Tooltip("Velocity at which max pitch is reached")]
        public float maxVelocityForPitch = 15f;
        [Tooltip("Minimum volume multiplier")]
        public float minVolumeMultiplier = 0.3f;
        
        [Header("Physics")]
        public float minVelocityForWallChange = 0.5f;
        public float minVelocityForSound = 0.2f;

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
            audioSource.maxDistance = 20f;
            
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
            if (collisionSound == null && !string.IsNullOrEmpty(colorName))
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
        /// </summary>
        private void LoadSoundForColor()
        {
            string path = "scene_2_sound_design/" + colorName.ToLower();
            collisionSound = Resources.Load<AudioClip>(path);
            
            if (collisionSound == null)
            {
                Debug.LogWarning($"PlaygroundCube {gameObject.name}: Could not load sound from Resources/{path}");
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
            if (collisionSound == null || audioSource == null) return;
            
            // Normalize velocity for parameter calculation
            float normalizedVelocity = Mathf.Clamp01(velocity / maxVelocityForPitch);
            
            // Physics-based pitch: faster impact = higher pitch
            float pitch = Mathf.Lerp(minPitch, maxPitch, normalizedVelocity);
            
            // Physics-based volume: harder impact = louder
            float volumeMultiplier = Mathf.Lerp(minVolumeMultiplier, 1f, normalizedVelocity);
            float finalVolume = baseVolume * volumeMultiplier * volumeScale;
            
            // Apply parameters
            audioSource.pitch = pitch;
            audioSource.volume = finalVolume;
            
            // Play the sound
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
