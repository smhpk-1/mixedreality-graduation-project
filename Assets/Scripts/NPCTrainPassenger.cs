using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Metro yolcu NPC'si.
///
/// Wandering  → StartBoarding()        → WalkToBoard → InsideTrain
/// InsideTrain → StartExiting()         → Exiting → WalkToStair → ClimbStair → Done
/// InsideTrain → DespawnWithTrain()     → Done (fade + destroy)
/// </summary>
public class NPCTrainPassenger : MonoBehaviour
{
    public enum Fate
    {
        ExitAndClimbStair,
        DespawnWithTrain
    }

    [Header("Atama")]
    public Transform  boardingPoint;
    public Fate       fate = Fate.ExitAndClimbStair;

    [Header("Çıkış (Fate = ExitAndClimbStair)")]
    public Transform   exitWaypoint;
    public Transform[] stairWaypoints;

    [Header("Ayarlar")]
    public float walkSpeed       = 1.4f;
    public float stairSpeed      = 0.9f;
    public float arrivalRadius   = 0.55f;
    public float fadeOutDuration = 1.4f;

    public enum State
    {
        Wandering, WalkToBoard, InsideTrain,
        Exiting, WalkToStair, ClimbStair, Done
    }

    public State CurrentState { get; private set; } = State.Wandering;

    private NavMeshAgent      nav;
    private NPCScene3Wanderer wanderer;
    private Renderer[]        renderers;
    private Transform         boardingTrainTransform;

    private void Awake()
    {
        wanderer  = GetComponent<NPCScene3Wanderer>();
        renderers = GetComponentsInChildren<Renderer>(true);
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
        boardingTrainTransform = trainTransform;
        StartCoroutine(BoardingCoroutine());
    }

    public void StartExiting(Transform overrideExitWaypoint = null, Transform[] overrideStairWaypoints = null)
    {
        if (CurrentState != State.InsideTrain) return;
        if (overrideExitWaypoint   != null) exitWaypoint   = overrideExitWaypoint;
        if (overrideStairWaypoints != null) stairWaypoints = overrideStairWaypoints;
        StartCoroutine(ExitCoroutine());
    }

    public void DespawnWithTrain()
    {
        if (CurrentState == State.Done) return;

        // Yalnızca gerçekten trene binmiş NPC'ler trenle birlikte yok olur.
        // Hâlâ peronda dolaşan / yürüyen NPC'leri silmek senaryoyu bozar —
        // bunun yerine wander moduna geri dönsünler ki sahnede kalmaya devam etsinler.
        if (CurrentState != State.InsideTrain)
        {
            // Trene yetişemeyen NPC: boarding'i bırak, wandering'e dön
            StopAllCoroutines();
            transform.SetParent(null, worldPositionStays: true);
            EnableNav(true);
            if (wanderer != null)
            {
                wanderer.externalControl = false;
                wanderer.externalSpeed   = 0f;
                wanderer.freezeAnimation = false;
            }
            CurrentState = State.Wandering;
            return;
        }

        CurrentState = State.Done;
        StopAllCoroutines();
        StartCoroutine(FadeAndDestroy());
    }

    // ── Coroutine: Binme ─────────────────────────────────────────────────
    private IEnumerator BoardingCoroutine()
    {
        CurrentState = State.WalkToBoard;

        // Wanderer'ı dış kontrolde tut — locomotion durur, animasyon devam eder
        if (wanderer != null)
        {
            wanderer.externalControl = true;
            wanderer.externalSpeed   = walkSpeed;
            wanderer.freezeAnimation = false;
        }

        nav = GetComponent<NavMeshAgent>();

        // Hedefe anında dön
        Vector3 toBoard = boardingPoint.position - transform.position;
        toBoard.y = 0f;
        if (toBoard.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(toBoard.normalized);

        EnableNav(true);
        if (nav != null) nav.speed = walkSpeed;
        if (nav != null && nav.isOnNavMesh) nav.SetDestination(boardingPoint.position);

        // Yürüme döngüsü — NavMesh varsa onunla, yetmediği yerde DirectStep ile devam et.
        // Stuck-detection: 1.5 saniye boyunca hız çok düşükse direkt adım moduna geç.
        float stuckTimer = 0f;
        bool  forcedDirect = false;
        while (true)
        {
            Vector3 destination = boardingPoint.position;
            if (FlatDist(transform.position, destination) <= arrivalRadius) break;

            RotateToward(destination);

            bool navOn = nav != null && nav.enabled && nav.isOnNavMesh;

            // NavMesh hedefe ulaşamıyorsa (Partial path → tren içi NavMesh'siz)
            // ya da NPC bir yere takılıp kalmışsa → DirectStep'e geç.
            bool partialPath = navOn && nav.pathStatus == NavMeshPathStatus.PathPartial;
            bool stuck       = navOn && nav.velocity.sqrMagnitude < 0.04f; // <0.2 m/s
            if (stuck) stuckTimer += Time.deltaTime; else stuckTimer = 0f;

            if (!forcedDirect && (partialPath || stuckTimer > 1.5f))
                forcedDirect = true;

            if (navOn && !forcedDirect)
            {
                nav.SetDestination(destination);
                if (wanderer != null)
                    wanderer.externalSpeed = Mathf.Max(nav.velocity.magnitude, 0.4f);
            }
            else
            {
                // NavMesh'i kapat ki agent direkt hareketi engellemesin
                if (navOn) { nav.ResetPath(); nav.enabled = false; }
                DirectStep(destination, walkSpeed);
                if (wanderer != null) wanderer.externalSpeed = walkSpeed;
            }

            yield return null;
        }

        // Trene parent et — NPC trenle birlikte hareket eder
        Transform parent = boardingTrainTransform
                        ?? (boardingPoint.parent != null ? boardingPoint.parent : boardingPoint);
        transform.SetParent(parent, worldPositionStays: true);
        EnableNav(false);

        // boardingPoint'in baktığı yöne dön
        Vector3 faceDir = boardingPoint.forward;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(faceDir);

        // Trenin içinde: kemik animasyonu da dur (hareketsiz yolcu)
        if (wanderer != null)
        {
            wanderer.externalSpeed   = 0f;
            wanderer.freezeAnimation = true;
        }
        CurrentState = State.InsideTrain;
    }

    // ── Coroutine: Çıkış + Merdiven ──────────────────────────────────────
    private IEnumerator ExitCoroutine()
    {
        // Trenden ayrıl
        transform.SetParent(null, worldPositionStays: true);
        CurrentState = State.Exiting;

        if (wanderer != null)
        {
            wanderer.freezeAnimation = false;
            wanderer.externalControl = true;
            wanderer.externalSpeed   = walkSpeed;
        }

        EnableNav(true);
        if (nav != null) nav.speed = walkSpeed;

        // Peron çıkış noktasına git
        if (exitWaypoint != null)
        {
            if (nav != null && nav.isOnNavMesh) nav.SetDestination(exitWaypoint.position);
            yield return WalkUntilArrival(exitWaypoint.position, walkSpeed);
        }

        // Merdiven waypoint'lerini sırayla geç
        if (stairWaypoints != null && stairWaypoints.Length > 0)
        {
            CurrentState = State.ClimbStair;
            if (nav != null) nav.speed = stairSpeed;
            if (wanderer != null) wanderer.externalSpeed = stairSpeed;

            foreach (Transform wp in stairWaypoints)
            {
                if (wp == null) continue;
                if (nav != null && nav.isOnNavMesh) nav.SetDestination(wp.position);
                yield return WalkUntilArrival(wp.position, stairSpeed);
            }
        }

        CurrentState = State.Done;
        yield return FadeAndDestroy();
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────

    private IEnumerator WalkUntilArrival(Vector3 destination, float speed)
    {
        float stuckTimer = 0f;
        bool  forcedDirect = false;

        while (FlatDist(transform.position, destination) > arrivalRadius)
        {
            RotateToward(destination);

            bool navOn       = nav != null && nav.enabled && nav.isOnNavMesh;
            bool partialPath = navOn && nav.pathStatus == NavMeshPathStatus.PathPartial;
            bool stuck       = navOn && nav.velocity.sqrMagnitude < 0.04f;
            if (stuck) stuckTimer += Time.deltaTime; else stuckTimer = 0f;

            if (!forcedDirect && (partialPath || stuckTimer > 1.5f))
                forcedDirect = true;

            if (navOn && !forcedDirect)
            {
                if (wanderer != null)
                    wanderer.externalSpeed = Mathf.Max(nav.velocity.magnitude, 0.4f);
            }
            else
            {
                if (navOn) { nav.ResetPath(); nav.enabled = false; }
                DirectStep(destination, speed);
                if (wanderer != null) wanderer.externalSpeed = speed;
            }

            yield return null;
        }
    }

    private void RotateToward(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir.normalized),
                8f * Time.deltaTime);
    }

    private void DirectStep(Vector3 target, float speed)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir.normalized),
                10f * Time.deltaTime);
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    private void EnableNav(bool active)
    {
        if (nav == null) return;
        if (active)
        {
            if (!nav.isOnNavMesh &&
                NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
        }
        nav.enabled = active;
    }

    // ── Fade + Destroy ───────────────────────────────────────────────────
    private IEnumerator FadeAndDestroy()
    {
        // Renderer materyallerini transparency'ye al
        float[] startAlphas = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material mat = renderers[i].material;
            Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
            startAlphas[i] = c.a;
            // URP Lit: _Surface=1 transparent. Standart shader'da da harmsız.
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

        float elapsed = 0f;
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
