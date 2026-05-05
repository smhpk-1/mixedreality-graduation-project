using UnityEngine;
using System.Collections;

/// <summary>
/// Metro sahnesindeki tek bir NPC'nin davranışını kontrol eder.
/// 
/// STATIC mod:   NPC'yi prefabın konumuna koyar, Idle animasyonu çalar.
/// ANIMATED mod: NPC belirtilen waypoint'ler arasında yürür.
/// 
/// KURULUM:
/// 1. NPC prefabına bu component'i ekle.
/// 2. Animator Controller ayarla (Mixamo / Unity Starter Assets).
///    Animator parametreleri:  float "Speed"  (0 = idle, 1 = yürüme)
/// 3. Mod'u Static ya da Animated olarak seç.
/// </summary>
public class NPCController : MonoBehaviour
{
    public enum NPCMode { Static, Animated }

    // ─── Genel ───────────────────────────────────────────────────────────────
    [Header("Mod")]
    public NPCMode mode = NPCMode.Static;

    // ─── Animasyon ───────────────────────────────────────────────────────────
    [Header("Animator (Animated mod için)")]
    [Tooltip("Animator bileşeni — boş bırakılırsa otomatik bulunur")]
    public Animator animator;
    [Tooltip("Animator'daki hız parametresinin adı")]
    public string speedParam = "Speed";

    // ─── Yürüme ──────────────────────────────────────────────────────────────
    [Header("Yürüme (Animated mod için)")]
    [Tooltip("NPC'nin gideceği noktalar. Boşsa sahnede ileri-geri yürür.")]
    public Transform[] waypoints;
    public float walkSpeed     = 1.2f;
    [Tooltip("Her waypoint'te bekleme süresi (saniye)")]
    public float waitAtPoint   = 2f;
    [Tooltip("Hedefe ne kadar yakınken \"ulaştı\" sayılır")]
    public float stoppingDist  = 0.3f;

    // ─── Bakma ───────────────────────────────────────────────────────────────
    [Header("Dönüş hızı")]
    public float rotationSpeed = 5f;

    // ─── Dahili durum ────────────────────────────────────────────────────────
    private int   currentWaypoint = 0;
    private bool  isWaiting       = false;
    private float animSpeed       = 0f;          // Animator'a gönderilen değer

    // ─── Başlangıç ───────────────────────────────────────────────────────────
    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (mode == NPCMode.Animated && (waypoints == null || waypoints.Length == 0))
        {
            // Waypoint atanmamışsa uyarı ver, Static'e geç
            Debug.LogWarning($"[NPCController] {name}: Animated mod seçildi ama waypoint yok. Static'e geçiliyor.");
            mode = NPCMode.Static;
        }

        SetAnimatorSpeed(mode == NPCMode.Static ? 0f : 1f);
    }

    // ─── Her kare ─────────────────────────────────────────────────────────────
    private void Update()
    {
        if (mode == NPCMode.Animated && !isWaiting)
            WalkToCurrentWaypoint();
    }

    // ─── Yürüme mantığı ──────────────────────────────────────────────────────
    private void WalkToCurrentWaypoint()
    {
        Transform target = waypoints[currentWaypoint];
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        float dist = dir.magnitude;

        if (dist < stoppingDist)
        {
            // Waypoint'e ulaşıldı
            StartCoroutine(WaitThenAdvance());
            return;
        }

        // Hareket
        transform.position += dir.normalized * walkSpeed * Time.deltaTime;

        // Dönüş
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

        SetAnimatorSpeed(1f);
    }

    private IEnumerator WaitThenAdvance()
    {
        isWaiting = true;
        SetAnimatorSpeed(0f);               // Idle'a geç
        yield return new WaitForSeconds(waitAtPoint);

        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        isWaiting = false;
        SetAnimatorSpeed(1f);               // Tekrar yürü
    }

    // ─── Yardımcılar ─────────────────────────────────────────────────────────
    private void SetAnimatorSpeed(float speed)
    {
        animSpeed = speed;
        if (animator != null && animator.isActiveAndEnabled)
            animator.SetFloat(speedParam, animSpeed);
    }

    // ─── Editor yardımı: waypoint'leri gizmos ile göster ────────────────────
    private void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.15f);
            if (i + 1 < waypoints.Length && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}
