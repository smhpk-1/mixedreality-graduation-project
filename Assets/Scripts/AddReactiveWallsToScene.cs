using UnityEngine;

namespace MusicSpace
{
    /// <summary>
    /// Utility script to add ColorReactiveWall component to existing walls in the scene.
    /// Attach this to any GameObject and use the context menu or let it run on Start.
    /// </summary>
    public class AddReactiveWallsToScene : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Names or partial names of objects to make reactive (e.g., 'Wall', 'Floor')")]
        public string[] wallNamePatterns = { "Wall", "Floor", "wall", "floor" };
        
        [Tooltip("If true, runs automatically when scene starts")]
        public bool runOnStart = true;
        
        [Tooltip("If true, also makes the floor reactive")]
        public bool includeFloor = true;

        private void Start()
        {
            if (runOnStart)
            {
                AddReactiveComponentsToWalls();
            }
        }

        [ContextMenu("Add ColorReactiveWall to All Walls")]
        public void AddReactiveComponentsToWalls()
        {
            int addedCount = 0;
            
            // Find all GameObjects with Colliders (potential walls)
            Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            
            foreach (Collider col in allColliders)
            {
                GameObject obj = col.gameObject;
                
                // Skip if already has ColorReactiveWall
                if (obj.GetComponent<ColorReactiveWall>() != null)
                {
                    Debug.Log($"Skipping {obj.name} - already has ColorReactiveWall");
                    continue;
                }
                
                // Check if name matches any pattern
                bool isWall = false;
                foreach (string pattern in wallNamePatterns)
                {
                    if (obj.name.Contains(pattern))
                    {
                        // Skip floor if not included
                        if (!includeFloor && obj.name.ToLower().Contains("floor"))
                            continue;
                            
                        isWall = true;
                        break;
                    }
                }
                
                if (isWall)
                {
                    AddReactiveWall(obj);
                    addedCount++;
                }
            }
            
            Debug.Log($"Added ColorReactiveWall to {addedCount} objects");
        }

        private void AddReactiveWall(GameObject obj)
        {
            ColorReactiveWall reactive = obj.AddComponent<ColorReactiveWall>();
            
            // Get current color from renderer
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Material mat = renderer.material; // Create instance
                Color currentColor;
                
                if (mat.HasProperty("_BaseColor"))
                    currentColor = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    currentColor = mat.GetColor("_Color");
                else
                    currentColor = mat.color;
                
                reactive.originalColor = currentColor;
                
                // Enable emission on the material
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
            
            // Guess surface type from name
            string nameLower = obj.name.ToLower();
            if (nameLower.Contains("metal"))
                reactive.surfaceType = SurfaceType.Metal;
            else if (nameLower.Contains("wood"))
                reactive.surfaceType = SurfaceType.Wood;
            else if (nameLower.Contains("glass"))
                reactive.surfaceType = SurfaceType.Glass;
            else if (nameLower.Contains("floor") || nameLower.Contains("stone"))
                reactive.surfaceType = SurfaceType.Stone;
            else
                reactive.surfaceType = SurfaceType.Concrete;
            
            reactive.useEmission = true;
            reactive.colorIntensity = 1.2f;
            reactive.emissionIntensity = 0.4f;
            
            // Add DestructibleWall to vertical walls (not floors)
            string objNameLower = obj.name.ToLower();
            if (!objNameLower.Contains("floor") && obj.GetComponent<DestructibleWall>() == null)
            {
                DestructibleWall destWall = obj.AddComponent<DestructibleWall>();
                destWall.requiredHits = 3;
                destWall.nextSceneName = "Scene 3";
            }
            
            Debug.Log($"Added ColorReactiveWall to: {obj.name} (Surface: {reactive.surfaceType})");
        }

        [ContextMenu("Remove All ColorReactiveWall Components")]
        public void RemoveAllReactiveWalls()
        {
            ColorReactiveWall[] all = FindObjectsByType<ColorReactiveWall>(FindObjectsSortMode.None);
            int count = all.Length;
            
            foreach (var reactive in all)
            {
                if (Application.isPlaying)
                    Destroy(reactive);
                else
                    DestroyImmediate(reactive);
            }
            
            Debug.Log($"Removed {count} ColorReactiveWall components");
        }
    }
}
