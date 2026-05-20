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

    public enum State
    {
        Wandering, WalkToBoard, InsideTrain, Exiting, Done
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
        nav       = GetComponent<NavMeshAgent>();
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

    public void StartExiting(Transform[] overrideExitPath = null)
    {
        if (CurrentState != State.InsideTrain) return;
        if (overrideExitPath != null) exitPath = overrideExitPath;
        StartCoroutine(ExitCoroutine());
    }

    public void DespawnWithTrain()
    {
        if (CurrentState == State.Done) return;

        // Sadece trene bindiyse fade-out. Hâlâ yoldaysa wandering'e geri dön.
        if (CurrentState != State.InsideTrain)
        {
            StopAllCoroutines();
            transform.SetParent(null, worldPositionStays: true);
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

        CurrentState = State.Done;
        StopAllCoroutines();
        StartCoroutine(FadeAndDestroy());
    }

    // ── Coroutine: Binme ─────────────────────────────────────────────────
    private IEnumerator BoardingCoroutine()
    {
        CurrentState = State.WalkToBoard;

        // Wanderer'ı dış kontrole al; NavMesh'i kapat (path-based yürüyüş)
        if (wanderer != null)
        {
            wanderer.externalControl = true;
            wanderer.externalSpeed   = walkSpeed;
            wanderer.freezeAnimation = false;
        }
        if (nav != null) nav.enabled = false;

        // 1) Biniş yolu waypoint'lerini sırayla yürü
        if (boardingPath != null)
        {
            for (int i = 0; i < boardingPath.Length; i++)
            {
                if (boardingPath[i] == null) continue;
                yield return WalkToWorld(boardingPath[i], walkSpeed);
            }
        }

        // 2) Son adım: kendi boarding spot'una
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
        yield return FadeAndDestroy();
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
        // Y'yi koruyarak XZ düzleminde hareket et (yokuş/merdiven NPC'yi havada bırakmasın
        // — merdiven waypoint'lerinin Y'leri sahnede doğru ayarlanmış olmalı, NPC waypoint
        // Y'sini takip eder)
        Vector3 step = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        transform.position = step;
    }

    // ── Yardımcı: Fade + Destroy ─────────────────────────────────────────
    private IEnumerator FadeAndDestroy()
    {
        float[] startAlphas = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material mat = renderers[i].material;
            Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
            startAlphas[i] = c.a;
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
