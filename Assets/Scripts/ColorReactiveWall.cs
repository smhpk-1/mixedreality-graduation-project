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
    /// A wall or floor that changes color when hit by a MusicCube.
    /// </summary>
    public class ColorReactiveWall : MonoBehaviour
    {
        [Header("Surface Properties")]
        public SurfaceType surfaceType = SurfaceType.Concrete;
        
        [Header("Color Settings")]
        public Color originalColor = Color.gray;
        public float colorTransitionSpeed = 5f;
        public float colorIntensity = 1.5f; // How bright the color becomes

        [Header("Visual Feedback")]
        public bool useEmission = true;
        public float emissionIntensity = 0.8f;

        private MeshRenderer meshRenderer;
        private Material material;
        private Color targetColor;
        private bool isTransitioning = false;
        private Coroutine colorCoroutine;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                // Create material instance
                material = meshRenderer.material;
                originalColor = material.color;
                targetColor = originalColor;
            }
        }

        public void ChangeColor(Color newColor)
        {
            if (material == null) return;

            // Stop any existing transition
            if (colorCoroutine != null)
            {
                StopCoroutine(colorCoroutine);
            }

            // Apply color with intensity boost
            targetColor = newColor * colorIntensity;
            colorCoroutine = StartCoroutine(TransitionColor(targetColor));

            // Apply emission for glow effect
            if (useEmission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", newColor * emissionIntensity);
            }
        }

        public void ResetColor()
        {
            if (material == null) return;

            // Stop any existing transition
            if (colorCoroutine != null)
            {
                StopCoroutine(colorCoroutine);
            }

            targetColor = originalColor;
            colorCoroutine = StartCoroutine(TransitionColor(originalColor));

            // Reset emission
            if (useEmission)
            {
                material.SetColor("_EmissionColor", Color.black);
            }
        }

        private IEnumerator TransitionColor(Color target)
        {
            isTransitioning = true;
            Color startColor = material.color;
            float elapsed = 0f;
            float duration = 1f / colorTransitionSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                material.color = Color.Lerp(startColor, target, t);
                yield return null;
            }

            material.color = target;
            isTransitioning = false;
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
