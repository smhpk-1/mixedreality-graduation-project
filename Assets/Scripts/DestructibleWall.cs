using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace MusicSpace
{
    /// <summary>
    /// Tracks hits from PlaygroundCubes on walls. 
    /// Provides progressive visual feedback: darkening on early hits,
    /// shaking on later hits (last 3), and collapse + scene transition on final hit.
    /// </summary>
    public class DestructibleWall : MonoBehaviour
    {
        [Header("Destruction Settings")]
        public int requiredHits = 10;
        public string nextSceneName = "Scene 3";
        
        [Header("Feedback Settings")]
        public float shakeDuration = 0.3f;
        public float baseShakeAmount = 0.03f;
        
        [Header("Progressive Damage")]
        [Tooltip("How many hits before the end to start shaking (e.g. 3 = last 3 hits shake)")]
        public int shakeStartsAtRemaining = 3;
        [Tooltip("How much to darken the wall per hit (0-1 range, cumulative)")]
        public float darkenPerHit = 0.06f;
        
        private int currentHits = 0;
        private bool isDestroyed = false;
        private bool isShaking = false;
        private Vector3 originalLocalPosition;
        private MeshRenderer meshRenderer;
        private Color initialColor;
        private bool hasStoredInitialColor = false;
        
        private void Start()
        {
            originalLocalPosition = transform.localPosition;
            meshRenderer = GetComponent<MeshRenderer>();
            
            // Store the initial color so we can darken it progressively
            if (meshRenderer != null && meshRenderer.material != null)
            {
                Material mat = meshRenderer.material;
                if (mat.HasProperty("_BaseColor"))
                    initialColor = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    initialColor = mat.GetColor("_Color");
                else
                    initialColor = mat.color;
                hasStoredInitialColor = true;
            }
        }

        public void TakeDamage(int delayMilliseconds = 0)
        {
            if (isDestroyed) return;

            currentHits++;
            int remaining = requiredHits - currentHits;
            Debug.Log($"[DestructibleWall] {gameObject.name} took damage. Hits: {currentHits}/{requiredHits} (remaining: {remaining})");

            if (currentHits >= requiredHits)
            {
                // Final hit — collapse and transition
                isDestroyed = true;
                StartCoroutine(DestroyAndTransition());
            }
            else
            {
                // Progressive darkening on every hit
                ApplyDamageVisual();
                
                // Shake only in the last N hits before destruction
                if (remaining <= shakeStartsAtRemaining && !isShaking)
                {
                    StartCoroutine(ShakeEffect(remaining));
                }
            }
        }

        /// <summary>
        /// Darken the wall slightly with each hit to show cumulative damage.
        /// </summary>
        private void ApplyDamageVisual()
        {
            if (!hasStoredInitialColor || meshRenderer == null) return;
            
            Material mat = meshRenderer.material;
            // Calculate how much to darken: more hits = darker
            float darkenFactor = 1f - (darkenPerHit * currentHits);
            darkenFactor = Mathf.Max(darkenFactor, 0.3f); // Don't go fully black
            
            Color damagedColor = initialColor * darkenFactor;
            damagedColor.a = 1f;
            
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", damagedColor);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", damagedColor);
            mat.color = damagedColor;
        }

        /// <summary>
        /// Shake effect that gets stronger as remaining hits decrease.
        /// </summary>
        private IEnumerator ShakeEffect(int remaining)
        {
            isShaking = true;
            float elapsed = 0f;
            
            // Shake intensity increases as we get closer to destruction
            // remaining=3 → mild, remaining=1 → intense
            float intensityMultiplier = 1f + ((float)(shakeStartsAtRemaining - remaining) / shakeStartsAtRemaining) * 3f;
            float currentShakeAmount = baseShakeAmount * intensityMultiplier;
            
            // Duration also increases for later hits
            float currentDuration = shakeDuration * (1f + (shakeStartsAtRemaining - remaining) * 0.5f);

            while (elapsed < currentDuration)
            {
                elapsed += Time.deltaTime;
                Vector3 randomOffset = Random.insideUnitSphere * currentShakeAmount;
                // Keep the wall generally in its plane
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
            
            // 1. Violent shake before collapsing
            float rumbleTime = 1.5f;
            float elapsed = 0f;
            while (elapsed < rumbleTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / rumbleTime;
                // Shake intensifies over time
                float shakeAmount = baseShakeAmount * Mathf.Lerp(3f, 8f, progress);
                Vector3 randomOffset = Random.insideUnitSphere * shakeAmount;
                transform.localPosition = originalLocalPosition + randomOffset;
                yield return null;
            }

            // 2. Collapse: sink down into the floor
            float sinkTime = 3.0f;
            elapsed = 0f;
            Vector3 startPos = transform.localPosition;
            Vector3 targetPos = startPos + Vector3.down * transform.localScale.y;

            // Turn off collisions so player can walk through
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            while (elapsed < sinkTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / sinkTime;
                // Smooth easing
                t = t * t * (3f - 2f * t);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            gameObject.SetActive(false);

            // 3. Wait before loading scene
            yield return new WaitForSeconds(1.0f);

            // 4. Load next scene
            Debug.Log($"[DestructibleWall] Loading Next Scene: {nextSceneName}...");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
