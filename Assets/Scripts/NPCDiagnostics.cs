using UnityEngine;

/// <summary>
/// VR build teşhis aracı — sahnedeki tüm NPC'lerin durumunu periyodik olarak loglar.
/// Anormallik (yatık rotation, havada/yer altında pozisyon, bozuk scale, beklenmedik parent)
/// tespit edince [NPCDiag] prefix'iyle uyarı basar.
///
/// Kurulum: Sahnede boş bir GameObject'e ekle (örn. TrainPassengerDirector'ın yanına).
/// logcat'te filtre: adb logcat -s Unity | grep NPCDiag
/// </summary>
public class NPCDiagnostics : MonoBehaviour
{
    [Tooltip("Kaç saniyede bir tam rapor bassın")]
    public float reportInterval = 3f;

    [Tooltip("Bu açıdan fazla yan/öne eğikse 'yatık' say (derece)")]
    public float tiltThreshold = 30f;

    [Tooltip("Bu Y değerinin altı/üstü 'anormal pozisyon' (metre). Hidden -10000 hariç tutulur.")]
    public float minY = -2f;
    public float maxY = 6f;

    private float timer;
    private NPCTrainPassenger[] passengers;
    private NPCScene3Wanderer[] wanderers;

    private void Start()
    {
        passengers = FindObjectsByType<NPCTrainPassenger>(FindObjectsSortMode.None);
        wanderers  = FindObjectsByType<NPCScene3Wanderer>(FindObjectsSortMode.None);

        Debug.Log($"[NPCDiag] Başladı. {passengers.Length} NPCTrainPassenger, {wanderers.Length} NPCScene3Wanderer bulundu. " +
                  $"Platform: {Application.platform}, FPS hedef: {Application.targetFrameRate}");

        // Her NPC'nin başlangıç durumunu logla
        foreach (var p in passengers)
        {
            if (p == null) continue;
            LogNpcDetail(p, "START");
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < reportInterval) return;
        timer = 0f;

        int anomalyCount = 0;
        foreach (var p in passengers)
        {
            if (p == null) continue;
            if (IsAnomalous(p, out string reason))
            {
                anomalyCount++;
                LogNpcDetail(p, "ANOMALY: " + reason);
            }
        }

        if (anomalyCount == 0)
            Debug.Log($"[NPCDiag] OK — anormal NPC yok. FPS: {1f / Mathf.Max(0.0001f, Time.smoothDeltaTime):F0}");
        else
            Debug.LogWarning($"[NPCDiag] {anomalyCount} anormal NPC! FPS: {1f / Mathf.Max(0.0001f, Time.smoothDeltaTime):F0}");
    }

    private bool IsAnomalous(NPCTrainPassenger p, out string reason)
    {
        reason = "";
        Transform t = p.transform;

        // Sahne dışına gizlenmiş NPC'ler normal (hidden state)
        if (t.position.y < -1000f)
            return false;

        // Eğiklik kontrolü (X ve Z euler — yatık/öne kapaklı)
        float xTilt = Mathf.DeltaAngle(0f, t.eulerAngles.x);
        float zTilt = Mathf.DeltaAngle(0f, t.eulerAngles.z);
        if (Mathf.Abs(xTilt) > tiltThreshold || Mathf.Abs(zTilt) > tiltThreshold)
        {
            reason = $"TILT x={xTilt:F0} z={zTilt:F0}";
            return true;
        }

        // Pozisyon kontrolü
        if (t.position.y < minY || t.position.y > maxY)
        {
            reason = $"BAD_Y y={t.position.y:F2}";
            return true;
        }

        // Scale kontrolü
        Vector3 s = t.localScale;
        if (Mathf.Abs(s.x - 1f) > 0.1f || Mathf.Abs(s.y - 1f) > 0.1f || Mathf.Abs(s.z - 1f) > 0.1f)
        {
            reason = $"BAD_SCALE {s.x:F2},{s.y:F2},{s.z:F2}";
            return true;
        }

        // NaN kontrolü
        if (float.IsNaN(t.position.x) || float.IsNaN(t.rotation.x))
        {
            reason = "NaN";
            return true;
        }

        return false;
    }

    private void LogNpcDetail(NPCTrainPassenger p, string tag)
    {
        Transform t = p.transform;
        string parentName = t.parent != null ? t.parent.name : "(none)";
        var wanderer = p.GetComponent<NPCScene3Wanderer>();
        var nav = p.GetComponent<UnityEngine.AI.NavMeshAgent>();

        string navInfo = nav != null
            ? $"nav[en={nav.enabled},onMesh={(nav.enabled && nav.isOnNavMesh)}]"
            : "nav[null]";

        string wInfo = wanderer != null
            ? $"wander[ext={wanderer.externalControl},frz={wanderer.freezeAnimation},spd={wanderer.externalSpeed:F1}]"
            : "wander[null]";

        Debug.Log($"[NPCDiag] {p.name} | {tag} | state={p.CurrentState} | " +
                  $"pos=({t.position.x:F1},{t.position.y:F1},{t.position.z:F1}) | " +
                  $"euler=({t.eulerAngles.x:F0},{t.eulerAngles.y:F0},{t.eulerAngles.z:F0}) | " +
                  $"scale=({t.localScale.x:F2},{t.localScale.y:F2},{t.localScale.z:F2}) | " +
                  $"parent={parentName} | {navInfo} | {wInfo}");
    }
}
