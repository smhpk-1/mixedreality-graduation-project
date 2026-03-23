using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace MusicSpace
{
    /// <summary>
    /// Tracks hits from cubes on walls. 
    /// Shows progressive damage: wall gradually sinks with each hit,
    /// shakes on the last 3 hits, and fully collapses on the final hit.
    /// Color changes are handled separately by ColorReactiveWall.
    /// </summary>
    public class DestructibleWall : MonoBehaviour
    {
        [Header("Destruction Settings")]
        public int requiredHits = 10;
        public string nextSceneName = "Scene 3";
        
        [Header("Progressive Damage")]
        [Tooltip("Total amount the wall sinks before final collapse (in units)")]
        public float maxSinkBeforeCollapse = 0.5f;
        
        [Header("Shake Settings")]
        [Tooltip("How many hits before the end to start shaking")]
        public int shakeStartsAtRemaining = 3;
        public float shakeDuration = 0.3f;
        public float baseShakeAmount = 0.03f;
        
        private int currentHits = 0;
        private bool isDestroyed = false;
        private bool isShaking = false;
        private bool isInitialized = false;
        private Vector3 originalLocalPosition;
        private float currentSinkOffset = 0f;
        
        /// <summary>
        /// Initialize position tracking. Called automatically on first TakeDamage
        /// or in Start, whichever comes first. This handles the case where
        /// AddComponent + TakeDamage happen in the same frame (Start hasn't run yet).
        /// </summary>
        private void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;
            originalLocalPosition = transform.localPosition;
            Debug.Log($"[DestructibleWall] {gameObject.name} initialized at {originalLocalPosition}. requiredHits={requiredHits}");
        }
        
        private void Start()
        {
            Initialize();
        }

        public void TakeDamage(int delayMilliseconds = 0)
        {
            // Initialize on first call — handles AddComponent + TakeDamage in same frame
            Initialize();
            
            if (isDestroyed) return;

            currentHits++;
            int remaining = requiredHits - currentHits;
            Debug.Log($"[DestructibleWall] {gameObject.name} hit! {currentHits}/{requiredHits} (remaining: {remaining})");

            if (currentHits >= requiredHits)
            {
                // Final hit — collapse and transition
                isDestroyed = true;
                Debug.Log($"[DestructibleWall] {gameObject.name} DESTROYED! Starting collapse...");
                StartCoroutine(DestroyAndTransition());
            }
            else
            {
                // Progressive sinking — wall drops a little with each hit
                float sinkPerHit = maxSinkBeforeCollapse / (requiredHits - 1);
                currentSinkOffset += sinkPerHit;
                
                Vector3 sunkPosition = originalLocalPosition + Vector3.down * currentSinkOffset;
                transform.localPosition = sunkPosition;
                
                Debug.Log($"[DestructibleWall] {gameObject.name} sunk by {currentSinkOffset:F3} units");
                
                // Shake on the last N hits before destruction
                if (remaining <= shakeStartsAtRemaining && !isShaking)
                {
                    StartCoroutine(ShakeEffect(remaining));
                }
            }
        }

        /// <summary>
        /// Shake effect that gets stronger as remaining hits decrease.
        /// </summary>
        private IEnumerator ShakeEffect(int remaining)
        {
            isShaking = true;
            
            // Shake intensity increases: remaining=3 → mild, remaining=1 → intense
            float intensityMultiplier = 1f + ((float)(shakeStartsAtRemaining - remaining) / shakeStartsAtRemaining) * 4f;
            float currentShakeAmount = baseShakeAmount * intensityMultiplier;
            
            // Duration increases for later hits
            float currentDuration = shakeDuration * (1f + (shakeStartsAtRemaining - remaining) * 0.5f);
            
            // The base position now includes our sink offset
            Vector3 basePosition = originalLocalPosition + Vector3.down * currentSinkOffset;
            float elapsed = 0f;

            while (elapsed < currentDuration)
            {
                elapsed += Time.deltaTime;
                Vector3 randomOffset = Random.insideUnitSphere * currentShakeAmount;
                randomOffset.z *= 0.2f; // Keep wall in its plane
                transform.localPosition = basePosition + randomOffset;
                yield return null;
            }

            transform.localPosition = basePosition;
            isShaking = false;
        }

        private IEnumerator DestroyAndTransition()
        {
            Debug.Log($"[DestructibleWall] {gameObject.name} collapsing! Next: {nextSceneName}");
            
            // 1. Violent shake before collapsing (1.5 seconds)
            Vector3 basePosition = originalLocalPosition + Vector3.down * currentSinkOffset;
            float rumbleTime = 1.5f;
            float elapsed = 0f;
            
            while (elapsed < rumbleTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / rumbleTime;
                float shakeAmount = baseShakeAmount * Mathf.Lerp(3f, 10f, progress);
                Vector3 randomOffset = Random.insideUnitSphere * shakeAmount;
                transform.localPosition = basePosition + randomOffset;
                yield return null;
            }

            // 2. Collapse: sink down into the floor (3 seconds)
            float sinkTime = 3.0f;
            elapsed = 0f;
            Vector3 startPos = transform.localPosition;
            Vector3 targetPos = startPos + Vector3.down * transform.localScale.y;

            // Turn off collisions
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            while (elapsed < sinkTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / sinkTime;
                t = t * t * (3f - 2f * t); // Smooth easing
                transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            gameObject.SetActive(false);

            // 3. Wait before loading scene
            yield return new WaitForSeconds(1.0f);

            // 4. Load next scene
            Debug.Log($"[DestructibleWall] Loading: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
