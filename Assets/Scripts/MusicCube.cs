using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

namespace MusicSpace
{
    /// <summary>
    /// A grabbable cube that plays a sound when colliding with surfaces.
    /// The sound is manipulated based on physics parameters.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Rigidbody))]
    public class MusicCube : MonoBehaviour
    {
        [Header("Identity")]
        public Color cubeColor = Color.white;
        public string cubeName = "Cube";

        [Header("Audio")]
        public AudioClip assignedSound;
        [Range(0.5f, 2f)] public float basePitch = 1f;
        [Range(0f, 1f)] public float baseVolume = 1f;

        [Header("Physics Audio Mapping")]
        [Tooltip("Minimum velocity to trigger sound")]
        public float minVelocityThreshold = 0.5f;
        [Tooltip("Velocity for maximum pitch shift")]
        public float maxVelocityForPitch = 10f;
        [Tooltip("How much velocity affects pitch (0-1)")]
        [Range(0f, 1f)] public float velocityToPitchInfluence = 0.5f;

        [Header("Spawn Settings")]
        public Vector3 spawnPosition;
        public float respawnDelay = 0.5f;

        private AudioSource audioSource;
        private Rigidbody rb;
        private XRGrabInteractable grabInteractable;
        private MeshRenderer meshRenderer;
        private Material originalMaterial;
        private bool hasCollided = false;
        private bool isGrabbed = false;
        private bool hasBeenThrown = false; // Track if cube was thrown
        private ColorReactiveWall currentAffectedWall = null;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            rb = GetComponent<Rigidbody>();
            grabInteractable = GetComponent<XRGrabInteractable>();
            meshRenderer = GetComponent<MeshRenderer>();

            // Store spawn position
            spawnPosition = transform.position;

            // Setup audio source
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // Full 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 20f;

            // Start floating (no gravity until grabbed)
            SetFloating(true);
        }

        private void Start()
        {
            // Apply color in Start (after cubeColor is set by spawner)
            ApplyColor();
        }

        /// <summary>
        /// Apply the cube color to the material
        /// </summary>
        public void ApplyColor()
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }
            
            if (meshRenderer == null) return;
            
            // Get or create material
            originalMaterial = meshRenderer.material;
            
            // Set base color - this works with any shader
            if (originalMaterial.HasProperty("_Color"))
            {
                originalMaterial.SetColor("_Color", cubeColor);
            }
            if (originalMaterial.HasProperty("_BaseColor"))
            {
                originalMaterial.SetColor("_BaseColor", cubeColor);
            }
            originalMaterial.color = cubeColor;
            
            Debug.Log($"Applied color {cubeColor} to {cubeName}");
        }

        /// <summary>
        /// Enable/disable floating state (gravity off = floating)
        /// </summary>
        private void SetFloating(bool floating)
        {
            if (rb == null) return;
            
            rb.useGravity = !floating;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            if (floating)
            {
                // Keep cube stationary in the air
                rb.linearDamping = 5f;
                rb.angularDamping = 5f;
            }
            else
            {
                // Realistic physics for VR throwing
                rb.linearDamping = 0.01f; // Daha hızlı ve uzun fırlatma
                rb.angularDamping = 0.01f;
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
            hasCollided = false;
            hasBeenThrown = false;
            
            // Enable gravity when grabbed (will fall/throw when released)
            SetFloating(false);
            
            // Reset any affected wall when picked up again
            if (currentAffectedWall != null)
            {
                currentAffectedWall.ResetColor();
                currentAffectedWall = null;
            }
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            isGrabbed = false;
            hasBeenThrown = true;
            // Gravity stays on after release so cube can be thrown
            // XR Grab ayarları: daha hızlı fırlatma için velocity scale artırılabilir
            // (Spawner prefabında ayarlanabilir)
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Only trigger once after being thrown (not while grabbed)
            if (isGrabbed || hasCollided) return;

            float velocity = collision.relativeVelocity.magnitude;
            
            // Check velocity threshold
            if (velocity < minVelocityThreshold) return;

            hasCollided = true;

            // Check if we hit a reactive wall
            ColorReactiveWall wall = collision.gameObject.GetComponent<ColorReactiveWall>();
            if (wall != null)
            {
                currentAffectedWall = wall;
                wall.ChangeColorInstant(cubeColor, 1.5f); // 1.5 saniye sonra eski rengine döner

                // Apply surface-based audio effects
                ApplySurfaceEffects(wall.surfaceType);
            }

            // Calculate audio parameters based on physics
            float normalizedVelocity = Mathf.Clamp01(velocity / maxVelocityForPitch);
            
            // Pitch: Higher velocity = higher pitch
            float pitchModifier = 1f + (normalizedVelocity * velocityToPitchInfluence);
            audioSource.pitch = basePitch * pitchModifier;
            
            // Volume: Based on impact force
            audioSource.volume = baseVolume * Mathf.Lerp(0.5f, 1f, normalizedVelocity);

            // Play the sound
            if (assignedSound != null)
            {
                audioSource.clip = assignedSound;
                audioSource.Play();
                
                // Start destruction sequence after sound finishes
                StartCoroutine(DestroyAfterSound());
            }
            else
            {
                // No sound assigned, destroy immediately
                StartCoroutine(DestroyAndRespawn());
            }
        }

        private void ApplySurfaceEffects(SurfaceType surfaceType)
        {
            // Get or add reverb filter
            AudioReverbFilter reverb = GetComponent<AudioReverbFilter>();
            if (reverb == null)
            {
                reverb = gameObject.AddComponent<AudioReverbFilter>();
            }

            // Get or add low pass filter for muffled effects
            AudioLowPassFilter lowPass = GetComponent<AudioLowPassFilter>();
            if (lowPass == null)
            {
                lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            }
            lowPass.cutoffFrequency = 22000; // Default: no filtering

            // Get or add high pass filter
            AudioHighPassFilter highPass = GetComponent<AudioHighPassFilter>();
            if (highPass == null)
            {
                highPass = gameObject.AddComponent<AudioHighPassFilter>();
            }
            highPass.cutoffFrequency = 10; // Default: no filtering

            switch (surfaceType)
            {
                case SurfaceType.Metal:
                    // Metallic: High reverb, bright sound
                    reverb.reverbPreset = AudioReverbPreset.Hallway;
                    highPass.cutoffFrequency = 200; // Remove low rumble
                    audioSource.pitch *= 1.1f; // Slightly higher pitch
                    break;

                case SurfaceType.Concrete:
                    // Concrete: Medium reverb, neutral
                    reverb.reverbPreset = AudioReverbPreset.StoneCorridor;
                    break;

                case SurfaceType.Wood:
                    // Wood: Low reverb, warm tone
                    reverb.reverbPreset = AudioReverbPreset.Room;
                    lowPass.cutoffFrequency = 8000; // Warm, muffled
                    audioSource.pitch *= 0.95f; // Slightly lower pitch
                    break;

                case SurfaceType.Glass:
                    // Glass: High frequencies, bright and sharp
                    reverb.reverbPreset = AudioReverbPreset.Bathroom;
                    highPass.cutoffFrequency = 500; // Very bright
                    audioSource.pitch *= 1.2f; // Higher pitch
                    break;

                case SurfaceType.Stone:
                    // Stone floor: Deep, bass-heavy
                    reverb.reverbPreset = AudioReverbPreset.Cave;
                    lowPass.cutoffFrequency = 5000; // Bass heavy
                    audioSource.pitch *= 0.85f; // Lower pitch
                    break;
            }
        }

        private IEnumerator DestroyAfterSound()
        {
            // Wait for sound to finish
            yield return new WaitForSeconds(assignedSound.length);
            
            yield return DestroyAndRespawn();
        }

        private IEnumerator DestroyAndRespawn()
        {
            // Reset wall color before destroying
            if (currentAffectedWall != null)
            {
                currentAffectedWall.ResetColor();
                currentAffectedWall = null;
            }

            yield return new WaitForSeconds(respawnDelay);
            
            // Reset and reposition instead of destroying
            ResetCube();
        }

        public void ResetCube()
        {
            // Reset position and physics
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;
            
            // Reset state
            hasCollided = false;
            isGrabbed = false;
            hasBeenThrown = false;
            
            // Go back to floating state
            SetFloating(true);
            
            // Remove audio filters (they'll be re-added on next collision)
            AudioReverbFilter reverb = GetComponent<AudioReverbFilter>();
            if (reverb != null) Destroy(reverb);
            
            AudioLowPassFilter lowPass = GetComponent<AudioLowPassFilter>();
            if (lowPass != null) Destroy(lowPass);
            
            AudioHighPassFilter highPass = GetComponent<AudioHighPassFilter>();
            if (highPass != null) Destroy(highPass);

            // Stop any playing audio
            audioSource.Stop();
            
            // Re-apply color in case it was changed
            ApplyColor();
        }
    }
}
