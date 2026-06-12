using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Scene 2 sokak ambiyansı yönetmeni — oda çöküp şehir açığa çıktıktan sonra
/// metroya yürüyüş sırasındaki sokak seslerini düzeltir ve zenginleştirir.
///
/// Düzeltmeler:
///  • Kedi ve konuşan insan kayıtları LOOP olarak çalıyordu — tek meow'un
///    sonsuz döngüsü bozuk duyuluyor. Artık aralıklı çalıyorlar: çal,
///    rastgele 6-20 sn bekle, tekrar (kedide hafif pitch varyasyonu).
///  • Saksofoncu (street musician) performans olduğu için loop'ta kalır —
///    sadece gerçekten çaldığından emin olunur.
///  • Boş bırakılmış kaynaklar (ShopWindow, StreetLamps) artık sessiz değil:
///    prosedürel dükkan radyosu ve flüoresan/lamba vızıltısı üretilir.
///
/// Zenginleştirme (şehir ancak oda çökünce duyulur):
///  • Uzak şehir uğultusu — kahverengi gürültü bed'i, oda yıkılınca fade-in
///  • Ara sıra geçen araba — rastgele yönden prosedürel whoosh
///
/// FactoryMusicDirector gibi kendi kendini kurar (sahneye obje eklemek
/// gerekmez). Quest 3S: birkaç ekstra AudioSource, partikül/ışık yok.
/// </summary>
public class StreetAmbienceDirector : MonoBehaviour
{
    public static StreetAmbienceDirector Instance { get; private set; }

    [Header("Şehir Bed'i")]
    [Range(0f, 1f)] public float cityBedVolume = 0.12f;
    [Tooltip("Oda çöktükten sonra bed'in yükselme süresi (saniye)")]
    public float cityFadeIn = 6f;

    [Header("Araba Geçişleri")]
    public float carIntervalMin = 12f;
    public float carIntervalMax = 28f;
    [Range(0f, 1f)] public float carVolume = 0.35f;

    private AudioSource bedSource;
    private AudioSource carSource;
    private AudioClip carWhoosh;
    private bool cityRevealed;
    private float pollTimer;
    private GameObject roomCeiling; // oda çöktüğünde deaktive olur — şehir sinyali

    // ── Bootstrap: Scene 2 yüklenince kendini kurar ─────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        TrySpawnForScene(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded += (scene, mode) => TrySpawnForScene(scene);
    }

    private static void TrySpawnForScene(Scene scene)
    {
        if (scene.name != "Scene 2") return;
        if (Instance != null) return;
        var go = new GameObject("StreetAmbienceDirector");
        go.AddComponent<StreetAmbienceDirector>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        FixStreetSources();
        BuildCityBed();
        carWhoosh = ProceduralStreet.CarWhoosh();
        StartCoroutine(CarPassLoop());

        // Odanın tavanını bul — oda çökünce deaktive olur, şehir o an açılır
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name == "Ceiling") { roomCeiling = t.gameObject; break; }
        }
        if (roomCeiling == null)
        {
            // Oda yoksa (veya isim farklıysa) şehir baştan açık say
            cityRevealed = true;
            StartCoroutine(FadeInBed());
        }
    }

    private void Update()
    {
        // Oda hâlâ ayakta mı? Tavan deaktive olduysa şehir açıldı.
        if (cityRevealed) return;
        pollTimer -= Time.deltaTime;
        if (pollTimer > 0f) return;
        pollTimer = 1f;

        if (roomCeiling == null || !roomCeiling.activeInHierarchy)
        {
            cityRevealed = true;
            StartCoroutine(FadeInBed());
        }
    }

    // ── Mevcut sokak kaynaklarını düzelt ────────────────────────────────

    private void FixStreetSources()
    {
        foreach (var pas in FindObjectsByType<PositionalAudioSource>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var src = pas.GetComponent<AudioSource>();
            if (src == null) continue;

            string clipName = pas.clip != null ? pas.clip.name.ToLower() : "";
            string objName  = pas.gameObject.name.ToLower();

            if (pas.clip == null)
            {
                // Boş kaynaklar: isme göre prosedürel içerik
                if (objName.Contains("shop"))
                {
                    src.clip = ProceduralStreet.ShopRadio();
                    src.loop = true; src.volume = 0.25f; src.Play();
                }
                else if (objName.Contains("lamp") || objName.Contains("light"))
                {
                    src.clip = ProceduralStreet.ElectricBuzz();
                    src.loop = true; src.volume = 0.15f; src.Play();
                }
                continue;
            }

            if (clipName.Contains("sax"))
            {
                // Sokak müzisyeni: performans, loop doğru — sadece çaldığından emin ol
                if (!src.isPlaying) { src.loop = true; src.Play(); }
                continue;
            }

            bool isCat   = clipName.Contains("cat") || objName.Contains("cat") || objName.Contains("kedi");
            bool isVoice = clipName.Contains("male") || clipName.Contains("female")
                        || clipName.Contains("voice") || clipName.Contains("talk");

            if (isCat || isVoice)
            {
                // Tek seferlik kayıtlar loop'ta robotlaşıyor → aralıklı çal
                src.Stop();
                src.loop = false;
                StartCoroutine(IntervalPlay(src,
                    minGap: isCat ? 8f : 6f,
                    maxGap: isCat ? 20f : 16f,
                    pitchVar: isCat ? 0.12f : 0.04f));
            }
        }
    }

    private IEnumerator IntervalPlay(AudioSource src, float minGap, float maxGap, float pitchVar)
    {
        // İlk çalmada herkes aynı anda başlamasın
        yield return new WaitForSeconds(Random.Range(0f, maxGap * 0.5f));
        while (src != null)
        {
            src.pitch = 1f + Random.Range(-pitchVar, pitchVar);
            src.Play();
            float clipLen = src.clip != null ? src.clip.length : 1f;
            yield return new WaitForSeconds(clipLen + Random.Range(minGap, maxGap));
        }
    }

    // ── Şehir bed'i ve araba geçişleri ──────────────────────────────────

    private void BuildCityBed()
    {
        bedSource = gameObject.AddComponent<AudioSource>();
        bedSource.clip = ProceduralStreet.CityRumble();
        bedSource.loop = true;
        bedSource.playOnAwake = false;
        bedSource.spatialBlend = 0f; // uzak şehir — yönsüz
        bedSource.volume = 0f;       // oda çökünce fade-in
        bedSource.Play();

        carSource = gameObject.AddComponent<AudioSource>();
        carSource.playOnAwake = false;
        carSource.loop = false;
        carSource.spatialBlend = 1f;
        carSource.rolloffMode = AudioRolloffMode.Linear;
        carSource.minDistance = 4f;
        carSource.maxDistance = 35f;
        carSource.dopplerLevel = 0f;
    }

    private IEnumerator FadeInBed()
    {
        float t = 0f;
        while (t < cityFadeIn)
        {
            t += Time.deltaTime;
            bedSource.volume = cityBedVolume * Mathf.Clamp01(t / cityFadeIn);
            yield return null;
        }
    }

    private IEnumerator CarPassLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(carIntervalMin, carIntervalMax));
            if (!cityRevealed) continue;

            var cam = Camera.main;
            if (cam == null) continue;

            // Dinleyicinin çevresinde rastgele bir yönden geç
            float ang = Random.Range(0f, Mathf.PI * 2f);
            Vector3 pos = cam.transform.position
                          + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * Random.Range(12f, 22f);
            carSource.transform.position = pos;
            carSource.pitch = Random.Range(0.8f, 1.15f);
            carSource.PlayOneShot(carWhoosh, carVolume);
        }
    }
}

/// <summary>
/// Sokak için prosedürel ses üretimi: dükkan radyosu, lamba vızıltısı,
/// şehir uğultusu, araba geçişi. Hiç ses dosyası gerektirmez.
/// </summary>
public static class ProceduralStreet
{
    /// <summary>Dükkan camından sızan lo-fi radyo — A minör arpej, yumuşak ve boğuk.</summary>
    public static AudioClip ShopRadio()
    {
        int rate = AudioSettings.outputSampleRate;
        float dur = 9.6f;                       // 8'lik nota grid'inde tam döngü
        int count = (int)(rate * dur);
        float[] s = new float[count];

        float[] notes = { 220f, 261.63f, 329.63f, 440f, 329.63f, 261.63f }; // A C E A E C
        float noteLen = dur / 24f;              // 24 nota — yavaş arpej

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            int idx = (int)(t / noteLen) % notes.Length;
            float nt = t % noteLen;
            float f = notes[idx];
            float env = Mathf.Min(nt / 0.04f, 1f) * Mathf.Exp(-nt * 5f);
            // Sadece temel + zayıf 2. harmonik → "duvar arkasından" boğukluğu
            s[i] = (Mathf.Sin(2f * Mathf.PI * f * t) + 0.2f * Mathf.Sin(4f * Mathf.PI * f * t))
                   * env * 0.5f;
        }
        return Bake("ShopRadio", s, rate, loopBlend: rate / 10);
    }

    /// <summary>Sokak lambası / tabela elektrik vızıltısı.</summary>
    public static AudioClip ElectricBuzz()
    {
        int rate = AudioSettings.outputSampleRate;
        int count = rate * 2;
        float[] s = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 100f * t)) * 0.25f;
            float hi = Mathf.Sin(2f * Mathf.PI * 200f * t) * 0.18f
                     + Mathf.Sin(2f * Mathf.PI * 300f * t) * 0.08f;
            // Hafif şebeke dalgalanması
            float flicker = 1f + 0.15f * Mathf.Sin(2f * Mathf.PI * 7.3f * t);
            s[i] = (sq + hi) * flicker * 0.5f;
        }
        return Bake("ElectricBuzz", s, rate, loopBlend: rate / 20);
    }

    /// <summary>Uzak şehir uğultusu — alçak geçirilmiş kahverengi gürültü.</summary>
    public static AudioClip CityRumble()
    {
        int rate = AudioSettings.outputSampleRate;
        int count = rate * 6;
        float[] s = new float[count];
        var rng = new System.Random(34);
        float brown = 0f, lp = 0f;
        for (int i = 0; i < count; i++)
        {
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            brown = Mathf.Clamp(brown + white * 0.02f, -1f, 1f); // entegre → koyu
            lp += (brown - lp) * 0.05f;                          // ekstra lowpass
            s[i] = lp;
        }
        Normalize(s, 0.8f);
        return Bake("CityRumble", s, rate, loopBlend: rate / 2);
    }

    /// <summary>Geçen araba — yükselip alçalan, gövdeli gürültü süpürmesi.</summary>
    public static AudioClip CarWhoosh()
    {
        int rate = AudioSettings.outputSampleRate;
        float dur = 2.4f;
        int count = (int)(rate * dur);
        float[] s = new float[count];
        var rng = new System.Random(67);
        float lp = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float p = t / dur;
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            // Yaklaşırken parlar (lowpass açılır), uzaklaşırken koyulaşır
            float cutoff = Mathf.Lerp(0.04f, 0.35f, Mathf.Sin(p * Mathf.PI));
            lp += (white - lp) * cutoff;
            float env = Mathf.Pow(Mathf.Sin(p * Mathf.PI), 1.5f); // yumuşak geç-git
            s[i] = lp * env;
        }
        Normalize(s, 0.9f);
        return Bake("CarWhoosh", s, rate, loopBlend: 0);
    }

    // ── Yardımcılar ─────────────────────────────────────────────────────

    private static void Normalize(float[] s, float target)
    {
        float peak = 0.0001f;
        for (int i = 0; i < s.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(s[i]));
        float g = target / peak;
        for (int i = 0; i < s.Length; i++) s[i] *= g;
    }

    /// <summary>Diziyi klibe çevirir; loopBlend > 0 ise sonu başa harmanlayıp dikişsiz döngü yapar.</summary>
    private static AudioClip Bake(string name, float[] s, int rate, int loopBlend)
    {
        if (loopBlend > 0 && loopBlend * 2 < s.Length)
        {
            for (int i = 0; i < loopBlend; i++)
            {
                float k = i / (float)loopBlend;
                int tail = s.Length - loopBlend + i;
                s[i] = s[i] * k + s[tail] * (1f - k);
            }
            // Harmanlanan kuyruğu kes
            float[] trimmed = new float[s.Length - loopBlend];
            System.Array.Copy(s, trimmed, trimmed.Length);
            s = trimmed;
        }
        var clip = AudioClip.Create(name, s.Length, 1, rate, false);
        clip.SetData(s, 0);
        return clip;
    }
}
