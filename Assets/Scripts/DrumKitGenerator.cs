using UnityEngine;

/// <summary>
/// Inspector'da "Generate Drum Kit" context menüsüyle sahneye gerçekçi
/// bir davul seti oluşturur. Tüm parçalar bu GameObject'in altında toplanır.
/// </summary>
public class DrumKitGenerator : MonoBehaviour
{
    [Header("Renk Paleti")]
    public Color shellColor      = new Color(0.20f, 0.45f, 0.55f); // mavi-gri kabuk
    public Color drumHeadColor   = new Color(0.85f, 0.82f, 0.75f); // krem deri
    public Color metalColor      = new Color(0.75f, 0.75f, 0.78f); // krom
    public Color cymbalColor     = new Color(0.80f, 0.65f, 0.10f); // pirinç
    public Color lugsColor       = new Color(0.70f, 0.70f, 0.72f); // vida/lug
    public Color carpetColor     = new Color(0.55f, 0.12f, 0.12f); // kırmızı halı

    // ── Context menu ──────────────────────────────────────────────────────────
    [ContextMenu("Generate Drum Kit")]
    public void Generate()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        BuildCarpet();
        BuildBassDrum();
        BuildSnareDrum();
        BuildHiHat();
        BuildRackTom(0);
        BuildRackTom(1);
        BuildFloorTom();
        BuildCrashCymbal();
        BuildRideCymbal();
    }

    // ── HALI ─────────────────────────────────────────────────────────────────
    void BuildCarpet()
    {
        Box("Carpet", transform, Vector3.zero, new Vector3(2.4f, 0.02f, 2.0f), carpetColor);
    }

    // ── BAS DAVUL ────────────────────────────────────────────────────────────
    void BuildBassDrum()
    {
        var root = Child("BassDrum", new Vector3(0, 0.34f, 0.1f));

        // Kabuk
        var shell = Cyl("Shell", root, Vector3.zero, new Vector3(0.66f, 0.58f, 0.66f), shellColor);
        shell.transform.localRotation = Quaternion.Euler(90, 0, 0);

        // Ön kafa
        FlatCyl("HeadFront", root, new Vector3(0, 0, -0.30f), 0.66f, drumHeadColor);
        // Arka kafa
        FlatCyl("HeadBack",  root, new Vector3(0, 0,  0.30f), 0.66f, drumHeadColor);

        // Hoop çemberleri
        Hoop("HoopFront", root, new Vector3(0, 0, -0.31f), 0.69f, metalColor);
        Hoop("HoopBack",  root, new Vector3(0, 0,  0.31f), 0.69f, metalColor);

        // Lug vidaları (8 adet)
        Lugs(root, 8, 0.36f, 0.24f, lugsColor);

        // Ayaklar (iki metal stand)
        Leg("LegL", root, new Vector3(-0.25f, -0.34f, 0f));
        Leg("LegR", root, new Vector3( 0.25f, -0.34f, 0f));

        // Bass pedal
        BassPedal(root);
    }

    void BassPedal(Transform parent)
    {
        var p = Child("Pedal", new Vector3(0, 0.01f, -0.38f), parent);

        // Pedal plakası
        Box("Plate", p, new Vector3(0, 0.02f, 0), new Vector3(0.12f, 0.02f, 0.28f), metalColor);
        // Çerçeve
        Box("Frame", p, new Vector3(0, 0.06f, 0.10f), new Vector3(0.02f, 0.10f, 0.02f), metalColor);
        // Tokmak kolu
        var arm = Box("BeaterArm", p, new Vector3(0, 0.14f, 0.08f), new Vector3(0.015f, 0.20f, 0.015f), metalColor);
        arm.transform.localRotation = Quaternion.Euler(20, 0, 0);
        // Tokmak başı
        Sph("BeaterHead", p, new Vector3(0, 0.27f, -0.01f), 0.04f, new Color(0.1f, 0.1f, 0.1f));
    }

    // ── TRAMPET (SNARE) ──────────────────────────────────────────────────────
    void BuildSnareDrum()
    {
        var root = Child("SnareDrum", new Vector3(-0.55f, 0.70f, -0.10f));

        var shell = Cyl("Shell", root, Vector3.zero, new Vector3(0.36f, 0.155f, 0.36f), shellColor);
        FlatCyl("HeadTop",    root, new Vector3(0,  0.080f, 0), 0.37f, drumHeadColor);
        FlatCyl("HeadBottom", root, new Vector3(0, -0.080f, 0), 0.37f, new Color(0.9f, 0.88f, 0.78f));
        Hoop("HoopTop",    root, new Vector3(0,  0.085f, 0), 0.385f, metalColor);
        Hoop("HoopBottom", root, new Vector3(0, -0.085f, 0), 0.385f, metalColor);
        Lugs(root, 8, 0.195f, 0.065f, lugsColor);

        // Snare wire (spiral tel)
        SnareWire(root);

        // Stand
        DrumStand("Stand", root, new Vector3(0, -0.55f, 0), 0.55f);
    }

    void SnareWire(Transform parent)
    {
        for (int i = 0; i < 8; i++)
        {
            float x = Mathf.Lerp(-0.14f, 0.14f, i / 7f);
            Box("Wire" + i, parent,
                new Vector3(x, -0.082f, 0),
                new Vector3(0.004f, 0.002f, 0.34f),
                metalColor);
        }
    }

    // ── HI-HAT ───────────────────────────────────────────────────────────────
    void BuildHiHat()
    {
        var root = Child("HiHat", new Vector3(-0.80f, 0, -0.40f));

        // Stand
        HiHatStand(root);

        // Alt zil (açık)
        var bot = Cyl("CymbalBottom", root, new Vector3(0, 0.82f, 0),
            new Vector3(0.38f, 0.012f, 0.38f), cymbalColor);
        bot.transform.localRotation = Quaternion.Euler(2, 0, 0);

        // Üst zil (hafif kapalı)
        var top = Cyl("CymbalTop", root, new Vector3(0, 0.855f, 0),
            new Vector3(0.36f, 0.016f, 0.36f), cymbalColor);
        top.transform.localRotation = Quaternion.Euler(-3, 0, 0);

        // Bell (zil kupası)
        Sph("Bell", root, new Vector3(0, 0.875f, 0), 0.06f, cymbalColor);

        // Merkez vida
        Cyl("Rod", root, new Vector3(0, 0.45f, 0),
            new Vector3(0.018f, 0.90f, 0.018f), metalColor);
    }

    void HiHatStand(Transform parent)
    {
        // Ana direk
        Cyl("Post", parent, new Vector3(0, 0.40f, 0),
            new Vector3(0.022f, 0.80f, 0.022f), metalColor);

        // 3 ayak
        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f * Mathf.Deg2Rad;
            float ex = Mathf.Sin(angle) * 0.32f;
            float ez = Mathf.Cos(angle) * 0.32f;

            var leg = Box("Leg" + i, parent,
                new Vector3(ex * 0.5f, 0.015f, ez * 0.5f),
                new Vector3(0.02f, 0.02f, 0.36f), metalColor);
            leg.transform.localRotation =
                Quaternion.Euler(0, -i * 120f, 0);
        }

        // Pedal
        Box("HHPedal", parent, new Vector3(0, 0.012f, 0.18f),
            new Vector3(0.10f, 0.018f, 0.26f), metalColor);
    }

    // ── RACK TOM ─────────────────────────────────────────────────────────────
    void BuildRackTom(int idx)
    {
        // 0 = sol, 1 = sağ; biraz eğimli
        float xPos   = (idx == 0) ? -0.28f : 0.28f;
        float tilt   = (idx == 0) ? -18f   : 18f;
        float dia    = (idx == 0) ? 0.30f  : 0.26f;
        float height = (idx == 0) ? 0.22f  : 0.20f;

        var root = Child("RackTom" + idx, new Vector3(xPos, 0.95f, -0.05f));
        root.transform.localRotation = Quaternion.Euler(tilt, 0, 0);

        Cyl("Shell", root, Vector3.zero, new Vector3(dia, height, dia), shellColor);
        FlatCyl("HeadTop",    root, new Vector3(0,  height * 0.5f, 0), dia + 0.01f, drumHeadColor);
        FlatCyl("HeadBottom", root, new Vector3(0, -height * 0.5f, 0), dia + 0.01f, drumHeadColor);
        Hoop("HoopTop",    root, new Vector3(0,  height * 0.5f + 0.005f, 0), dia + 0.02f, metalColor);
        Hoop("HoopBottom", root, new Vector3(0, -height * 0.5f - 0.005f, 0), dia + 0.02f, metalColor);
        Lugs(root, 6, dia * 0.5f + 0.01f, height * 0.3f, lugsColor);

        // Bağlantı kolu (bass drum'a)
        var arm = Box("MountArm", root, new Vector3(0, height * 0.5f + 0.06f, 0),
            new Vector3(0.018f, 0.12f, 0.018f), metalColor);
    }

    // ── FLOOR TOM ────────────────────────────────────────────────────────────
    void BuildFloorTom()
    {
        var root = Child("FloorTom", new Vector3(0.72f, 0.44f, 0f));

        float dia = 0.42f, h = 0.38f;
        Cyl("Shell", root, Vector3.zero, new Vector3(dia, h, dia), shellColor);
        FlatCyl("HeadTop",    root, new Vector3(0,  h * 0.5f, 0), dia + 0.01f, drumHeadColor);
        FlatCyl("HeadBottom", root, new Vector3(0, -h * 0.5f, 0), dia + 0.01f, drumHeadColor);
        Hoop("HoopTop",    root, new Vector3(0,  h * 0.5f + 0.005f, 0), dia + 0.022f, metalColor);
        Hoop("HoopBottom", root, new Vector3(0, -h * 0.5f - 0.005f, 0), dia + 0.022f, metalColor);
        Lugs(root, 8, dia * 0.5f + 0.01f, h * 0.28f, lugsColor);

        // 3 bacak
        for (int i = 0; i < 3; i++)
        {
            float angle = (i * 120f + 30f) * Mathf.Deg2Rad;
            float lx = Mathf.Sin(angle) * 0.22f;
            float lz = Mathf.Cos(angle) * 0.22f;
            Cyl("Leg" + i, root,
                new Vector3(lx, -h * 0.5f - 0.22f, lz),
                new Vector3(0.018f, 0.44f, 0.018f), metalColor);
        }
    }

    // ── CRASH ZİLİ ───────────────────────────────────────────────────────────
    void BuildCrashCymbal()
    {
        var root = Child("CrashCymbal", new Vector3(-1.05f, 0, 0.15f));

        CymbalStand("Stand", root, 1.15f);

        var cym = Cyl("Cymbal", root, new Vector3(0, 1.15f, 0),
            new Vector3(0.44f, 0.012f, 0.44f), cymbalColor);
        cym.transform.localRotation = Quaternion.Euler(-10, 15, 0);

        Sph("Bell", root, new Vector3(0, 1.16f, 0), 0.055f, cymbalColor);
    }

    // ── RIDE ZİLİ ────────────────────────────────────────────────────────────
    void BuildRideCymbal()
    {
        var root = Child("RideCymbal", new Vector3(1.05f, 0, 0.0f));

        CymbalStand("Stand", root, 1.05f);

        var cym = Cyl("Cymbal", root, new Vector3(0, 1.05f, 0),
            new Vector3(0.54f, 0.014f, 0.54f), cymbalColor);
        cym.transform.localRotation = Quaternion.Euler(-8, -10, 0);

        Sph("Bell", root, new Vector3(0, 1.065f, 0), 0.065f, cymbalColor);
    }

    // ── STAND YARDIMCILARI ────────────────────────────────────────────────────
    void CymbalStand(string name, Transform parent, float postHeight)
    {
        Cyl(name + "Post", parent, new Vector3(0, postHeight * 0.5f, 0),
            new Vector3(0.020f, postHeight, 0.020f), metalColor);

        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f * Mathf.Deg2Rad;
            var leg = Box(name + "Leg" + i, parent,
                new Vector3(Mathf.Sin(angle) * 0.20f, 0.015f, Mathf.Cos(angle) * 0.20f),
                new Vector3(0.018f, 0.018f, 0.40f), metalColor);
            leg.transform.localRotation = Quaternion.Euler(0, -i * 120f, 0);
        }
    }

    void DrumStand(string name, Transform parent, Vector3 localPos, float postHeight)
    {
        Cyl(name + "Post", parent,
            localPos + Vector3.up * postHeight * 0.5f,
            new Vector3(0.022f, postHeight, 0.022f), metalColor);

        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f * Mathf.Deg2Rad;
            var leg = Box(name + "Leg" + i, parent,
                localPos + new Vector3(Mathf.Sin(angle) * 0.18f, 0.012f, Mathf.Cos(angle) * 0.18f),
                new Vector3(0.018f, 0.018f, 0.36f), metalColor);
            leg.transform.localRotation = Quaternion.Euler(0, -i * 120f, 0);
        }
    }

    void Leg(string name, Transform parent, Vector3 localPos)
    {
        Cyl(name, parent, localPos, new Vector3(0.022f, 0.18f, 0.022f), metalColor)
            .transform.localRotation = Quaternion.Euler(0, 0, 25 * (localPos.x < 0 ? -1 : 1));
    }

    // ── LUG VİDALARI ─────────────────────────────────────────────────────────
    void Lugs(Transform parent, int count, float radius, float yOffset, Color col)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count) * Mathf.Deg2Rad;
            float lx    = Mathf.Sin(angle) * radius;
            float lz    = Mathf.Cos(angle) * radius;

            var lug = Box("Lug" + i, parent,
                new Vector3(lx,  yOffset, lz),
                new Vector3(0.022f, 0.038f, 0.018f), col);
            lug.transform.localRotation =
                Quaternion.Euler(0, -i * (360f / count), 0);

            var lug2 = Box("Lug" + i + "b", parent,
                new Vector3(lx, -yOffset, lz),
                new Vector3(0.022f, 0.038f, 0.018f), col);
            lug2.transform.localRotation = lug.transform.localRotation;
        }
    }

    // ── PRİMİTİF YARDIMCILARI ────────────────────────────────────────────────
    Transform Child(string n, Vector3 pos, Transform parent = null)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent ?? transform, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    GameObject Cyl(string n, Transform parent, Vector3 lpos, Vector3 lscale, Color col)
    {
        var go = Prim(PrimitiveType.Cylinder, n, parent, lpos, lscale, col);
        return go;
    }

    void FlatCyl(string n, Transform parent, Vector3 lpos, float diameter, Color col)
    {
        Prim(PrimitiveType.Cylinder, n, parent, lpos,
            new Vector3(diameter, 0.008f, diameter), col);
    }

    void Hoop(string n, Transform parent, Vector3 lpos, float diameter, Color col)
    {
        Prim(PrimitiveType.Cylinder, n, parent, lpos,
            new Vector3(diameter, 0.014f, diameter), col);
    }

    GameObject Box(string n, Transform parent, Vector3 lpos, Vector3 lscale, Color col)
        => Prim(PrimitiveType.Cube, n, parent, lpos, lscale, col);

    void Sph(string n, Transform parent, Vector3 lpos, float diameter, Color col)
        => Prim(PrimitiveType.Sphere, n, parent, lpos, Vector3.one * diameter, col);

    GameObject Prim(PrimitiveType type, string n, Transform parent,
        Vector3 lpos, Vector3 lscale, Color col)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = n;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lpos;
        go.transform.localScale    = lscale;

        // Collider sadece üst seviye parçalarda
        var col2 = go.GetComponent<Collider>();
        if (col2) DestroyImmediate(col2);

        var rend = go.GetComponent<Renderer>();
        if (rend)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", col);

            // Metaller için metallic/smoothness
            if (col == metalColor || col == cymbalColor || col == lugsColor)
            {
                mat.SetFloat("_Metallic",    0.9f);
                mat.SetFloat("_Smoothness",  0.8f);
            }
            else if (col == shellColor)
            {
                mat.SetFloat("_Metallic",   0.0f);
                mat.SetFloat("_Smoothness", 0.6f);
            }
            else
            {
                mat.SetFloat("_Metallic",   0.0f);
                mat.SetFloat("_Smoothness", 0.2f);
            }

            rend.material = mat;
        }

        return go;
    }
}
