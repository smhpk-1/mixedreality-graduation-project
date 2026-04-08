using UnityEngine;

namespace MusicSpace
{
    /// <summary>
    /// Generates a small outdoor city environment around a metro entrance for Scene 2.
    /// Includes: ground (road + sidewalks), buildings, trees, street lamps, metro entrance with stairs.
    /// </summary>
    public class MetroEntranceCityGenerator : MonoBehaviour
    {
        [Header("Ground Dimensions")]
        public float roadWidth = 8f;
        public float sidewalkWidth = 4f;
        public float streetLength = 40f;
        public float sidewalkHeight = 0.15f;

        [Header("Buildings")]
        public int buildingsPerSide = 5;
        public float minBuildingHeight = 6f;
        public float maxBuildingHeight = 16f;
        public float minBuildingWidth = 4f;
        public float maxBuildingWidth = 7f;
        public float buildingDepth = 6f;
        public float buildingGap = 0.5f;

        [Header("Trees")]
        public int treesPerSide = 3;
        public float trunkRadius = 0.15f;
        public float trunkHeight = 2.5f;
        public float canopyRadius = 1.2f;

        [Header("Street Lamps")]
        public int lampsPerSide = 4;
        public float lampHeight = 4f;
        public float lampPoleRadius = 0.06f;

        [Header("Metro Entrance")]
        public float entranceWidth = 4f;
        public float entranceDepth = 3f;
        public float entranceHeight = 3f;
        public int stairCount = 10;
        public float stairDepth = 5f;

        [Header("Colors")]
        public Color roadColor = new Color(0.2f, 0.2f, 0.22f);
        public Color sidewalkColor = new Color(0.6f, 0.6f, 0.58f);
        public Color curbColor = new Color(0.5f, 0.5f, 0.48f);

        [Header("Building Colors")]
        public Color[] buildingColors = new Color[]
        {
            new Color(0.75f, 0.72f, 0.68f), // Beige
            new Color(0.6f, 0.58f, 0.55f),  // Gray stone
            new Color(0.8f, 0.78f, 0.7f),   // Cream
            new Color(0.55f, 0.5f, 0.45f),  // Brown gray
            new Color(0.7f, 0.65f, 0.6f),   // Warm gray
        };
        public Color windowColor = new Color(0.4f, 0.55f, 0.7f, 0.8f);
        public Color roofColor = new Color(0.3f, 0.28f, 0.26f);

        [Header("Nature Colors")]
        public Color trunkColor = new Color(0.35f, 0.25f, 0.15f);
        public Color canopyColor = new Color(0.2f, 0.45f, 0.15f);
        public Color canopyColorAlt = new Color(0.15f, 0.4f, 0.12f);

        [Header("Street Furniture Colors")]
        public Color lampPostColor = new Color(0.25f, 0.25f, 0.25f);
        public Color lampLightColor = new Color(1f, 0.95f, 0.8f);
        public Color metroEntranceColor = new Color(0.35f, 0.35f, 0.38f);
        public Color metroRailingColor = new Color(0.4f, 0.4f, 0.42f);
        public Color metroSignColor = new Color(0.1f, 0.2f, 0.6f);

        private Transform cityRoot;
        private int seed = 42;

        private void Start()
        {
            GenerateCity();
        }

        [ContextMenu("Generate City")]
        public void GenerateCity()
        {
            // Cleanup existing
            Transform existing = transform.Find("CityEnvironment");
            if (existing != null)
            {
                if (Application.isPlaying)
                    Destroy(existing.gameObject);
                else
                    DestroyImmediate(existing.gameObject);
            }

            Random.State oldState = Random.state;
            Random.InitState(seed);

            cityRoot = new GameObject("CityEnvironment").transform;
            cityRoot.parent = transform;
            cityRoot.localPosition = Vector3.zero;

            GenerateGround();
            GenerateBuildings();
            GenerateTrees();
            GenerateStreetLamps();
            GenerateMetroEntrance();

            Random.state = oldState;
        }

        private void GenerateGround()
        {
            Transform groundRoot = new GameObject("Ground").transform;
            groundRoot.parent = cityRoot;
            groundRoot.localPosition = Vector3.zero;

            float totalWidth = roadWidth + sidewalkWidth * 2;

            // Road
            GameObject road = CreateBox("Road", new Vector3(roadWidth, 0.05f, streetLength));
            road.transform.parent = groundRoot;
            road.transform.localPosition = new Vector3(0f, -0.025f, 0f);
            ApplyMaterial(road, roadColor);

            // Road markings (center line)
            float markingLength = 2f;
            float markingGap = 2f;
            Transform markingsRoot = new GameObject("RoadMarkings").transform;
            markingsRoot.parent = groundRoot;
            markingsRoot.localPosition = Vector3.zero;

            for (float z = -streetLength / 2f + 1f; z < streetLength / 2f - 1f; z += markingLength + markingGap)
            {
                GameObject marking = CreateBox("Marking", new Vector3(0.15f, 0.06f, markingLength));
                marking.transform.parent = markingsRoot;
                marking.transform.localPosition = new Vector3(0f, 0f, z + markingLength / 2f);
                ApplyMaterial(marking, Color.white);
            }

            // Left sidewalk
            GameObject leftSidewalk = CreateBox("LeftSidewalk", new Vector3(sidewalkWidth, sidewalkHeight, streetLength));
            leftSidewalk.transform.parent = groundRoot;
            leftSidewalk.transform.localPosition = new Vector3(-(roadWidth / 2f + sidewalkWidth / 2f), sidewalkHeight / 2f, 0f);
            ApplyMaterial(leftSidewalk, sidewalkColor);

            // Right sidewalk
            GameObject rightSidewalk = CreateBox("RightSidewalk", new Vector3(sidewalkWidth, sidewalkHeight, streetLength));
            rightSidewalk.transform.parent = groundRoot;
            rightSidewalk.transform.localPosition = new Vector3(roadWidth / 2f + sidewalkWidth / 2f, sidewalkHeight / 2f, 0f);
            ApplyMaterial(rightSidewalk, sidewalkColor);

            // Curbs
            float curbHeight = sidewalkHeight + 0.05f;
            float curbWidth = 0.15f;
            GameObject leftCurb = CreateBox("LeftCurb", new Vector3(curbWidth, curbHeight, streetLength));
            leftCurb.transform.parent = groundRoot;
            leftCurb.transform.localPosition = new Vector3(-(roadWidth / 2f + curbWidth / 2f), curbHeight / 2f, 0f);
            ApplyMaterial(leftCurb, curbColor);

            GameObject rightCurb = CreateBox("RightCurb", new Vector3(curbWidth, curbHeight, streetLength));
            rightCurb.transform.parent = groundRoot;
            rightCurb.transform.localPosition = new Vector3(roadWidth / 2f + curbWidth / 2f, curbHeight / 2f, 0f);
            ApplyMaterial(rightCurb, curbColor);
        }

        private void GenerateBuildings()
        {
            Transform buildingsRoot = new GameObject("Buildings").transform;
            buildingsRoot.parent = cityRoot;
            buildingsRoot.localPosition = Vector3.zero;

            // Left side buildings
            GenerateBuildingRow(buildingsRoot, -1);
            // Right side buildings
            GenerateBuildingRow(buildingsRoot, 1);
        }

        private void GenerateBuildingRow(Transform parent, int side)
        {
            string sideName = side < 0 ? "Left" : "Right";
            Transform rowRoot = new GameObject($"Buildings_{sideName}").transform;
            rowRoot.parent = parent;
            rowRoot.localPosition = Vector3.zero;

            float xOffset = side * (roadWidth / 2f + sidewalkWidth + buildingDepth / 2f);
            float zStart = -streetLength / 2f + 2f;

            // Skip the metro entrance area on the right side
            float metroZoneStart = -entranceWidth / 2f - 2f;
            float metroZoneEnd = entranceWidth / 2f + 2f;

            for (int i = 0; i < buildingsPerSide; i++)
            {
                float bWidth = Random.Range(minBuildingWidth, maxBuildingWidth);
                float bHeight = Random.Range(minBuildingHeight, maxBuildingHeight);
                float zPos = zStart + bWidth / 2f;

                // Skip metro zone on right side
                if (side > 0 && zPos > metroZoneStart && zPos < metroZoneEnd)
                {
                    zStart = metroZoneEnd + buildingGap;
                    zPos = zStart + bWidth / 2f;
                }

                if (zPos + bWidth / 2f > streetLength / 2f - 2f) break;

                Color bColor = buildingColors[i % buildingColors.Length];
                GameObject building = GenerateSingleBuilding($"Building_{sideName}_{i}", bWidth, bHeight, buildingDepth, bColor);
                building.transform.parent = rowRoot;
                building.transform.localPosition = new Vector3(xOffset, 0f, zPos);

                zStart = zPos + bWidth / 2f + buildingGap;
            }
        }

        private GameObject GenerateSingleBuilding(string name, float width, float height, float depth, Color baseColor)
        {
            GameObject building = new GameObject(name);

            // Main body
            GameObject body = CreateBox("Body", new Vector3(width, height, depth));
            body.transform.parent = building.transform;
            body.transform.localPosition = new Vector3(0f, height / 2f, 0f);
            ApplyMaterial(body, baseColor);

            // Roof
            GameObject roof = CreateBox("Roof", new Vector3(width + 0.3f, 0.2f, depth + 0.3f));
            roof.transform.parent = building.transform;
            roof.transform.localPosition = new Vector3(0f, height + 0.1f, 0f);
            ApplyMaterial(roof, roofColor);

            // Windows
            float windowSize = 0.8f;
            float windowSpacingX = 1.8f;
            float windowSpacingY = 2.5f;
            float windowDepthOffset = 0.05f;

            int windowCols = Mathf.FloorToInt((width - 1f) / windowSpacingX);
            int windowRows = Mathf.FloorToInt((height - 1.5f) / windowSpacingY);

            Transform windowsRoot = new GameObject("Windows").transform;
            windowsRoot.parent = building.transform;
            windowsRoot.localPosition = Vector3.zero;

            // Front face windows
            for (int row = 0; row < windowRows; row++)
            {
                for (int col = 0; col < windowCols; col++)
                {
                    float wx = -((windowCols - 1) * windowSpacingX) / 2f + col * windowSpacingX;
                    float wy = 2f + row * windowSpacingY;

                    GameObject window = CreateBox($"Window_{row}_{col}", new Vector3(windowSize, windowSize * 1.2f, windowDepthOffset));
                    window.transform.parent = windowsRoot;
                    window.transform.localPosition = new Vector3(wx, wy, -depth / 2f - windowDepthOffset / 2f);
                    ApplyMaterial(window, windowColor, 0.7f, 0.9f);
                }
            }

            // Door
            GameObject door = CreateBox("Door", new Vector3(1f, 2.2f, windowDepthOffset));
            door.transform.parent = building.transform;
            door.transform.localPosition = new Vector3(0f, 1.1f, -depth / 2f - windowDepthOffset / 2f);
            ApplyMaterial(door, new Color(0.3f, 0.2f, 0.15f));

            return building;
        }

        private void GenerateTrees()
        {
            Transform treesRoot = new GameObject("Trees").transform;
            treesRoot.parent = cityRoot;
            treesRoot.localPosition = Vector3.zero;

            for (int side = -1; side <= 1; side += 2)
            {
                float xPos = side * (roadWidth / 2f + sidewalkWidth * 0.6f);
                float spacing = streetLength / (treesPerSide + 1);

                for (int i = 0; i < treesPerSide; i++)
                {
                    float zPos = -streetLength / 2f + spacing * (i + 1);

                    // Skip metro entrance zone on right side
                    if (side > 0 && Mathf.Abs(zPos) < entranceWidth / 2f + 3f)
                        continue;

                    GameObject tree = GenerateTree($"Tree_{(side < 0 ? "L" : "R")}_{i}");
                    tree.transform.parent = treesRoot;
                    tree.transform.localPosition = new Vector3(xPos, sidewalkHeight, zPos);
                }
            }
        }

        private GameObject GenerateTree(string name)
        {
            GameObject tree = new GameObject(name);

            // Trunk (cylinder)
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = tree.transform;
            trunk.transform.localPosition = new Vector3(0f, trunkHeight / 2f, 0f);
            trunk.transform.localScale = new Vector3(trunkRadius * 2f, trunkHeight / 2f, trunkRadius * 2f);
            ApplyMaterial(trunk, trunkColor);

            // Canopy layers (3 spheres for a fuller look)
            Color cColor = Random.value > 0.5f ? canopyColor : canopyColorAlt;

            GameObject canopy1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy1.name = "Canopy_Main";
            canopy1.transform.parent = tree.transform;
            canopy1.transform.localPosition = new Vector3(0f, trunkHeight + canopyRadius * 0.6f, 0f);
            canopy1.transform.localScale = Vector3.one * canopyRadius * 2f;
            ApplyMaterial(canopy1, cColor);

            GameObject canopy2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy2.name = "Canopy_Top";
            canopy2.transform.parent = tree.transform;
            canopy2.transform.localPosition = new Vector3(0f, trunkHeight + canopyRadius * 1.4f, 0f);
            canopy2.transform.localScale = Vector3.one * canopyRadius * 1.4f;
            ApplyMaterial(canopy2, cColor * 1.1f);

            GameObject canopy3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy3.name = "Canopy_Side";
            canopy3.transform.parent = tree.transform;
            canopy3.transform.localPosition = new Vector3(canopyRadius * 0.4f, trunkHeight + canopyRadius * 0.5f, canopyRadius * 0.3f);
            canopy3.transform.localScale = Vector3.one * canopyRadius * 1.2f;
            ApplyMaterial(canopy3, cColor * 0.9f);

            return tree;
        }

        private void GenerateStreetLamps()
        {
            Transform lampsRoot = new GameObject("StreetLamps").transform;
            lampsRoot.parent = cityRoot;
            lampsRoot.localPosition = Vector3.zero;

            for (int side = -1; side <= 1; side += 2)
            {
                float xPos = side * (roadWidth / 2f + sidewalkWidth * 0.3f);
                float spacing = streetLength / (lampsPerSide + 1);

                for (int i = 0; i < lampsPerSide; i++)
                {
                    float zPos = -streetLength / 2f + spacing * (i + 1);

                    // Skip metro entrance zone on right side
                    if (side > 0 && Mathf.Abs(zPos) < entranceWidth / 2f + 2f)
                        continue;

                    GameObject lamp = GenerateStreetLamp($"Lamp_{(side < 0 ? "L" : "R")}_{i}");
                    lamp.transform.parent = lampsRoot;
                    lamp.transform.localPosition = new Vector3(xPos, sidewalkHeight, zPos);
                }
            }
        }

        private GameObject GenerateStreetLamp(string name)
        {
            GameObject lamp = new GameObject(name);

            // Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.parent = lamp.transform;
            pole.transform.localPosition = new Vector3(0f, lampHeight / 2f, 0f);
            pole.transform.localScale = new Vector3(lampPoleRadius * 2f, lampHeight / 2f, lampPoleRadius * 2f);
            ApplyMaterial(pole, lampPostColor);

            // Lamp head (horizontal arm)
            GameObject arm = CreateBox("Arm", new Vector3(0.08f, 0.08f, 0.8f));
            arm.transform.parent = lamp.transform;
            arm.transform.localPosition = new Vector3(0f, lampHeight, 0.4f);
            ApplyMaterial(arm, lampPostColor);

            // Light housing
            GameObject housing = CreateBox("Housing", new Vector3(0.25f, 0.1f, 0.4f));
            housing.transform.parent = lamp.transform;
            housing.transform.localPosition = new Vector3(0f, lampHeight - 0.1f, 0.7f);
            ApplyMaterial(housing, lampPostColor);

            // Light bulb (emissive)
            GameObject bulb = CreateBox("Bulb", new Vector3(0.2f, 0.04f, 0.35f));
            bulb.transform.parent = lamp.transform;
            bulb.transform.localPosition = new Vector3(0f, lampHeight - 0.17f, 0.7f);
            ApplyMaterial(bulb, lampLightColor, 0f, 0.5f, lampLightColor, 2f);

            // Point light
            GameObject lightObj = new GameObject("StreetLight");
            lightObj.transform.parent = lamp.transform;
            lightObj.transform.localPosition = new Vector3(0f, lampHeight - 0.3f, 0.7f);
            Light pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = lampLightColor;
            pointLight.intensity = 1.5f;
            pointLight.range = 10f;
            pointLight.shadows = LightShadows.Soft;

            return lamp;
        }

        private void GenerateMetroEntrance()
        {
            Transform metroRoot = new GameObject("MetroEntrance").transform;
            metroRoot.parent = cityRoot;

            // Position on right sidewalk
            float xPos = roadWidth / 2f + sidewalkWidth / 2f;
            metroRoot.localPosition = new Vector3(xPos, sidewalkHeight, 0f);

            // Entrance frame - two pillars and a top beam
            float pillarSize = 0.3f;

            // Left pillar
            GameObject leftPillar = CreateBox("LeftPillar", new Vector3(pillarSize, entranceHeight, pillarSize));
            leftPillar.transform.parent = metroRoot;
            leftPillar.transform.localPosition = new Vector3(-entranceWidth / 2f, entranceHeight / 2f, 0f);
            ApplyMaterial(leftPillar, metroEntranceColor);

            // Right pillar
            GameObject rightPillar = CreateBox("RightPillar", new Vector3(pillarSize, entranceHeight, pillarSize));
            rightPillar.transform.parent = metroRoot;
            rightPillar.transform.localPosition = new Vector3(entranceWidth / 2f, entranceHeight / 2f, 0f);
            ApplyMaterial(rightPillar, metroEntranceColor);

            // Top beam
            GameObject topBeam = CreateBox("TopBeam", new Vector3(entranceWidth + pillarSize, 0.3f, entranceDepth));
            topBeam.transform.parent = metroRoot;
            topBeam.transform.localPosition = new Vector3(0f, entranceHeight + 0.15f, entranceDepth / 2f);
            ApplyMaterial(topBeam, metroEntranceColor);

            // Canopy/Roof over entrance
            GameObject canopy = CreateBox("Canopy", new Vector3(entranceWidth + 1f, 0.1f, entranceDepth + 1f));
            canopy.transform.parent = metroRoot;
            canopy.transform.localPosition = new Vector3(0f, entranceHeight + 0.35f, entranceDepth / 2f);
            ApplyMaterial(canopy, metroEntranceColor * 0.8f);

            // Metro sign
            GameObject sign = CreateBox("MetroSign", new Vector3(entranceWidth * 0.8f, 0.6f, 0.1f));
            sign.transform.parent = metroRoot;
            sign.transform.localPosition = new Vector3(0f, entranceHeight + 0.65f, -0.05f);
            ApplyMaterial(sign, metroSignColor, 0f, 0.3f, metroSignColor, 1f);

            // "M" letter on sign
            GameObject mLetter = CreateBox("M_Letter", new Vector3(0.4f, 0.4f, 0.12f));
            mLetter.transform.parent = metroRoot;
            mLetter.transform.localPosition = new Vector3(0f, entranceHeight + 0.65f, -0.1f);
            ApplyMaterial(mLetter, Color.white);

            // Side walls of stairway
            float stairTotalDepth = stairDepth;
            float stairTotalDrop = entranceHeight;

            GameObject leftWall = CreateBox("LeftStairWall", new Vector3(0.2f, entranceHeight + 1f, stairTotalDepth + entranceDepth));
            leftWall.transform.parent = metroRoot;
            leftWall.transform.localPosition = new Vector3(-entranceWidth / 2f, entranceHeight / 2f - stairTotalDrop / 2f, entranceDepth / 2f + stairTotalDepth / 2f);
            ApplyMaterial(leftWall, metroEntranceColor * 0.9f);

            GameObject rightWall = CreateBox("RightStairWall", new Vector3(0.2f, entranceHeight + 1f, stairTotalDepth + entranceDepth));
            rightWall.transform.parent = metroRoot;
            rightWall.transform.localPosition = new Vector3(entranceWidth / 2f, entranceHeight / 2f - stairTotalDrop / 2f, entranceDepth / 2f + stairTotalDepth / 2f);
            ApplyMaterial(rightWall, metroEntranceColor * 0.9f);

            // Stairs going down
            Transform stairsRoot = new GameObject("Stairs").transform;
            stairsRoot.parent = metroRoot;
            stairsRoot.localPosition = new Vector3(0f, 0f, entranceDepth);

            float stepWidth = entranceWidth - 0.4f;
            float stepHeight = stairTotalDrop / stairCount;
            float stepDepth = stairTotalDepth / stairCount;

            for (int i = 0; i < stairCount; i++)
            {
                GameObject step = CreateBox($"Step_{i}", new Vector3(stepWidth, stepHeight, stepDepth));
                step.transform.parent = stairsRoot;
                step.transform.localPosition = new Vector3(0f, -stepHeight * i - stepHeight / 2f, stepDepth * i + stepDepth / 2f);
                ApplyMaterial(step, Color.Lerp(sidewalkColor, metroEntranceColor, 0.5f));
            }

            // Railings
            float railingHeight = 0.9f;
            float railingRadius = 0.03f;

            for (int side = -1; side <= 1; side += 2)
            {
                float railX = side * (stepWidth / 2f + 0.05f);
                Transform railRoot = new GameObject($"Railing_{(side < 0 ? "Left" : "Right")}").transform;
                railRoot.parent = metroRoot;
                railRoot.localPosition = Vector3.zero;

                // Railing posts
                for (int i = 0; i <= stairCount; i += 2)
                {
                    float pY = -stepHeight * i + railingHeight / 2f;
                    float pZ = entranceDepth + stepDepth * i;

                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.name = $"Post_{i}";
                    post.transform.parent = railRoot;
                    post.transform.localPosition = new Vector3(railX, pY, pZ);
                    post.transform.localScale = new Vector3(railingRadius * 2f, railingHeight / 2f, railingRadius * 2f);
                    ApplyMaterial(post, metroRailingColor);
                }
            }

            // Floor at bottom of stairs (for transition trigger)
            GameObject bottomFloor = CreateBox("BottomFloor", new Vector3(stepWidth, 0.1f, 2f));
            bottomFloor.transform.parent = metroRoot;
            bottomFloor.transform.localPosition = new Vector3(0f, -stairTotalDrop - 0.05f, entranceDepth + stairTotalDepth + 1f);
            ApplyMaterial(bottomFloor, metroEntranceColor);

            // Scene transition trigger zone (invisible)
            GameObject trigger = new GameObject("SceneTransitionTrigger");
            trigger.transform.parent = metroRoot;
            trigger.transform.localPosition = new Vector3(0f, -stairTotalDrop + 1f, entranceDepth + stairTotalDepth + 1f);
            BoxCollider triggerCollider = trigger.AddComponent<BoxCollider>();
            triggerCollider.size = new Vector3(stepWidth, 2f, 2f);
            triggerCollider.isTrigger = true;
        }

        // === Helper Methods ===

        private GameObject CreateBox(string name, Vector3 size)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.localScale = size;
            return obj;
        }

        private void ApplyMaterial(GameObject obj, Color color, float metallic = 0f, float smoothness = 0.3f, Color? emissionColor = null, float emissionIntensity = 0f)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            if (emissionColor.HasValue && emissionIntensity > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor.Value * emissionIntensity);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            renderer.material = mat;
        }
    }
}
