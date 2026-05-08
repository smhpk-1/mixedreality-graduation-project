using UnityEngine;

/// <summary>
/// Sahneye basit bir stage speaker kabini oluşturur.
/// Hierarchy'de boş bir GameObject'e ekle, Play modunda veya Editor'da çalışır.
/// </summary>
public class StageSpeakerGenerator : MonoBehaviour
{
    [Header("Kabin Boyutları")]
    public float cabinetWidth  = 0.6f;
    public float cabinetHeight = 1.2f;
    public float cabinetDepth  = 0.5f;

    [Header("Speaker Konileri")]
    public int   wooferCount   = 2;   // büyük bas hoparlör
    public bool  addTweeter    = true; // küçük tiz hoparlör
    public float wooferRadius  = 0.18f;
    public float tweeterRadius = 0.07f;

    [Header("Materyal Renkleri")]
    public Color cabinetColor  = new Color(0.08f, 0.08f, 0.08f);
    public Color grillColor    = new Color(0.15f, 0.15f, 0.15f);
    public Color coneColor     = new Color(0.05f, 0.05f, 0.05f);
    public Color metalColor    = new Color(0.3f, 0.3f, 0.3f);

    [Header("Işık")]
    public bool addStatusLight = true;
    public Color statusLightColor = Color.red;

    [ContextMenu("Generate Speaker")]
    public void Generate()
    {
        // Eski çocukları temizle (tekrar generate için)
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        BuildCabinet();
    }

    private void BuildCabinet()
    {
        // ── Ana kabin ───────────────────────────────────────────
        GameObject cabinet = CreatePrimitive(PrimitiveType.Cube, "Cabinet",
            Vector3.zero,
            new Vector3(cabinetWidth, cabinetHeight, cabinetDepth),
            cabinetColor);

        // ── Ön ızgara (grill) — kabin önüne ince plaka ──────────
        float grillZ = cabinetDepth * 0.5f + 0.005f;
        CreatePrimitive(PrimitiveType.Cube, "Grill",
            new Vector3(0, 0, grillZ),
            new Vector3(cabinetWidth - 0.02f, cabinetHeight - 0.02f, 0.01f),
            grillColor);

        // ── Woofer konileri ──────────────────────────────────────
        float spacing = cabinetHeight / (wooferCount + 1);
        float startY  = -cabinetHeight * 0.5f + spacing;
        float topUsed = startY + spacing * (wooferCount - 1); // son woofer'ın Y'si

        for (int i = 0; i < wooferCount; i++)
        {
            float y = startY + spacing * i;
            AddSpeakerCone("Woofer_" + i, new Vector3(0, y, grillZ + 0.01f),
                wooferRadius, 0.04f, coneColor, metalColor);
        }

        // ── Tweeter ──────────────────────────────────────────────
        if (addTweeter)
        {
            float tweeterY = topUsed + spacing * 0.5f;
            tweeterY = Mathf.Clamp(tweeterY,
                -cabinetHeight * 0.5f + tweeterRadius + 0.05f,
                 cabinetHeight * 0.5f - tweeterRadius - 0.05f);

            AddSpeakerCone("Tweeter", new Vector3(0, tweeterY, grillZ + 0.01f),
                tweeterRadius, 0.02f, coneColor, metalColor);
        }

        // ── Köşe metal bantları ──────────────────────────────────
        AddCornerStrips();

        // ── Ayaklar ─────────────────────────────────────────────
        AddFeet();

        // ── Durum ışığı ──────────────────────────────────────────
        if (addStatusLight)
        {
            float ledY = cabinetHeight * 0.5f - 0.05f;
            float ledX = cabinetWidth * 0.5f - 0.05f;
            GameObject led = CreatePrimitive(PrimitiveType.Sphere, "StatusLED",
                new Vector3(ledX, ledY, grillZ + 0.015f),
                Vector3.one * 0.025f,
                statusLightColor, emissive: true);

            // Küçük nokta ışığı
            var light = led.AddComponent<UnityEngine.Light>();
            light.color     = statusLightColor;
            light.intensity = 0.5f;
            light.range     = 0.3f;
            light.type      = LightType.Point;
        }
    }

    // ── Yardımcı: speaker konisi (dış halka + iç koni) ──────────
    private void AddSpeakerCone(string name, Vector3 localPos,
        float outerR, float depth, Color coneCol, Color ringCol)
    {
        // Dış halka (ince disk)
        GameObject ring = CreatePrimitive(PrimitiveType.Cylinder, name + "_Ring",
            localPos,
            new Vector3(outerR * 2f, 0.008f, outerR * 2f),
            ringCol);
        // Cylinder varsayılan Y-eksen — ön yüzüne bakmak için döndür
        ring.transform.localRotation = Quaternion.Euler(90, 0, 0);

        // İç koni (içe basan disk)
        GameObject cone = CreatePrimitive(PrimitiveType.Cylinder, name + "_Cone",
            localPos - new Vector3(0, 0, depth * 0.5f),
            new Vector3(outerR * 1.4f, depth * 0.5f, outerR * 1.4f),
            coneCol);
        cone.transform.localRotation = Quaternion.Euler(90, 0, 0);

        // Merkez düğme
        CreatePrimitive(PrimitiveType.Sphere, name + "_Dust",
            localPos - new Vector3(0, 0, depth),
            Vector3.one * outerR * 0.35f,
            metalColor);
    }

    // ── Köşe metal bantları ──────────────────────────────────────
    private void AddCornerStrips()
    {
        float w = cabinetWidth  * 0.5f;
        float h = cabinetHeight * 0.5f;
        float d = cabinetDepth  * 0.5f;
        float t = 0.012f; // kalınlık
        float s = 0.025f; // şerit genişliği

        // 4 dikey köşe şeridi
        Vector3[] corners = {
            new Vector3( w, 0, d), new Vector3(-w, 0, d),
            new Vector3( w, 0,-d), new Vector3(-w, 0,-d)
        };
        foreach (var c in corners)
        {
            CreatePrimitive(PrimitiveType.Cube, "CornerStrip",
                c, new Vector3(t, cabinetHeight, t), metalColor);
        }

        // Üst ve alt yatay şeritler (ön)
        CreatePrimitive(PrimitiveType.Cube, "TopStrip",
            new Vector3(0, h, d),
            new Vector3(cabinetWidth, t, t), metalColor);
        CreatePrimitive(PrimitiveType.Cube, "BottomStrip",
            new Vector3(0, -h, d),
            new Vector3(cabinetWidth, t, t), metalColor);
    }

    // ── Kauçuk ayaklar ───────────────────────────────────────────
    private void AddFeet()
    {
        float w   = cabinetWidth  * 0.5f - 0.06f;
        float d   = cabinetDepth  * 0.5f - 0.06f;
        float botY = -cabinetHeight * 0.5f - 0.025f;

        Vector3[] positions = {
            new Vector3( w, botY,  d), new Vector3(-w, botY,  d),
            new Vector3( w, botY, -d), new Vector3(-w, botY, -d)
        };
        foreach (var p in positions)
        {
            CreatePrimitive(PrimitiveType.Cylinder, "Foot", p,
                new Vector3(0.04f, 0.025f, 0.04f),
                new Color(0.1f, 0.1f, 0.1f));
        }
    }

    // ── Primitive oluşturucu ─────────────────────────────────────
    private GameObject CreatePrimitive(PrimitiveType type, string objName,
        Vector3 localPos, Vector3 localScale, Color color, bool emissive = false)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = objName;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;

        // Collider kaldır (kabine zaten var)
        if (objName != "Cabinet")
        {
            var col = go.GetComponent<Collider>();
            if (col) DestroyImmediate(col);
        }

        // Materyal
        var rend = go.GetComponent<Renderer>();
        if (rend)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2f);
            }
            rend.material = mat;
        }

        return go;
    }
}
