using System.Collections;
using UnityEngine;

/// <summary>
/// Metro treni kontrolcüsü.
/// Tren tünelden gelir, istasyonda durur, kapılar açılır/kapanır, tren gider.
/// "Static Mode" açıksa tren sabit durur (animasyon yok).
/// </summary>
public class SubwayTrainController : MonoBehaviour
{
    [Header("Mod")]
    [Tooltip("İşaretliyse tren sabit durur, hareket etmez.")]
    public bool staticMode = true;

    [Header("Kapı Objeleri")]
    [Tooltip("Trende ayrı obje olan kapı transformları (sol/sağ kapılar vb.)")]
    public Transform[] doors;

    [Tooltip("Kapı açık rotasyonu (Y ekseni derece)")]
    [Range(0f, 120f)] public float doorOpenAngle = 90f;

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

    [Tooltip("Kapılar açık kalma süresi (saniye)")]
    public float doorOpenDuration = 5f;

    [Tooltip("İstasyona ulaşmadan önce bekleme süresi")]
    public float initialDelay = 2f;

    // Dahili
    private Quaternion[] doorClosedRotations;
    private float        lockedY; // Trenin ray üzerindeki Y pozisyonu korunur

    private void Start()
    {
        // Kapı başlangıç rotasyonlarını kaydet
        if (doors != null && doors.Length > 0)
        {
            doorClosedRotations = new Quaternion[doors.Length];
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null)
                    doorClosedRotations[i] = doors[i].localRotation;
            }
        }

        if (staticMode)
        {
            // Sabit mod: sadece durur
            return;
        }

        // Trenin mevcut Y'sini kilitle — ray hizalaması korunur
        lockedY = transform.position.y;

        StartCoroutine(TrainRoutine());
    }

    private IEnumerator TrainRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            // Tren mevcut pozisyonundan hareket eder, ışınlama yok

            if (stopPoint != null)
                yield return MoveTo(stopPoint.position, arrivalSpeed);

            // 2) Kapıları aç
            yield return SetDoors(open: true, duration: 0.8f);

            // 3) Kapılar açık bekle
            yield return new WaitForSeconds(doorOpenDuration);

            // 4) Kapıları kapat
            yield return SetDoors(open: false, duration: 0.8f);

            // 5) Exit'e git
            if (exitPoint != null)
                yield return MoveTo(exitPoint.position, departureSpeed);

            // 6) Start'a geri dön (tünel arkasına), sonra tekrarla
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
        target = WithLockedY(target); // Y'yi asla değiştirme
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    private IEnumerator SetDoors(bool open, float duration)
    {
        if (doors == null || doors.Length == 0) yield break;

        float elapsed = 0f;
        Quaternion[] startRots = new Quaternion[doors.Length];
        Quaternion[] targetRots = new Quaternion[doors.Length];

        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] == null) continue;
            startRots[i] = doors[i].localRotation;

            if (open)
            {
                // Her kapıya uygun açılma yönü: çift kapılarda i çift = sol, tek = sağ
                float sign = (i % 2 == 0) ? 1f : -1f;
                targetRots[i] = doorClosedRotations[i] * Quaternion.Euler(0f, sign * doorOpenAngle, 0f);
            }
            else
            {
                targetRots[i] = doorClosedRotations[i];
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null)
                    doors[i].localRotation = Quaternion.Slerp(startRots[i], targetRots[i], t);
            }
            yield return null;
        }
    }

    // Inspector'dan test etmek için
    [ContextMenu("Kapıları Aç")]
    private void DebugOpenDoors() => StartCoroutine(SetDoors(open: true, duration: 0.8f));

    [ContextMenu("Kapıları Kapat")]
    private void DebugCloseDoors() => StartCoroutine(SetDoors(open: false, duration: 0.8f));
}
