using UnityEngine;
using System.Collections;

namespace MusicSpace
{
    public enum SurfaceType
    {
        Metal,      // High reverb, bright
        Concrete,   // Medium reverb, neutral
        Wood,       // Low reverb, warm
        Glass,      // Sharp, high frequencies
        Stone       // Deep, bass-heavy (floor)
    }

    /// <summary>
    /// A wall or floor that changes color when hit by a PlaygroundCube.
    /// Color changes are triggered by PlaygroundCube.OnCollisionEnter calling ChangeColorInstant().
    /// </summary>
    public class ColorReactiveWall : MonoBehaviour
    {
        [Header("Surface Properties")]
        public SurfaceType surfaceType = SurfaceType.Concrete;
        
        [Header("Color Settings")]
        public Color originalColor = Color.gray;
        public float colorTransitionSpeed = 5f;
        public float colorIntensity = 1.2f; // How bright the color becomes

        [Header("Visual Feedback")]
        public bool useEmission = true;
        public float emissionIntensity = 0.5f;

        private MeshRenderer meshRenderer;
        private Material material;
        private Color targetColor;
        private bool isTransitioning = false;
        private Coroutine colorCoroutine;
        private Coroutine revertCoroutine;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                // Create material instance to avoid affecting other objects
                material = meshRenderer.material;
                
                // Store original color from material
                if (material.HasProperty("_BaseColor"))
                    originalColor = material.GetColor("_BaseColor");
                else if (material.HasProperty("_Color"))
                    originalColor = material.GetColor("_Color");
                else
                    originalColor = material.color;
                    
                targetColor = originalColor;
                
                Debug.Log($"ColorReactiveWall initialized: {gameObject.name}, original color: {originalColor}");
            }
        }

        /// <summary>
        /// Instantly change wall color. If revertDelay is 0 or negative, color persists until changed again.
        /// </summary>
        /// <param name="newColor">The new color to apply</param>
        /// <param name="revertDelay">Time in seconds before reverting. Use 0 for permanent change.</param>
        public void ChangeColorInstant(Color newColor, float revertDelay = 0f)
        {
            if (material == null) 
            {
                Debug.LogWarning($"ColorReactiveWall {gameObject.name}: material is null!");
                return;
            }
            
            // Stop any existing revert coroutine
            if (revertCoroutine != null)
            {
                StopCoroutine(revertCoroutine);
                revertCoroutine = null;
            }
            
            if (colorCoroutine != null)
            {
                StopCoroutine(colorCoroutine);
                colorCoroutine = null;
            }
            
            // Apply color immediately with intensity boost
            Color target = newColor * colorIntensity;
            target.a = 1f; // Ensure full opacity
            
            Debug.Log($"ColorReactiveWall {gameObject.name}: Changing color to {newColor} (target: {target})");
            
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", target);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", target);
            material.color = target;
                
            // Apply emission for glow effect
            if (useEmission)
            {
                material.EnableKeyword("_EMISSION");
                Color emissionColor = newColor * emissionIntensity;
                material.SetColor("_EmissionColor", emissionColor);
            }
            
            // Only start revert coroutine if delay is positive
            // If revertDelay <= 0, color stays until another cube hits
            if (revertDelay > 0f)
            {
                revertCoroutine = StartCoroutine(RevertAfterDelay(revertDelay));
            }
        }

        /// <summary>
        /// Reset wall to original color
        /// </summary>
        public void ResetColor()
        {
            if (material == null) return;

            // Stop any existing coroutines
            if (colorCoroutine != null)
            {
                StopCoroutine(colorCoroutine);
                colorCoroutine = null;
            }
            if (revertCoroutine != null)
            {
                StopCoroutine(revertCoroutine);
                revertCoroutine = null;
            }
            
            // Reset to original color
            targetColor = originalColor;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", originalColor);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", originalColor);
            material.color = originalColor;
            
            // Disable emission
            if (useEmission)
            {
                material.SetColor("_EmissionColor", Color.black);
            }
        }

        private IEnumerator RevertAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ResetColor();
        }

        // Visual indicator in editor
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GetSurfaceGizmoColor();
            Gizmos.DrawWireCube(transform.position, transform.localScale * 1.01f);
        }

        private Color GetSurfaceGizmoColor()
        {
            switch (surfaceType)
            {
                case SurfaceType.Metal: return Color.cyan;
                case SurfaceType.Concrete: return Color.gray;
                case SurfaceType.Wood: return new Color(0.6f, 0.3f, 0.1f);
                case SurfaceType.Glass: return Color.white;
                case SurfaceType.Stone: return new Color(0.4f, 0.4f, 0.35f);
                default: return Color.white;
            }
        }
    }
}
