using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NPC'lerin altına yumuşak dairesel "blob" gölge koyar.
///
/// SORUN: Scene 3'te gerçek zamanlı directional ışık yok (istasyon baked area
/// ışıklarla aydınlatılıyor) → dinamik NPC'ler HİÇ gölge düşürmüyor. Düz
/// ekranda fark edilmiyor ama VR'da derinlik algısı yüzünden NPC'ler "havada
/// asılı" görünüyor. Klasik mobil/VR çözümü: her zaman görünen ucuz blob gölge.
///
/// Kurulum gerekmez: NPC'li sahne yüklenince her NPCScene3Wanderer'a kendini
/// ekler. Materyal Resources/NPCBlobShadow.mat'tan gelir (shader'ın build'e
/// girmesi garanti olsun diye asset olarak; radyal doku runtime'da üretilir).
/// </summary>
public class NPCBlobShadow : MonoBehaviour
{
    [Tooltip("Gölge yarıçapı (metre)")]
    public float radius = 0.42f;

    [Tooltip("Tam yerdeyken gölge opaklığı")]
    [Range(0f, 1f)] public float maxAlpha = 0.55f;

    [Tooltip("Ayak bu kadar yüksekteyse gölge tamamen söner (metre)")]
    public float fadeHeight = 0.8f;

    public LayerMask groundMask = ~0;

    private static Material sharedMaterial;
    private static Mesh quadMesh;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Transform quad;
    private MeshRenderer rend;
    private MaterialPropertyBlock props;
    private readonly RaycastHit[] hits = new RaycastHit[8];

    // ── Bootstrap: NPC'li sahnelerde kendini kurar ───────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        ApplyToScene();
        SceneManager.sceneLoaded += (scene, mode) => ApplyToScene();
    }

    private static void ApplyToScene()
    {
        var wanderers = Object.FindObjectsByType<NPCScene3Wanderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var w in wanderers)
        {
            if (w != null && w.GetComponent<NPCBlobShadow>() == null)
                w.gameObject.AddComponent<NPCBlobShadow>();
        }
    }

    private void Start()
    {
        EnsureSharedAssets();
        if (sharedMaterial == null)
        {
            enabled = false;
            return;
        }

        // Quad dünya uzayında bağımsız durur: NPC eğilse/ölçeklense de gölge düz kalır
        var go = new GameObject($"BlobShadow_{name}");
        quad = go.transform;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = quadMesh;
        rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = sharedMaterial;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        rend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        props = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        if (rend == null) return;

        // Gizlenmiş NPC (tren döngüsü y=-10000) → gölge kapalı
        if (transform.position.y < -100f)
        {
            rend.enabled = false;
            return;
        }

        // Ayak hizasının az üstünden aşağı tara, kendimize ait collider'ları atla
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        int n = Physics.RaycastNonAlloc(origin, Vector3.down, hits, 4f,
                                        groundMask, QueryTriggerInteraction.Ignore);
        RaycastHit best = default;
        bool found = false;
        for (int i = 0; i < n; i++)
        {
            if (hits[i].transform == null || hits[i].transform.IsChildOf(transform)) continue;
            if (!found || hits[i].distance < best.distance) { best = hits[i]; found = true; }
        }

        if (!found)
        {
            rend.enabled = false;
            return;
        }

        rend.enabled = true;
        quad.position = best.point + best.normal * 0.015f;
        quad.rotation = Quaternion.LookRotation(-best.normal); // quad +Z'ye bakar → yere yatır
        quad.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

        // Ayak yerden uzaklaştıkça gölge solar (zıplama/havada durma durumları)
        float gap = Mathf.Max(0f, transform.position.y - best.point.y);
        float a = maxAlpha * Mathf.Clamp01(1f - gap / fadeHeight);
        props.SetColor(BaseColorId, new Color(0f, 0f, 0f, a));
        rend.SetPropertyBlock(props);
    }

    private static void EnsureSharedAssets()
    {
        if (sharedMaterial != null) return;

        Material template = Resources.Load<Material>("NPCBlobShadow");
        if (template == null)
        {
            Debug.LogWarning("[NPCBlobShadow] Resources/NPCBlobShadow.mat bulunamadı — gölge kapalı.");
            return;
        }

        sharedMaterial = new Material(template);
        sharedMaterial.mainTexture = BuildRadialTexture(64);
        if (sharedMaterial.HasProperty("_BaseMap"))
            sharedMaterial.SetTexture("_BaseMap", sharedMaterial.mainTexture);

        quadMesh = BuildQuad();
    }

    /// <summary>Yumuşak kenarlı radyal gradyan dokusu (merkez opak, kenar şeffaf).</summary>
    private static Texture2D BuildRadialTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a); // smoothstep — yumuşak kenar
                pixels[y * size + x] = new Color32(0, 0, 0, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    private static Mesh BuildQuad()
    {
        var mesh = new Mesh { name = "BlobShadowQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f), new Vector3(0.5f,  0.5f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 1f), new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    // NPC kapatılırsa başıboş gölge kalmasın (quad dünya uzayında bağımsız)
    private void OnDisable()
    {
        if (quad != null) quad.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (quad != null) quad.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (quad != null) Destroy(quad.gameObject);
    }
}
