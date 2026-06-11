using UnityEngine;

/// <summary>
/// Basit ama tanınabilir bir (alto) saksofon modeli generate eder.
/// Prosedürel primitive + custom cone mesh ile inşa edilir, altın materyal kullanır.
///
/// Kullanım:
///   1) Sahnede boş bir GameObject oluştur, adını "Saxophone" yap
///   2) Bu component'i ekle
///   3) Inspector'da bileşenin sağ üst ⋮ menüsünden "Generate Saxophone"
///   (veya Tools → Create → Saxophone menüsünü kullan — sahneye direkt ekler)
///
/// Sonra NPC'nin eline yerleştirmek için Saxophone'u NPC'nin el bone'una
/// child yapıp pozisyon/rotasyon ayarla.
/// </summary>
[ExecuteAlways]
public class SaxophoneGenerator : MonoBehaviour
{
    [Header("Renkler")]
    public Color brassColor      = new Color(0.83f, 0.69f, 0.22f); // pirinç/altın gövde
    public Color keyColor        = new Color(0.85f, 0.85f, 0.88f); // gümüş tuşlar
    public Color mouthpieceColor = new Color(0.06f, 0.06f, 0.06f); // siyah ağızlık

    [Header("Boyut")]
    [Tooltip("Toplam ölçek çarpanı (1 = ~0.65m alto sax boyu)")]
    public float scale = 1f;

    [Header("Materyal")]
    [Range(0f, 1f)] public float metallic   = 0.9f;
    [Range(0f, 1f)] public float smoothness = 0.8f;

    private Material brassMat, keyMat, mouthMat;

    [ContextMenu("Generate Saxophone")]
    public void Generate()
    {
        ClearChildren();
        CreateMaterials();
        BuildSaxophone();
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
            else DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    private void CreateMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        brassMat = MakeMat(shader, brassColor, metallic, smoothness);
        keyMat   = MakeMat(shader, keyColor,   0.95f,    0.9f);
        mouthMat = MakeMat(shader, mouthpieceColor, 0.1f, 0.4f);
    }

    private Material MakeMat(Shader shader, Color c, float met, float smooth)
    {
        var m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic", met);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        // İki taraflı render — çan ağzı açık olduğu için iç yüzey de görünsün
        // (tek taraflı olursa bell içi şeffaf/boş görünür)
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f); // 0 = Both, 1 = Front, 2 = Back
        return m;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SAKSOFON İNŞA
    // Yerel koordinat: gövde +Y boyunca dikey, çan öne (+Z) ve aşağı kıvrık.
    // ═══════════════════════════════════════════════════════════════════════
    private void BuildSaxophone()
    {
        float s = scale;

        // J / U şekli: sağ kol = dikey gövde + boyun + ağızlık,
        //              sol kol = aşağıdan yukarı açılan çan, altta bow ile bağlı.
        // +Y yukarı, +Z öne (çan ve boyun öne kıvrılır).

        // ── 1) Ana gövde — dikey boru, aşağı doğru hafif genişler ─────────
        var body = CreateConePart("Body",
            radiusBottom: 0.050f * s, radiusTop: 0.034f * s,
            height: 0.40f * s, segments: 24, mat: brassMat);
        body.transform.localPosition = new Vector3(0f, 0.16f * s, 0f);

        // ── 2) Bow — alt U dönüşü, gövde ile çanı bağlayan kıvrım ─────────
        var bow = CreateSphere("Bow", 1f, brassMat);
        bow.transform.localPosition = new Vector3(0f, 0.11f * s, 0.045f * s);
        bow.transform.localScale = new Vector3(0.11f * s, 0.085f * s, 0.13f * s);

        // ── 3) Çan (bell) — bow'dan yukarı-öne açılan genişleyen ağız ─────
        // Alt ucu dar (bow'a bağlı), üst ucu geniş (ağız), hafif öne eğik.
        var bell = CreateConePart("Bell",
            radiusBottom: 0.034f * s, radiusTop: 0.105f * s,
            height: 0.28f * s, segments: 28, mat: brassMat);
        bell.transform.localPosition = new Vector3(0f, 0.14f * s, 0.075f * s);
        bell.transform.localRotation = Quaternion.Euler(16f, 0f, 0f);

        // ── 4) Boyun (neck) — gövde üstünden öne-yukarı kıvrık ince boru ──
        var neck = CreateConePart("Neck",
            radiusBottom: 0.030f * s, radiusTop: 0.022f * s,
            height: 0.13f * s, segments: 16, mat: brassMat);
        neck.transform.localPosition = new Vector3(0f, 0.55f * s, 0.015f * s);
        neck.transform.localRotation = Quaternion.Euler(42f, 0f, 0f);

        // ── 5) Ağızlık (mouthpiece) — siyah, boyun ucunda ────────────────
        var mouth = CreateConePart("Mouthpiece",
            radiusBottom: 0.021f * s, radiusTop: 0.012f * s,
            height: 0.055f * s, segments: 14, mat: mouthMat);
        mouth.transform.localPosition = new Vector3(0f, 0.625f * s, 0.075f * s);
        mouth.transform.localRotation = Quaternion.Euler(58f, 0f, 0f);

        // ── 6) Tuşlar (keys) — gövdenin ÖN yüzünde gümüş düğmeler ─────────
        int keyCount = 5;
        for (int i = 0; i < keyCount; i++)
        {
            float t = i / (float)(keyCount - 1);
            var key = CreateSphere("Key_" + i, 1f, keyMat);
            float y = Mathf.Lerp(0.22f * s, 0.46f * s, t);
            // gövde önüne, yüzeyine yakın
            key.transform.localPosition = new Vector3(0f, y, 0.045f * s);
            key.transform.localScale = Vector3.one * 0.020f * s;
        }

        // ── 7) Tuş çubuğu (key rod) — gövde önünde ince dikey çubuk ───────
        var rod = CreateCylinder("KeyRod", 0.004f * s, 0.14f * s, keyMat);
        rod.transform.localPosition = new Vector3(0.035f * s, 0.34f * s, 0.025f * s);

        Debug.Log("[SaxophoneGenerator] Saksofon oluşturuldu (J şekli).");
    }

    // ── Yardımcı: konik boru parçası (frustum mesh) ──────────────────────
    private GameObject CreateConePart(string name, float radiusBottom, float radiusTop,
                                      float height, int segments, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = BuildFrustumMesh(radiusBottom, radiusTop, height, segments);
        mr.sharedMaterial = mat;
        return go;
    }

    private GameObject CreateCylinder(string name, float radius, float height, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private GameObject CreateSphere(string name, float radius, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * radius * 2f;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // ── Custom mesh: frustum (kesik koni) — alt/üst yarıçap farklı boru ──
    private Mesh BuildFrustumMesh(float rBottom, float rTop, float height, int segments)
    {
        var mesh = new Mesh { name = "Frustum" };

        int vCount = (segments + 1) * 2 + 2; // yan halkalar + 2 merkez (cap)
        var verts = new Vector3[vCount];
        var norms = new Vector3[vCount];

        // Yan yüzey halkaları
        for (int i = 0; i <= segments; i++)
        {
            float ang = (i / (float)segments) * Mathf.PI * 2f;
            float cos = Mathf.Cos(ang), sin = Mathf.Sin(ang);

            verts[i]                = new Vector3(cos * rBottom, 0f,     sin * rBottom);
            verts[i + segments + 1] = new Vector3(cos * rTop,    height, sin * rTop);

            Vector3 nrm = new Vector3(cos, 0f, sin).normalized;
            norms[i] = nrm;
            norms[i + segments + 1] = nrm;
        }

        // Cap merkezleri
        int botCenter = (segments + 1) * 2;
        int topCenter = botCenter + 1;
        verts[botCenter] = new Vector3(0f, 0f, 0f);
        verts[topCenter] = new Vector3(0f, height, 0f);
        norms[botCenter] = Vector3.down;
        norms[topCenter] = Vector3.up;

        var tris = new System.Collections.Generic.List<int>();

        // Yan yüzeyler
        for (int i = 0; i < segments; i++)
        {
            int b0 = i, b1 = i + 1;
            int t0 = i + segments + 1, t1 = i + 1 + segments + 1;

            tris.Add(b0); tris.Add(t0); tris.Add(b1);
            tris.Add(b1); tris.Add(t0); tris.Add(t1);
        }

        // Alt cap
        for (int i = 0; i < segments; i++)
        {
            tris.Add(botCenter); tris.Add(i + 1); tris.Add(i);
        }
        // Üst cap
        for (int i = 0; i < segments; i++)
        {
            int t0 = i + segments + 1, t1 = i + 1 + segments + 1;
            tris.Add(topCenter); tris.Add(t0); tris.Add(t1);
        }

        mesh.vertices  = verts;
        mesh.normals   = norms;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        return mesh;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Create/Saxophone")]
    public static void CreateInScene()
    {
        var go = new GameObject("Saxophone");
        var gen = go.AddComponent<SaxophoneGenerator>();
        gen.Generate();
        UnityEditor.Selection.activeGameObject = go;
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Saxophone");
    }
#endif
}
