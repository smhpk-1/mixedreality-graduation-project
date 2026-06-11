using UnityEngine;

/// <summary>
/// Sokak ses kaynağı — herhangi bir objeye (lamba, dükkan, radyo, vending, kedi vb.) eklenir.
/// 3D pozisyonel ses ayarlarını otomatik yapar. Clip'i Inspector'dan sürükleyip atman yeterli;
/// loop, spatial blend, mesafe rolloff hazır gelir.
///
/// Logic Pro export'unu aldığında: clip alanına .wav'ı sürükle → çalışır.
/// Clip boşsa sessizdir, hata vermez (export bekliyorsun).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PositionalAudioSource : MonoBehaviour
{
    [Header("Ses")]
    [Tooltip("Çalacak ses dosyası. Logic export'unu buraya sürükle. Boşsa sessiz (hata yok).")]
    public AudioClip clip;

    [Tooltip("Sürekli döngü (ambient/vızıltı/hum için açık, tek seferlik efekt için kapalı)")]
    public bool loop = true;

    [Tooltip("Sahne başlar başlamaz otomatik çalsın mı")]
    public bool playOnStart = true;

    [Range(0f, 1f)]
    [Tooltip("Ses seviyesi")]
    public float volume = 0.7f;

    [Header("3D Mesafe")]
    [Tooltip("Bu mesafeye kadar ses tam seviyede (metre)")]
    public float minDistance = 1.5f;

    [Tooltip("Bu mesafeden sonra ses duyulmaz (metre). Küçük = lokal, büyük = geniş alan")]
    public float maxDistance = 12f;

    [Header("Çeşitlilik (opsiyonel)")]
    [Tooltip("Pitch'i hafif rastgele yap — aynı sesli çok obje varsa monotonluğu kırar")]
    public bool randomizePitch = false;
    [Range(0f, 0.3f)] public float pitchVariation = 0.08f;

    [Tooltip("Başlangıçta rastgele gecikme — birden çok loop sesi aynı anda başlamasın")]
    public bool randomStartDelay = true;
    [Range(0f, 3f)] public float maxStartDelay = 1.5f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        ConfigureSource();
    }

    private void Start()
    {
        if (playOnStart && clip != null)
        {
            float delay = randomStartDelay ? Random.Range(0f, maxStartDelay) : 0f;
            source.PlayDelayed(delay);
        }
    }

    private void ConfigureSource()
    {
        source.clip          = clip;
        source.loop          = loop;
        source.playOnAwake   = false;       // Start() kontrol ediyor
        source.volume        = volume;
        source.spatialBlend  = 1f;          // tam 3D
        source.rolloffMode   = AudioRolloffMode.Linear;
        source.minDistance   = minDistance;
        source.maxDistance   = maxDistance;
        source.dopplerLevel  = 0f;          // sokakta doppler istemeyiz

        if (randomizePitch)
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
    }

    /// <summary>Runtime'da clip değiştirip yeniden çalmak için.</summary>
    public void SetClipAndPlay(AudioClip newClip)
    {
        clip = newClip;
        if (source == null) source = GetComponent<AudioSource>();
        source.clip = newClip;
        if (newClip != null) source.Play();
    }

    // Inspector'da değer değişince (editor'da) ayarları canlı güncelle
    private void OnValidate()
    {
        if (source == null) source = GetComponent<AudioSource>();
        if (source != null)
        {
            source.clip         = clip;
            source.loop         = loop;
            source.volume       = volume;
            source.spatialBlend = 1f;
            source.rolloffMode  = AudioRolloffMode.Linear;
            source.minDistance  = minDistance;
            source.maxDistance  = maxDistance;
        }
    }

    // Scene view'da ses menzilini göster
    private void OnDrawGizmosSelected()
    {
        // İç çember — tam ses
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, minDistance);
        // Dış çember — duyulma sınırı
        Gizmos.color = new Color(0.9f, 0.7f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}
