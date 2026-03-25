using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace MusicSpace
{
    /// <summary>
    /// Tracks hits from cubes on walls. 
    /// Shows progressive damage: wall gradually sinks with each hit,
    /// shakes on the last 3 hits, and fully collapses on the final hit.
    /// After 3 walls are destroyed, the ceiling also collapses for realism.
    /// Color changes are handled separately by ColorReactiveWall.
    /// </summary>
    public class DestructibleWall : MonoBehaviour
    {
        [Header("Destruction Settings")]
        public int requiredHits = 20;
        public string nextSceneName = "Scene 3";
        
        [Header("Progressive Damage")]
        [Tooltip("Total amount the wall sinks before final collapse (in units)")]
        public float maxSinkBeforeCollapse = 0.5f;
        
        [Header("Shake Settings")]
        [Tooltip("How many hits before the end to start shaking")]
        public int shakeStartsAtRemaining = 3;
        public float shakeDuration = 0.3f;
        public float baseShakeAmount = 0.03f;
        
        [Header("Ceiling Collapse")]
        [Tooltip("How many walls must be destroyed before ceiling collapses")]
        public int wallsNeededForCeilingCollapse = 3;
        
        // Static counter shared across all DestructibleWall instances
        private static int totalWallsDestroyed = 0;
        private static bool ceilingCollapsed = false;
        
        private int currentHits = 0;
        private bool isDestroyed = false;
        private bool isShaking = false;
        private bool isInitialized = false;
        private Vector3 originalLocalPosition;
        private float currentSinkOffset = 0f;
        
        /// <summary>
        /// Reset static counters when scene loads (prevents stale state on scene reload)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            totalWallsDestroyed = 0;
            ceilingCollapsed = false;
        }

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
                // Final hit — collapse wall
                isDestroyed = true;
                Debug.Log($"[DestructibleWall] {gameObject.name} DESTROYED!");
                StartCoroutine(CollapseWall());
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

        private IEnumerator ShakeEffect(int remaining)
        {
            isShaking = true;
            
            float intensityMultiplier = 1f + ((float)(shakeStartsAtRemaining - remaining) / shakeStartsAtRemaining) * 4f;
            float currentShakeAmount = baseShakeAmount * intensityMultiplier;
            float currentDuration = shakeDuration * (1f + (shakeStartsAtRemaining - remaining) * 0.5f);
            
            Vector3 basePosition = originalLocalPosition + Vector3.down * currentSinkOffset;
            float elapsed = 0f;

            while (elapsed < currentDuration)
            {
                elapsed += Time.deltaTime;
                Vector3 randomOffset = Random.insideUnitSphere * currentShakeAmount;
                randomOffset.z *= 0.2f;
                transform.localPosition = basePosition + randomOffset;
                yield return null;
            }

            transform.localPosition = basePosition;
            isShaking = false;
        }

        /// <summary>
        /// Collapses this wall, then checks if ceiling should also collapse.
        /// </summary>
        private IEnumerator CollapseWall()
        {
            // 1. Violent shake (1.5 seconds)
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

            // 2. Sink into the floor (3 seconds)
            float sinkTime = 3.0f;
            elapsed = 0f;
            Vector3 startPos = transform.localPosition;
            Vector3 targetPos = startPos + Vector3.down * transform.localScale.y;

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

            // 3. Track destroyed walls BEFORE deactivating (coroutines stop on SetActive(false))
            totalWallsDestroyed++;
            Debug.Log($"[DestructibleWall] Total walls destroyed: {totalWallsDestroyed}/{wallsNeededForCeilingCollapse}");
            
            bool shouldCollapseCeiling = totalWallsDestroyed >= wallsNeededForCeilingCollapse && !ceilingCollapsed;
            
            if (shouldCollapseCeiling)
            {
                ceilingCollapsed = true;
                Debug.Log("[DestructibleWall] Enough walls destroyed — collapsing ceiling!");
                
                // Hide this wall but DON'T deactivate — we need coroutines to keep running
                Renderer renderer = GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false;
                
                // Run ceiling collapse from this still-active object
                StartCoroutine(CollapseCeiling());
            }
            else
            {
                // No ceiling collapse needed — safe to deactivate
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Finds the Ceiling object and makes it collapse dramatically,
        /// then transitions to the next scene.
        /// </summary>
        private IEnumerator CollapseCeiling()
        {
            // Search for ceiling by name in the scene
            GameObject ceiling = null;
            
            // Try to find in same parent hierarchy first
            Transform parent = transform.parent;
            while (parent != null)
            {
                Transform ceilingTransform = parent.Find("Ceiling");
                if (ceilingTransform != null)
                {
                    ceiling = ceilingTransform.gameObject;
                    break;
                }
                parent = parent.parent;
            }
            
            // Fallback: search entire scene
            if (ceiling == null)
            {
                foreach (GameObject obj in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                {
                    if (obj.name.ToLower().Contains("ceiling"))
                    {
                        ceiling = obj;
                        break;
                    }
                }
            }
            
            if (ceiling == null)
            {
                Debug.LogWarning("[DestructibleWall] Ceiling not found — skipping ceiling collapse");
                yield return new WaitForSeconds(1.0f);
                SceneManager.LoadScene(nextSceneName);
                yield break;
            }
            
            Debug.Log($"[DestructibleWall] Collapsing ceiling: {ceiling.name}");
            
            // Brief pause before ceiling starts falling
            yield return new WaitForSeconds(0.5f);
            
            // 1. Ceiling shakes and creaks (2 seconds)
            Vector3 ceilingStart = ceiling.transform.localPosition;
            float shakeTime = 2.0f;
            float elapsed = 0f;
            
            while (elapsed < shakeTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / shakeTime;
                float intensity = Mathf.Lerp(0.01f, 0.08f, progress);
                Vector3 offset = Random.insideUnitSphere * intensity;
                offset.y *= 0.3f; // Mostly horizontal shake
                ceiling.transform.localPosition = ceilingStart + offset;
                yield return null;
            }
            
            // 2. Ceiling falls down (2.5 seconds)
            Collider ceilingCol = ceiling.GetComponent<Collider>();
            if (ceilingCol != null) ceilingCol.enabled = false;
            
            float fallTime = 2.5f;
            elapsed = 0f;
            Vector3 fallStart = ceiling.transform.localPosition;
            float ceilingHeight = ceiling.transform.localScale.y;
            float fallDistance = ceiling.transform.localPosition.y + ceilingHeight; // Fall to below floor
            Vector3 fallTarget = fallStart + Vector3.down * fallDistance;
            
            while (elapsed < fallTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fallTime;
                // Accelerating fall (gravity-like)
                t = t * t;
                ceiling.transform.localPosition = Vector3.Lerp(fallStart, fallTarget, t);
                yield return null;
            }
            
            ceiling.SetActive(false);
            
            // 3. Clean up remaining room objects (floor, light, etc.)
            yield return new WaitForSeconds(1.0f);
            
            // Find and disable floor
            Transform roomParent = ceiling.transform.parent;
            if (roomParent != null)
            {
                Transform floor = roomParent.Find("Floor");
                if (floor != null)
                {
                    // Sink the floor
                    float floorSinkTime = 2.0f;
                    elapsed = 0f;
                    Vector3 floorStart = floor.localPosition;
                    Vector3 floorEnd = floorStart + Vector3.down * 5f;
                    
                    while (elapsed < floorSinkTime)
                    {
                        elapsed += Time.deltaTime;
                        float t = elapsed / floorSinkTime;
                        t = t * t;
                        floor.localPosition = Vector3.Lerp(floorStart, floorEnd, t);
                        yield return null;
                    }
                    floor.gameObject.SetActive(false);
                }
                
                // Disable room light
                Transform roomLight = roomParent.Find("RoomLight");
                if (roomLight != null) roomLight.gameObject.SetActive(false);
            }
            
            Debug.Log("[DestructibleWall] Room fully collapsed! City environment revealed.");
        }
    }
}
