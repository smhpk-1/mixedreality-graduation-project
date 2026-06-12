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
        
        // ========================================
        // GLOBAL VOLUME SETTINGS (same for all cubes)
        // Change these values to adjust all cubes at once
        // ========================================
        private const float GLOBAL_LOOP_VOLUME = 0.7f;      // Grab sound volume (0.0 - 1.0)
        private const float GLOBAL_COLLISION_VOLUME = 1.0f; // Collision sound volume (0.0 - 1.0)

        // Klipler arası yükseklik farkını dengeleme: tüm renkler bu RMS hedefine çekilir
        private const float TARGET_LOOP_RMS = 0.18f;
        // Küp bırakılınca loop'un yavaşça sönme süresi (saniye)
        private const float RELEASE_FADE_DURATION = 1.2f;
        // Duvarın "yanıt" tonunun sönme süresi (saniye)
        private const float WALL_TONE_DURATION = 1.6f;
        
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
        public float minVelocityForWallChange = 0.3f; // Lowered for easier triggering
        public float minVelocityForSound = 0.1f;

        private XRGrabInteractable grabInteractable;
        private Rigidbody rb;
        private AudioSource loopAudioSource;    // For looping grab sound
        private AudioSource collisionAudioSource; // For collision one-shots
        private bool hasHitWall = false;
        private float volumeNormalizer = 1f;    // klip yüksekliğini dengeleyen çarpan
        private Coroutine loopFadeCoroutine;
        private Coroutine collisionFadeCoroutine;

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
            loopAudioSource.volume = GLOBAL_LOOP_VOLUME;
            
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

            // Klipler arası ses yüksekliği farkını dengele
            ComputeVolumeNormalizer();
        }

        /// <summary>
        /// Klibin RMS yüksekliğini ölçüp hedef seviyeye çeken çarpanı hesaplar —
        /// renk kayıtlarının yükseklikleri birbirinden çok farklı olduğu için şart.
        /// </summary>
        private void ComputeVolumeNormalizer()
        {
            if (loopSound == null) return;

            // İlk ~4 saniye ölçüm için yeterli
            int sampleCount = Mathf.Min(loopSound.samples, loopSound.frequency * 4) * loopSound.channels;
            float[] data = new float[sampleCount];
            if (!loopSound.GetData(data, 0)) return; // sıkıştırılmış klip okunamayabilir

            double sum = 0;
            for (int i = 0; i < data.Length; i++) sum += data[i] * data[i];
            float rms = Mathf.Sqrt((float)(sum / data.Length));

            if (rms > 0.0001f)
                volumeNormalizer = Mathf.Clamp(TARGET_LOOP_RMS / rms, 0.3f, 3f);
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
            hasHitWall = false;
            
            // Start playing the looping sound when grabbed
            StartLoopSound();
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            // Küp elden çıktığı anda loop yavaşça fade out olarak biter —
            // baştan başlamaz, aniden de kesilmez
            FadeOutLoopSound(RELEASE_FADE_DURATION);
        }

        /// <summary>
        /// Start playing the looping sound assigned to this cube's color
        /// </summary>
        private void StartLoopSound()
        {
            if (loopAudioSource != null && loopSound != null)
            {
                // Devam eden bir fade varsa iptal et — tam sesle yeniden başla
                if (loopFadeCoroutine != null)
                {
                    StopCoroutine(loopFadeCoroutine);
                    loopFadeCoroutine = null;
                }
                loopAudioSource.clip = loopSound;
                loopAudioSource.volume = GLOBAL_LOOP_VOLUME * volumeNormalizer;
                loopAudioSource.pitch = 1f;
                loopAudioSource.loop = true; // Enable looping while grabbed
                loopAudioSource.Play();
            }
        }

        /// <summary>
        /// Loop sesini verilen sürede yavaşça söndürüp durdurur.
        /// </summary>
        private void FadeOutLoopSound(float duration)
        {
            if (loopAudioSource == null || !loopAudioSource.isPlaying) return;
            if (loopFadeCoroutine != null) StopCoroutine(loopFadeCoroutine);
            loopFadeCoroutine = StartCoroutine(FadeAndStop(loopAudioSource, duration));
        }

        /// <summary>
        /// Stop the looping sound immediately
        /// </summary>
        private void StopLoopSound()
        {
            if (loopFadeCoroutine != null)
            {
                StopCoroutine(loopFadeCoroutine);
                loopFadeCoroutine = null;
            }
            if (loopAudioSource != null && loopAudioSource.isPlaying)
            {
                loopAudioSource.Stop();
            }
        }

        private IEnumerator FadeAndStop(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;
            while (elapsed < duration && source.isPlaying)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
            source.Stop();
            source.volume = startVolume;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Skip if already hit wall (prevent multiple triggers)
            if (hasHitWall) return;
            
            float velocity = collision.relativeVelocity.magnitude;
            string hitObjectName = collision.gameObject.name.ToLower();
            
            // Check if we hit a reactive wall - search on object AND parents
            ColorReactiveWall wall = collision.gameObject.GetComponent<ColorReactiveWall>();
            if (wall == null)
            {
                wall = collision.gameObject.GetComponentInParent<ColorReactiveWall>();
            }
            
            // Check if this is a reactive surface by name (wall, ceiling)
            bool isReactiveSurface = hitObjectName.Contains("wall") || hitObjectName.Contains("ceiling");
            
            // If it's a reactive surface but doesn't have the component, add it
            // (only ColorReactiveWall — DestructibleWall is managed by Scene2RoomGenerator)
            if (wall == null && isReactiveSurface)
            {
                wall = collision.gameObject.AddComponent<ColorReactiveWall>();
            }
            
            // Process collision with reactive wall
            if (wall != null && velocity >= minVelocityForWallChange)
            {
                hasHitWall = true;

                // Stop the loop sound when hitting wall
                StopLoopSound();

                // Color change and DestructibleWall damage are handled by
                // ColorReactiveWall.OnCollisionEnter — no need to call here

                // Duvar küpün rengini aldığı gibi sesini de alır: küpün tonu
                // çarpma noktasından duvardan sönerek yankılanır
                SpawnWallTone(collision, velocity);

                // Respawn cube after delay
                StartCoroutine(RespawnAfterDelay());
            }
            else if (velocity > minVelocityForSound)
            {
                // Play sound for non-wall collisions (floor, other cubes) with reduced intensity
                PlayCollisionSound(velocity, 0.5f);
            }
        }

        /// <summary>
        /// Duvarın "yanıtı": küpün tonu çarpma noktasında ayrı bir kaynaktan çalar,
        /// hız bazlı pitch ile, ve sönerek biter. Küpten bağımsız obje olduğu için
        /// küp respawn olunca ses spawn noktasına taşınmaz.
        /// </summary>
        private void SpawnWallTone(Collision collision, float velocity)
        {
            if (loopSound == null) return;

            Vector3 contactPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;

            float normalizedVelocity = Mathf.Clamp01(velocity / maxVelocityForPitch);
            float pitch  = Mathf.Lerp(minPitch, maxPitch, normalizedVelocity);
            float volume = GLOBAL_COLLISION_VOLUME * volumeNormalizer
                           * Mathf.Lerp(minVolumeMultiplier, 1f, normalizedVelocity);

            var go = new GameObject("WallTone_" + colorName);
            go.transform.position = contactPoint;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = 20f;
            src.clip = loopSound;
            src.pitch = pitch;
            src.volume = volume;
            src.Play();

            // Kendi kendini söndürüp yok eder — küpün yaşam döngüsünden bağımsız
            go.AddComponent<WallToneFader>().fadeDuration = WALL_TONE_DURATION;
        }

        /// <summary>
        /// Play collision sound with physics-based parameter manipulation
        /// (yer/küp çarpmaları — kısa, sönerek biten dokunuşlar)
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
            float finalVolume = GLOBAL_COLLISION_VOLUME * volumeNormalizer * volumeMultiplier * volumeScale;

            // Apply parameters
            collisionAudioSource.pitch = pitch;
            collisionAudioSource.volume = finalVolume;

            // Uzun klip baştan sona çalmasın: çal ve kısa sürede söndür
            collisionAudioSource.clip = loopSound;
            collisionAudioSource.Play();
            if (collisionFadeCoroutine != null) StopCoroutine(collisionFadeCoroutine);
            collisionFadeCoroutine = StartCoroutine(FadeAndStop(collisionAudioSource, 0.6f));
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
            // Tüm sesleri kesin olarak sustur — respawn olan küp bir daha
            // ele alınana kadar SESSİZ kalır (ses karmaşası olmasın)
            StopLoopSound();
            if (collisionFadeCoroutine != null)
            {
                StopCoroutine(collisionFadeCoroutine);
                collisionFadeCoroutine = null;
            }
            if (collisionAudioSource != null) collisionAudioSource.Stop();

            // Reset position and rotation
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;
            
            // Reset physics
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Reset state
            hasHitWall = false;
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

    /// <summary>
    /// Duvardaki yanıt tonunu kendi kendine söndürür ve objeyi yok eder.
    /// Küpten bağımsız çalışır — küp respawn olsa veya yok edilse bile ses
    /// doğru yerden söner.
    /// </summary>
    public class WallToneFader : MonoBehaviour
    {
        public float fadeDuration = 1.6f;

        private AudioSource source;
        private float startVolume;
        private float elapsed;

        private void Start()
        {
            source = GetComponent<AudioSource>();
            if (source == null) { Destroy(gameObject); return; }
            startVolume = source.volume;
        }

        private void Update()
        {
            if (source == null) return;
            elapsed += Time.deltaTime;
            if (elapsed >= fadeDuration || !source.isPlaying)
            {
                Destroy(gameObject);
                return;
            }
            source.volume = startVolume * (1f - elapsed / fadeDuration);
        }
    }
}
