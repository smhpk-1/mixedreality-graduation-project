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
            // Küp çarpınca rengi değiştir, ayrılınca geri döndür
            private int cubesTouching = 0;

            private void OnCollisionEnter(Collision collision)
            {
                if (collision.gameObject.CompareTag("Cube"))
                {
                    Debug.Log("Çarpışma: " + collision.gameObject.name);
                    var cubeRenderer = collision.gameObject.GetComponent<Renderer>();
                    if (cubeRenderer != null)
                    {
                        Material cubeMat = cubeRenderer.material;
                        Color cubeColor;
                        if (cubeMat.HasProperty("_BaseColor"))
                            cubeColor = cubeMat.GetColor("_BaseColor");
                        else if (cubeMat.HasProperty("_Color"))
                            cubeColor = cubeMat.GetColor("_Color");
                        else
                            cubeColor = cubeMat.color;
                        Debug.Log($"Duvara atanacak renk: {cubeColor}");
                        ChangeColorInstant(cubeColor, 1.5f);
                        cubesTouching++;
                    }
                }
            }

            private void OnCollisionExit(Collision collision)
            {
                if (collision.gameObject.CompareTag("Cube"))
                {
                    cubesTouching = Mathf.Max(0, cubesTouching - 1);
                    // Sadece son küp ayrıldığında eski rengine dön
                    if (cubesTouching == 0)
                    {
                        ResetColor();
                    }
                }
            }
        private Coroutine revertCoroutine;
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
                // URP: _BaseColor veya _Color kullan
                if (material.HasProperty("_BaseColor"))
                    originalColor = material.GetColor("_BaseColor");
                else if (material.HasProperty("_Color"))
                    originalColor = material.GetColor("_Color");
                else
                    originalColor = material.color;
                targetColor = originalColor;
            }
        }

        public void ChangeColor(Color newColor)
            // Eski metot, anlık değişim için ChangeColorInstant kullanılacak
        {
            if (material == null) return;

            // Stop any existing transition
            if (colorCoroutine != null)
            {
                StopCoroutine(colorCoroutine);
            }

            // URP: _BaseColor veya _Color kullanarak rengi değiştir
            targetColor = newColor * colorIntensity;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", targetColor);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", targetColor);
            else
                material.color = targetColor;

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
            if (revertCoroutine != null)
            {
                StopCoroutine(revertCoroutine);
                revertCoroutine = null;
            }
            // Rengi ve emisyonu eski haline getir
            targetColor = originalColor;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", originalColor);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", originalColor);
            else
                material.color = originalColor;
            colorCoroutine = StartCoroutine(TransitionColor(originalColor));
            if (useEmission)
            {
                material.SetColor("_EmissionColor", Color.black);
            }
        }

        /// <summary>
        /// Instantly change wall color. If revertDelay is 0 or negative, color persists until changed again.
        /// </summary>
        /// <param name="newColor">The new color to apply</param>
        /// <param name="revertDelay">Time in seconds before reverting. Use 0 for permanent change.</param>
        public void ChangeColorInstant(Color newColor, float revertDelay = 1.5f)
        {
            if (material == null) return;
            
            // Stop any existing revert coroutine
            if (revertCoroutine != null)
            {
                StopCoroutine(revertCoroutine);
                revertCoroutine = null;
            }
            
            // Apply color immediately
            Color target = newColor * colorIntensity;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", target);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", target);
            else
                material.color = target;
                
            // Apply emission for glow effect
            if (useEmission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", newColor * emissionIntensity);
            }
            
            // Only start revert coroutine if delay is positive
            // If revertDelay <= 0, color stays until another cube hits
            if (revertDelay > 0f)
            {
                revertCoroutine = StartCoroutine(RevertAfterDelay(revertDelay));
            }
        }

        private IEnumerator RevertAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ResetColor();
        }

        private IEnumerator TransitionColor(Color target)
        {
            isTransitioning = true;
            Color startColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") :
                (material.HasProperty("_Color") ? material.GetColor("_Color") : material.color);
            float elapsed = 0f;
            float duration = 1f / colorTransitionSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Color lerped = Color.Lerp(startColor, target, t);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", lerped);
                else if (material.HasProperty("_Color"))
                    material.SetColor("_Color", lerped);
                else
                    material.color = lerped;
                yield return null;
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", target);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", target);
            else
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
