using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Çöp arabası — oyuncuyu takip eder, içine atılan çöpleri sayar.
/// 20 çöp toplanınca Scene 4'e geçer.
///
/// Kurulum:
///   1) Bu component'i Prefab_TrashCart root'una ekle
///   2) Üzerine BoxCollider ekle, "Is Trigger" işaretle, üst açıklık hizasında konumla
///   3) Player alanını boş bırakırsan otomatik olarak "XR Origin (XR Rig)" aranır
///   4) Collection Sound boş kalabilir — sonradan eklersin
///
/// Çöp objelerinde olması gereken:
///   • TrashItem component (mevcut)
///   • XRGrabInteractable
///   • Rigidbody + Collider (trigger DEĞİL)
/// </summary>
[DisallowMultipleComponent]
public class TrashCart : MonoBehaviour
{
    [Header("Oyuncu Takibi")]
    [Tooltip("Takip edilecek hedef (oyuncu). Boş bırakırsan XR Origin otomatik bulunur.")]
    public Transform player;

    [Tooltip("ESKİ — artık kullanılmıyor. Yaw'a bağlı offset her kafa dönüşünde cart'ı yüzün önüne savuruyordu.")]
    public Vector3 followOffset = new Vector3(0.7f, 0f, 0.3f);

    [Tooltip("Cart peşinden yürürken hızı (m/sn)")]
    public float followSpeed = 1.6f;

    [Tooltip("Oyuncudan bu mesafeden uzaklaşırsa cart anında yetişsin (teleport)")]
    public float maxDistance = 7f;

    [Tooltip("Cart oyuncudan bu mesafede durur — görüşü kapatmaz ama el uzanır")]
    public float followDistance = 1.5f;

    [Tooltip("Oyuncu bundan fazla uzaklaşınca cart takibe başlar (ölü bölge — kafa dönüşünde KIPIRDAMAZ)")]
    public float startFollowDistance = 2.8f;

    [Tooltip("Cart hareket ettiği yöne dönsün mü? (kafa yaw'ına asla bağlanmaz)")]
    public bool faceWithPlayer = true;

    [Header("Çöp Toplama")]
    [Tooltip("Bu kadar çöp toplanınca Scene 4'e geçilir")]
    public int targetCount = 20;

    [Tooltip("Hedefe ulaşılınca yüklenecek sahne")]
    public string nextSceneName = "Scene 4";

    [Tooltip("Sahne yüklenmeden önce ses çalmasına izin vermek için bekleme")]
    public float sceneLoadDelay = 1.0f;

    [Header("Ses (sonradan atanacak)")]
    public AudioSource audioSource;
    public AudioClip collectionSound;

    [Header("Debug")]
    [Tooltip("Şu ana kadar toplanan çöp sayısı (runtime'da güncellenir)")]
    public int currentCount = 0;

    // ── Eventler — UI veya başka script'ler bağlanabilir ─────────────────
    public event System.Action<int, int> OnTrashCollected; // (current, target)
    public event System.Action OnGoalReached;

    private float lockedY;       // Cart'ın zemin Y'si
    private bool  goalReached;

    private void Awake()
    {
        // Cart fiziği — kinematic (oyuncuya çarpıp itilmesin).
        // KRİTİK: Rigidbody YOKSA EKLE — child collider'ların (InteriorTrigger)
        // OnTrigger olayları ancak root'ta rigidbody varsa bu script'e ulaşır.
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;
    }

    private void Start()
    {
        lockedY = transform.position.y;

        // Oyuncuyu otomatik bul (Inspector boşsa)
        if (player == null)
        {
            var xrRig = GameObject.Find("XR Origin (XR Rig)");
            if (xrRig != null)
                player = xrRig.transform;
            else if (Camera.main != null)
                player = Camera.main.transform;
            else
                Debug.LogWarning("[TrashCart] Oyuncu bulunamadı. Inspector'dan player atayın.");
        }

        // AudioSource hazırla
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D ses

        EnsureInteriorTrigger();
    }

    /// <summary>
    /// Sepetin İÇ HACMİNİ kaplayan trigger oluşturur. İnce "üst açıklık" trigger'ı
    /// hızlı fırlatmalarda tünelleniyor (discrete collision frame atlıyor) ve içine
    /// düşüp dibe oturan çöp trigger'ın altında kaldığı için hiç sayılmıyordu.
    /// İç hacim trigger'ı sepette DURAN çöpü her frame yakalar (OnTriggerStay).
    /// Alt sınırı yerden yüksek tutulur ki cart yerdeki çöplerin üzerinden
    /// geçerken onları "toplamış" olmasın.
    /// </summary>
    private void EnsureInteriorTrigger()
    {
        if (transform.Find("InteriorTrigger") != null) return;

        // Cart'ın görsel sınırlarını topla
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        var go = new GameObject("InteriorTrigger");
        go.transform.SetParent(transform, false);

        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;

        // Üst yarı: sepet ağzından gövde ortasına kadar — yer seviyesine inmez
        float bottom = Mathf.Lerp(b.min.y, b.max.y, 0.45f);
        float top    = b.max.y + 0.15f; // ağzın hemen üstü de yakalansın
        Vector3 worldCenter = new Vector3(b.center.x, (bottom + top) * 0.5f, b.center.z);
        Vector3 worldSize   = new Vector3(b.size.x * 0.85f, top - bottom, b.size.z * 0.85f);

        box.center = transform.InverseTransformPoint(worldCenter);
        Vector3 ls = transform.lossyScale;
        box.size = new Vector3(worldSize.x / Mathf.Max(0.001f, ls.x),
                               worldSize.y / Mathf.Max(0.001f, ls.y),
                               worldSize.z / Mathf.Max(0.001f, ls.z));
    }

    private bool following; // ölü bölge takip durumu

    private void LateUpdate()
    {
        if (player == null) return;

        // Evcil hayvan takibi: kafa/gövde DÖNÜŞÜ cart'ı asla oynatmaz.
        // Sadece oyuncu YÜRÜYÜP uzaklaşınca peşinden gelir, followDistance'ta durur.
        Vector3 p = player.position; p.y = lockedY;
        Vector3 c = transform.position; c.y = lockedY;
        Vector3 toCart = c - p;
        float dist = toCart.magnitude;
        Vector3 dirFromPlayer = dist > 0.01f ? toCart / dist : transform.forward;

        // Çok uzak kaldıysa ışınlan (oyuncunun olduğu tarafa, followDistance kadar yakına)
        if (dist > maxDistance)
        {
            transform.position = p + dirFromPlayer * followDistance;
            following = false;
            return;
        }

        if (!following && dist > startFollowDistance)
            following = true;

        if (!following) return;

        // Hedef: oyuncuya followDistance kalana dek aynı doğrultuda yaklaş
        Vector3 target = p + dirFromPlayer * followDistance;
        Vector3 next = Vector3.MoveTowards(c, target, followSpeed * Time.deltaTime);
        Vector3 moveDir = next - c;
        transform.position = new Vector3(next.x, lockedY, next.z);

        // Gerçek bir el arabası gibi gittiği yöne dönsün
        if (faceWithPlayer && moveDir.sqrMagnitude > 0.000001f)
        {
            Quaternion face = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, face, 5f * Time.deltaTime);
        }

        if (Vector3.Distance(next, target) < 0.05f)
            following = false; // vardı — oyuncu tekrar uzaklaşana kadar kıpırdama
    }

    // ── Çöp düştüğünde tetiklenen olay ───────────────────────────────────
    private void OnTriggerEnter(Collider other) => TryCollect(other);
    private void OnTriggerStay(Collider other) => TryCollect(other);

    private void TryCollect(Collider other)
    {
        if (goalReached) return;

        // Çöp objesini bul — root'a kadar git ki nested mesh'ler de yakalansın
        GameObject trashRoot = FindTrashRoot(other);
        if (trashRoot == null) return;

        // FIX 1: Eğer çöp şu an oyuncunun elindeyse SAYMA.
        // Aksi halde oyuncu cart'a yaklaşınca elindeki çöp anında silinir.
        var grab = trashRoot.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null && grab.isSelected)
        {
            // Oyuncu hala tutuyor, bırakmasını bekle
            return;
        }

        // FIX 2: Çöp en az bir kez grab edilmemişse SAYMA.
        // Aksi halde cart oyuncuyu takip ederken yerdeki sabit çöplerin üzerine geliyor
        // ve hepsini bir anda topluyor. Sadece "kullanıcı tarafından dokunulmuş" çöpleri kabul et.
        var marker = trashRoot.GetComponent<TrashGrabbedMarker>();
        if (grab != null && marker == null)
        {
            return;
        }

        // FIX 3: Bırakılalı en az 0.15s geçmiş olmalı. El değiştirme / regrip
        // sırasındaki anlık deselect, cart oyuncunun dibinde olduğu için çöpü
        // "eldeyken" toplatıyordu — alma anında sayım bu yüzden oluyordu.
        if (marker != null && Time.time - marker.LastReleaseTime < 0.15f)
        {
            return;
        }

        CollectTrash(trashRoot);
    }

    /// <summary>
    /// Collider üzerinde TrashItem veya GrabbableTrash var mı diye kontrol eder.
    /// Bulduğunda o component'in bulunduğu GameObject'i döndürür.
    /// </summary>
    private static GameObject FindTrashRoot(Collider col)
    {
        // TrashItem var mı?
        var trashItem = col.GetComponentInParent<TrashItem>();
        if (trashItem != null) return trashItem.gameObject;

        // GrabbableTrash var mı?
        var grabbable = col.GetComponentInParent<GrabbableTrash>();
        if (grabbable != null) return grabbable.gameObject;

        return null;
    }

    private void CollectTrash(GameObject trash)
    {
        currentCount++;

        // Ses çal
        if (audioSource != null && collectionSound != null)
            audioSource.PlayOneShot(collectionSound);

        OnTrashCollected?.Invoke(currentCount, targetCount);
        Debug.Log($"[TrashCart] Toplanan: {currentCount}/{targetCount} ({trash.name})");

        // Çöpü yok et
        Destroy(trash);

        // Hedef tamamlandı mı?
        if (currentCount >= targetCount)
        {
            goalReached = true;
            OnGoalReached?.Invoke();
            Invoke(nameof(LoadNextScene), sceneLoadDelay);
        }
    }

    private void LoadNextScene()
    {
        Debug.Log($"[TrashCart] Hedef tamamlandı! {nextSceneName} yükleniyor.");
        SceneManager.LoadScene(nextSceneName);
    }

    // ── Gizmo: Inspector'da takip mesafeleri görünsün ────────────────────
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.6f);
        Gizmos.DrawWireSphere(player.position, followDistance);
        Gizmos.color = new Color(1f, 0.7f, 0f, 0.6f);
        Gizmos.DrawWireSphere(player.position, startFollowDistance);

        Vector3 desired = transform.position;
        desired.y = Application.isPlaying ? lockedY : transform.position.y;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(desired, 0.3f);
        Gizmos.DrawLine(player.position, desired);
    }
}
