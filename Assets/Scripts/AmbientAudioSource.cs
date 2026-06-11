using UnityEngine;

/// <summary>
/// Global ortam sesi — tüm sahneye eşit yayılır (mesafeden bağımsız 2D).
/// Şehir uğultusu, akşam ambiyansı, rüzgar gibi "her yerde duyulan" sesler için.
///
/// Kurulum: Boş bir GameObject'e ekle (örn. "StreetAmbience").
/// Clip'i Logic export'undan sürükle. Konum önemli değil (2D ses).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AmbientAudioSource : MonoBehaviour
{
    [Header("Ortam Sesi")]
    [Tooltip("Çalacak ambiyans dosyası. Logic export'unu sürükle. Boşsa sessiz.")]
    public AudioClip clip;

    [Range(0f, 1f)]
    [Tooltip("Ses seviyesi — ambiyans genelde düşük tutulur (0.2-0.4)")]
    public float volume = 0.3f;

    [Tooltip("Açılışta yumuşak fade-in süresi (saniye)")]
    public float fadeInDuration = 2f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.clip         = clip;
        source.loop         = true;
        source.playOnAwake  = false;
        source.spatialBlend = 0f;   // tam 2D — her yerde eşit
        source.volume       = 0f;   // fade-in için sıfırdan başla
    }

    private void Start()
    {
        if (clip != null)
        {
            source.Play();
            if (fadeInDuration > 0f)
                StartCoroutine(FadeIn());
            else
                source.volume = volume;
        }
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, volume, t / fadeInDuration);
            yield return null;
        }
        source.volume = volume;
    }

    private void OnValidate()
    {
        if (source == null) source = GetComponent<AudioSource>();
        if (source != null)
        {
            source.clip         = clip;
            source.loop         = true;
            source.spatialBlend = 0f;
        }
    }
}
