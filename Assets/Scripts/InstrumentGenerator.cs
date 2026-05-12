using UnityEngine;

/// <summary>
/// Sahneye bas gitar, elektro gitar ve synth klavye generate eder.
/// Her biri ayrı ContextMenu komutuyla oluşturulur.
/// </summary>
public class InstrumentGenerator : MonoBehaviour
{
    [Header("Renkler")]
    public Color bodyColor    = new Color(0.08f, 0.08f, 0.08f); // siyah gövde
    public Color neckColor    = new Color(0.55f, 0.38f, 0.18f); // ahşap sap
    public Color fretColor    = new Color(0.75f, 0.75f, 0.78f); // krom perde
    public Color stringColor  = new Color(0.80f, 0.78f, 0.60f); // tel
    public Color pickupColor  = new Color(0.12f, 0.12f, 0.12f); // manyetik
    public Color knobColor    = new Color(0.20f, 0.20f, 0.20f); // düğme
    public Color pickguardColor = new Color(0.92f, 0.92f, 0.92f); // beyaz pickguard
    public Color synthBodyColor = new Color(0.85f, 0.08f, 0.08f); // kırmızı synth

    // ─────────────────────────────────────────────────────────────────────────
    [ContextMenu("Generate Bass Guitar")]
    public void GenerateBass()
    {
        ClearChildren();
        BuildBassGuitar();
    }

    [ContextMenu("Generate Electric Guitar")]
    public void GenerateElectricGuitar()
    {
        ClearChildren();
        BuildElectricGuitar();
    }

    [ContextMenu("Generate Synth Keyboard")]
    public void GenerateSynth()
    {
        ClearChildren();
        BuildSynth();
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // BAS GİTAR
    // ═════════════════════════════════════════════════════════════════════════
    void BuildBassGuitar()
    {
        // Gövde — iki üst boynuz, alt dolgun kütle
        var body = Sub("Body", Vector3.zero);
        Box("BodyMain",  body, Vector3.zero,                  new Vector3(0.38f, 0.06f, 0.30f), bodyColor);
        Box("HornUpper", body, new Vector3(-0.08f, 0, 0.17f), new Vector3(0.14f, 0.06f, 0.12f), bodyColor);
        Box("HornLower", body, new Vector3( 0.04f, 0, 0.14f), new Vector3(0.10f, 0.06f, 0.10f), bodyColor);

        // Sap (neck) — gövdeden sola uzanır
        var neck = Sub("Neck", new Vector3(-0.62f, 0, 0.04f));
        Box("NeckMain",  neck, Vector3.zero,            new Vector3(0.90f, 0.04f, 0.07f), neckColor);
        Box("Headstock", neck, new Vector3(-0.50f, 0, 0f), new Vector3(0.12f, 0.04f, 0.09f), neckColor);

        // Perdeler (6 adet)
        for (int i = 0; i < 6; i++)
            Box("Fret" + i, neck, new Vector3(-0.38f + i * 0.13f, 0.025f, 0), new Vector3(0.008f, 0.01f, 0.075f), fretColor);

        // Teller (4 adet)
        for (int i = 0; i < 4; i++)
        {
            float z = -0.025f + i * 0.018f;
            Box("String" + i, neck, new Vector3(0, 0.032f, z), new Vector3(0.90f, 0.004f, 0.003f), stringColor);
        }

        // Tuner vidaları
        for (int i = 0; i < 4; i++)
            Sph("Tuner" + i, neck, new Vector3(-0.54f, 0.03f, -0.03f + i * 0.02f), 0.012f, fretColor);

        // Manyetikler (2 pickup)
        Box("PickupNeck",   body, new Vector3(-0.06f, 0.038f,  0.01f), new Vector3(0.08f, 0.015f, 0.07f), pickupColor);
        Box("PickupBridge", body, new Vector3( 0.07f, 0.038f,  0.01f), new Vector3(0.08f, 0.015f, 0.07f), pickupColor);

        // Bridge
        Box("Bridge", body, new Vector3(0.14f, 0.038f, 0.01f), new Vector3(0.06f, 0.012f, 0.075f), fretColor);

        // Knob'lar
        Sph("Knob1", body, new Vector3(0.10f, 0.045f,  -0.06f), 0.018f, knobColor);
        Sph("Knob2", body, new Vector3(0.14f, 0.045f,  -0.06f), 0.018f, knobColor);

        // Jack
        Cyl("Jack", body, new Vector3(0.18f, 0, -0.06f), new Vector3(0.018f, 0.06f, 0.018f), fretColor)
            .localRotation = Quaternion.Euler(0, 0, 90);

        // Gitar yatay duracak şekilde döndür
        transform.localRotation = Quaternion.Euler(0, 90, 15);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ELEKTRO GİTAR (Stratocaster stili)
    // ═════════════════════════════════════════════════════════════════════════
    void BuildElectricGuitar()
    {
        // Gövde
        var body = Sub("Body", Vector3.zero);
        Box("BodyMain",     body, Vector3.zero,                   new Vector3(0.36f, 0.055f, 0.32f), bodyColor);
        Box("WaistCutaway", body, new Vector3(-0.10f, 0, 0.16f),  new Vector3(0.12f, 0.055f, 0.10f), bodyColor);
        Box("LowerBout",    body, new Vector3( 0.02f, 0, -0.05f), new Vector3(0.10f, 0.055f, 0.08f), bodyColor);

        // Pickguard (beyaz)
        Box("Pickguard", body, new Vector3(-0.02f, 0.032f, 0.03f), new Vector3(0.22f, 0.008f, 0.22f), pickguardColor);

        // Sap
        var neck = Sub("Neck", new Vector3(-0.65f, 0, 0.05f));
        Box("NeckMain",  neck, Vector3.zero,               new Vector3(1.0f, 0.038f, 0.065f), neckColor);
        Box("Headstock", neck, new Vector3(-0.54f, 0, 0f), new Vector3(0.14f, 0.038f, 0.085f), neckColor);

        // Perdeler (8 adet)
        for (int i = 0; i < 8; i++)
            Box("Fret" + i, neck, new Vector3(-0.43f + i * 0.11f, 0.022f, 0), new Vector3(0.006f, 0.008f, 0.065f), fretColor);

        // Teller (6 adet)
        for (int i = 0; i < 6; i++)
        {
            float z = -0.025f + i * 0.010f;
            Box("String" + i, neck, new Vector3(0, 0.026f, z), new Vector3(1.0f, 0.003f, 0.003f), stringColor);
        }

        // Tuner'lar (3 sol 3 sağ)
        for (int i = 0; i < 3; i++)
        {
            Sph("TunerL" + i, neck, new Vector3(-0.54f, 0.025f, 0.02f + i * 0.018f), 0.012f, fretColor);
            Sph("TunerR" + i, neck, new Vector3(-0.54f, 0.025f, -0.02f - i * 0.018f), 0.012f, fretColor);
        }

        // 3 Single-coil pickup
        for (int i = 0; i < 3; i++)
            Box("Pickup" + i, body, new Vector3(-0.10f + i * 0.085f, 0.036f, 0.04f), new Vector3(0.06f, 0.012f, 0.055f), pickupColor);

        // Bridge + tremolo
        Box("Bridge",   body, new Vector3(0.14f, 0.036f, 0.04f),  new Vector3(0.07f, 0.010f, 0.065f), fretColor);
        Box("Tremolo",  body, new Vector3(0.16f, 0.036f, -0.02f), new Vector3(0.025f, 0.010f, 0.015f), fretColor);

        // Knob'lar (1 vol, 2 tone)
        Sph("VolKnob",   body, new Vector3( 0.10f, 0.042f, -0.08f), 0.018f, knobColor);
        Sph("ToneKnob1", body, new Vector3( 0.13f, 0.042f, -0.08f), 0.015f, knobColor);
        Sph("ToneKnob2", body, new Vector3( 0.16f, 0.042f, -0.08f), 0.015f, knobColor);

        // Pick-up selector switch
        Box("Switch", body, new Vector3(-0.08f, 0.042f, -0.07f), new Vector3(0.008f, 0.02f, 0.035f), fretColor);

        transform.localRotation = Quaternion.Euler(0, 90, 15);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SYNTH KLAVYE (MS-1 stili)
    // ═════════════════════════════════════════════════════════════════════════
    void BuildSynth()
    {
        var body = Sub("SynthBody", Vector3.zero);

        // Ana gövde kasası
        Box("Case",     body, new Vector3(0, 0.06f, 0),  new Vector3(0.90f, 0.10f, 0.38f), synthBodyColor);
        Box("CaseBase", body, new Vector3(0, 0.01f, 0),  new Vector3(0.90f, 0.02f, 0.38f), new Color(0.06f, 0.06f, 0.06f));

        // Tuş takımı bölgesi (alt yarı)
        BuildKeys(body, new Vector3(0, 0.12f, 0.06f));

        // Panel bölgesi (üst yarı — knob'lar, slider'lar)
        BuildPanel(body, new Vector3(0, 0.125f, -0.10f));

        // Pitch / Mod wheel
        Cyl("PitchWheel", body, new Vector3(-0.40f, 0.14f, 0.0f), new Vector3(0.025f, 0.06f, 0.06f), knobColor)
            .localRotation = Quaternion.Euler(90, 0, 0);
        Cyl("ModWheel",   body, new Vector3(-0.40f, 0.14f, -0.07f), new Vector3(0.025f, 0.06f, 0.06f), new Color(0.3f, 0.3f, 0.3f))
            .localRotation = Quaternion.Euler(90, 0, 0);

        // Ayaklar
        Box("FootL", body, new Vector3(-0.40f, -0.01f, 0), new Vector3(0.04f, 0.02f, 0.36f), new Color(0.05f, 0.05f, 0.05f));
        Box("FootR", body, new Vector3( 0.40f, -0.01f, 0), new Vector3(0.04f, 0.02f, 0.36f), new Color(0.05f, 0.05f, 0.05f));
    }

    void BuildKeys(Transform parent, Vector3 center)
    {
        int whiteCount = 25; // 2 oktav + 1
        float keyW  = 0.026f, keyH = 0.018f, keyD = 0.12f;
        float totalW = whiteCount * (keyW + 0.002f);
        float startX = -totalW * 0.5f;

        // Beyaz tuşlar
        for (int i = 0; i < whiteCount; i++)
        {
            float x = startX + i * (keyW + 0.002f);
            Box("WhiteKey" + i, parent,
                center + new Vector3(x, 0, 0),
                new Vector3(keyW, keyH, keyD),
                Color.white);
        }

        // Siyah tuşlar (diyez/bemol) — standard piano pattern
        int[] blackPattern = { 0, 1, 3, 4, 5 }; // 7'li grupta siyah tuş ofsetleri
        for (int oct = 0; oct < 3; oct++)
        {
            foreach (int b in blackPattern)
            {
                int idx = oct * 7 + b;
                if (idx >= whiteCount - 1) break;
                float x = startX + (idx + 0.5f) * (keyW + 0.002f) + keyW * 0.1f;
                Box("BlackKey" + oct + "_" + b, parent,
                    center + new Vector3(x, keyH * 0.6f, -keyD * 0.2f),
                    new Vector3(keyW * 0.6f, keyH * 0.8f, keyD * 0.6f),
                    new Color(0.05f, 0.05f, 0.05f));
            }
        }
    }

    void BuildPanel(Transform parent, Vector3 center)
    {
        // Knob'lar (12 adet — 3 satır 4 sütun)
        for (int row = 0; row < 2; row++)
        for (int col = 0; col < 6; col++)
        {
            float x = -0.30f + col * 0.10f;
            float z = center.z + row * -0.055f;
            Sph("Knob_" + row + "_" + col, parent,
                new Vector3(x, center.y + 0.018f, z), 0.016f,
                new Color(0.15f, 0.15f, 0.15f));
        }

        // Slider'lar (4 adet)
        for (int s = 0; s < 4; s++)
        {
            float x = -0.15f + s * 0.10f;
            // Kanal
            Box("SliderTrack" + s, parent,
                new Vector3(x, center.y + 0.005f, center.z - 0.12f),
                new Vector3(0.008f, 0.005f, 0.07f),
                new Color(0.05f, 0.05f, 0.05f));
            // Başlık
            Box("SliderCap" + s, parent,
                new Vector3(x, center.y + 0.015f, center.z - 0.10f),
                new Vector3(0.016f, 0.012f, 0.018f),
                new Color(0.9f, 0.9f, 0.9f));
        }

        // Display ekran
        Box("Display", parent,
            new Vector3(0.32f, center.y + 0.012f, center.z - 0.06f),
            new Vector3(0.10f, 0.008f, 0.06f),
            new Color(0.05f, 0.20f, 0.05f));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // YARDIMCILAR
    // ─────────────────────────────────────────────────────────────────────────
    Transform Sub(string n, Vector3 pos)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    GameObject Box(string n, Transform parent, Vector3 lpos, Vector3 lscale, Color col)
        => Prim(PrimitiveType.Cube, n, parent, lpos, lscale, col);

    Transform Cyl(string n, Transform parent, Vector3 lpos, Vector3 lscale, Color col)
    {
        Prim(PrimitiveType.Cylinder, n, parent, lpos, lscale, col);
        return parent.Find(n);
    }

    void Sph(string n, Transform parent, Vector3 lpos, float d, Color col)
        => Prim(PrimitiveType.Sphere, n, parent, lpos, Vector3.one * d, col);

    GameObject Prim(PrimitiveType type, string n, Transform parent,
        Vector3 lpos, Vector3 lscale, Color col)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = n;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lpos;
        go.transform.localScale    = lscale;

        var c = go.GetComponent<Collider>();
        if (c) DestroyImmediate(c);

        var rend = go.GetComponent<Renderer>();
        if (rend)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", col);

            bool isMetal = (col == fretColor || col == stringColor);
            mat.SetFloat("_Metallic",   isMetal ? 0.85f : 0.0f);
            mat.SetFloat("_Smoothness", isMetal ? 0.80f : 0.3f);
            rend.material = mat;
        }

        return go;
    }
}
