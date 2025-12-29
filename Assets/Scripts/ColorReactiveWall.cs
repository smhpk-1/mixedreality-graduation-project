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
    /// A wall or floor that changes color when hit by any cube with a Rigidbody.
    /// Detects collisions and extracts color from the cube's material.
    /// </summary>
    public class ColorReactiveWall : MonoBehaviour
    {
        [Header("Surface Properties")]
        public SurfaceType surfaceType = SurfaceType.Concrete;
        
        [Header("Color Settings")]
        public Color originalColor = Color.gray;
        public float colorTransitionSpeed = 5f;
        public float colorIntensity = 1.2f;

        [Header("Visual Feedback")]
        public bool useEmission = true;
        public float emissionIntensity = 0.5f;
        
        [Header("Collision Settings")]
        public float minVelocityToTrigger = 0.5f;

        private MeshRenderer meshRenderer;
        private Material material;
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
                
            }
            else
            {
                Debug.LogWarning($"ColorReactiveWall {gameObject.name}: No MeshRenderer found!");
            }
            
            // Ensure collider exists for collision detection
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }
            else if (col.isTrigger)
            {
                col.isTrigger = false;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Check if the colliding object has a Rigidbody (i.e., it's a thrown object)
            Rigidbody rb = collision.rigidbody;
            if (rb == null) return;
            
            // Check velocity threshold
            float velocity = collision.relativeVelocity.magnitude;
            if (velocity < minVelocityToTrigger) return;
            
            // Get color from the cube's renderer
            Renderer cubeRenderer = collision.gameObject.GetComponent<Renderer>();
            if (cubeRenderer == null) return;
            
            Material cubeMat = cubeRenderer.sharedMaterial;
            if (cubeMat == null) cubeMat = cubeRenderer.material;
            
            Color cubeColor = GetColorFromMaterial(cubeMat);
            
            // Ignore very dark colors (probably not a colored cube)
            if (cubeColor.r < 0.1f && cubeColor.g < 0.1f && cubeColor.b < 0.1f) return;
            
            // Change wall color to match the cube (permanent until next hit)
            ChangeColorInstant(cubeColor, 0f);
        }
        
        // Fallback for trigger colliders
        private void OnTriggerEnter(Collider other)
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb == null) return;
            
            Renderer cubeRenderer = other.GetComponent<Renderer>();
            if (cubeRenderer == null) return;
            
            Material cubeMat = cubeRenderer.sharedMaterial ?? cubeRenderer.material;
            Color cubeColor = GetColorFromMaterial(cubeMat);
            
            if (cubeColor.r < 0.1f && cubeColor.g < 0.1f && cubeColor.b < 0.1f) return;
            
            ChangeColorInstant(cubeColor, 0f);
        }

        private Color GetColorFromMaterial(Material mat)
        {
            if (mat == null) return Color.white;
            
            if (mat.HasProperty("_BaseColor"))
                return mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color"))
                return mat.GetColor("_Color");
            else
                return mat.color;
        }

        /// <summary>
        /// Instantly change wall color. If revertDelay is 0 or negative, color persists until changed again.
        /// </summary>
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
            
            // Apply color immediately with intensity boost
            Color target = newColor * colorIntensity;
            target.a = 1f;
            
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
            
            // Only revert if delay is positive
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

            if (revertCoroutine != null)
            {
                StopCoroutine(revertCoroutine);
                revertCoroutine = null;
            }
            
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", originalColor);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", originalColor);
            material.color = originalColor;
            
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
