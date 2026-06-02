using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene 3 NPC-Tren koordinatörü.
///
/// Train 1 (Train_Prefab): NPC'ler 1. durakta train1BoardingPath ile biner →
///   tren gider → 2. durakta train1ExitPath ile iner ve merdivene çıkar → fade out
/// Train 2 (Train_Prefab 2): NPC'ler train2BoardingPath ile biner → trenle gider → fade out
///
/// Path'ler waypoint zincirleridir — NPC NavMesh kullanmaz, sadece waypoint'leri
/// sırayla yürür. Bu yüzden kullanıcı yolu istediği gibi (kapıdan içeri vs.) çizebilir.
///
/// Tren kapıları, tüm yolcular boarding spot'larına varana kadar açık kalır
/// (SubwayTrainController.waitForPassengers).
/// </summary>
public class TrainPassengerDirector : MonoBehaviour
{
    [Header("Trenler")]
    public SubwayTrainController train1;
    public SubwayTrainController train2;

    [Header("Biniş Noktaları (NPC'lerin tren içindeki spotları)")]
    [Tooltip("Train_Prefab içine yerleştirilmiş Empty objeler — 10 adet")]
    public Transform[] train1BoardingPoints;
    [Tooltip("Train_Prefab 2 içine yerleştirilmiş Empty objeler — 12 adet")]
    public Transform[] train2BoardingPoints;

    [Header("Biniş Yolları (Waypoint zincirleri — peron → kapı → tren içi)")]
    [Tooltip("Train 1 için biniş yolu — tüm Train 1 NPC'leri bu zinciri sırayla yürür")]
    public Transform[] train1BoardingPath;
    [Tooltip("Train 2 için biniş yolu — tüm Train 2 NPC'leri bu zinciri sırayla yürür")]
    public Transform[] train2BoardingPath;

    [Header("Train 1 İniş Yolu (Yeni — full waypoint chain)")]
    [Tooltip("Train 1 iniş + merdiven yolu (tren içi → peron → merdiven üstü). " +
             "Dolu ise aşağıdaki eski Exit Waypoint + Stair Waypoints YERİNE kullanılır.")]
    public Transform[] train1ExitPath;

    [Header("Eski Alanlar (Geriye Dönük — train1ExitPath boşsa kullanılır)")]
    [Tooltip("[Eski] Trenden indikten sonra önce gidilecek peron noktası")]
    public Transform exitWaypoint;
    [Tooltip("[Eski] Merdiven waypoint'leri: alt → üst (StairWP_0..3)")]
    public Transform[] stairWaypoints;

    [Header("NPC Grupları")]
    [Tooltip("Train 1'e binecek NPC'ler (10 adet)")]
    public NPCTrainPassenger[] train1Passengers;
    [Tooltip("Train 2'ye binecek NPC'ler (12 adet)")]
    public NPCTrainPassenger[] train2Passengers;

    [Header("NPC Cycle (sonsuz döngü)")]
    [Tooltip("True: NPC'ler tren ile gidince anında görünmez olur, " +
             "kısa süre sonra kendi konumlarına teleport olup tekrar görünür olurlar. " +
             "False: tek seferlik, NPC'ler kalıcı yok olur.")]
    public bool loopForever = true;
    [Tooltip("Tren NPC'lerle gittikten kaç saniye sonra NPC'ler tekrar konumlarında belirsin. " +
             "Tren bir sonraki döngüsünde durağa varmadan ÖNCE bitmiş olmalı (yoksa NPC'ler hazır olmaz).")]
    public float cycleResetDelay = 5f;
    [Tooltip("Reset sırasında her NPC arası gecikme (0 = hepsi aynı anda belirir)")]
    public float npcSpawnInterval = 0f;

    // İzleme: hangi tren şu an "tüm yolcular bindi mi?" diye bekleniyor
    private bool watchingTrain1Boarding = false;
    private bool watchingTrain2Boarding = false;
    // İzleme: hangi grup şu an "hepsi Done mı, reset zamanlandı mı?"
    private bool train1ResetScheduled = false;
    private bool train2ResetScheduled = false;

    private void Start()
    {
        // ── Train 1 için "full exit path" oluştur ─────────────────────────
        Transform[] train1FullExit = BuildTrain1ExitPath();

        // ── Train 1 NPC yapılandırması ───────────────────────────────────
        for (int i = 0; i < train1Passengers.Length; i++)
        {
            var npc = train1Passengers[i];
            if (npc == null) continue;
            npc.fate         = NPCTrainPassenger.Fate.ExitAndClimbStair;
            npc.boardingPath = train1BoardingPath;
            npc.exitPath     = train1FullExit;

            if (train1BoardingPoints != null && train1BoardingPoints.Length > 0)
                npc.boardingPoint = train1BoardingPoints[i % train1BoardingPoints.Length];
        }

        // ── Train 2 NPC yapılandırması ───────────────────────────────────
        for (int i = 0; i < train2Passengers.Length; i++)
        {
            var npc = train2Passengers[i];
            if (npc == null) continue;
            npc.fate         = NPCTrainPassenger.Fate.DespawnWithTrain;
            npc.boardingPath = train2BoardingPath;
            // Train 2 NPC'leri inmez → exitPath gerekmiyor

            if (train2BoardingPoints != null && train2BoardingPoints.Length > 0)
                npc.boardingPoint = train2BoardingPoints[i % train2BoardingPoints.Length];
        }

        // ── Trenleri yolcu bekleme moduna al ─────────────────────────────
        if (train1 != null) train1.waitForPassengers = true;
        if (train2 != null) train2.waitForPassengers = true;

        // ── Event aboneliği ──────────────────────────────────────────────
        // Her iki tren de aynı davranır: durakta NPC'ler biner, exit'te NPC'ler kaybolur, döngü tekrarlar
        if (train1 != null)
        {
            train1.OnArrivedAtStop += OnTrain1ArrivedAtStop;
            train1.OnReachedExit   += OnTrain1ReachedExit;
        }

        if (train2 != null)
        {
            train2.OnArrivedAtStop += OnTrain2ArrivedAtStop;
            train2.OnReachedExit   += OnTrain2ReachedExit;
        }
    }

    private Transform[] BuildTrain1ExitPath()
    {
        // train1ExitPath doluysa direkt kullan
        if (train1ExitPath != null && train1ExitPath.Length > 0)
            return train1ExitPath;

        // Yoksa eski alanlardan birleştir: exitWaypoint + stairWaypoints
        var list = new List<Transform>();
        if (exitWaypoint != null) list.Add(exitWaypoint);
        if (stairWaypoints != null) list.AddRange(stairWaypoints);
        return list.ToArray();
    }


    private void OnDestroy()
    {
        if (train1 != null)
        {
            train1.OnArrivedAtStop -= OnTrain1ArrivedAtStop;
            train1.OnReachedExit   -= OnTrain1ReachedExit;
        }

        if (train2 != null)
        {
            train2.OnArrivedAtStop -= OnTrain2ArrivedAtStop;
            train2.OnReachedExit   -= OnTrain2ReachedExit;
        }
    }

    // ── Her frame: boarding & reset tetikleyicileri ──────────────────────
    private void Update()
    {
        if (watchingTrain1Boarding && AllInState(train1Passengers, NPCTrainPassenger.State.InsideTrain))
        {
            watchingTrain1Boarding = false;
            train1?.SignalReadyToClose();
        }
        if (watchingTrain2Boarding && AllInState(train2Passengers, NPCTrainPassenger.State.InsideTrain))
        {
            watchingTrain2Boarding = false;
            train2?.SignalReadyToClose();
        }

        // Cycle: tüm NPC'ler Done state'inde mi → reset zamanla
        if (loopForever)
        {
            if (!train1ResetScheduled && train1Passengers != null && train1Passengers.Length > 0
                && AllInState(train1Passengers, NPCTrainPassenger.State.Done))
            {
                train1ResetScheduled = true;
                StartCoroutine(ResetGroupAfterDelay(train1Passengers, cycleResetDelay,
                    () => train1ResetScheduled = false));
            }
            if (!train2ResetScheduled && train2Passengers != null && train2Passengers.Length > 0
                && AllInState(train2Passengers, NPCTrainPassenger.State.Done))
            {
                train2ResetScheduled = true;
                StartCoroutine(ResetGroupAfterDelay(train2Passengers, cycleResetDelay,
                    () => train2ResetScheduled = false));
            }
        }
    }

    private static bool AllInState(NPCTrainPassenger[] passengers, NPCTrainPassenger.State state)
    {
        if (passengers == null || passengers.Length == 0) return false;
        foreach (var npc in passengers)
        {
            if (npc == null) continue;
            if (npc.CurrentState != state) return false;
        }
        return true;
    }

    private System.Collections.IEnumerator ResetGroupAfterDelay(
        NPCTrainPassenger[] group, float delay, System.Action onDone)
    {
        yield return new UnityEngine.WaitForSeconds(delay);

        // Her NPC arasında npcSpawnInterval kadar bekleyerek fade-in'leri stagger et
        for (int i = 0; i < group.Length; i++)
        {
            if (group[i] == null) continue;
            group[i].ResetForNextCycle();
            yield return new UnityEngine.WaitForSeconds(npcSpawnInterval);
        }

        // KRİTİK FIX: tüm NPC'ler Done state'inden çıkana kadar bekle, sonra flag'i serbest bırak
        // (Yoksa Update Done görüp tekrar reset zamanlar → sonsuz tekrar reset bug'ı)
        while (true)
        {
            bool anyStillDone = false;
            foreach (var npc in group)
            {
                if (npc == null) continue;
                if (npc.CurrentState == NPCTrainPassenger.State.Done)
                {
                    anyStillDone = true;
                    break;
                }
            }
            if (!anyStillDone) break;
            yield return null;
        }

        onDone?.Invoke();
    }

    // ── Train 1 & Train 2: aynı davranış ─────────────────────────────────
    // Her duruşta: wandering durumundaki tüm NPC'ler boarding'e başlar
    // Her exit'te: trene binmiş tüm NPC'ler kaybolur (trenle gitmiş sayılır)
    private void OnTrain1ArrivedAtStop(int stopCount)
    {
        foreach (var npc in train1Passengers)
            npc?.StartBoarding(train1.transform);
        watchingTrain1Boarding = true;
    }

    private void OnTrain1ReachedExit()
    {
        foreach (var npc in train1Passengers)
            npc?.DespawnWithTrain();
    }

    private void OnTrain2ArrivedAtStop(int stopCount)
    {
        foreach (var npc in train2Passengers)
            npc?.StartBoarding(train2.transform);
        watchingTrain2Boarding = true;
    }

    private void OnTrain2ReachedExit()
    {
        foreach (var npc in train2Passengers)
            npc?.DespawnWithTrain();
    }

    // ── Gizmo: Scene view'da path'leri göster ────────────────────────────
    private void OnDrawGizmos()
    {
        DrawPath(train1BoardingPath, new Color(0.2f, 0.6f, 1f, 0.9f));
        DrawPath(train2BoardingPath, new Color(0.5f, 0.8f, 0.2f, 0.9f));
        DrawPath(train1ExitPath != null && train1ExitPath.Length > 0
                 ? train1ExitPath
                 : BuildTrain1ExitPath(),
                 new Color(1f, 0.6f, 0.1f, 0.9f));
    }

    private static void DrawPath(Transform[] path, Color color)
    {
        if (path == null || path.Length == 0) return;
        Gizmos.color = color;
        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] == null) continue;
            Gizmos.DrawSphere(path[i].position, 0.18f);
            if (i > 0 && path[i - 1] != null)
                Gizmos.DrawLine(path[i - 1].position, path[i].position);
        }
    }
}
