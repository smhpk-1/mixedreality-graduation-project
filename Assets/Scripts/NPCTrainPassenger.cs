using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Metro yolcu NPC'si.
/// 
/// Senaryo:
///   Wandering  → (Director çağırır StartBoarding) → WalkToBoard → InsideTrain
///   InsideTrain → (Director çağırır StartExiting)  → Exiting → WalkToStair → ClimbStair → Done
///   InsideTrain → (Director çağırır DespawnWithTrain) → Done (fade + destroy)
/// 
/// boardingPoint : Tren içindeki hedef nokta (Empty Transform). Tren durakken
///                 world-space pozisyonu NPC'nin yürüyeceği yer olur.
/// exitWaypoint  : Trenden inerken önce gidilecek peron noktası.
/// stairWaypoints: Merdiveni tırmanan waypoint zinciri (alt → üst).
/// </summary>
public class NPCTrainPassenger : MonoBehaviour
{
    public enum Fate
    {
        ExitAndClimbStair,   // Train_Prefab grubu: iner, merdiveni çıkar, yok olur
        DespawnWithTrain     // Train_Prefab 2 grubu: trenle birlikte kaybolur
    }

    [Header("Atama")]
    public Transform  boardingPoint;  // Tren içindeki boş obje (tren durunca world-pos'u hedef)
    public Fate       fate = Fate.ExitAndClimbStair;

    [Header("Çıkış (Fate = ExitAndClimbStair)")]
    [Tooltip("Trenden indikten sonra önce gidilecek peron noktası")]
    public Transform  exitWaypoint;
    [Tooltip("Merdiven waypoint zinciri: alt noktadan üst noktaya sırayla")]
    public Transform[] stairWaypoints;

    [Header("Ayarlar")]
    public float walkSpeed       = 1.2f;
    public float stairSpeed      = 0.7f;
    public float arrivalRadius   = 0.55f;
    public float fadeOutDuration = 1.4f;

    // ── İç durum ─────────────────────────────────────────────────────────
    public enum State
    {
        Wandering, WalkToBoard, InsideTrain,
        Exiting, WalkToStair, ClimbStair, Done
    }

    public State CurrentState { get; private set; } = State.Wandering;

    private NavMeshAgent      nav;
    private NPCScene3Wanderer wanderer;
    private Renderer[]        renderers;
    private Transform         boardingTrainTransform; // Director'dan gelir

    // ── Başlangıç ────────────────────────────────────────────────────────
    private void Awake()
    {
        nav       = GetComponent<NavMeshAgent>();
        wanderer  = GetComponent<NPCScene3Wanderer>();
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    // ── Dışarıdan tetikleyiciler ─────────────────────────────────────────

    /// <summary>Director tarafından: NPC trene binmeye başlasın.</summary>
    public void StartBoarding(Transform trainTransform = null)
    {
        if (CurrentState != State.Wandering) return;
        if (boardingPoint == null)
        {
            Debug.LogWarning($"[NPCTrainPassenger] {name}: boardingPoint atanmamış!", this);
            return;
        }
        boardingTrainTransform = trainTransform; // tren parent'ı sakla
        StartCoroutine(BoardingCoroutine());
    }

    /// <summary>Director tarafından (Train_Prefab grubu): NPC insin ve merdivene gitsin.</summary>
    public void StartExiting(Transform overrideExitWaypoint = null, Transform[] overrideStairWaypoints = null)
    {
        if (CurrentState != State.InsideTrain) return;
        if (overrideExitWaypoint   != null) exitWaypoint   = overrideExitWaypoint;
        if (overrideStairWaypoints != null) stairWaypoints = overrideStairWaypoints;
        StartCoroutine(ExitCoroutine());
    }

    /// <summary>Director tarafından (Train_Prefab 2 grubu): tren sahneden çıkınca kaybol.</summary>
    public void DespawnWithTrain()
    {
        if (CurrentState == State.Done) return;
        CurrentState = State.Done;
        StopAllCoroutines();
        StartCoroutine(FadeAndDestroy());
    }

    // ── Coroutine: Binme ─────────────────────────────────────────────────
    private IEnumerator BoardingCoroutine()
    {
        CurrentState = State.WalkToBoard;
        // Locomotion'ı durdur ama animasyon (LateUpdate) devam etsin
        if (wanderer != null) wanderer.externalControl = true;

        // nav null olabilir (NPCScene3Wanderer Start'ta ekler) — geç al
        if (nav == null) nav = GetComponent<NavMeshAgent>();

        // Hedefe hemen dön — vucüt ve yüz trene baksın
        Vector3 initialDir = boardingPoint.position - transform.position;
        initialDir.y = 0f;
        if (initialDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(initialDir.normalized);

        EnableNav(true);
        if (nav != null) nav.speed = walkSpeed;
        if (nav != null && nav.isOnNavMesh) nav.SetDestination(boardingPoint.position);

        // NavMesh üzerinde ilerle; her frame hedefi güncelle (tren sabit de olsa)
        while (true)
        {
            Vector3 destination = boardingPoint.position; // her frame taze pozisyon
            float dist = FlatDist(transform.position, destination);
            if (dist <= arrivalRadius) break;

            // Her zaman hedefe bak (NavMesh aktif olsun ya da olmasın)
            RotateToward(destination);

            // NavMesh path geçersizse veya NavMesh yoksa direkt yürü
            if (nav == null || !nav.enabled || !nav.isOnNavMesh || !nav.hasPath || nav.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                if (nav != null && nav.isOnNavMesh) nav.SetDestination(destination);
                DirectStep(destination, walkSpeed);
            }

            // Wanderer'a anlık hızı besle (animasyon için)
            if (wanderer != null)
                wanderer.externalSpeed = (nav != null && nav.enabled && nav.isOnNavMesh) ? Mathf.Max(nav.velocity.magnitude, 0.1f) : walkSpeed;

            yield return null;
        }

        // Trene parentla — Director'dan gelen transform öncelikli, yoksa boardingPoint zinciri
        Transform parent = boardingTrainTransform
                        ?? (boardingPoint.parent != null ? boardingPoint.parent : boardingPoint);
        transform.SetParent(parent);
        EnableNav(false);

        // boardingPoint'in baktığı yöne dön
        Vector3 faceDir = boardingPoint.forward;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(faceDir);

        // Trenin içinde: tüm animasyonu dondur (hareketsiz yolcu)
        if (wanderer != null)
        {
            wanderer.externalSpeed    = 0f;
            wanderer.freezeAnimation  = true;
        }
        CurrentState = State.InsideTrain;
    }

    // ── Coroutine: Çıkış + Merdiven ─────────────────────────────────────
    private IEnumerator ExitCoroutine()
    {
        // Trenden ayrıl
        transform.SetParent(null);
        CurrentState = State.Exiting;

        EnableNav(true);
        nav.speed = walkSpeed;
        // Animasyonu aç, tekrar yürüme animasyonu
        if (wanderer != null)
        {
            wanderer.freezeAnimation = false;
            wanderer.externalSpeed = walkSpeed;
        }

        // Peron çıkış noktasına git
        if (exitWaypoint != null)
        {
            if (nav.isOnNavMesh) nav.SetDestination(exitWaypoint.position);
            yield return WalkUntilArrival(exitWaypoint.position, walkSpeed);
        }

        // Merdiven waypoint'lerini sırayla geç
        CurrentState = State.WalkToStair;
        if (stairWaypoints != null)
        {
            CurrentState = State.ClimbStair;
            nav.speed = stairSpeed;
            if (wanderer != null) wanderer.externalSpeed = stairSpeed;
            foreach (Transform wp in stairWaypoints)
            {
                if (wp == null) continue;
                if (nav.isOnNavMesh) nav.SetDestination(wp.position);
                yield return WalkUntilArrival(wp.position, stairSpeed);
            }
        }

        CurrentState = State.Done;
        yield return FadeAndDestroy();
    }

    // ── Yardımcı: hedefe ulaşana kadar bekle ────────────────────────────
    private IEnumerator WalkUntilArrival(Vector3 destination, float speed)
    {
        while (FlatDist(transform.position, destination) > arrivalRadius)
        {
            RotateToward(destination);

            if (!nav.hasPath || nav.pathStatus == NavMeshPathStatus.PathInvalid)
                DirectStep(destination, speed);

            // Anlık hız → animasyon
            if (wanderer != null)
                wanderer.externalSpeed = nav.enabled ? nav.velocity.magnitude : speed;

            yield return null;
        }
    }

    // ── Yardımcı: hedefe bak ─────────────────────────────────────────────
    private void RotateToward(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir.normalized),
                6f * Time.deltaTime);
    }

    // ── Yardımcı: NavMesh olmadan adım at ───────────────────────────────
    private void DirectStep(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir.normalized),
                8f * Time.deltaTime);
        }
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    // ── Yardımcı: NavMeshAgent aç/kapa ───────────────────────────────────
    private void EnableNav(bool active)
    {
        if (nav == null) return;
        if (active && !nav.isOnNavMesh)
        {
            // NavMesh üzerinde değilse en yakın noktaya itetle
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                transform.position = hit.position;
        }
        nav.enabled = active;
    }

    // ── Yardımcı: Fade + Destroy ─────────────────────────────────────────
    private IEnumerator FadeAndDestroy()
    {
        float elapsed = 0f;
        // Renderer'ların başlangıç alpha'larını kaydet
        float[] startAlphas = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material mat = renderers[i].material;
            // URP'de _BaseColor, Standard'da _Color
            Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
            startAlphas[i] = c.a;
            // Transparency'yi aç
            mat.SetFloat("_Surface", 1f);        // URP Transparent
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Material mat = renderers[i].material;
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = startAlphas[i] * t;
                    mat.SetColor("_BaseColor", c);
                }
                else
                {
                    Color c = mat.color;
                    c.a = startAlphas[i] * t;
                    mat.color = c;
                }
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    // ── Yardımcı ─────────────────────────────────────────────────────────
    private static float FlatDist(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmosSelected()
    {
        if (boardingPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(boardingPoint.position, 0.25f);
            Gizmos.DrawLine(transform.position, boardingPoint.position);
        }
        if (exitWaypoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(exitWaypoint.position, 0.25f);
        }
        if (stairWaypoints != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < stairWaypoints.Length; i++)
            {
                if (stairWaypoints[i] == null) continue;
                Gizmos.DrawWireSphere(stairWaypoints[i].position, 0.2f);
                if (i > 0 && stairWaypoints[i - 1] != null)
                    Gizmos.DrawLine(stairWaypoints[i - 1].position, stairWaypoints[i].position);
            }
        }
    }
}
