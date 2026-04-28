using UnityEngine;

namespace MusicSpace
{
    /// <summary>
    /// Generates a pedestrian city street with buildings on both sides and a metro entrance at the end.
    /// No car road — just a walkable area between buildings leading to the metro station entrance.
    /// </summary>
    public class MetroEntranceCityGenerator : MonoBehaviour
    {
        [Header("Pedestrian Street")]
        public float streetWidth = 10f;
        public float streetLength = 30f;

        [Header("Buildings")]
        public int buildingsPerSide = 4;
        public float minBuildingHeight = 8f;
        public float maxBuildingHeight = 18f;
        public float minBuildingWidth = 5f;
        public float maxBuildingWidth = 8f;
        public float buildingDepth = 7f;
        public float buildingGap = 0.8f;

        [Header("Trees")]
        public int treeCount = 4;
        public float trunkRadius = 0.15f;
        public float trunkHeight = 2.5f;
        public float canopyRadius = 1.2f;

        [Header("Street Lamps")]
        public int lampsPerSide = 3;
        public float lampHeight = 4f;
        public float lampPoleRadius = 0.06f;

        [Header("Metro Entrance")]
        public float entranceWidth = 5f;
        public float entranceDepth = 3.5f;
        public float entranceHeight = 3.5f;
        public int stairCount = 12;
        public float stairDepth = 6f;
        public string metroStationName = "METRO";

        [Header("Colors - Ground")]
        public Color pavementColor = new Color(0.55f, 0.53f, 0.5f);
        public Color pavementTileColor = new Color(0.6f, 0.58f, 0.55f);

        [Header("Colors - Buildings")]
        public Color[] buildingColors = new Color[]
        {
            new Color(0.78f, 0.75f, 0.7f),
            new Color(0.65f, 0.62f, 0.58f),
            new Color(0.82f, 0.8f, 0.73f),
            new Color(0.58f, 0.53f, 0.48f),
            new Color(0.72f, 0.68f, 0.63f),
        };
        public Color windowColor = new Color(0.4f, 0.55f, 0.7f, 0.8f);
        public Color roofColor = new Color(0.3f, 0.28f, 0.26f);
        public Color shopfrontColor = new Color(0.35f, 0.3f, 0.25f);
        public Color awningColor = new Color(0.6f, 0.15f, 0.1f);

        [Header("Colors - Nature")]
        public Color trunkColor = new Color(0.35f, 0.25f, 0.15f);
        public Color canopyColor = new Color(0.2f, 0.45f, 0.15f);
        public Color canopyColorAlt = new Color(0.15f, 0.4f, 0.12f);

        [Header("Colors - Street Furniture")]
        public Color lampPostColor = new Color(0.2f, 0.2f, 0.2f);
        public Color lampLightColor = new Color(1f, 0.95f, 0.8f);
        public Color benchColor = new Color(0.35f, 0.25f, 0.15f);

        [Header("Colors - Metro")]
        public Color metroEntranceColor = new Color(0.4f, 0.4f, 0.43f);
        public Color metroRailingColor = new Color(0.5f, 0.5f, 0.52f);
        public Color metroSignBgColor = new Color(0.05f, 0.1f, 0.5f);
        public Color metroSignTextColor = new Color(1f, 1f, 1f);
        public Color metroSignGlowColor = new Color(0.2f, 0.4f, 1f);

        private Transform cityRoot;
        private int seed = 42;

        private void Start()
        {
            // City is pre-generated in the editor via [ContextMenu("Generate City")].
            // Do NOT regenerate at runtime — it would destroy any manually added objects.
        }

        [ContextMenu("Generate City")]
        public void GenerateCity()
        {
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

            GenerateBaseGround();
            GeneratePedestrianStreet();
            GenerateBuildings();
            GenerateTrees();
            GenerateStreetLamps();
            GenerateBenches();
            GenerateMetroEntrance();
            GenerateLighting();

            Random.state = oldState;
        }

        private void GenerateBaseGround()
        {
            float baseSize = streetLength + 60f;
            float halfBase = baseSize / 2f;
            // Calculate groundY so all BaseGround pieces sit at world y ≈ -0.1 (player feet level)
            float worldGroundY = -0.1f;
            float groundY = worldGroundY - transform.position.y;
            float groundThick = 0.5f;
            Color groundColor = new Color(0.22f, 0.22f, 0.2f);

            // Hole dimensions — must cover the ENTIRE underground tunnel
            float holeWidth = entranceWidth + 2f;
            float walkingDist = 4f; // must match walkingDistance in GenerateMetroEntrance
            float elevDep = 2.5f;   // must match elevDepth in GenerateMetroEntrance
            float holeDepth = entranceDepth + stairDepth + walkingDist + elevDep + 2f; // full tunnel + margin
            float holeZ = streetLength; // metro entrance Z position
            float halfHoleW = holeWidth / 2f;

            // Split ground into 4 pieces around the hole:
            // 1) FRONT piece: from -halfBase to holeZ (everything before the hole)
            float frontLength = holeZ + halfBase;
            GameObject front = CreateBox("BaseGround_Front", new Vector3(baseSize, groundThick, frontLength));
            front.transform.parent = cityRoot;
            front.transform.localPosition = new Vector3(0f, groundY, -halfBase + frontLength / 2f);
            ApplyMaterial(front, groundColor);
            front.isStatic = true;

            // 2) BACK piece: from holeZ+holeDepth to +halfBase
            float backStart = holeZ + holeDepth;
            float backLength = halfBase - backStart + halfBase; // remaining
            if (backLength > 0.1f)
            {
                GameObject back = CreateBox("BaseGround_Back", new Vector3(baseSize, groundThick, backLength));
                back.transform.parent = cityRoot;
                back.transform.localPosition = new Vector3(0f, groundY, backStart + backLength / 2f);
                ApplyMaterial(back, groundColor);
                back.isStatic = true;
            }

            // 3) LEFT strip: beside hole on left
            GameObject leftStrip = CreateBox("BaseGround_Left", new Vector3(halfBase - halfHoleW, groundThick, holeDepth));
            leftStrip.transform.parent = cityRoot;
            leftStrip.transform.localPosition = new Vector3(-(halfHoleW + (halfBase - halfHoleW) / 2f), groundY, holeZ + holeDepth / 2f);
            ApplyMaterial(leftStrip, groundColor);
            leftStrip.isStatic = true;

            // 4) RIGHT strip: beside hole on right
            GameObject rightStrip = CreateBox("BaseGround_Right", new Vector3(halfBase - halfHoleW, groundThick, holeDepth));
            rightStrip.transform.parent = cityRoot;
            rightStrip.transform.localPosition = new Vector3(halfHoleW + (halfBase - halfHoleW) / 2f, groundY, holeZ + holeDepth / 2f);
            ApplyMaterial(rightStrip, groundColor);
            rightStrip.isStatic = true;
        }

        private void GenerateLighting()
        {
            GameObject sunObj = new GameObject("Sun_DirectionalLight");
            sunObj.transform.parent = cityRoot;
            sunObj.transform.localPosition = new Vector3(0f, 20f, 0f);
            sunObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            Light sunLight = sunObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, 0.96f, 0.9f);
            sunLight.intensity = 2f;
            sunLight.shadows = LightShadows.Soft;
        }

        private void GeneratePedestrianStreet()
        {
            Transform groundRoot = new GameObject("PedestrianGround").transform;
            groundRoot.parent = cityRoot;
            groundRoot.localPosition = Vector3.zero;

            // Main pavement
            GameObject pavement = CreateBox("Pavement", new Vector3(streetWidth, 0.1f, streetLength));
            pavement.transform.parent = groundRoot;
            pavement.transform.localPosition = new Vector3(0f, 0.05f, streetLength / 2f);
            ApplyMaterial(pavement, pavementColor);

            // Decorative tile strips along the sides
            for (int side = -1; side <= 1; side += 2)
            {
                float xPos = side * (streetWidth / 2f - 0.3f);
                GameObject strip = CreateBox($"TileStrip_{(side < 0 ? "L" : "R")}", new Vector3(0.6f, 0.11f, streetLength));
                strip.transform.parent = groundRoot;
                strip.transform.localPosition = new Vector3(xPos, 0.055f, streetLength / 2f);
                ApplyMaterial(strip, pavementTileColor);
            }

            // Small plaza area in front of metro entrance (stops before the entrance hole)
            float plazaWidth = streetWidth + 4f;
            float plazaDepth = 2f;
            GameObject plaza = CreateBox("MetroPlaza", new Vector3(plazaWidth, 0.1f, plazaDepth));
            plaza.transform.parent = groundRoot;
            plaza.transform.localPosition = new Vector3(0f, 0.05f, streetLength - plazaDepth / 2f);
            ApplyMaterial(plaza, pavementTileColor);
        }

        private void GenerateBuildings()
        {
            Transform buildingsRoot = new GameObject("Buildings").transform;
            buildingsRoot.parent = cityRoot;
            buildingsRoot.localPosition = Vector3.zero;

            GenerateBuildingRow(buildingsRoot, -1);
            GenerateBuildingRow(buildingsRoot, 1);
        }

        private void GenerateBuildingRow(Transform parent, int side)
        {
            string sideName = side < 0 ? "Left" : "Right";
            Transform rowRoot = new GameObject($"Buildings_{sideName}").transform;
            rowRoot.parent = parent;
            rowRoot.localPosition = Vector3.zero;

            float xOffset = side * (streetWidth / 2f + buildingDepth / 2f);
            float zStart = 1f;

            for (int i = 0; i < buildingsPerSide; i++)
            {
                float bWidth = Random.Range(minBuildingWidth, maxBuildingWidth);
                float bHeight = Random.Range(minBuildingHeight, maxBuildingHeight);
                float zPos = zStart + bWidth / 2f;

                if (zPos + bWidth / 2f > streetLength - 1f) break;

                Color bColor = buildingColors[i % buildingColors.Length];
                GameObject building = GenerateSingleBuilding($"Building_{sideName}_{i}", bWidth, bHeight, buildingDepth, bColor, side);
                building.transform.parent = rowRoot;
                building.transform.localPosition = new Vector3(xOffset, 0f, zPos);

                zStart = zPos + bWidth / 2f + buildingGap;
            }
        }

        private GameObject GenerateSingleBuilding(string name, float width, float height, float depth, Color baseColor, int side)
        {
            GameObject building = new GameObject(name);

            // Main body
            GameObject body = CreateBox("Body", new Vector3(width, height, depth));
            body.transform.parent = building.transform;
            body.transform.localPosition = new Vector3(0f, height / 2f, 0f);
            ApplyMaterial(body, baseColor);

            // Roof
            GameObject roof = CreateBox("Roof", new Vector3(width + 0.3f, 0.25f, depth + 0.3f));
            roof.transform.parent = building.transform;
            roof.transform.localPosition = new Vector3(0f, height + 0.125f, 0f);
            ApplyMaterial(roof, roofColor);

            // Windows on the street-facing side
            float windowWidth = 0.9f;
            float windowHeight = 1.1f;
            float windowSpacingX = 2f;
            float windowSpacingY = 2.8f;
            float windowInset = 0.06f;

            int windowCols = Mathf.FloorToInt((width - 1.5f) / windowSpacingX);
            int windowRows = Mathf.FloorToInt((height - 4f) / windowSpacingY);
            float facingDir = -side;
            float faceX = facingDir * depth / 2f;

            Transform windowsRoot = new GameObject("Windows").transform;
            windowsRoot.parent = building.transform;
            windowsRoot.localPosition = Vector3.zero;

            for (int row = 0; row < windowRows; row++)
            {
                for (int col = 0; col < windowCols; col++)
                {
                    float wz = -((windowCols - 1) * windowSpacingX) / 2f + col * windowSpacingX;
                    float wy = 4.5f + row * windowSpacingY;

                    GameObject window = CreateBox($"Window_{row}_{col}", new Vector3(windowInset, windowHeight, windowWidth));
                    window.transform.parent = windowsRoot;
                    window.transform.localPosition = new Vector3(faceX + facingDir * windowInset / 2f, wy, wz);
                    ApplyMaterial(window, windowColor, 0.5f, 0.9f);
                }
            }

            // Ground floor shopfront
            float shopHeight = 3f;
            GameObject shopfront = CreateBox("Shopfront", new Vector3(windowInset, shopHeight, width - 0.5f));
            shopfront.transform.parent = building.transform;
            shopfront.transform.localPosition = new Vector3(faceX + facingDir * windowInset / 2f, shopHeight / 2f, 0f);
            ApplyMaterial(shopfront, shopfrontColor);

            // Shop window (glass)
            GameObject shopWindow = CreateBox("ShopWindow", new Vector3(windowInset + 0.01f, 2f, width * 0.6f));
            shopWindow.transform.parent = building.transform;
            shopWindow.transform.localPosition = new Vector3(faceX + facingDir * (windowInset / 2f + 0.005f), 1.5f, 0f);
            ApplyMaterial(shopWindow, new Color(0.5f, 0.65f, 0.8f, 0.7f), 0.3f, 0.95f);

            // Awning over shop
            GameObject awning = CreateBox("Awning", new Vector3(1f, 0.05f, width * 0.7f));
            awning.transform.parent = building.transform;
            awning.transform.localPosition = new Vector3(faceX + facingDir * 0.5f, shopHeight + 0.1f, 0f);
            Color aColor = Random.value > 0.5f ? awningColor : new Color(0.15f, 0.3f, 0.5f);
            ApplyMaterial(awning, aColor);

            return building;
        }

        private void GenerateTrees()
        {
            Transform treesRoot = new GameObject("Trees").transform;
            treesRoot.parent = cityRoot;
            treesRoot.localPosition = Vector3.zero;

            float spacing = streetLength / (treeCount + 1);
            for (int i = 0; i < treeCount; i++)
            {
                float zPos = spacing * (i + 1);
                // Alternate sides
                float xPos = (i % 2 == 0 ? -1f : 1f) * (streetWidth / 2f - 1.2f);

                GameObject tree = GenerateTree($"Tree_{i}");
                tree.transform.parent = treesRoot;
                tree.transform.localPosition = new Vector3(xPos, 0.1f, zPos);
            }
        }

        private GameObject GenerateTree(string name)
        {
            GameObject tree = new GameObject(name);

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = tree.transform;
            trunk.transform.localPosition = new Vector3(0f, trunkHeight / 2f, 0f);
            trunk.transform.localScale = new Vector3(trunkRadius * 2f, trunkHeight / 2f, trunkRadius * 2f);
            ApplyMaterial(trunk, trunkColor);

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
                float xPos = side * (streetWidth / 2f - 0.5f);
                float spacing = streetLength / (lampsPerSide + 1);

                for (int i = 0; i < lampsPerSide; i++)
                {
                    float zPos = spacing * (i + 1);

                    GameObject lamp = GenerateStreetLamp($"Lamp_{(side < 0 ? "L" : "R")}_{i}");
                    lamp.transform.parent = lampsRoot;
                    lamp.transform.localPosition = new Vector3(xPos, 0.1f, zPos);
                }
            }
        }

        private GameObject GenerateStreetLamp(string name)
        {
            GameObject lamp = new GameObject(name);

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.parent = lamp.transform;
            pole.transform.localPosition = new Vector3(0f, lampHeight / 2f, 0f);
            pole.transform.localScale = new Vector3(lampPoleRadius * 2f, lampHeight / 2f, lampPoleRadius * 2f);
            ApplyMaterial(pole, lampPostColor);

            // Classic lamp head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "LampHead";
            head.transform.parent = lamp.transform;
            head.transform.localPosition = new Vector3(0f, lampHeight + 0.15f, 0f);
            head.transform.localScale = new Vector3(0.35f, 0.25f, 0.35f);
            ApplyMaterial(head, lampLightColor, 0f, 0.5f, lampLightColor, 3f);

            GameObject lightObj = new GameObject("StreetLight");
            lightObj.transform.parent = lamp.transform;
            lightObj.transform.localPosition = new Vector3(0f, lampHeight, 0f);
            Light pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = lampLightColor;
            pointLight.intensity = 1.5f;
            pointLight.range = 12f;
            pointLight.shadows = LightShadows.Soft;

            return lamp;
        }

        private void GenerateBenches()
        {
            Transform benchRoot = new GameObject("Benches").transform;
            benchRoot.parent = cityRoot;
            benchRoot.localPosition = Vector3.zero;

            float[] benchPositions = { streetLength * 0.25f, streetLength * 0.5f, streetLength * 0.75f };
            for (int i = 0; i < benchPositions.Length; i++)
            {
                float xPos = (i % 2 == 0 ? -1f : 1f) * (streetWidth / 2f - 1.5f);
                GameObject bench = GenerateBench($"Bench_{i}");
                bench.transform.parent = benchRoot;
                bench.transform.localPosition = new Vector3(xPos, 0.1f, benchPositions[i]);
                // Rotate to face the street center
                bench.transform.rotation = Quaternion.Euler(0f, (i % 2 == 0) ? 90f : -90f, 0f);
            }
        }

        private GameObject GenerateBench(string name)
        {
            GameObject bench = new GameObject(name);

            // Seat
            GameObject seat = CreateBox("Seat", new Vector3(1.5f, 0.08f, 0.5f));
            seat.transform.parent = bench.transform;
            seat.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            ApplyMaterial(seat, benchColor);

            // Back
            GameObject back = CreateBox("Back", new Vector3(1.5f, 0.5f, 0.08f));
            back.transform.parent = bench.transform;
            back.transform.localPosition = new Vector3(0f, 0.7f, -0.22f);
            ApplyMaterial(back, benchColor);

            // Legs
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject leg = CreateBox($"Leg_{(i < 0 ? "L" : "R")}", new Vector3(0.08f, 0.45f, 0.08f));
                leg.transform.parent = bench.transform;
                leg.transform.localPosition = new Vector3(i * 0.6f, 0.225f, 0f);
                ApplyMaterial(leg, lampPostColor);
            }

            return bench;
        }

        private void GenerateMetroEntrance()
        {
            Transform metroRoot = new GameObject("MetroEntrance").transform;
            metroRoot.parent = cityRoot;
            metroRoot.localPosition = new Vector3(0f, 0.1f, streetLength);

            float stairTotalDrop = entranceHeight;
            float wallThick = 0.3f;
            float ceilingAtTop = entranceHeight + 0.5f;
            float elevDepth = 2.5f;
            float walkingDistance = 4f; // walking area between stairs and elevator
            float totalTunnelLength = entranceDepth + stairDepth + walkingDistance + elevDepth;
            float totalHeight = ceilingAtTop + stairTotalDrop;
            float bottomY = -stairTotalDrop;

            // === ENTRANCE FRAME (visible from street) ===
            float pillarSize = 0.35f;
            GameObject leftPillar = CreateBox("LeftPillar", new Vector3(pillarSize, entranceHeight, pillarSize));
            leftPillar.transform.parent = metroRoot;
            leftPillar.transform.localPosition = new Vector3(-entranceWidth / 2f, entranceHeight / 2f, 0f);
            ApplyMaterial(leftPillar, metroEntranceColor, 0.3f, 0.4f);

            GameObject rightPillar = CreateBox("RightPillar", new Vector3(pillarSize, entranceHeight, pillarSize));
            rightPillar.transform.parent = metroRoot;
            rightPillar.transform.localPosition = new Vector3(entranceWidth / 2f, entranceHeight / 2f, 0f);
            ApplyMaterial(rightPillar, metroEntranceColor, 0.3f, 0.4f);

            GameObject canopy = CreateBox("Canopy", new Vector3(entranceWidth + 2f, 0.12f, entranceDepth + 1.5f));
            canopy.transform.parent = metroRoot;
            canopy.transform.localPosition = new Vector3(0f, ceilingAtTop, entranceDepth / 2f);
            ApplyMaterial(canopy, metroEntranceColor * 0.7f, 0.4f, 0.5f);

            // === METRO SIGN ===
            float signWidth = entranceWidth + 1f;
            float signHeight = 1f;
            GameObject signBg = CreateBox("MetroSign_Background", new Vector3(signWidth, signHeight, 0.15f));
            signBg.transform.parent = metroRoot;
            signBg.transform.localPosition = new Vector3(0f, entranceHeight + 1.2f, -0.1f);
            ApplyMaterial(signBg, metroSignBgColor, 0f, 0.3f, metroSignGlowColor, 1.5f);

            float borderThickness = 0.08f;
            GameObject borderTop = CreateBox("SignBorder_Top", new Vector3(signWidth + 0.1f, borderThickness, 0.18f));
            borderTop.transform.parent = metroRoot;
            borderTop.transform.localPosition = new Vector3(0f, entranceHeight + 1.2f + signHeight / 2f + borderThickness / 2f, -0.1f);
            ApplyMaterial(borderTop, metroSignTextColor, 0.5f, 0.7f);

            GameObject borderBot = CreateBox("SignBorder_Bottom", new Vector3(signWidth + 0.1f, borderThickness, 0.18f));
            borderBot.transform.parent = metroRoot;
            borderBot.transform.localPosition = new Vector3(0f, entranceHeight + 1.2f - signHeight / 2f - borderThickness / 2f, -0.1f);
            ApplyMaterial(borderBot, metroSignTextColor, 0.5f, 0.7f);

            GameObject mCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mCircle.name = "M_Symbol";
            mCircle.transform.parent = metroRoot;
            mCircle.transform.localPosition = new Vector3(-signWidth / 2f + 0.8f, entranceHeight + 1.2f, -0.2f);
            mCircle.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mCircle.transform.localScale = new Vector3(0.7f, 0.1f, 0.7f);
            ApplyMaterial(mCircle, metroSignTextColor, 0f, 0.3f, metroSignTextColor, 2f);

            string text = metroStationName;
            float letterWidth = 0.35f;
            float letterSpacing = 0.45f;
            float textStartX = -((text.Length - 1) * letterSpacing) / 2f + 0.5f;
            Transform textRoot = new GameObject("SignText").transform;
            textRoot.parent = metroRoot;
            textRoot.localPosition = Vector3.zero;
            for (int i = 0; i < text.Length; i++)
            {
                GameObject letter = CreateBox($"Letter_{text[i]}", new Vector3(letterWidth, 0.5f, 0.08f));
                letter.transform.parent = textRoot;
                letter.transform.localPosition = new Vector3(textStartX + i * letterSpacing, entranceHeight + 1.2f, -0.22f);
                ApplyMaterial(letter, metroSignTextColor, 0f, 0.3f, metroSignTextColor, 2f);
            }

            GameObject signPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            signPole.name = "StandingSignPole";
            signPole.transform.parent = metroRoot;
            signPole.transform.localPosition = new Vector3(-entranceWidth / 2f - 1.5f, 1.5f, -0.5f);
            signPole.transform.localScale = new Vector3(0.08f, 1.5f, 0.08f);
            ApplyMaterial(signPole, lampPostColor);

            GameObject standingSign = CreateBox("StandingSign", new Vector3(1.2f, 0.8f, 0.1f));
            standingSign.transform.parent = metroRoot;
            standingSign.transform.localPosition = new Vector3(-entranceWidth / 2f - 1.5f, 3.2f, -0.5f);
            ApplyMaterial(standingSign, metroSignBgColor, 0f, 0.3f, metroSignGlowColor, 1f);

            // === FULLY ENCLOSED TUNNEL (walls + ceiling + floor + back wall) ===
            // Left wall — full height, full tunnel length
            GameObject leftWall = CreateBox("LeftWall", new Vector3(wallThick, totalHeight, totalTunnelLength));
            leftWall.transform.parent = metroRoot;
            leftWall.transform.localPosition = new Vector3(
                -entranceWidth / 2f - wallThick / 2f,
                ceilingAtTop / 2f - stairTotalDrop / 2f,
                totalTunnelLength / 2f);
            ApplyMaterial(leftWall, metroEntranceColor * 0.85f, 0.1f, 0.3f);

            // Right wall
            GameObject rightWall = CreateBox("RightWall", new Vector3(wallThick, totalHeight, totalTunnelLength));
            rightWall.transform.parent = metroRoot;
            rightWall.transform.localPosition = new Vector3(
                entranceWidth / 2f + wallThick / 2f,
                ceilingAtTop / 2f - stairTotalDrop / 2f,
                totalTunnelLength / 2f);
            ApplyMaterial(rightWall, metroEntranceColor * 0.85f, 0.1f, 0.3f);

            // Inner wall surfaces (visible from inside)
            GameObject leftInner = CreateBox("LeftInnerWall", new Vector3(0.05f, totalHeight, totalTunnelLength));
            leftInner.transform.parent = metroRoot;
            leftInner.transform.localPosition = new Vector3(
                -entranceWidth / 2f + 0.025f,
                ceilingAtTop / 2f - stairTotalDrop / 2f,
                totalTunnelLength / 2f);
            ApplyMaterial(leftInner, new Color(0.82f, 0.8f, 0.76f), 0.05f, 0.2f);

            GameObject rightInner = CreateBox("RightInnerWall", new Vector3(0.05f, totalHeight, totalTunnelLength));
            rightInner.transform.parent = metroRoot;
            rightInner.transform.localPosition = new Vector3(
                entranceWidth / 2f - 0.025f,
                ceilingAtTop / 2f - stairTotalDrop / 2f,
                totalTunnelLength / 2f);
            ApplyMaterial(rightInner, new Color(0.82f, 0.8f, 0.76f), 0.05f, 0.2f);

            // Ceiling — covers entire tunnel
            GameObject stairCeiling = CreateBox("TunnelCeiling", new Vector3(entranceWidth + wallThick * 2, 0.25f, totalTunnelLength));
            stairCeiling.transform.parent = metroRoot;
            stairCeiling.transform.localPosition = new Vector3(0f, ceilingAtTop, totalTunnelLength / 2f);
            ApplyMaterial(stairCeiling, metroEntranceColor * 0.7f, 0.1f, 0.3f);

            // Floor at bottom — only from where stairs END to back wall (not under stairs)
            float stairEndZ = entranceDepth + stairDepth;
            float bottomFloorLength = walkingDistance + elevDepth;
            // Make floor wider (wall to wall) and thicker to prevent falling through
            GameObject bottomFloor = CreateBox("BottomFloor", new Vector3(entranceWidth + wallThick * 2, 0.5f, bottomFloorLength + 1f));
            bottomFloor.transform.parent = metroRoot;
            bottomFloor.transform.localPosition = new Vector3(0f, bottomY - 0.25f, stairEndZ + bottomFloorLength / 2f);
            ApplyMaterial(bottomFloor, metroEntranceColor * 0.75f, 0.1f, 0.3f);

            // Back wall — closes off the very end
            GameObject backWall = CreateBox("BackWall", new Vector3(entranceWidth + wallThick * 2, totalHeight, wallThick));
            backWall.transform.parent = metroRoot;
            backWall.transform.localPosition = new Vector3(0f, ceilingAtTop / 2f - stairTotalDrop / 2f, totalTunnelLength + wallThick / 2f);
            ApplyMaterial(backWall, metroEntranceColor * 0.8f, 0.1f, 0.3f);

            // === STAIRS ===
            Transform stairsRoot = new GameObject("Stairs").transform;
            stairsRoot.parent = metroRoot;
            stairsRoot.localPosition = new Vector3(0f, 0f, entranceDepth);

            float stepWidth = entranceWidth - 0.4f;
            float stepHeight = stairTotalDrop / stairCount;
            float stepDepth = stairDepth / stairCount;

            // Step_Before: flat landing before stairs begin
            GameObject stepBefore = CreateBox("Step_Before", new Vector3(stepWidth, 0.15f, entranceDepth));
            stepBefore.transform.parent = stairsRoot;
            stepBefore.transform.localPosition = new Vector3(0f, 0f, -entranceDepth / 2f);
            ApplyMaterial(stepBefore, Color.Lerp(pavementColor, metroEntranceColor, 0.5f));

            for (int i = 0; i < stairCount; i++)
            {
                GameObject step = CreateBox($"Step_{i}", new Vector3(stepWidth, stepHeight, stepDepth));
                step.transform.parent = stairsRoot;
                step.transform.localPosition = new Vector3(0f, -stepHeight * i - stepHeight / 2f, stepDepth * i + stepDepth / 2f);
                ApplyMaterial(step, Color.Lerp(pavementColor, metroEntranceColor, 0.5f));
            }

            // Invisible ramp collider over all stairs — prevents falling between steps
            float rampLength = stairDepth;
            float rampHeight = stairTotalDrop;
            GameObject rampCollider = CreateBox("StairRamp", new Vector3(stepWidth, 0.05f, Mathf.Sqrt(rampLength * rampLength + rampHeight * rampHeight)));
            rampCollider.transform.parent = stairsRoot;
            rampCollider.transform.localPosition = new Vector3(0f, -rampHeight / 2f, rampLength / 2f);
            float rampAngle = Mathf.Atan2(rampHeight, rampLength) * Mathf.Rad2Deg;
            rampCollider.transform.localRotation = Quaternion.Euler(rampAngle, 0f, 0f);
            // Make ramp invisible — only the collider matters
            Renderer rampRenderer = rampCollider.GetComponent<Renderer>();
            if (rampRenderer != null) rampRenderer.enabled = false;

            // Railings
            float railingHeight = 0.9f;
            float railingRadius = 0.03f;
            for (int side = -1; side <= 1; side += 2)
            {
                float railX = side * (stepWidth / 2f + 0.08f);
                Transform railRoot = new GameObject($"Railing_{(side < 0 ? "Left" : "Right")}").transform;
                railRoot.parent = metroRoot;
                railRoot.localPosition = Vector3.zero;
                for (int i = 0; i <= stairCount; i += 2)
                {
                    float pY = -stepHeight * i + railingHeight / 2f;
                    float pZ = entranceDepth + stepDepth * i;
                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.name = $"Post_{i}";
                    post.transform.parent = railRoot;
                    post.transform.localPosition = new Vector3(railX, pY, pZ);
                    post.transform.localScale = new Vector3(railingRadius * 2f, railingHeight / 2f, railingRadius * 2f);
                    ApplyMaterial(post, metroRailingColor, 0.6f, 0.5f);
                }
            }

            // Stair lighting
            for (int i = 0; i < 4; i++)
            {
                float lZ = entranceDepth + stairDepth * (i + 1) / 5f;
                float lY = -stairTotalDrop * (i + 1) / 5f + 2f;
                GameObject stairLight = new GameObject($"StairLight_{i}");
                stairLight.transform.parent = metroRoot;
                stairLight.transform.localPosition = new Vector3(0f, lY, lZ);
                Light pl = stairLight.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = new Color(1f, 0.95f, 0.85f);
                pl.intensity = 1.5f;
                pl.range = 6f;
            }

            // Walking area light
            GameObject walkLight = new GameObject("WalkAreaLight");
            walkLight.transform.parent = metroRoot;
            walkLight.transform.localPosition = new Vector3(0f, bottomY + 2.5f, stairEndZ + walkingDistance / 2f);
            Light wLight = walkLight.AddComponent<Light>();
            wLight.type = LightType.Point;
            wLight.color = new Color(1f, 0.95f, 0.85f);
            wLight.intensity = 1.5f;
            wLight.range = 6f;

            // === ELEVATOR — after walking distance from stairs ===
            float elevWidth = entranceWidth - 0.4f;
            float elevHeight = 3f;
            float doorZ = stairEndZ + walkingDistance;
            GenerateElevator(metroRoot, bottomY, doorZ, elevWidth, elevDepth, elevHeight);
        }

        private void GenerateElevator(Transform parent, float floorY, float doorZ, float elevWidth, float elevDepth, float elevHeight)
        {
            Transform elevRoot = new GameObject("Elevator").transform;
            elevRoot.parent = parent;
            elevRoot.localPosition = new Vector3(0f, floorY, doorZ);

            float wallThickness = 0.12f;
            Color elevWallColor = new Color(0.55f, 0.55f, 0.58f);
            Color elevDoorColor = new Color(0.6f, 0.6f, 0.63f);
            Color elevFloorColor = new Color(0.4f, 0.4f, 0.38f);
            Color buttonColor = new Color(0.8f, 0.7f, 0.1f);

            // Floor — wider and thicker to prevent falling through
            GameObject elevFloor = CreateBox("ElevatorFloor", new Vector3(elevWidth + 0.5f, 0.3f, elevDepth + 0.5f));
            elevFloor.transform.parent = elevRoot;
            elevFloor.transform.localPosition = new Vector3(0f, 0.15f, elevDepth / 2f);
            ApplyMaterial(elevFloor, elevFloorColor, 0.2f, 0.4f);

            // Back wall
            GameObject backWall = CreateBox("ElevBackWall", new Vector3(elevWidth, elevHeight, wallThickness));
            backWall.transform.parent = elevRoot;
            backWall.transform.localPosition = new Vector3(0f, elevHeight / 2f, elevDepth);
            ApplyMaterial(backWall, elevWallColor, 0.4f, 0.5f);

            // Left wall
            GameObject leftWall = CreateBox("ElevLeftWall", new Vector3(wallThickness, elevHeight, elevDepth));
            leftWall.transform.parent = elevRoot;
            leftWall.transform.localPosition = new Vector3(-elevWidth / 2f, elevHeight / 2f, elevDepth / 2f);
            ApplyMaterial(leftWall, elevWallColor, 0.4f, 0.5f);

            // Right wall
            GameObject rightWall = CreateBox("ElevRightWall", new Vector3(wallThickness, elevHeight, elevDepth));
            rightWall.transform.parent = elevRoot;
            rightWall.transform.localPosition = new Vector3(elevWidth / 2f, elevHeight / 2f, elevDepth / 2f);
            ApplyMaterial(rightWall, elevWallColor, 0.4f, 0.5f);

            // Ceiling
            GameObject ceiling = CreateBox("ElevCeiling", new Vector3(elevWidth, wallThickness, elevDepth));
            ceiling.transform.parent = elevRoot;
            ceiling.transform.localPosition = new Vector3(0f, elevHeight, elevDepth / 2f);
            ApplyMaterial(ceiling, elevWallColor * 0.9f, 0.3f, 0.4f);

            // === DOORS — start CLOSED (at center) ===
            float doorHeight = 2.5f;
            float doorHalfWidth = elevWidth / 2f;
            float doorThickness = 0.08f;

            GameObject leftDoor = CreateBox("LeftDoor", new Vector3(doorHalfWidth, doorHeight, doorThickness));
            leftDoor.transform.parent = elevRoot;
            leftDoor.transform.localPosition = new Vector3(-doorHalfWidth / 2f, doorHeight / 2f, 0f);
            ApplyMaterial(leftDoor, elevDoorColor, 0.5f, 0.6f);

            GameObject rightDoor = CreateBox("RightDoor", new Vector3(doorHalfWidth, doorHeight, doorThickness));
            rightDoor.transform.parent = elevRoot;
            rightDoor.transform.localPosition = new Vector3(doorHalfWidth / 2f, doorHeight / 2f, 0f);
            ApplyMaterial(rightDoor, elevDoorColor, 0.5f, 0.6f);

            // Door seam
            GameObject doorSeam = CreateBox("DoorSeam", new Vector3(0.02f, doorHeight, doorThickness + 0.01f));
            doorSeam.transform.parent = elevRoot;
            doorSeam.transform.localPosition = new Vector3(0f, doorHeight / 2f, 0f);
            ApplyMaterial(doorSeam, new Color(0.15f, 0.15f, 0.15f));

            // Door frame
            GameObject doorFrameTop = CreateBox("DoorFrameTop", new Vector3(elevWidth + 0.1f, 0.15f, 0.15f));
            doorFrameTop.transform.parent = elevRoot;
            doorFrameTop.transform.localPosition = new Vector3(0f, doorHeight + 0.075f, 0f);
            ApplyMaterial(doorFrameTop, metroEntranceColor, 0.3f, 0.4f);

            // Arrow indicator above door
            GameObject arrowDown = CreateBox("ArrowDown", new Vector3(0.3f, 0.3f, 0.05f));
            arrowDown.transform.parent = elevRoot;
            arrowDown.transform.localPosition = new Vector3(0f, doorHeight + 0.4f, -0.05f);
            arrowDown.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            ApplyMaterial(arrowDown, new Color(0f, 0.8f, 0f), 0f, 0.3f, new Color(0f, 0.8f, 0f), 2f);

            // Button panel (inside)
            GameObject buttonPanel = CreateBox("ButtonPanel", new Vector3(0.03f, 0.4f, 0.2f));
            buttonPanel.transform.parent = elevRoot;
            buttonPanel.transform.localPosition = new Vector3(elevWidth / 2f - 0.18f, 1.2f, 0.4f);
            ApplyMaterial(buttonPanel, new Color(0.3f, 0.3f, 0.32f), 0.5f, 0.6f);

            GameObject btnDown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btnDown.name = "Button_Down";
            btnDown.transform.parent = elevRoot;
            btnDown.transform.localPosition = new Vector3(elevWidth / 2f - 0.2f, 1.25f, 0.4f);
            btnDown.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            btnDown.transform.localScale = new Vector3(0.06f, 0.02f, 0.06f);
            ApplyMaterial(btnDown, buttonColor, 0f, 0.3f, buttonColor, 2f);

            // Ceiling light
            GameObject elevLight = new GameObject("ElevatorLight");
            elevLight.transform.parent = elevRoot;
            elevLight.transform.localPosition = new Vector3(0f, elevHeight - 0.2f, elevDepth / 2f);
            Light eLight = elevLight.AddComponent<Light>();
            eLight.type = LightType.Point;
            eLight.color = new Color(1f, 0.98f, 0.9f);
            eLight.intensity = 2f;
            eLight.range = 5f;

            GameObject lightFixture = CreateBox("LightFixture", new Vector3(0.6f, 0.04f, 0.6f));
            lightFixture.transform.parent = elevRoot;
            lightFixture.transform.localPosition = new Vector3(0f, elevHeight - 0.05f, elevDepth / 2f);
            ApplyMaterial(lightFixture, Color.white, 0f, 0.3f, Color.white, 3f);

            // Handrail
            GameObject handrail = CreateBox("Handrail", new Vector3(elevWidth - 0.4f, 0.05f, 0.05f));
            handrail.transform.parent = elevRoot;
            handrail.transform.localPosition = new Vector3(0f, 1f, elevDepth - 0.1f);
            ApplyMaterial(handrail, metroRailingColor, 0.7f, 0.6f);

            // === PROXIMITY TRIGGER — distance-based, Camera.main, no tag dependency ===
            GameObject proximityTrigger = new GameObject("ElevatorProximityTrigger");
            proximityTrigger.transform.parent = elevRoot;
            proximityTrigger.transform.localPosition = new Vector3(0f, 1.5f, -1.5f);
            proximityTrigger.AddComponent<ElevatorProximityTrigger>();

            // === INSIDE TRIGGER — detects player inside, closes doors + scene transition ===
            GameObject insideTrigger = new GameObject("ElevatorInsideTrigger");
            insideTrigger.transform.parent = elevRoot;
            insideTrigger.transform.localPosition = new Vector3(0f, elevHeight / 2f, elevDepth / 2f);
            BoxCollider insideCollider = insideTrigger.AddComponent<BoxCollider>();
            insideCollider.size = new Vector3(elevWidth - 0.3f, elevHeight, elevDepth - 0.3f);
            insideCollider.isTrigger = true;
            insideTrigger.AddComponent<ElevatorSceneTransition>();
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
