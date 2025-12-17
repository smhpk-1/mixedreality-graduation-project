using UnityEngine;

namespace MusicSpace
{
    /// <summary>
    /// Generates the room for Scene 2 with reactive walls and floor.
    /// Each surface has a different material type for audio effects.
    /// </summary>
    public class Scene2RoomGenerator : MonoBehaviour
    {
        [Header("Room Dimensions")]
        public float width = 8f;
        public float length = 8f;
        public float height = 4f;
        public float wallThickness = 0.2f;

        [Header("Colors")]
        public Color floorColor = new Color(0.25f, 0.25f, 0.3f);      // Dark stone
        public Color frontWallColor = new Color(0.5f, 0.55f, 0.6f);   // Metal gray
        public Color backWallColor = new Color(0.45f, 0.45f, 0.45f);  // Concrete
        public Color leftWallColor = new Color(0.4f, 0.3f, 0.2f);     // Wood brown
        public Color rightWallColor = new Color(0.7f, 0.75f, 0.8f);   // Glass white

        [Header("Lighting")]
        public Color ambientColor = new Color(0.1f, 0.1f, 0.15f);
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
            if (existing != null) DestroyImmediate(existing.gameObject);

            roomRoot = new GameObject("Scene2_Room").transform;
            roomRoot.parent = transform;
            roomRoot.localPosition = Vector3.zero;

            float halfWidth = width / 2f;
            float halfLength = length / 2f;
            float halfHeight = height / 2f;

            // Create surfaces with different types
            // Floor - Stone (deep, bass-heavy)
            CreateWall("Floor", 
                new Vector3(0, -wallThickness / 2f, 0),
                new Vector3(width, wallThickness, length),
                floorColor,
                SurfaceType.Stone);

            // Front Wall (+Z) - Metal (high reverb, bright)
            CreateWall("Wall_Front_Metal",
                new Vector3(0, halfHeight, halfLength + wallThickness / 2f),
                new Vector3(width, height, wallThickness),
                frontWallColor,
                SurfaceType.Metal);

            // Back Wall (-Z) - Concrete (medium reverb, neutral)
            CreateWall("Wall_Back_Concrete",
                new Vector3(0, halfHeight, -halfLength - wallThickness / 2f),
                new Vector3(width, height, wallThickness),
                backWallColor,
                SurfaceType.Concrete);

            // Left Wall (-X) - Wood (low reverb, warm)
            CreateWall("Wall_Left_Wood",
                new Vector3(-halfWidth - wallThickness / 2f, halfHeight, 0),
                new Vector3(wallThickness, height, length),
                leftWallColor,
                SurfaceType.Wood);

            // Right Wall (+X) - Glass (sharp, high frequencies)
            CreateWall("Wall_Right_Glass",
                new Vector3(halfWidth + wallThickness / 2f, halfHeight, 0),
                new Vector3(wallThickness, height, length),
                rightWallColor,
                SurfaceType.Glass);

            // Ceiling (no reactive - just visual)
            CreateCeiling("Ceiling",
                new Vector3(0, height + wallThickness / 2f, 0),
                new Vector3(width, wallThickness, length));

            Debug.Log("Scene 2 Room generated with reactive surfaces!");
        }

        private void CreateWall(string name, Vector3 position, Vector3 scale, Color color, SurfaceType surfaceType)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.parent = roomRoot;
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;

            // Setup material
            MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Smoothness", GetSmoothnessForSurface(surfaceType));
            renderer.material = mat;

            // Add ColorReactiveWall component
            ColorReactiveWall reactiveWall = wall.AddComponent<ColorReactiveWall>();
            reactiveWall.surfaceType = surfaceType;
            reactiveWall.originalColor = color;

            // Make it static for physics optimization
            wall.isStatic = false; // Can't be static if we're changing material
        }

        private void CreateCeiling(string name, Vector3 position, Vector3 scale)
        {
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = name;
            ceiling.transform.parent = roomRoot;
            ceiling.transform.localPosition = position;
            ceiling.transform.localScale = scale;

            // Dark ceiling
            MeshRenderer renderer = ceiling.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.15f, 0.15f, 0.2f);
            renderer.material = mat;

            ceiling.isStatic = true;
        }

        private float GetSmoothnessForSurface(SurfaceType type)
        {
            switch (type)
            {
                case SurfaceType.Metal: return 0.8f;
                case SurfaceType.Glass: return 0.95f;
                case SurfaceType.Concrete: return 0.2f;
                case SurfaceType.Wood: return 0.3f;
                case SurfaceType.Stone: return 0.1f;
                default: return 0.5f;
            }
        }

        private void SetupLighting()
        {
            // Set ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;

            // Create main light if not exists
            Light existingLight = FindFirstObjectByType<Light>();
            if (existingLight == null)
            {
                GameObject lightObj = new GameObject("Main Light");
                lightObj.transform.parent = roomRoot;
                lightObj.transform.position = new Vector3(0, height - 0.5f, 0);
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = mainLightIntensity;
                light.shadows = LightShadows.Soft;
            }

            // Add some point lights for atmosphere
            CreatePointLight("Light_Center", new Vector3(0, height - 0.5f, 0), new Color(1f, 0.95f, 0.9f), 15f, 0.8f);
        }

        private void CreatePointLight(string name, Vector3 position, Color color, float range, float intensity)
        {
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

            // Arrows indicating surfaces
            DrawSurfaceLabel(new Vector3(0, 0.1f, 0), "Stone Floor");
            DrawSurfaceLabel(new Vector3(0, height / 2f, halfLength), "Metal Wall");
            DrawSurfaceLabel(new Vector3(0, height / 2f, -halfLength), "Concrete Wall");
            DrawSurfaceLabel(new Vector3(-halfWidth, height / 2f, 0), "Wood Wall");
            DrawSurfaceLabel(new Vector3(halfWidth, height / 2f, 0), "Glass Wall");
        }

        private void DrawSurfaceLabel(Vector3 pos, string label)
        {
            Gizmos.DrawSphere(pos, 0.1f);
        }
    }
}
