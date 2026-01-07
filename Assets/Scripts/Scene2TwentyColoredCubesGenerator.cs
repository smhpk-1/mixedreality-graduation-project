using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MusicSpace
{
    /// <summary>
    /// Generates 20 colored cubes (5 colors x 4 each) for Scene 2.
    /// Cubes can be grabbed and thrown at reactive walls to change their colors.
    /// When grabbed, cubes play a looping sound based on their color.
    /// </summary>
    public class Scene2TwentyColoredCubesGenerator : MonoBehaviour
    {
        [Header("Cube Settings")]
        public int cubesPerColor = 4;
        public float cubeScale = 0.3f; // Standardized cube size
        public float spacing = 1.2f;
        
        [Header("Layout")]
        public Vector3 gridCenter = new Vector3(0, 0.15f, 0);
        
        [Header("Materials")]
        public Material[] cubeMaterials = new Material[5];
        
        public readonly string[] colorNames = { "Red", "Blue", "Green", "Yellow", "Purple" };
        public readonly Color[] defaultColors = new Color[5] {
            new Color(1f, 0.2f, 0.2f),      // Red
            new Color(0.2f, 0.4f, 1f),      // Blue
            new Color(0.2f, 1f, 0.3f),      // Green
            new Color(1f, 0.95f, 0.2f),     // Yellow
            new Color(0.7f, 0.2f, 0.9f)     // Purple
        };

        private void Start()
        {
            // First, ensure all existing cubes in scene have PlaygroundCube component
            SetupExistingCubes();
            
            // Then generate new cubes if none exist
            Transform existingCubes = transform.Find("PlaygroundCubes");
            if (existingCubes == null || existingCubes.childCount == 0)
            {
                // Only generate if no cubes exist yet as children
                // Check if there are already cubes as direct children
                bool hasDirectChildCubes = false;
                foreach (Transform child in transform)
                {
                    if (child.name.StartsWith("Cube_"))
                    {
                        hasDirectChildCubes = true;
                        break;
                    }
                }
                
                if (!hasDirectChildCubes)
                {
                    GenerateCubes();
                }
            }
        }
        
        /// <summary>
        /// Setup existing cubes in the scene with PlaygroundCube component
        /// </summary>
        private void SetupExistingCubes()
        {
            // Find all cubes that are children of this transform
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Cube_"))
                {
                    SetupCubeComponent(child.gameObject);
                }
            }
            
            // Also find cubes tagged as "Cube" in the scene
            GameObject[] taggedCubes = GameObject.FindGameObjectsWithTag("Cube");
            foreach (GameObject cube in taggedCubes)
            {
                SetupCubeComponent(cube);
            }
        }
        
        /// <summary>
        /// Add PlaygroundCube component to an existing cube if missing
        /// </summary>
        private void SetupCubeComponent(GameObject cube)
        {
            // Skip if already has PlaygroundCube
            if (cube.GetComponent<PlaygroundCube>() != null) return;
            
            // Add PlaygroundCube component
            PlaygroundCube pc = cube.AddComponent<PlaygroundCube>();
            pc.spawnPosition = cube.transform.position;
            
            // Detect color from name
            string cubeName = cube.name.ToLower();
            if (cubeName.Contains("red"))
            {
                pc.colorName = "red";
                pc.cubeColor = defaultColors[0];
            }
            else if (cubeName.Contains("blue"))
            {
                pc.colorName = "blue";
                pc.cubeColor = defaultColors[1];
            }
            else if (cubeName.Contains("green"))
            {
                pc.colorName = "green";
                pc.cubeColor = defaultColors[2];
            }
            else if (cubeName.Contains("yellow"))
            {
                pc.colorName = "yellow";
                pc.cubeColor = defaultColors[3];
            }
            else if (cubeName.Contains("purple"))
            {
                pc.colorName = "purple";
                pc.cubeColor = defaultColors[4];
            }
            
            // Make sure cube has proper tag
            cube.tag = "Cube";
            
            Debug.Log($"Setup PlaygroundCube on {cube.name} with color {pc.colorName}");
        }

#if UNITY_EDITOR
        [ContextMenu("Generate 20 Colored Cubes (5x4)")]
#endif
        public void GenerateCubes()
        {
            // Clean up existing cubes
            Transform existingCubes = transform.Find("PlaygroundCubes");
            if (existingCubes != null)
            {
                if (Application.isPlaying)
                    Destroy(existingCubes.gameObject);
                else
                    DestroyImmediate(existingCubes.gameObject);
            }
            
            // Create parent for organization
            GameObject cubesRoot = new GameObject("PlaygroundCubes");
            cubesRoot.transform.parent = this.transform;
            cubesRoot.transform.localPosition = Vector3.zero;

            // Create materials if not assigned
            EnsureMaterials();

            // Calculate grid dimensions (5 columns x 4 rows)
            int columns = 5;
            int rows = cubesPerColor;
            float totalWidth = (columns - 1) * spacing;
            float totalDepth = (rows - 1) * spacing;
            float startX = gridCenter.x - totalWidth / 2f;
            float startZ = gridCenter.z - totalDepth / 2f;

            int cubeIndex = 0;
            for (int colorIdx = 0; colorIdx < 5; colorIdx++)
            {
                for (int i = 0; i < cubesPerColor; i++)
                {
                    // Calculate position in grid
                    int col = cubeIndex % columns;
                    int row = cubeIndex / columns;
                    float x = startX + col * spacing;
                    float z = startZ + row * spacing;
                    float y = gridCenter.y + cubeScale / 2f;
                    
                    Vector3 spawnPos = new Vector3(x, y, z);
                    
                    // Create the cube
                    GameObject cube = CreatePlaygroundCube(
                        $"Cube_{colorNames[colorIdx]}_{i + 1}",
                        spawnPos,
                        defaultColors[colorIdx],
                        cubeMaterials[colorIdx]
                    );
                    
                    cube.transform.parent = cubesRoot.transform;
                    cubeIndex++;
                }
            }
            
            Debug.Log($"Generated {cubeIndex} playground cubes for Scene 2");
        }

        private void EnsureMaterials()
        {
            for (int i = 0; i < 5; i++)
            {
                if (cubeMaterials.Length <= i || cubeMaterials[i] == null)
                {
                    // Try URP first, fallback to Standard
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    var mat = new Material(shader);
                    mat.color = defaultColors[i];
                    
                    // Set color for URP compatibility
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", defaultColors[i]);
                    
                    if (cubeMaterials.Length <= i)
                        System.Array.Resize(ref cubeMaterials, i + 1);
                    cubeMaterials[i] = mat;
                }
            }
        }

        private GameObject CreatePlaygroundCube(string name, Vector3 position, Color color, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.tag = "Cube"; // Important for ColorReactiveWall detection
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * cubeScale;

            // Apply material and color
            Renderer renderer = cube.GetComponent<Renderer>();
            if (material != null)
            {
                Material instanceMat = Application.isPlaying ? Instantiate(material) : material;
                instanceMat.color = color;
                if (instanceMat.HasProperty("_BaseColor"))
                    instanceMat.SetColor("_BaseColor", color);
                renderer.material = instanceMat;
            }

            // Add Rigidbody for physics
            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Add XR Grab Interactable
            XRGrabInteractable grab = cube.AddComponent<XRGrabInteractable>();
            grab.movementType = XRGrabInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;
            grab.forceGravityOnDetach = true;
            grab.interactionLayers = -1; // Interact with everything
            
            // Ensure collider is registered
            Collider col = cube.GetComponent<Collider>();
            if (col != null && !grab.colliders.Contains(col))
            {
                grab.colliders.Add(col);
            }

            // Add PlaygroundCube component for wall interaction
            PlaygroundCube playgroundCube = cube.AddComponent<PlaygroundCube>();
            playgroundCube.cubeColor = color;
            playgroundCube.spawnPosition = position;
            
            // Extract color name from cube name (e.g., "Cube_Red_1" -> "red")
            string cubeName = name.ToLower();
            if (cubeName.Contains("red")) playgroundCube.colorName = "red";
            else if (cubeName.Contains("blue")) playgroundCube.colorName = "blue";
            else if (cubeName.Contains("green")) playgroundCube.colorName = "green";
            else if (cubeName.Contains("yellow")) playgroundCube.colorName = "yellow";
            else if (cubeName.Contains("purple")) playgroundCube.colorName = "purple";

            return cube;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Visualize cube positions in editor
            int columns = 5;
            int rows = cubesPerColor;
            float totalWidth = (columns - 1) * spacing;
            float totalDepth = (rows - 1) * spacing;
            float startX = gridCenter.x - totalWidth / 2f;
            float startZ = gridCenter.z - totalDepth / 2f;

            int cubeIndex = 0;
            for (int colorIdx = 0; colorIdx < 5; colorIdx++)
            {
                Gizmos.color = defaultColors[colorIdx];
                for (int i = 0; i < cubesPerColor; i++)
                {
                    int col = cubeIndex % columns;
                    int row = cubeIndex / columns;
                    float x = startX + col * spacing;
                    float z = startZ + row * spacing;
                    float y = gridCenter.y + cubeScale / 2f;
                    
                    Gizmos.DrawWireCube(new Vector3(x, y, z), Vector3.one * cubeScale);
                    cubeIndex++;
                }
            }
        }
#endif
    }
}