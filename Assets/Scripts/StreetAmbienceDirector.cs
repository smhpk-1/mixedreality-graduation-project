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

    [Header("Rüzgar Bed'i")]
    [Tooltip("2D yönsüz rüzgar ambiyansı — belirli bir yerden gelmez")]
    [Range(0f, 1f)] public float cityBedVolume = 0.14f;
    [Tooltip("Oda çöktükten sonra bed'in yükselme süresi (saniye)")]
    public float cityFadeIn = 6f;

    [Header("Storefront / Tabela")]
    [Range(0f, 1f)] public float storefrontVolume = 0.3f;
    [Range(0f, 1f)] public float signCreakVolume = 0.25f;

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
        SetupStorefronts();
        SetupSigns();
        BuildCityBed();
        carWhoosh = ProceduralStreet.CarWhoosh();
        StartCoroutine(CarPassLoop());

        // Oyuncu adım sesleri — yürüme hissi için
        var fs = new GameObject("PlayerFootsteps");
        fs.transform.SetParent(transform, false);
        fs.AddComponent<PlayerFootsteps>();

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

    // ── Storefront'lar: her dükkandan farklı, gündelik bir ses ──────────

    /// <summary>
    /// Sahnedeki "Shopfront" objelerine sırayla farklı ambiyanslar bağlar:
    /// restoran (mırıltı + çatal-bıçak), market (dolap uğultusu + barkod bipleri),
    /// dükkan radyosu. Hepsi 3D lokal kaynak — önünden geçerken duyulur.
    /// </summary>
    private void SetupStorefronts()
    {
        var shopfronts = new List<Transform>();
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name == "Shopfront") shopfronts.Add(t);
        if (shopfronts.Count == 0) return;

        // Deterministik sıra (pozisyona göre) — her çalışmada aynı dükkan aynı sesi alsın
        shopfronts.Sort((a, b) => (a.position.x + a.position.z * 1000f)
                        .CompareTo(b.position.x + b.position.z * 1000f));

        for (int i = 0; i < shopfronts.Count; i++)
        {
            AudioClip clip;
            float vol = storefrontVolume;
            switch (i % 3)
            {
                case 0:  clip = ProceduralStreet.RestaurantAmbience(); break;
                case 1:  clip = ProceduralStreet.MarketAmbience(); vol *= 0.85f; break;
                default: clip = ProceduralStreet.ShopRadio(); vol *= 0.8f; break;
            }

            var go = new GameObject("ShopfrontAmbience_" + i);
            go.transform.SetParent(shopfronts[i], false);
            go.transform.localPosition = Vector3.up * 1.6f;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1.5f;
            src.maxDistance = 9f;
            src.dopplerLevel = 0f;
            src.volume = vol;
            src.time = Random.Range(0f, clip.length); // hepsi aynı anda başlamasın
            src.Play();
        }
    }

    // ── Tabelalar: rüzgarda sallanan metal gıcırtısı ────────────────────

    private void SetupSigns()
    {
        var creak = ProceduralStreet.SignCreak();
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name != "StandingSign") continue;

            var go = new GameObject("SignCreak");
            go.transform.SetParent(t, false);
            go.transform.localPosition = Vector3.up * 1.2f;
            var src = go.AddComponent<AudioSource>();
            src.clip = creak;
            src.loop = false;
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = 6f;
            src.dopplerLevel = 0f;
            src.volume = signCreakVolume;
            StartCoroutine(IntervalPlay(src, minGap: 9f, maxGap: 26f, pitchVar: 0.18f));
        }
    }

    // ── Rüzgar bed'i ve araba geçişleri ─────────────────────────────────

    private void BuildCityBed()
    {
        bedSource = gameObject.AddComponent<AudioSource>();
        bedSource.clip = ProceduralStreet.WindAmbience();
        bedSource.loop = true;
        bedSource.playOnAwake = false;
        bedSource.spatialBlend = 0f; // rüzgar yönsüz AMBIYANSTIR — belirli bir kaynaktan gelmez
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

    /// <summary>
    /// Rüzgar ambiyansı. Eski "CityRumble" (kahverengi gürültü) dalga sesi gibi
    /// tınlıyordu — dalga karakteri periyodik kabarma/çekilmeden gelir. Rüzgar ise
    /// sürekli bir orta-bant hışırtıdır: esinti LFO'ları filtreyi açıp kapar
    /// (sertleşince tizleşir) ve genliği yavaşça dalgalandırır. 18 sn dikişsiz döngü.
    /// </summary>
    public static AudioClip WindAmbience()
    {
        int rate = AudioSettings.outputSampleRate;
        float dur = 18f;
        int count = (int)(rate * dur);
        float[] s = new float[count];
        var rng = new System.Random(11);
        float lpFast = 0f, lpSlow = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            // Esinti zarfı: eş ölçülü olmayan iki yavaş LFO — asla aynı deseni tekrarlamaz
            float gust = 0.55f
                       + 0.30f * Mathf.Sin(2f * Mathf.PI * 0.071f * t + 1.3f)
                       + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.187f * t + 4.1f);
            gust = Mathf.Clamp01(gust);

            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            // Esinti sertleştikçe filtre açılır → hışırtı tizleşir (gerçek rüzgar karakteri)
            float k = Mathf.Lerp(0.05f, 0.30f, gust);
            lpFast += (white - lpFast) * k;
            lpSlow += (white - lpSlow) * k * 0.35f;
            float band = lpFast - lpSlow; // orta bant — boğuk uğultu değil, hışırtı

            s[i] = band * (0.30f + 0.70f * gust);
        }
        Normalize(s, 0.75f);
        return Bake("WindAmbience", s, rate, loopBlend: rate);
    }

    /// <summary>Restoran vitrini: konuşma mırıltısı + seyrek çatal-bıçak tıkırtısı.</summary>
    public static AudioClip RestaurantAmbience()
    {
        int rate = AudioSettings.outputSampleRate;
        float dur = 14f;
        int count = (int)(rate * dur);
        float[] s = new float[count];
        var rng = new System.Random(52);

        // 3 mırıltı akışı: ses bandında (200-800 Hz) gürültü, her biri kendi
        // konuşma ritminde dalgalanır — camın ardından duyulan kalabalık
        float[] lpF = new float[3], lpS = new float[3];
        float[] amRate = { 1.7f, 2.3f, 1.1f };
        float[] amPhase = { 0.4f, 2.9f, 5.2f };
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float v = 0f;
            for (int m = 0; m < 3; m++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lpF[m] += (white - lpF[m]) * 0.12f;
                lpS[m] += (white - lpS[m]) * 0.03f;
                float am = 0.4f + 0.6f * Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * amRate[m] * t + amPhase[m]));
                v += (lpF[m] - lpS[m]) * am;
            }
            s[i] = v * 0.5f;
        }

        // Seyrek çatal-bıçak: kısa, tiz metalik pingler
        for (int c = 0; c < 9; c++)
        {
            int start = (int)((float)rng.NextDouble() * (dur - 0.3f) * rate);
            float f = 1800f + (float)rng.NextDouble() * 1700f;
            int len = (int)(0.12f * rate);
            for (int i = 0; i < len && start + i < count; i++)
            {
                float t = i / (float)rate;
                s[start + i] += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 45f) * 0.35f;
            }
        }
        Normalize(s, 0.7f);
        return Bake("RestaurantAmbience", s, rate, loopBlend: rate / 2);
    }

    /// <summary>Market vitrini: soğutucu dolap uğultusu + ara sıra barkod bipleri.</summary>
    public static AudioClip MarketAmbience()
    {
        int rate = AudioSettings.outputSampleRate;
        float dur = 12f;
        int count = (int)(rate * dur);
        float[] s = new float[count];
        var rng = new System.Random(73);
        float lp = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            // Dolap kompresörü: 49 Hz + oktav, hafif dalgalı
            float hum = (Mathf.Sin(2f * Mathf.PI * 49f * t)
                       + 0.45f * Mathf.Sin(2f * Mathf.PI * 98f * t))
                       * (0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 0.23f * t));
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * 0.04f; // fan hışırtısı
            s[i] = hum * 0.4f + lp * 0.35f;
        }

        // Barkod bipleri: 1 kHz çift bip, rastgele 3 yerde
        for (int b = 0; b < 3; b++)
        {
            int start = (int)((float)rng.NextDouble() * (dur - 0.5f) * rate);
            for (int rep = 0; rep < 2; rep++)
            {
                int s0 = start + (int)(rep * 0.15f * rate);
                int len = (int)(0.07f * rate);
                for (int i = 0; i < len && s0 + i < count; i++)
                    s[s0 + i] += Mathf.Sin(2f * Mathf.PI * 1000f * i / rate) * 0.25f
                                 * Mathf.Min(1f, (len - i) / (len * 0.3f));
            }
        }
        Normalize(s, 0.65f);
        return Bake("MarketAmbience", s, rate, loopBlend: rate / 2);
    }

    /// <summary>Rüzgarda sallanan tabela gıcırtısı — düşen perdeli, vibratolu metal sesi.</summary>
    public static AudioClip SignCreak()
    {
        int rate = AudioSettings.outputSampleRate;
        float dur = 1.1f;
        int count = (int)(rate * dur);
        float[] s = new float[count];
        var rng = new System.Random(29);
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float p = t / dur;
            // Perde 850 → 480 Hz kayar, üstüne büyüyen vibrato — "gıcırt"
            float f = Mathf.Lerp(850f, 480f, p)
                    + Mathf.Sin(2f * Mathf.PI * 23f * t) * 40f * p;
            phase += 2f * Mathf.PI * f / rate;
            float env = Mathf.Sin(p * Mathf.PI); // yumuşak gir-çık
            float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.15f;
            s[i] = (Mathf.Sin(phase) * 0.8f + noise) * env;
        }
        Normalize(s, 0.6f);
        return Bake("SignCreak", s, rate, loopBlend: 0);
    }

    /// <summary>Beton üzerinde tek adım sesi — variant: 0/1/2 farklı karakter.</summary>
    public static AudioClip Footstep(int variant)
    {
        int rate = AudioSettings.outputSampleRate;
        float dur = 0.16f;
        int count = (int)(rate * dur);
        float[] s = new float[count];
        var rng = new System.Random(40 + variant);
        float lp = 0f;
        float thumpF = 68f + variant * 7f;
        float k = 0.16f + variant * 0.04f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * k;
            // Topuk teması: gürültü "scuff" + alçak gövde vuruşu
            s[i] = lp * Mathf.Exp(-t * 50f) * 0.8f
                 + Mathf.Sin(2f * Mathf.PI * thumpF * t) * Mathf.Exp(-t * 35f) * 0.6f;
        }
        Normalize(s, 0.8f);
        return Bake("Footstep_" + variant, s, rate, loopBlend: 0);
    }

    /// <summary>Asansör "ding" — iki tonlu varış zili.</summary>
    public static AudioClip ElevatorDing()
    {
        int rate = AudioSettings.outputSampleRate;
        float dur = 1.0f;
        int count = (int)(rate * dur);
        float[] s = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            s[i] = Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * Mathf.Exp(-t * 5f) * 0.6f; // E6
            if (t > 0.22f)
            {
                float t2 = t - 0.22f;
                s[i] += Mathf.Sin(2f * Mathf.PI * 1046.5f * t2) * Mathf.Exp(-t2 * 4.5f) * 0.6f; // C6
            }
        }
        Normalize(s, 0.7f);
        return Bake("ElevatorDing", s, rate, loopBlend: 0);
    }

    /// <summary>Asansör kapısı kayma sesi — süresi kapı animasyonuna uyarlanır.</summary>
    public static AudioClip DoorSlide(float duration)
    {
        int rate = AudioSettings.outputSampleRate;
        int count = (int)(rate * Mathf.Max(0.3f, duration));
        float[] s = new float[count];
        var rng = new System.Random(58);
        float lp = 0f;
        for (int i = 0; i < count; i++)
        {
            float p = i / (float)count;
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * 0.10f;
            float env = Mathf.Sin(p * Mathf.PI); // başla-yumuşa-bit
            s[i] = lp * env;
        }
        Normalize(s, 0.5f);
        return Bake("DoorSlide", s, rate, loopBlend: 0);
    }

    /// <summary>Asansör kabin motoru — hareket süresi boyunca alçak uğultu, fade'li.</summary>
    public static AudioClip MotorHum(float duration)
    {
        int rate = AudioSettings.outputSampleRate;
        int count = (int)(rate * Mathf.Max(1f, duration));
        float[] s = new float[count];
        var rng = new System.Random(64);
        float lp = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float p = i / (float)count;
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * 0.03f;
            float env = Mathf.Min(Mathf.Min(t / 0.8f, 1f), (count - i) / (rate * 0.8f));
            s[i] = (Mathf.Sin(2f * Mathf.PI * 38f * t) * 0.6f
                  + Mathf.Sin(2f * Mathf.PI * 76f * t) * 0.25f
                  + lp * 0.2f) * Mathf.Clamp01(env);
        }
        Normalize(s, 0.6f);
        return Bake("MotorHum", s, rate, loopBlend: 0);
    }

    /// <summary>Asansör çağrı butonu tık sesi.</summary>
    public static AudioClip ButtonClick()
    {
        int rate = AudioSettings.outputSampleRate;
        int count = (int)(rate * 0.06f);
        float[] s = new float[count];
        var rng = new System.Random(81);
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            s[i] = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 120f) * 0.5f
                 + Mathf.Sin(2f * Mathf.PI * 2100f * t) * Mathf.Exp(-t * 90f) * 0.5f;
        }
        Normalize(s, 0.7f);
        return Bake("ButtonClick", s, rate, loopBlend: 0);
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
