using UnityEngine;

/// <summary>
/// Scene 3 NPC-Tren koordinatörü.
///
/// Train 1 (Train_Prefab): NPC'ler 1. durakta biner → tren gider → 2. durakta iner →
///                         TrainExitWaypoint → StairWP_0..3 → fade out
/// Train 2 (Train_Prefab 2): NPC'ler binince trenle birlikte exit'e gider → fade out
/// </summary>
public class TrainPassengerDirector : MonoBehaviour
{
    [Header("Trenler")]
    public SubwayTrainController train1;
    public SubwayTrainController train2;

    [Header("Biniş Noktaları")]
    [Tooltip("Train_Prefab içine yerleştirilmiş Empty objeler — 10 adet")]
    public Transform[] train1BoardingPoints;
    [Tooltip("Train_Prefab 2 içine yerleştirilmiş Empty objeler — 12 adet")]
    public Transform[] train2BoardingPoints;

    [Header("Train 1 İniş & Merdiven")]
    [Tooltip("Trenden indikten sonra önce gidilecek peron noktası")]
    public Transform exitWaypoint;
    [Tooltip("Merdiven waypoint'leri: alt → üst (StairWP_0..3)")]
    public Transform[] stairWaypoints;

    [Header("NPC Grupları")]
    [Tooltip("Train 1'e binecek NPC'ler (10 adet)")]
    public NPCTrainPassenger[] train1Passengers;
    [Tooltip("Train 2'ye binecek NPC'ler (12 adet)")]
    public NPCTrainPassenger[] train2Passengers;

    private void Start()
    {
        // ── Train 1 NPC yapılandırması ───────────────────────────────────
        for (int i = 0; i < train1Passengers.Length; i++)
        {
            var npc = train1Passengers[i];
            if (npc == null) continue;
            npc.fate           = NPCTrainPassenger.Fate.ExitAndClimbStair;
            npc.exitWaypoint   = exitWaypoint;
            npc.stairWaypoints = stairWaypoints;

            if (train1BoardingPoints != null && train1BoardingPoints.Length > 0)
                npc.boardingPoint = train1BoardingPoints[i % train1BoardingPoints.Length];
        }

        // ── Train 2 NPC yapılandırması ───────────────────────────────────
        for (int i = 0; i < train2Passengers.Length; i++)
        {
            var npc = train2Passengers[i];
            if (npc == null) continue;
            npc.fate = NPCTrainPassenger.Fate.DespawnWithTrain;

            if (train2BoardingPoints != null && train2BoardingPoints.Length > 0)
                npc.boardingPoint = train2BoardingPoints[i % train2BoardingPoints.Length];
        }

        // ── Event aboneliği ──────────────────────────────────────────────
        if (train1 != null)
            train1.OnArrivedAtStop += OnTrain1ArrivedAtStop;

        if (train2 != null)
        {
            train2.OnArrivedAtStop += OnTrain2ArrivedAtStop;
            train2.OnReachedExit   += OnTrain2ReachedExit;
        }
    }

    private void OnDestroy()
    {
        if (train1 != null)
            train1.OnArrivedAtStop -= OnTrain1ArrivedAtStop;

        if (train2 != null)
        {
            train2.OnArrivedAtStop -= OnTrain2ArrivedAtStop;
            train2.OnReachedExit   -= OnTrain2ReachedExit;
        }
    }

    // ── Train 1: iki duraklı senaryo ──────────────────────────────────────
    // 1. durak → biniş başlasın.   2. durak → iniş başlasın (merdivenden çıkış).
    private void OnTrain1ArrivedAtStop(int stopCount)
    {
        if (stopCount == 1)
        {
            foreach (var npc in train1Passengers)
                npc?.StartBoarding(train1.transform);
        }
        else if (stopCount == 2)
        {
            foreach (var npc in train1Passengers)
                npc?.StartExiting(exitWaypoint, stairWaypoints);
        }
    }

    // ── Train 2: tek yön senaryosu ────────────────────────────────────────
    private void OnTrain2ArrivedAtStop(int stopCount)
    {
        if (stopCount == 1)
        {
            foreach (var npc in train2Passengers)
                npc?.StartBoarding(train2.transform);
        }
    }

    private void OnTrain2ReachedExit()
    {
        foreach (var npc in train2Passengers)
            npc?.DespawnWithTrain();
    }
}
