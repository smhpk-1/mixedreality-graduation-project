using UnityEngine;

/// <summary>
/// Scene 3 NPC-Tren koordinatörü.
/// 
/// Atama:
///   train1Passengers → Train_Prefab ile gider, döner, Stair_1'den çıkar
///   train2Passengers → Train_Prefab 2 ile gider, sahneden kaybolur
///
/// Inspector kurulum:
///   1) train1 = Train_Prefab'in SubwayTrainController'ı
///   2) train2 = Train_Prefab 2'nin SubwayTrainController'ı
///   3) train1BoardingPoint / train2BoardingPoint = Tren içinde boş obje
///   4) exitWaypoint = Trenden indikten sonra gidilecek peron noktası
///   5) stairWaypoints = Stair_1 merdiven waypoint zinciri (alt → üst)
///   6) train1Passengers / train2Passengers = Sahnedeki NPC listesi
/// </summary>
public class TrainPassengerDirector : MonoBehaviour
{
    [Header("Trenler")]
    public SubwayTrainController train1; // Dönen tren (Exit + Stair)
    public SubwayTrainController train2; // Tek yön tren (Despawn)

    [Header("Biniş Noktaları")]
    [Tooltip("Train_Prefab içine yerleştirilmiş boş objeler — her NPC farklı noktaya gider")]
    public Transform[] train1BoardingPoints;
    [Tooltip("Train_Prefab 2 içine yerleştirilmiş boş objeler")]
    public Transform[] train2BoardingPoints;

    [Header("İniş & Merdiven (Train_Prefab grubu)")]
    [Tooltip("Trenden indikten sonra önce gidilecek peron noktası")]
    public Transform exitWaypoint;
    [Tooltip("Stair_1 merdiven waypoint'leri: alt → üst sırayla")]
    public Transform[] stairWaypoints;

    [Header("NPC Grupları")]
    public NPCTrainPassenger[] train1Passengers;
    public NPCTrainPassenger[] train2Passengers;

    // ── Başlangıç ────────────────────────────────────────────────────────
    private void Start()
    {
        // Train1 NPC'lerini yapılandır — her birine farklı boarding noktası
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

        // Train2 NPC'lerini yapılandır — her birine farklı boarding noktası
        for (int i = 0; i < train2Passengers.Length; i++)
        {
            var npc = train2Passengers[i];
            if (npc == null) continue;
            npc.fate = NPCTrainPassenger.Fate.DespawnWithTrain;

            if (train2BoardingPoints != null && train2BoardingPoints.Length > 0)
                npc.boardingPoint = train2BoardingPoints[i % train2BoardingPoints.Length];
        }

        // Train1 event'leri
        if (train1 != null)
        {
            train1.OnDoorsOpened  += OnTrain1DoorsOpened;
            train1.OnArrivedAtStop += OnTrain1ArrivedAtStop;
            train1.OnReachedExit  += OnTrain1ReachedExit;
        }

        // Train2 event'leri
        if (train2 != null)
        {
            train2.OnDoorsOpened  += OnTrain2DoorsOpened;
            train2.OnReachedExit  += OnTrain2ReachedExit;
        }
    }

    private void OnDestroy()
    {
        if (train1 != null)
        {
            train1.OnDoorsOpened   -= OnTrain1DoorsOpened;
            train1.OnArrivedAtStop -= OnTrain1ArrivedAtStop;
            train1.OnReachedExit   -= OnTrain1ReachedExit;
        }
        if (train2 != null)
        {
            train2.OnDoorsOpened  -= OnTrain2DoorsOpened;
            train2.OnReachedExit  -= OnTrain2ReachedExit;
        }
    }

    // ── Train 1 event handler'ları ────────────────────────────────────────

    /// <summary>
    /// Tren durdu → NPC'ler kapı açılmadan önce yürümeye başlasın (daha fazla süre).
    /// </summary>
    private void OnTrain1DoorsOpened()
    {
        // Kapı açıldığında boarding OnArrivedAtStop'ta zaten başladı, ek işlem yok.
    }

    /// <summary>
    /// Tren durağa vardı → NPC'ler hemen yürümeye başlasın (kapı açılmadan önce).
    /// </summary>
    private void OnTrain1ArrivedAtStop(int stopCount)
    {
        if (stopCount == 1)
        {
            foreach (var npc in train1Passengers)
                npc?.StartBoarding(train1.transform);
        }
    }

    /// <summary>
    /// Train1 çıkış noktasına ulaştı → tüm Train1 NPC'leri trenle birlikte kaybolur.
    /// </summary>
    private void OnTrain1ReachedExit()
    {
        foreach (var npc in train1Passengers)
            npc?.DespawnWithTrain();
    }

    // ── Train 2 event handler'ları ────────────────────────────────────────

    /// <summary>Train2 kapıları açtı → NPC'ler biner.</summary>
    private void OnTrain2DoorsOpened()
    {
        foreach (var npc in train2Passengers)
            npc?.StartBoarding(train2.transform);
    }

    /// <summary>Train2 exitPoint'e ulaştı → NPC'ler trenle kaybolur.</summary>
    private void OnTrain2ReachedExit()
    {
        foreach (var npc in train2Passengers)
            npc?.DespawnWithTrain();
    }
}
