using UnityEngine;

namespace MusicSpace
{
    /// <summary>
    /// Generates the room for Scene 2 with reactive walls and floor.
    /// Each surface has a different material type for audio effects.
    /// Walls change color when hit by PlaygroundCubes.
    /// </summary>
    public class Scene2RoomGenerator : MonoBehaviour
    {
        [Header("Room Dimensions")]
        public float width = 8f;
        public float length = 8f;
        public float height = 4f;
        public float wallThickness = 0.2f;

        [Header("Wall Colors (Initial)")]
        public Color floorColor = new Color(0.3f, 0.3f, 0.35f);       // Dark stone
        public Color frontWallColor = new Color(0.6f, 0.65f, 0.7f);   // Metal gray (lighter)
        public Color backWallColor = new Color(0.55f, 0.55f, 0.55f);  // Concrete (lighter)
        public Color leftWallColor = new Color(0.5f, 0.4f, 0.3f);     // Wood brown (lighter)
        public Color rightWallColor = new Color(0.75f, 0.8f, 0.85f);  // Glass white

        [Header("Lighting")]
        public Color ambientColor = new Color(0.15f, 0.15f, 0.2f);
        public float mainLightIntensity = 1.5f;

        private Transform roomRoot;

        private void Start()
        {
            GenerateRoom();
            SetupLighting();
        }

        [ContextMenu("Generate Room")]
        public void GenerateRoom()
        {
            // Cleanup existing
            Transform existing = transform.Find("Scene2_Room");
            if (existing != null)
            {
                if (Application.isPlaying)
                    Destroy(existing.gameObject);
                else
                    DestroyImmediate(existing.gameObject);
            }

            roomRoot = new GameObject("Scene2_Room").transform;
            roomRoot.parent = transform;
            roomRoot.localPosition = Vector3.zero;

            float halfWidth = width / 2f;
            float halfLength = length / 2f;
            float halfHeight = height / 2f;

            // Create surfaces with different types
            // Floor - Stone (deep, bass-heavy)
            CreateReactiveWall("Floor", 
                new Vector3(0, -wallThickness / 2f, 0),
                new Vector3(width, wallThickness, length),
                floorColor,
                SurfaceType.Stone);

            // Front Wall (+Z) - Metal (high reverb, bright)
            CreateReactiveWall("Wall_Front_Metal",
                new Vector3(0, halfHeight, halfLength + wallThickness / 2f),
                new Vector3(width, height, wallThickness),
                frontWallColor,
                SurfaceType.Metal);

            // Back Wall (-Z) - Concrete (medium reverb, neutral)
            CreateReactiveWall("Wall_Back_Concrete",
                new Vector3(0, halfHeight, -halfLength - wallThickness / 2f),
                new Vector3(width, height, wallThickness),
                backWallColor,
                SurfaceType.Concrete);

            // Left Wall (-X) - Wood (low reverb, warm)
            CreateReactiveWall("Wall_Left_Wood",
                new Vector3(-halfWidth - wallThickness / 2f, halfHeight, 0),
                new Vector3(wallThickness, height, length),
                leftWallColor,
                SurfaceType.Wood);

            // Right Wall (+X) - Glass (sharp, high frequencies)
            CreateReactiveWall("Wall_Right_Glass",
                new Vector3(halfWidth + wallThickness / 2f, halfHeight, 0),
                new Vector3(wallThickness, height, length),
                rightWallColor,
                SurfaceType.Glass);

            // Ceiling (no reactive - just visual)
            CreateCeiling("Ceiling",
                new Vector3(0, height + wallThickness / 2f, 0),
                new Vector3(width, wallThickness, length));

            Debug.Log("Scene 2 Room generated with 4 reactive walls + reactive floor!");
        }

        private void CreateReactiveWall(string name, Vector3 position, Vector3 scale, Color color, SurfaceType surfaceType)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.parent = roomRoot;
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;

            // Setup material - use URP Lit shader
            MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            
            // Set color on all possible properties
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", GetSmoothnessForSurface(surfaceType));
            
            // Enable emission capability
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            
            renderer.material = mat;

            // Add ColorReactiveWall component - THIS IS THE KEY!
            ColorReactiveWall reactiveWall = wall.AddComponent<ColorReactiveWall>();
            reactiveWall.surfaceType = surfaceType;
            reactiveWall.originalColor = color;
            reactiveWall.useEmission = true;
            reactiveWall.colorIntensity = 1.2f;
            reactiveWall.emissionIntensity = 0.4f;

            Debug.Log($"Created reactive wall: {name} with ColorReactiveWall component");
        }

        private void CreateCeiling(string name, Vector3 position, Vector3 scale)
        {
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = name;
            ceiling.transform.parent = roomRoot;
            ceiling.transform.localPosition = position;
            ceiling.transform.localScale = scale;

            // Dark ceiling - not reactive
            MeshRenderer renderer = ceiling.GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = new Color(0.2f, 0.2f, 0.25f);
            renderer.material = mat;
        }

        private float GetSmoothnessForSurface(SurfaceType type)
        {
            switch (type)
            {
                case SurfaceType.Metal: return 0.7f;
                case SurfaceType.Glass: return 0.9f;
                case SurfaceType.Concrete: return 0.2f;
                case SurfaceType.Wood: return 0.3f;
                case SurfaceType.Stone: return 0.15f;
                default: return 0.5f;
            }
        }

        private void SetupLighting()
        {
            // Set ambient lighting - brighter for Scene 2
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;

            // Disable any existing directional lights
            Light[] existingLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in existingLights)
            {
                if (l.type == LightType.Directional)
                {
                    l.intensity = mainLightIntensity;
                    l.color = Color.white;
                }
            }

            // Add center point light for atmosphere
            CreatePointLight("Light_Center", new Vector3(0, height - 0.5f, 0), new Color(1f, 0.98f, 0.95f), 20f, 1.2f);
        }

        private void CreatePointLight(string name, Vector3 position, Color color, float range, float intensity)
        {
            // Check if light already exists
            Transform existing = roomRoot?.Find(name);
            if (existing != null) return;
            
            GameObject lightObj = new GameObject(name);
            lightObj.transform.parent = roomRoot;
            lightObj.transform.position = position;

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw room bounds
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3(0, height / 2f, 0), new Vector3(width, height, length));

            // Draw surface type labels
            Gizmos.color = Color.yellow;
            float halfWidth = width / 2f;
            float halfLength = length / 2f;

            Gizmos.DrawSphere(new Vector3(0, 0.1f, 0), 0.1f); // Floor
            Gizmos.DrawSphere(new Vector3(0, height / 2f, halfLength), 0.1f); // Front
            Gizmos.DrawSphere(new Vector3(0, height / 2f, -halfLength), 0.1f); // Back
            Gizmos.DrawSphere(new Vector3(-halfWidth, height / 2f, 0), 0.1f); // Left
            Gizmos.DrawSphere(new Vector3(halfWidth, height / 2f, 0), 0.1f); // Right
        }
    }
}
