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

    [Tooltip("Oyuncuya göre yerel offset: X=sağ, Z=ileri/geri. Y kilitli (zemin).")]
    public Vector3 followOffset = new Vector3(0.7f, 0f, 0.3f);

    [Tooltip("Takip yumuşaklığı — yüksek = hızlı yetişir, düşük = yumuşak gecikir")]
    public float followSpeed = 3f;

    [Tooltip("Oyuncudan bu mesafeden uzaklaşırsa cart anında yetişsin (teleport)")]
    public float maxDistance = 4f;

    [Tooltip("Cart oyuncunun baktığı yöne dönsün mü? (tekerlekler ileri doğru görünür)")]
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
        // Cart fiziği — kinematic (oyuncuya çarpıp itilmesin)
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
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
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // Oyuncunun YZ rotation'ına göre offset hesapla (sadece Y rotation kullan, eğilmesin)
        float yaw = player.eulerAngles.y;
        Quaternion playerYaw = Quaternion.Euler(0f, yaw, 0f);
        Vector3 worldOffset = playerYaw * followOffset;
        Vector3 desired = player.position + worldOffset;
        desired.y = lockedY; // Zemine kilitli

        // Mesafe çok büyükse teleport
        float dist = Vector3.Distance(new Vector3(transform.position.x, lockedY, transform.position.z),
                                      desired);
        if (dist > maxDistance)
        {
            transform.position = desired;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        }

        // Cart'ın yönü
        if (faceWithPlayer)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, playerYaw, followSpeed * Time.deltaTime);
        }
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
        if (grab != null && !HasBeenGrabbedBefore(grab))
        {
            return;
        }

        CollectTrash(trashRoot);
    }

    /// <summary>
    /// XRGrabInteractable'da "interactionsSelectedCount" history mevcut değil — onun yerine
    /// item üzerinde marker bir component ararız (TrashItemWasGrabbed). Bu component grab
    /// edildiğinde otomatik eklenir.
    /// </summary>
    private static bool HasBeenGrabbedBefore(
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab)
    {
        return grab.GetComponent<TrashGrabbedMarker>() != null;
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

    // ── Gizmo: Inspector'da cart'ın gideceği konum görünsün ──────────────
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        float yaw = player.eulerAngles.y;
        Quaternion playerYaw = Quaternion.Euler(0f, yaw, 0f);
        Vector3 desired = player.position + playerYaw * followOffset;
        desired.y = Application.isPlaying ? lockedY : transform.position.y;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(desired, 0.3f);
        Gizmos.DrawLine(player.position, desired);
    }
}
