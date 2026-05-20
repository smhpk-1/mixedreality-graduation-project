using System.Collections;
using UnityEngine;

/// <summary>
/// Metro treni kontrolcüsü.
/// Tren tünelden gelir, istasyonda durur, kapılar kayarak açılır/kapanır, tren gider.
/// "Static Mode" açıksa tren sabit durur (animasyon yok).
/// </summary>
public class SubwayTrainController : MonoBehaviour
{
    [Header("Mod")]
    [Tooltip("İşaretliyse tren sabit durur, hareket etmez.")]
    public bool staticMode = false;

    [Header("Kayan Kapılar")]
    [Tooltip("Sol taraftaki kapı panelleri — açılınca slideLocalAxis negatif yönde kayar")]
    public Transform[] leftDoors;

    [Tooltip("Sağ taraftaki kapı panelleri — açılınca slideLocalAxis pozitif yönde kayar")]
    public Transform[] rightDoors;

    [Tooltip("Kapının kayacağı yerel eksen (genellikle Vector3.right = X ekseni)")]
    public Vector3 slideLocalAxis = Vector3.right;

    [Tooltip("Kapının ne kadar kayacağı (metre)")]
    public float slideDistance = 0.7f;

    [Tooltip("Kapı açılma/kapanma animasyon süresi (saniye)")]
    public float doorAnimDuration = 0.6f;

    [Header("Hareket (Static Mode kapalıyken)")]
    [Tooltip("Trenin başlangıç noktası (tünel girişi)")]
    public Transform startPoint;

    [Tooltip("Trenin duracağı nokta (peron)")]
    public Transform stopPoint;

    [Tooltip("Trenin çıkış noktası (karşı tünel)")]
    public Transform exitPoint;

    [Tooltip("İstasyona giriş hızı")]
    public float arrivalSpeed = 8f;

    [Tooltip("İstasyondan çıkış hızı")]
    public float departureSpeed = 10f;

    [Tooltip("Kapılar açık kalma süresi — minimum bekleme süresi. waitForPassengers açıksa bu süreden sonra da yolcuları bekler.")]
    public float doorOpenDuration = 4f;

    [Tooltip("İstasyona ulaşmadan önce bekleme süresi")]
    public float initialDelay = 2f;

    [Header("Yolcu Bekleme Modu")]
    [Tooltip("Açıksa: kapılar açıldıktan sonra Director SignalReadyToClose() çağırana kadar bekler. Kapalıysa: sadece doorOpenDuration kadar bekler.")]
    public bool waitForPassengers = false;

    [Tooltip("waitForPassengers açıkken maksimum bekleme süresi (saniye). Bu süreyi aşarsa tren yine hareket eder.")]
    public float maxPassengerWaitTime = 60f;

    private bool readyToClose = false;

    /// <summary>Director çağırır: tüm yolcular bindi, tren kapanıp gidebilir.</summary>
    public void SignalReadyToClose()
    {
        readyToClose = true;
    }

    // ── Eventler ─────────────────────────────────────────────────────────
    /// <summary>Tren istasyona ulaştığında. Parametre: kaçıncı duruş (1'den başlar).</summary>
    public event System.Action<int> OnArrivedAtStop;
    /// <summary>Kapılar tamamen açıldığında.</summary>
    public event System.Action OnDoorsOpened;
    /// <summary>Kapılar kapanmaya başlamadan hemen önce.</summary>
    public event System.Action OnDoorsClosing;
    /// <summary>Kapılar kapandı, tren hareket edecek.</summary>
    public event System.Action OnDeparted;
    /// <summary>Tren exitPoint'e ulaştı (NPC'ler için despawn anı).</summary>
    public event System.Action OnReachedExit;

    /// <summary>Kaçıncı kez istasyona durduğunu gösterir (1 = ilk geliş).</summary>
    public int StopCount { get; private set; }

    // Dahili
    private Vector3[] leftDoorClosedPositions;
    private Vector3[] rightDoorClosedPositions;
    private float     lockedY;

    private void Start()
    {
        // Kapı kapalı pozisyonlarını kaydet
        SaveDoorPositions();

        if (staticMode) return;

        lockedY = transform.position.y;
        StartCoroutine(TrainRoutine());
    }

    private void SaveDoorPositions()
    {
        if (leftDoors != null)
        {
            leftDoorClosedPositions = new Vector3[leftDoors.Length];
            for (int i = 0; i < leftDoors.Length; i++)
                if (leftDoors[i] != null)
                    leftDoorClosedPositions[i] = leftDoors[i].localPosition;
        }
        if (rightDoors != null)
        {
            rightDoorClosedPositions = new Vector3[rightDoors.Length];
            for (int i = 0; i < rightDoors.Length; i++)
                if (rightDoors[i] != null)
                    rightDoorClosedPositions[i] = rightDoors[i].localPosition;
        }
    }

    private IEnumerator TrainRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            // 1) İstasyona gel
            if (stopPoint != null)
                yield return MoveTo(stopPoint.position, arrivalSpeed);

            StopCount++;
            OnArrivedAtStop?.Invoke(StopCount);

            // 2) Kapıları aç
            yield return SlideDoors(open: true);
            OnDoorsOpened?.Invoke();

            // 3) Minimum bekleme (NPC'lerin yola çıkması için)
            yield return new WaitForSeconds(doorOpenDuration);

            // 4) Yolcu bekleme modu: Director SignalReadyToClose() çağırana kadar bekle
            if (waitForPassengers)
            {
                readyToClose = false;
                float waited = 0f;
                while (!readyToClose && waited < maxPassengerWaitTime)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
                readyToClose = false; // sonraki durak için sıfırla
            }

            // 5) Kapılar kapanmadan önce NPC'leri uyar
            OnDoorsClosing?.Invoke();
            yield return SlideDoors(open: false);

            // 5) Kapılar kapandıktan 1 saniye sonra hareket et
            yield return new WaitForSeconds(1f);
            OnDeparted?.Invoke();

            // 6) Exit'e git
            if (exitPoint != null)
                yield return MoveTo(exitPoint.position, departureSpeed);

            OnReachedExit?.Invoke();

            // 7) Start'a geri dön (tünel arkasına), sonra tekrarla
            if (startPoint != null)
                yield return MoveTo(startPoint.position, departureSpeed);

            yield return new WaitForSeconds(initialDelay);
        }
    }

    private Vector3 WithLockedY(Vector3 target)
    {
        return new Vector3(target.x, lockedY, target.z);
    }

    private IEnumerator MoveTo(Vector3 target, float speed)
    {
        target = WithLockedY(target);
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    /// <summary>
    /// Sol kapılar slideLocalAxis negatif yönde, sağ kapılar pozitif yönde kayar.
    /// </summary>
    private IEnumerator SlideDoors(bool open)
    {
        bool hasLeft  = leftDoors  != null && leftDoors.Length  > 0;
        bool hasRight = rightDoors != null && rightDoors.Length > 0;
        if (!hasLeft && !hasRight) yield break;

        Vector3 leftOffset  = -slideLocalAxis.normalized * slideDistance;
        Vector3 rightOffset =  slideLocalAxis.normalized * slideDistance;

        Vector3[] leftStart   = new Vector3[hasLeft  ? leftDoors.Length  : 0];
        Vector3[] rightStart  = new Vector3[hasRight ? rightDoors.Length : 0];
        Vector3[] leftTarget  = new Vector3[leftStart.Length];
        Vector3[] rightTarget = new Vector3[rightStart.Length];

        for (int i = 0; i < leftStart.Length; i++)
        {
            if (leftDoors[i] == null) continue;
            leftStart[i]  = leftDoors[i].localPosition;
            leftTarget[i] = open ? leftDoorClosedPositions[i] + leftOffset
                                 : leftDoorClosedPositions[i];
        }
        for (int i = 0; i < rightStart.Length; i++)
        {
            if (rightDoors[i] == null) continue;
            rightStart[i]  = rightDoors[i].localPosition;
            rightTarget[i] = open ? rightDoorClosedPositions[i] + rightOffset
                                  : rightDoorClosedPositions[i];
        }

        float elapsed = 0f;
        while (elapsed < doorAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / doorAnimDuration));

            for (int i = 0; i < leftStart.Length; i++)
                if (leftDoors[i] != null)
                    leftDoors[i].localPosition = Vector3.Lerp(leftStart[i], leftTarget[i], t);

            for (int i = 0; i < rightStart.Length; i++)
                if (rightDoors[i] != null)
                    rightDoors[i].localPosition = Vector3.Lerp(rightStart[i], rightTarget[i], t);

            yield return null;
        }
    }

    // Inspector'dan Play modunda test etmek için
    [ContextMenu("Kapıları Aç")]
    private void DebugOpenDoors()
    {
        SaveDoorPositions();
        StartCoroutine(SlideDoors(open: true));
    }

    [ContextMenu("Kapıları Kapat")]
    private void DebugCloseDoors() => StartCoroutine(SlideDoors(open: false));
}
