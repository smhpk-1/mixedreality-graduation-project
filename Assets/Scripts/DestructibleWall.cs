using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace MusicSpace
{
    /// <summary>
    /// Tracks hits from PlaygroundCubes on walls. 
    /// Adds visual shaking feedback upon hits, and upon reaching the required hit count,
    /// slowly crumbles the wall down and transitions to the next scene.
    /// </summary>
    public class DestructibleWall : MonoBehaviour
    {
        [Header("Destruction Settings")]
        public int requiredHits = 10;
        public string nextSceneName = "Scene 3";
        
        [Header("Feedback Settings")]
        public float shakeDuration = 0.2f;
        public float baseShakeAmount = 0.05f;
        
        private int currentHits = 0;
        private bool isDestroyed = false;
        private bool isShaking = false;
        private Vector3 originalLocalPosition;
        
        private void Start()
        {
            originalLocalPosition = transform.localPosition;
        }

        public void TakeDamage(int delayMilliseconds = 0)
        {
            if (isDestroyed) return;

            currentHits++;
            Debug.Log($"[DestructibleWall] {gameObject.name} took damage. Hits: {currentHits}/{requiredHits}");

            if (currentHits >= requiredHits)
            {
                isDestroyed = true;
                StartCoroutine(DestroyAndTransition());
            }
            else
            {
                if (!isShaking)
                {
                    StartCoroutine(ShakeEffect());
                }
            }
        }

        private IEnumerator ShakeEffect()
        {
            isShaking = true;
            float elapsed = 0f;
            
            // The shake gets stronger as we approach requiredHits
            float shakeMultiplier = 1f + ((float)currentHits / requiredHits);
            float currentShakeAmount = baseShakeAmount * shakeMultiplier;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * currentShakeAmount;
                // Keep the wall generally in its plane (don't shake too much depth-wise)
                randomOffset.z *= 0.2f; 
                transform.localPosition = originalLocalPosition + randomOffset;
                
                yield return null;
            }

            transform.localPosition = originalLocalPosition;
            isShaking = false;
        }

        private IEnumerator DestroyAndTransition()
        {
            Debug.Log($"[DestructibleWall] {gameObject.name} is collapsing! Transitioning to {nextSceneName}...");
            
            // 1. Violent Shake before collapsing
            float rumbleTime = 1.0f;
            float elapsed = 0f;
            while (elapsed < rumbleTime)
            {
                elapsed += Time.deltaTime;
                Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * (baseShakeAmount * 3f);
                transform.localPosition = originalLocalPosition + randomOffset;
                yield return null;
            }

            // 2. Collapse mechanism: sink down into the floor
            float sinkTime = 3.0f;
            elapsed = 0f;
            Vector3 startPos = transform.localPosition;
            Vector3 targetPos = startPos + Vector3.down * transform.localScale.y;

            // Optional: try to turn off collisions so player can walk towards it
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            while (elapsed < sinkTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / sinkTime;
                // Add an easing effect
                t = t * t * (3f - 2f * t);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            gameObject.SetActive(false);

            // 3. Optional wait before loading scene
            yield return new WaitForSeconds(1.0f);

            // 4. Load Scene 3 (Ensure it's in Build Settings)
            Debug.Log($"[DestructibleWall] Loading Next Scene: {nextSceneName}...");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
