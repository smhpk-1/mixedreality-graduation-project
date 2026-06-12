using UnityEngine;

/// <summary>
/// Proje geneli loudness yardımcıları — "master pass"in ortak referansı.
///
/// Sorun: sahnelerin kaynak sesleri çok farklı seviyelerde (ölçülen: scene1
/// makineleri −18.7…−29.5 LUFS, scene4 stem'leri −18.7…−32.4 LUFS). Her
/// director kendi karışım oranlarını bu dengesiz malzemenin üstüne kurunca
/// sahneden sahneye algılanan yükseklik tutarsızlaşıyor.
///
/// Çözüm: tüm director'lar malzemeyi yüklerken AYNI RMS hedefine çeker
/// (ReferenceDb), karışım oranları (ostinatoVolume, stem.volume...) bu eşit
/// zeminin üstünde sanatsal offset olarak çalışır. Sahne toplamı her
/// director'daki masterVolume ile trimlenebilir.
/// </summary>
public static class AudioMasterUtil
{
    /// <summary>Ortak kaynak hedefi: −20 dBFS RMS (yaklaşık konuşma altı müzik yatağı).</summary>
    public const float ReferenceDb = -20f;

    /// <summary>
    /// Klibin RMS'ini örnekleyerek ölçer (lineer, 0-1). Uzun klipleri komple
    /// RAM'e açmamak için 3 kısa pencere okur (%10, %50, %90 konumlarından).
    /// </summary>
    public static float MeasureRms(AudioClip clip)
    {
        if (clip == null || clip.samples <= 0) return 0f;

        int ch = Mathf.Max(1, clip.channels);
        int windowFrames = Mathf.Min(clip.samples, clip.frequency / 2); // 0.5 sn
        float[] buf = new float[windowFrames * ch];

        float sum = 0f;
        int counted = 0;
        float[] positions = { 0.1f, 0.5f, 0.9f };
        foreach (float p in positions)
        {
            int offset = Mathf.Clamp((int)(clip.samples * p), 0, clip.samples - windowFrames);
            if (!clip.GetData(buf, offset)) continue;
            for (int i = 0; i < buf.Length; i++) sum += buf[i] * buf[i];
            counted += buf.Length;
        }

        if (counted == 0) return 0f;
        return Mathf.Sqrt(sum / counted);
    }

    /// <summary>Ham örnek dizisinin RMS'i (slice atölyeleri için).</summary>
    public static float MeasureRms(float[] data)
    {
        if (data == null || data.Length == 0) return 0f;
        float sum = 0f;
        for (int i = 0; i < data.Length; i++) sum += data[i] * data[i];
        return Mathf.Sqrt(sum / data.Length);
    }

    /// <summary>
    /// Klibi ReferenceDb'ye getirecek volume çarpanı. Aşırı boost/cut'a karşı
    /// ±maxAdjustDb ile sınırlanır (sessiz FX yatağını bağırtmamak için).
    /// </summary>
    public static float NormalizationGain(AudioClip clip, float maxAdjustDb = 9f)
        => GainFor(MeasureRms(clip), maxAdjustDb);

    /// <summary>Ölçülmüş lineer RMS için normalizasyon çarpanı.</summary>
    public static float GainFor(float rms, float maxAdjustDb = 9f)
    {
        if (rms < 0.00001f) return 1f; // sessizlik — dokunma
        float targetLinear = DbToLinear(ReferenceDb);
        float gain = targetLinear / rms;
        float max = DbToLinear(maxAdjustDb);
        return Mathf.Clamp(gain, 1f / max, max);
    }

    /// <summary>Örnek dizisini ReferenceDb RMS'e ölçekler (kafa boşluğu için peak klempli).</summary>
    public static void NormalizeToReference(float[] data, float maxAdjustDb = 12f)
    {
        float g = GainFor(MeasureRms(data), maxAdjustDb);

        // Peak güvenliği: normalizasyon sonrası tepe 0.95'i aşmasın
        float peak = 0f;
        for (int i = 0; i < data.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
        if (peak * g > 0.95f) g = 0.95f / Mathf.Max(0.0001f, peak);

        for (int i = 0; i < data.Length; i++) data[i] *= g;
    }

    public static float DbToLinear(float db) => Mathf.Pow(10f, db / 20f);
    public static float LinearToDb(float lin) => 20f * Mathf.Log10(Mathf.Max(0.00001f, lin));
}
