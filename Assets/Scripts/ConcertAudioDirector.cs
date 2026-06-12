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
    }

    [Header("Stem'ler")]
    public Stem[] stems;

    [Header("Ayarlar")]
    public bool  playOnAwake = true;
    public float startDelay  = 0f;

    private void Awake()
    {
        foreach (var stem in stems)
        {
            GameObject target = (stem.mode == AudioMode.Spatial3D && stem.sourceObject != null)
                ? stem.sourceObject
                : gameObject; // 2D stem'ler bu objeye eklenir

            var src = target.AddComponent<AudioSource>();
            src.clip        = stem.clip;
            src.volume      = stem.volume;
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
        float[] startVolumes = new float[stems.Length];
        for (int i = 0; i < stems.Length; i++)
            startVolumes[i] = stems[i].runtimeSource != null ? stems[i].runtimeSource.volume : 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            for (int i = 0; i < stems.Length; i++)
                if (stems[i].runtimeSource != null)
                    stems[i].runtimeSource.volume = startVolumes[i] * k;
            yield return null;
        }
        Stop();
    }
}
