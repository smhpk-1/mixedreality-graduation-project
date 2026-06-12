using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Metro yolcu NPC'si — waypoint zincirleri ile yürür (NavMesh kullanılmaz biniş/iniş'te).
///
/// Wandering → StartBoarding() → boardingPath waypoint'leri → boardingPoint → InsideTrain
/// InsideTrain → StartExiting()  → exitPath waypoint'leri → Done (fade + destroy)
/// InsideTrain → DespawnWithTrain() → Done (fade + destroy)
///
/// NPC peronda dolaşırken NavMeshAgent kullanır. Director StartBoarding çağırınca
/// NavMesh kapatılır, NPC sadece atanan waypoint'leri sırayla düz çizgide yürür.
/// Bu, NPC'nin duvardan/trenden geçmesini önler — path'i kullanıcı kapıdan içeri çizer.
/// </summary>
public class NPCTrainPassenger : MonoBehaviour
{
    public enum Fate
    {
        ExitAndClimbStair,
        DespawnWithTrain
    }

    [Header("Atama")]
    [Tooltip("Tren içinde NPC'nin son duracağı nokta (her NPC'ye özel)")]
    public Transform boardingPoint;

    [Tooltip("Biniş yolu — NPC bu waypoint'leri sırayla yürür, sonra boardingPoint'e gider")]
    public Transform[] boardingPath;

    [Tooltip("İniş yolu — tren içi → peron → merdiven üstü TEK zincir (sadece ExitAndClimbStair fate için)")]
    public Transform[] exitPath;

    public Fate fate = Fate.ExitAndClimbStair;

    [Header("Ayarlar")]
    public float walkSpeed       = 1.8f;
    public float stairSpeed      = 1.2f;
    public float arrivalRadius   = 0.55f;
    public float fadeOutDuration = 1.4f;
    public float fadeInDuration  = 1.4f;

    public enum State
    {
        Wandering, WalkToBoard, InsideTrain, Exiting, Done
    }

    public State CurrentState { get; private set; } = State.Wandering;

    private NavMeshAgent      nav;
    private NPCScene3Wanderer wanderer;
    private Renderer[]        renderers;
    private Transform         boardingTrainTransform;

    // NPC'nin ilk kez boarding'e başladığı andaki konum — reset'te bu konuma geri döner
    private Vector3    initialPosition;
    private Quaternion initialRotation;
    private bool       initialPositionCaptured = false;

    // NPC'nin orijinal localScale'i — parent/unparent sonrası geri yüklemek için
    private Vector3 originalLocalScale = Vector3.one;

    private void Awake()
    {
        wanderer  = GetComponent<NPCScene3Wanderer>();
        renderers = GetComponentsInChildren<Renderer>(true);
        nav       = GetComponent<NavMeshAgent>();

        // Orijinal localScale'i kaydet (genelde 1,1,1)
        originalLocalScale = transform.localScale;
    }

    // ── Dışarıdan tetikleyiciler ─────────────────────────────────────────

    public void StartBoarding(Transform trainTransform = null)
    {
        if (CurrentState != State.Wandering) return;
        if (boardingPoint == null)
        {
            Debug.LogWarning($"[NPCTrainPassenger] {name}: boardingPoint atanmamış!", this);
            return;
        }

        // İLK boarding'de NPC'nin tam o andaki konumunu yakala —
        // reset'te bu noktaya geri döner. Sonraki cycle'larda da aynı konum kullanılır.
        if (!initialPositionCaptured)
        {
            // FLOATING FIX: konumu yakalamadan önce zemine otur. Havada
            // yakalanan konum, her cycle reset'inde NPC'yi havaya geri koyuyordu.
            NPCGrounding.SnapToGround(transform);
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            initialPositionCaptured = true;
        }

        boardingTrainTransform = trainTransform;
        StartCoroutine(BoardingCoroutine());
    }

    public void StartExiting(Transform[] overrideExitPath = null)
    {
        if (CurrentState != State.InsideTrain) return;
        if (overrideExitPath != null) exitPath = overrideExitPath;
        StartCoroutine(ExitCoroutine());
    }

    public void DespawnWithTrain()
    {
        if (CurrentState == State.Done) return;

        // Trene binmemişse: wandering'e dön
        if (CurrentState != State.InsideTrain)
        {
            StopAllCoroutines();
            transform.SetParent(null, worldPositionStays: true);
            transform.localScale = originalLocalScale;
            if (wanderer != null)
            {
                wanderer.externalControl = false;
                wanderer.externalSpeed   = 0f;
                wanderer.freezeAnimation = false;
            }
            if (nav != null) nav.enabled = true;
            CurrentState = State.Wandering;
            return;
        }

        // Trene binmiş: trenden ayrıl, sahne dışına teleport et (LOD'a dokunma)
        StopAllCoroutines();
        transform.SetParent(null, worldPositionStays: true);
        transform.localScale = originalLocalScale; // non-uniform parent scale bug'ını düzelt
        if (nav != null) nav.enabled = false;
        if (wanderer != null) wanderer.freezeAnimation = true;
        transform.position = HiddenPosition; // sahne dışı
        CurrentState = State.Done;
    }

    // NPC saklanırken konacağı sahne dışı nokta (yerin çok altı)
    private static readonly Vector3 HiddenPosition = new Vector3(0f, -10000f, 0f);

    /// <summary>Director çağırır: NPC kendi orijinal konumuna teleport + fade in.</summary>
    public void ResetForNextCycle()
    {
        StopAllCoroutines();
        StartCoroutine(ResetCoroutine());
    }

    // ── Coroutine: Binme ─────────────────────────────────────────────────
    private IEnumerator BoardingCoroutine()
    {
        CurrentState = State.WalkToBoard;

        // Wanderer'ı dış kontrole al
        if (wanderer != null)
        {
            wanderer.externalControl = true;
            wanderer.externalSpeed   = walkSpeed;
            wanderer.freezeAnimation = false;
        }

        // 1) FAZ 1: NPC kendi konumundan ilk waypoint'e NavMesh ile gitsin
        //    (NavMesh duvarları respect eder — NPC duvardan geçmez)
        Transform firstWP = (boardingPath != null && boardingPath.Length > 0)
                            ? boardingPath[0] : boardingPoint;
        if (firstWP != null)
            yield return ApproachViaNavMesh(firstWP, walkSpeed);

        // 2) FAZ 2: NavMesh'i kapat, geri kalan waypoint'leri straight-line yürü
        //    (Path kullanıcı tarafından çizildi, kapıdan geçtiği biliniyor)
        if (nav != null) nav.enabled = false;

        if (boardingPath != null)
        {
            for (int i = 1; i < boardingPath.Length; i++) // i=1: ilki zaten yürüdük
            {
                if (boardingPath[i] == null) continue;
                yield return WalkToWorld(boardingPath[i], walkSpeed);
            }
        }

        // 3) Son adım: kendi boarding spot'una
        yield return WalkToWorld(boardingPoint, walkSpeed);

        // 3) Trene parent et — NPC trenle birlikte hareket eder
        Transform parent = boardingTrainTransform
                        ?? (boardingPoint.parent != null ? boardingPoint.parent : boardingPoint);
        transform.SetParent(parent, worldPositionStays: true);

        // 4) boardingPoint'in baktığı yöne dön (oturma/ayakta durma yönü)
        Vector3 faceDir = boardingPoint.forward;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(faceDir);

        // 5) Trende: hareketsiz dur, animasyon donsun
        if (wanderer != null)
        {
            wanderer.externalSpeed   = 0f;
            wanderer.freezeAnimation = true;
        }
        CurrentState = State.InsideTrain;
    }

    // ── Coroutine: İniş ──────────────────────────────────────────────────
    private IEnumerator ExitCoroutine()
    {
        // Trenden ayrıl (parent null)
        transform.SetParent(null, worldPositionStays: true);
        CurrentState = State.Exiting;

        if (wanderer != null)
        {
            wanderer.freezeAnimation = false;
            wanderer.externalControl = true;
            wanderer.externalSpeed   = walkSpeed;
        }
        if (nav != null) nav.enabled = false;

        // İniş yolu waypoint'lerini sırayla yürü
        if (exitPath != null)
        {
            for (int i = 0; i < exitPath.Length; i++)
            {
                Transform wp = exitPath[i];
                if (wp == null) continue;
                // Stair waypoint'leri için stairSpeed (basit kural: ismi "Stair" ile başlıyorsa)
                float speed = wp.name.StartsWith("Stair", System.StringComparison.OrdinalIgnoreCase)
                              ? stairSpeed : walkSpeed;
                yield return WalkToWorld(wp, speed);
            }
        }

        CurrentState = State.Done;
        if (nav != null) nav.enabled = false;
        if (wanderer != null) wanderer.freezeAnimation = true;
        transform.position = HiddenPosition;
        yield break;
    }

    // ── Yardımcı: hedefe NavMesh ile yaklaş (duvarları respect eder) ────
    // NPC'nin bulunduğu yerden ilk waypoint'e gitmek için kullanılır.
    // NavMesh yoksa veya path geçersizse, fallback olarak DirectStep'e döner.
    private IEnumerator ApproachViaNavMesh(Transform target, float speed)
    {
        if (target == null) yield break;
        if (nav == null) nav = GetComponent<NavMeshAgent>();

        if (nav == null)
        {
            yield return WalkToWorld(target, speed);
            yield break;
        }

        nav.enabled = true;

        // NavMesh dışındaysak en yakın noktaya snap
        if (!nav.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                transform.position = hit.position;
        }

        if (!nav.isOnNavMesh)
        {
            // Hala NavMesh'te değil — düz yürü
            yield return WalkToWorld(target, speed);
            yield break;
        }

        // KRİTİK: wandering pause'undan kalmış isStopped/path durumunu temizle
        nav.isStopped       = false;
        nav.updatePosition  = true;
        nav.updateRotation  = false;
        nav.ResetPath();
        nav.speed           = speed;
        nav.stoppingDistance = 0.1f;
        nav.SetDestination(target.position);

        // Path hesaplanmasını bekle (pathPending iken velocity 0 olur, kayma olmasın)
        float pathWaitTimeout = 1f;
        while (nav.pathPending && pathWaitTimeout > 0f)
        {
            pathWaitTimeout -= Time.deltaTime;
            if (wanderer != null) wanderer.externalSpeed = 0f; // path hazırlanırken sabit dur
            yield return null;
        }

        // Path hazır olmasına rağmen geçersizse direkt fallback'e geç (duvarları zorlama)
        if (nav.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            nav.enabled = false;
            yield return WalkToWorld(target, speed);
            yield break;
        }

        float timeout = 25f;
        while (FlatDist(transform.position, target.position) > arrivalRadius)
        {
            timeout -= Time.deltaTime;
            if (timeout <= 0f) break;

            // Path runtime'da geçersiz olduysa kır
            if (!nav.pathPending && nav.pathStatus == NavMeshPathStatus.PathInvalid)
                break;

            // Partial path'in sonundaysa fallback (NavMesh oraya ulaşamıyor)
            if (!nav.pathPending && nav.pathStatus == NavMeshPathStatus.PathPartial &&
                nav.remainingDistance <= nav.stoppingDistance + 0.05f)
                break;

            // KRİTİK: NavMeshAgent updateRotation=false olduğu için hareket yönüne
            // dönmeyi manuel yap (yoksa NPC geri/yan gidiyormuş gibi görünür)
            Vector3 vel = nav.velocity;
            vel.y = 0f;
            if (vel.sqrMagnitude > 0.04f) // > 0.2 m/s
            {
                Quaternion targetRot = Quaternion.LookRotation(vel.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
            }
            else
            {
                // Velocity çok düşükse desiredVelocity'ye dön (path başında)
                Vector3 desired = nav.desiredVelocity;
                desired.y = 0f;
                if (desired.sqrMagnitude > 0.04f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(desired.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
                }
            }

            if (wanderer != null)
                wanderer.externalSpeed = Mathf.Max(nav.velocity.magnitude, 0.4f);

            yield return null;
        }
    }

    // ── Yardımcı: bir Transform'a düz çizgide yürü ───────────────────────
    private IEnumerator WalkToWorld(Transform target, float speed)
    {
        if (target == null) yield break;
        while (FlatDist(transform.position, target.position) > arrivalRadius)
        {
            Vector3 dest = target.position;
            RotateToward(dest);
            DirectStep(dest, speed);
            if (wanderer != null) wanderer.externalSpeed = speed;
            yield return null;
        }
    }

    // ── Yardımcı: hedefe dön ─────────────────────────────────────────────
    private void RotateToward(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir.normalized),
                10f * Time.deltaTime);
    }

    // ── Yardımcı: düz çizgide adım at ────────────────────────────────────
    private void DirectStep(Vector3 target, float speed)
    {
        Vector3 step = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // FLOATING FIX: waypoint Y'si kaba rotadır ("doğru ayarlanmış olmalı"
        // varsayımı havada yürüyen NPC üretiyordu). Küçük farkları gerçek
        // zemine yumuşakça oturt (±35cm). Daha büyük farklar bilinçli köprüdür
        // (peron-tren boşluğu) — orada waypoint Y'sine güvenmeye devam et.
        if (NPCGrounding.TryGetGroundY(step, transform, out float groundY))
        {
            float footY = groundY + 0.02f;
            if (Mathf.Abs(footY - step.y) <= 0.35f)
                step.y = Mathf.MoveTowards(step.y, footY, 2f * Time.deltaTime);
        }

        transform.position = step;
    }

    // ── Yardımcı: Fade Out (görünmez yap ama destroy ETME) ──────────────
    private IEnumerator FadeOut()
    {
        EnableTransparency();
        float[] startAlphas = SnapshotAlphas();

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            SetAlphas(startAlphas, t);
            yield return null;
        }
        SetAlphas(startAlphas, 0f); // tamamen invisible
    }

    // ── Yardımcı: Fade In (invisible → görünür) ─────────────────────────
    private IEnumerator FadeIn()
    {
        EnableTransparency();

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Material mat = renderers[i].material;
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = t;
                    mat.SetColor("_BaseColor", c);
                }
                else
                {
                    Color c = mat.color;
                    c.a = t;
                    mat.color = c;
                }
            }
            yield return null;
        }
    }

    private void EnableTransparency()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material mat = renderers[i].material;
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            mat.renderQueue = 3000;
        }
    }

    private float[] SnapshotAlphas()
    {
        float[] arr = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material mat = renderers[i].material;
            Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
            arr[i] = c.a;
        }
        return arr;
    }

    private void SetAlphas(float[] startAlphas, float t)
    {
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
    }

    // ── Coroutine: Reset (BASİT — sahne dışından kendi yerine teleport) ──
    private IEnumerator ResetCoroutine()
    {
        // Trenden ayrıl (güvenlik)
        transform.SetParent(null, worldPositionStays: true);
        transform.localScale = originalLocalScale; // her ihtimale karşı scale restore

        // Kendi orijinal konumuna teleport (sahne dışından geri gelir)
        if (initialPositionCaptured)
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            // Güvenlik: teleport sonrası zemine otur (initialPosition eski bir
            // build'de havada yakalanmış olabilir)
            NPCGrounding.SnapToGround(transform);
        }

        // Wandering moduna geri dön — bir sonraki tren gelince StartBoarding tetiklenir
        if (wanderer != null)
        {
            wanderer.externalControl = false;
            wanderer.externalSpeed   = 0f;
            wanderer.freezeAnimation = false;
        }
        if (nav != null) nav.enabled = true;

        CurrentState = State.Wandering;
        yield break;
    }

    private static float FlatDist(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // ── Gizmo: Scene view'da path'i göster ───────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (boardingPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(boardingPoint.position, 0.25f);
        }

        // Biniş yolu (mavi)
        if (boardingPath != null && boardingPath.Length > 0)
        {
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.9f);
            Vector3 prev = transform.position;
            for (int i = 0; i < boardingPath.Length; i++)
            {
                if (boardingPath[i] == null) continue;
                Gizmos.DrawWireSphere(boardingPath[i].position, 0.2f);
                Gizmos.DrawLine(prev, boardingPath[i].position);
                prev = boardingPath[i].position;
            }
            if (boardingPoint != null)
                Gizmos.DrawLine(prev, boardingPoint.position);
        }

        // İniş yolu (sarı → turuncu)
        if (exitPath != null && exitPath.Length > 0)
        {
            Vector3 prev = boardingPoint != null ? boardingPoint.position : transform.position;
            for (int i = 0; i < exitPath.Length; i++)
            {
                if (exitPath[i] == null) continue;
                Gizmos.color = Color.Lerp(Color.yellow, new Color(1f, 0.4f, 0f), i / (float)exitPath.Length);
                Gizmos.DrawWireSphere(exitPath[i].position, 0.2f);
                Gizmos.DrawLine(prev, exitPath[i].position);
                prev = exitPath[i].position;
            }
        }
    }
}
