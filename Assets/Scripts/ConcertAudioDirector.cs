using UnityEngine;
using System.Collections;

/// <summary>
/// Hybrid concert audio: bazı stem'ler 2D stereo (kulaklıktan direkt),
/// bazıları sahnedeki bir GameObject'ten 3D spatial olarak gelir.
/// Tüm stem'ler DSP clock ile senkron başlar.
/// </summary>
public class ConcertAudioDirector : MonoBehaviour
{
    public enum AudioMode { Stereo2D, Spatial3D }

    [System.Serializable]
    public class Stem
    {
        public string      label;
        public AudioClip   clip;
        [Range(0f, 1f)]
        public float       volume     = 1f;
        public AudioMode   mode       = AudioMode.Stereo2D;

        [Tooltip("Sadece Spatial3D modunda: sesin geldiği sahne objesi (speaker, enstrüman vb.)")]
        public GameObject  sourceObject;

        [Tooltip("Spatial3D: sesin tam volume duyulduğu mesafe (metre) — bu mesafeye kadar ses kısılmaz")]
        public float       minDistance = 8f;

        [Tooltip("Spatial3D: sesin tamamen kesileceği maksimum mesafe (metre)")]
        public float       maxDistance = 60f;

        // Runtime'da oluşturulur
        [System.NonSerialized] public AudioSource runtimeSource;
        [System.NonSerialized] public float normGain = 1f; // RMS eşitleme çarpanı
    }

    [Header("Stem'ler")]
    public Stem[] stems;

    [Header("Ayarlar")]
    public bool  playOnAwake = true;
    public float startDelay  = 0f;

    [Header("Master (loudness pass)")]
    [Tooltip("Sahne master'ı — tüm stem'lere uygulanır. Sahneler arası denge için trim.")]
    [Range(0f, 2f)] public float masterVolume = 1f;

    [Tooltip("Stem'leri yüklerken ortak RMS hedefine eşitle (ölçülen yayılım 14 dB'di: " +
             "drum −18.7 / bass −24.3 / FX −32.4 LUFS). stem.volume bu eşit zeminin üstünde offset olur.")]
    public bool normalizeStems = true;

    [Header("Ducking")]
    [Tooltip("Duck hedefine yumuşak geçiş süresi (saniye). Sequencer doldukça konser kısılır.")]
    public float duckSmoothTime = 1.5f;

    // Sequencer dolduğunda konser yerini oyuncunun loop'una bırakır:
    // her dolu slot konseri 1/12 kısar, 12 slotta konser tamamen susar.
    private float duckTarget  = 1f;
    private float duckCurrent = 1f;
    private bool  fadingOut;

    /// <summary>
    /// Sahnedeki grubun "çalma" şiddeti (0-1): duck ve final fade dahil.
    /// NPCMusicianPerformer bunu okur — müzik kısıldıkça grup yavaşlar, durur.
    /// </summary>
    public float PerformanceLevel { get; private set; } = 1f;

    private void Awake()
    {
        foreach (var stem in stems)
        {
            GameObject target = (stem.mode == AudioMode.Spatial3D && stem.sourceObject != null)
                ? stem.sourceObject
                : gameObject; // 2D stem'ler bu objeye eklenir

            // Loudness pass: her stem ortak RMS zeminine eşitlenir
            stem.normGain = normalizeStems ? AudioMasterUtil.NormalizationGain(stem.clip) : 1f;

            var src = target.AddComponent<AudioSource>();
            src.clip        = stem.clip;
            src.volume      = EffectiveVolume(stem);
            src.loop        = false;
            src.playOnAwake = false;
            src.priority    = 0;

            if (stem.mode == AudioMode.Stereo2D)
            {
                src.spatialBlend = 0f;
                src.spatialize   = false;
            }
            else
            {
                src.spatialBlend   = 1f;
                src.spatialize     = true;
                src.rolloffMode    = AudioRolloffMode.Linear; // daha geniş alanda eşit ses
                src.minDistance    = stem.minDistance;
                src.maxDistance    = stem.maxDistance;
                src.dopplerLevel   = 0f;
            }

            stem.runtimeSource = src;
        }
    }

    private void Start()
    {
        if (playOnAwake) Play();
    }

    private void Update()
    {
        // Duck hedefine yumuşak yaklaşım — fade-out sırasında karışma
        if (fadingOut || Mathf.Approximately(duckCurrent, duckTarget)) return;
        duckCurrent = Mathf.MoveTowards(duckCurrent, duckTarget,
                                        Time.deltaTime / Mathf.Max(0.01f, duckSmoothTime));
        PerformanceLevel = duckCurrent;
        foreach (var stem in stems)
            if (stem.runtimeSource != null)
                stem.runtimeSource.volume = EffectiveVolume(stem) * duckCurrent;
    }

    /// <summary>Stem'in nihai seviyesi: sanatsal offset × RMS eşitleme × sahne master'ı.</summary>
    private float EffectiveVolume(Stem stem)
        => stem.volume * stem.normGain * masterVolume;

    /// <summary>
    /// Konser seviyesi hedefi (0-1). Sequencer her dolduğunda azaltılır —
    /// oyuncunun loop'u sahnedeki grubun yerini alır.
    /// </summary>
    public void SetDuckTarget(float value)
    {
        duckTarget = Mathf.Clamp01(value);
    }

    /// <summary>Tüm stem'leri DSP clock ile senkron başlatır.</summary>
    public void Play()
    {
        double startDSP = AudioSettings.dspTime + startDelay + 0.1;
        foreach (var stem in stems)
            if (stem.runtimeSource != null && stem.clip != null)
                stem.runtimeSource.PlayScheduled(startDSP);
    }

    public void Stop()
    {
        foreach (var stem in stems)
            stem.runtimeSource?.Stop();
    }

    public void Pause()
    {
        foreach (var stem in stems)
            stem.runtimeSource?.Pause();
    }

    public void UnPause()
    {
        foreach (var stem in stems)
            stem.runtimeSource?.UnPause();
    }

    /// <summary>
    /// Tüm stem'leri verilen sürede yavaşça söndürüp durdurur.
    /// Sequencer finali konser müziğini bununla eritir.
    /// </summary>
    public void FadeOutAll(float duration)
    {
        StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        fadingOut = true; // duck Update'i devre dışı — fade tek kontrol olsun
        float[] startVolumes = new float[stems.Length];
        for (int i = 0; i < stems.Length; i++)
            startVolumes[i] = stems[i].runtimeSource != null ? stems[i].runtimeSource.volume : 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            PerformanceLevel = duckCurrent * k; // grup da fade ile birlikte söner
            for (int i = 0; i < stems.Length; i++)
                if (stems[i].runtimeSource != null)
                    stems[i].runtimeSource.volume = startVolumes[i] * k;
            yield return null;
        }
        PerformanceLevel = 0f;
        Stop();
    }
}
