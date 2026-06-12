using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// "Studio as a Compositional Tool" — Scene 1 jeneratif müzik motoru.
///
/// Brian Eno yaklaşımı: stüdyo (burada runtime audio engine) bir enstrümandır.
/// Ham makine kayıtları (Resources/scene_1_sound_design) bant gibi kesilir,
/// pitch'lenir ve katmanlanır:
///
///   1. OSTINATO  — machine_4 kaydından kesilen transient "vuruş" dilimi,
///      DSP saatine kilitli 16-step'lik bir ritim kalıbında döner.
///      Fabrikanın kalp atışı: root + beşli, bar sonunda oktav aksanı.
///
///   2. TAPE LOOPS — Music for Airports tekniği: machine_3 / machine_1
///      kayıtlarından uzun tonal dilimler, her biri FARKLI ve eşölçülemez
///      periyotlarla (21.3s, 26.7s, 33.1s...) kendini yeniden çalar.
///      Katmanlar hiçbir zaman aynı hizada buluşmaz → sürekli değişen doku.
///      ("Repetition is a form of change.")
///
///   3. STEAM BREATH — machine_7_steam dilimi her 4 bar'da bir "nefes" olarak.
///
///   4. KUANTİZE NOTALAR — BinCollector'dan gelen doğru/yanlış sıralama
///      olayları grid'in bir sonraki 16'lığına kuantize edilir. Doğru küpler
///      minör pentatonik jeneratif bir melodi yürütür; skor 30'a yaklaştıkça
///      register yükselir (emeğin kendisi kompozisyonu anomaliye taşır).
///      Yanlış küpler disonan (küçük ikili) ama yine ritmik bir cluster çalar.
///
/// Sahneye elle eklemek gerekmez: scene1 yüklendiğinde kendini bootstrap eder.
/// </summary>
public class FactoryMusicDirector : MonoBehaviour
{
    public static FactoryMusicDirector Instance { get; private set; }

    [Header("Tempo / Ton")]
    [Tooltip("Fabrika temposu (BPM) — ağır, mekanik bir yürüyüş")]
    public float bpm = 76f;

    /// <summary>
    /// Scene 4 finali döngüyü kapattıysa true (SequencerFinaleDirector set eder).
    /// İkinci vardiya: tempo bir tık daha hızlı, tape loop'ların yarısı hafif
    /// falsolu — döngü her turda biraz daha bozuluyor. Oyuncu fark etmeyebilir;
    /// zaten mesele de bu.
    /// </summary>
    public static bool SecondShift = false;

    [Tooltip("Pattern uzunluğu (16'lık adım sayısı)")]
    public int stepsPerBar = 16;

    [Header("Mix Seviyeleri")]
    [Range(0f, 1f)] public float ostinatoVolume  = 0.65f;
    [Range(0f, 1f)] public float tapeLoopVolume  = 0.45f;
    [Range(0f, 1f)] public float steamVolume     = 0.35f;
    [Range(0f, 1f)] public float noteVolume      = 1f;

    [Header("Mekansal Ses (3D)")]
    [Tooltip("Katman seslerinin tam volume duyulduğu mesafe — yaklaştıkça ses bu mesafeye kadar artar")]
    public float layerMinDistance = 2.5f;
    [Tooltip("Katman seslerinin tamamen söndüğü mesafe")]
    public float layerMaxDistance = 35f;
    [Tooltip("Kutu (bin) notalarının tam volume mesafesi — oyuncu kutunun başındayken net duyulsun")]
    public float noteMinDistance = 4f;
    public float noteMaxDistance = 30f;

    [Tooltip("Sahnedeki ham makine ambiyans loop'ları bu çarpanla kısılır ki kompozisyon duyulsun")]
    [Range(0f, 1f)] public float ambienceDuck = 0.35f;

    [Header("Steam Nefesi")]
    [Tooltip("Kaç bar'da bir steam 'nefes' duyulur")]
    public int steamEveryBars = 4;

    // ── Minör pentatonik (yarım ton offsetleri) ─────────────────────────
    private static readonly int[] Pentatonic = { 0, 3, 5, 7, 10 };

    // Eno tape loop periyotları (saniye) — kasıtlı olarak eşölçülemez
    private static readonly float[] LoopPeriods = { 21.3f, 26.7f, 33.1f, 39.9f };

    // Tape loop pitch'leri (yarım ton): root, kalın beşli, küçük üçlü, dörtlü
    private static readonly int[] LoopSemitones = { 0, -5, 3, 5 };

    private const double ScheduleLookahead = 0.6; // saniye — DSP planlama penceresi

    // ── Kesilmiş "enstrümanlar" ──────────────────────────────────────────
    private AudioClip hitSlice;     // machine_4 transient'i — perküsyon (kısa)
    private AudioClip tickSlice;    // machine_1 transient'i — hafif offbeat
    private AudioClip steamSlice;   // machine_7'den nefes
    private AudioClip[] tapeSlices; // uzun tonal dilimler (Eno loop'ları)
    private AudioClip confirmTone;  // prosedürel harmonik ton — doğru küp onayı
    private AudioClip errorBuzz;    // prosedürel sert vızıltı — yanlış küp hatası

    // ── Mekansal çapalar: sesler sahnedeki gerçek makine objelerinden çıkar ──
    private Transform anchorHit;    // machine_4 objesinin konumu — ostinato vuruşları
    private Transform anchorTick;   // konveyör — offbeat tikleri
    private Transform anchorSteam;  // machine_7 objesi — buhar nefesi
    private readonly List<Transform> tapeAnchors = new List<Transform>();

    // ── Sequencer durumu ────────────────────────────────────────────────
    private double dspStartTime;
    private double stepDuration;
    private long   nextStepIndex;
    private readonly double[] nextLoopTimes = new double[LoopPeriods.Length];

    // One-shot kaynak havuzu (PlayScheduled üst üste binen sesler için ayrı kaynak ister)
    private readonly List<AudioSource> pool = new List<AudioSource>();
    private int poolCursor;
    private const int PoolSize = 20;

    // Jeneratif melodi durumu
    private int melodyIndex;
    private int correctCount;
    private System.Random rng = new System.Random(1973); // Discreet Music yılı

    // Rüya modu: anomali (Glitch) başlayınca emeğin ritmi söner, rüya katmanları kabarır
    private float rhythmWeight = 1f;
    [Tooltip("Anomali sonrası ostinato'nun sönme süresi (saniye)")]
    public float rhythmFadeOut = 6f;

    // ── Bootstrap: scene1 yüklenince kendini kurar ──────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        TrySpawnForScene(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded += (scene, mode) => TrySpawnForScene(scene);
    }

    private static void TrySpawnForScene(Scene scene)
    {
        if (scene.name != "scene1") return;
        if (Instance != null) return;
        var go = new GameObject("FactoryMusicDirector");
        go.AddComponent<FactoryMusicDirector>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (!LoadAndSliceSources())
        {
            Debug.LogWarning("[FactoryMusicDirector] Kaynak sesler bulunamadı — müzik motoru kapalı.");
            enabled = false;
            return;
        }

        GenerateFeedbackTones();
        BuildSourcePool();
        ScanAnchorsAndDuckAmbience();

        // İkinci vardiya: bant biraz daha hızlı akar
        if (SecondShift) bpm += 6f;

        stepDuration = 60.0 / bpm / 4.0; // 16'lık süre
        dspStartTime = AudioSettings.dspTime + 0.4;
        nextStepIndex = 0;

        for (int i = 0; i < LoopPeriods.Length; i++)
        {
            // Loop'lar farklı anlarda girsin diye kademeli başlangıç
            nextLoopTimes[i] = dspStartTime + i * 3.7;
        }
    }

    private void Update()
    {
        // Anomali başladı mı? Ritim (emek) söner, tape loop'lar (rüya) %50 kabarır.
        var gm = ConveyorShift.GameManager.Instance;
        bool dreaming = gm != null && gm.CurrentState != ConveyorShift.GameManager.GameState.WorkState;
        rhythmWeight = Mathf.MoveTowards(rhythmWeight, dreaming ? 0f : 1f,
                                         Time.deltaTime / Mathf.Max(0.1f, rhythmFadeOut));

        double horizon = AudioSettings.dspTime + ScheduleLookahead;

        // 1. Ostinato adımlarını planla
        while (StepTime(nextStepIndex) < horizon)
        {
            if (rhythmWeight > 0.01f)
                ScheduleStep(nextStepIndex, StepTime(nextStepIndex));
            nextStepIndex++;
        }

        // 2. Eno tape loop'larını planla — her katman kendi periyodunda,
        //    odaya dağılmış FARKLI makine objelerinden duyulur
        float tapeVol = tapeLoopVolume * (1f + 0.5f * (1f - rhythmWeight));
        for (int i = 0; i < LoopPeriods.Length; i++)
        {
            Transform anchor = tapeAnchors.Count > 0 ? tapeAnchors[i % tapeAnchors.Count] : null;
            while (nextLoopTimes[i] < horizon)
            {
                // İkinci vardiyada tek sayılı loop'lar çeyrek ton falsolu —
                // aynı fabrika, ama artık bir şey "yanlış" tınlıyor
                double detune = (SecondShift && (i & 1) == 1) ? 0.4 : 0.0;
                ScheduleAnchored(tapeSlices[i % tapeSlices.Length], nextLoopTimes[i],
                                 SemitoneToPitch(LoopSemitones[i] + detune), tapeVol, anchor);
                nextLoopTimes[i] += LoopPeriods[i];
            }
        }
    }

    private double StepTime(long step) => dspStartTime + step * stepDuration;

    // ── Ostinato kalıbı ─────────────────────────────────────────────────
    private void ScheduleStep(long step, double time)
    {
        int stepInBar = (int)(step % stepsPerBar);
        long bar      = step / stepsPerBar;
        float vol     = ostinatoVolume * rhythmWeight;

        // Ana vuruşlar machine_4 objesinden: 1 ve 9 — root; 5 ve 13 — kalın beşli (piston)
        if (stepInBar == 0)
            ScheduleAnchored(hitSlice, time, SemitoneToPitch(-12), vol, anchorHit);
        else if (stepInBar == 8)
            ScheduleAnchored(hitSlice, time, SemitoneToPitch(-12), vol * 0.9f, anchorHit);
        else if (stepInBar == 4 || stepInBar == 12)
            ScheduleAnchored(hitSlice, time, SemitoneToPitch(-17), vol * 0.8f, anchorHit);

        // Offbeat tikleri konveyörden: zayıf 16'lıklarda seyrek, hafif rastgele
        if (stepInBar % 4 == 2 && rng.NextDouble() < 0.7)
            ScheduleAnchored(tickSlice, time, SemitoneToPitch(rng.NextDouble() < 0.5 ? 0 : 7),
                             vol * 0.35f, anchorTick);

        // Bar kapanışı aksanı: son 16'lıkta oktav üstü kısa vuruş
        if (stepInBar == stepsPerBar - 1 && bar % 2 == 1)
            ScheduleAnchored(hitSlice, time, SemitoneToPitch(0), vol * 0.5f, anchorHit);

        // Steam nefesi machine_7 objesinden: her steamEveryBars bar'da bir
        if (stepInBar == 0 && bar % steamEveryBars == 0 && bar > 0)
            ScheduleAnchored(steamSlice, time, 1f, steamVolume, anchorSteam);
    }

    // ── BinCollector API: kuantize notalar ──────────────────────────────

    /// <summary>Doğru sıralama — jeneratif pentatonik melodi notası, grid'e kuantize.</summary>
    public void PlayCorrectNote(Vector3 worldPos, bool isRedCube)
    {
        correctCount++;

        // Melodi yürüyüşü: çoğunlukla komşu dereceye adım, bazen sıçrama
        melodyIndex += rng.NextDouble() < 0.75 ? (rng.NextDouble() < 0.5 ? 1 : -1)
                                               : (rng.NextDouble() < 0.5 ? 2 : -2);
        melodyIndex = Mathf.Clamp(melodyIndex, 0, Pentatonic.Length * 2 - 1);

        // Emek ilerledikçe register yükselir: 0-30 küp → 0..+7 yarım ton taban
        int rise = Mathf.RoundToInt(Mathf.Clamp01(correctCount / 30f) * 7f);

        int degree   = Pentatonic[melodyIndex % Pentatonic.Length];
        int octave   = (melodyIndex / Pentatonic.Length) * 12;
        int redBlue  = isRedCube ? 7 : 0; // kırmızı küpler beşli yukarıda konuşur

        // Pitch tavanı: çok tizleşirse dilim aşırı kısalır ve duyulmaz olur.
        // +15 yarım tonu aşan notaları oktav aşağı sar.
        int semis = degree + octave + rise + redBlue;
        while (semis > 15) semis -= 12;

        // Net duyulan onay: makine gürültüsü yerine harmonik ton —
        // gürültü diliminin pitch'i algılanamadığı için onay/hata ayrımı kayboluyordu
        double t = NextGridTime(quantizeTo: 1); // bir sonraki 16'lık
        ScheduleNote(confirmTone, t, semis, noteVolume, worldPos);

        // Her 5. doğru küpte küçük bir "onay" ek notası — beşli paralel
        if (correctCount % 5 == 0)
            ScheduleNote(confirmTone, t + stepDuration * 2,
                         semis - 5, noteVolume * 0.7f, worldPos);
    }

    /// <summary>Yanlış sıralama — disonan ama ritmik cluster (küçük ikili), grid'e kuantize.</summary>
    public void PlayWrongNote(Vector3 worldPos)
    {
        // Klasik çift "bızz" hata sesi — sert vızıltı, grid'e kuantize.
        // Eskiden gürültü dilimi kullanılıyordu; pitch algılanamadığından
        // onay sesinden ayırt edilemiyordu.
        double t = NextGridTime(quantizeTo: 2); // bir sonraki 8'lik
        ScheduleNote(errorBuzz, t, 0, noteVolume, worldPos);
        ScheduleNote(errorBuzz, t + stepDuration, -2, noteVolume * 0.85f, worldPos);
    }

    /// <summary>Kutu notası: kutunun konumundan, yakın mesafede tam volume duyulur.</summary>
    private void ScheduleNote(AudioClip clip, double dspTime, int semitones,
                              float volume, Vector3 worldPos)
    {
        ScheduleOneShot(clip, dspTime, SemitoneToPitch(semitones), volume,
                        worldPos, true, noteMinDistance, noteMaxDistance);
    }

    /// <summary>Bir sonraki grid noktasının DSP zamanı (quantizeTo: kaç 16'lıkta bir).</summary>
    private double NextGridTime(int quantizeTo)
    {
        double now  = AudioSettings.dspTime + 0.03; // ufak güvenlik payı
        double span = stepDuration * quantizeTo;
        long   n    = (long)System.Math.Ceiling((now - dspStartTime) / span);
        return dspStartTime + n * span;
    }

    // ── Ses planlama altyapısı ──────────────────────────────────────────

    /// <summary>Sahnedeki bir objeden (çapadan) çıkan 3D ses planlar. Çapa yoksa 2D düşer.</summary>
    private void ScheduleAnchored(AudioClip clip, double dspTime, float pitch,
                                  float volume, Transform anchor)
    {
        if (anchor != null)
            ScheduleOneShot(clip, dspTime, pitch, volume, anchor.position, true,
                            layerMinDistance, layerMaxDistance);
        else
            ScheduleOneShot(clip, dspTime, pitch, volume, Vector3.zero, false,
                            layerMinDistance, layerMaxDistance);
    }

    private void ScheduleOneShot(AudioClip clip, double dspTime, float pitch,
                                 float volume, Vector3 pos, bool spatial,
                                 float minDist, float maxDist)
    {
        if (clip == null) return;
        AudioSource src = NextFreeSource();
        src.transform.position = pos;
        src.clip         = clip;
        src.pitch        = pitch;
        src.volume       = volume;
        src.spatialBlend = spatial ? 1f : 0f;
        src.minDistance  = minDist;
        src.maxDistance  = maxDist;
        src.PlayScheduled(dspTime);
    }

    private AudioSource NextFreeSource()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            poolCursor = (poolCursor + 1) % pool.Count;
            if (!pool[poolCursor].isPlaying) return pool[poolCursor];
        }
        // Hepsi doluysa en eskisini gasp et
        poolCursor = (poolCursor + 1) % pool.Count;
        return pool[poolCursor];
    }

    private void BuildSourcePool()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            var child = new GameObject($"Voice_{i:00}");
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.dopplerLevel = 0f;
            src.minDistance  = 2f;
            src.maxDistance  = 30f;
            src.rolloffMode  = AudioRolloffMode.Linear;
            pool.Add(src);
        }
    }

    private void ScanAnchorsAndDuckAmbience()
    {
        // Sahnedeki ham makine loop'larını bul: hem mekansal çapa olarak kullan
        // (kompozisyon o objelerden duyulacak), hem de seslerini kıs —
        // kaynak malzeme zaten onlar; ikisi aynı anda tam seste çamur olur.
        foreach (var src in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (src.transform.IsChildOf(transform)) continue;
            if (src.clip == null || !src.loop) continue;

            string n = src.clip.name.Replace(" ", "");
            if      (n.StartsWith("machine_4")) anchorHit   = src.transform;
            else if (n.StartsWith("machine_7")) anchorSteam = src.transform;
            else if (n.StartsWith("conveyor"))  anchorTick  = src.transform;

            if (n.StartsWith("machine_3") || n.StartsWith("machine_1"))
                tapeAnchors.Add(src.transform);

            if (n.StartsWith("machine"))
                src.volume *= ambienceDuck;
        }

        // Eksik çapalar için yedekler — ses kaynaksız (2D) kalmasın
        if (anchorHit  == null) anchorHit  = anchorTick ?? (tapeAnchors.Count > 0 ? tapeAnchors[0] : null);
        if (anchorTick == null) anchorTick = anchorHit;
        if (anchorSteam == null) anchorSteam = anchorHit;
        if (tapeAnchors.Count == 0 && anchorHit != null) tapeAnchors.Add(anchorHit);

        // Tape katmanları odada dağılsın: çapa sayısı azsa steam/konveyörü de kat
        if (tapeAnchors.Count < LoopPeriods.Length)
        {
            if (anchorSteam != null && !tapeAnchors.Contains(anchorSteam)) tapeAnchors.Add(anchorSteam);
            if (anchorTick  != null && !tapeAnchors.Contains(anchorTick))  tapeAnchors.Add(anchorTick);
        }
    }

    private static float SemitoneToPitch(double semitones)
        => (float)System.Math.Pow(2.0, semitones / 12.0);

    // ── Bant kesme atölyesi (tape splicing) ─────────────────────────────

    private bool LoadAndSliceSources()
    {
        AudioClip[] all = Resources.LoadAll<AudioClip>("scene_1_sound_design");
        AudioClip machineHit = null, machineTonalA = null, machineTonalB = null, steam = null;

        foreach (var c in all)
        {
            if (c == null) continue;
            string n = c.name.Replace(" ", "");
            if      (n.StartsWith("machine_4")) machineHit    = c;
            else if (n.StartsWith("machine_3")) machineTonalA = c;
            else if (n.StartsWith("machine_1")) machineTonalB = c;
            else if (n.StartsWith("machine_7")) steam         = c;
        }

        if (machineHit == null && machineTonalA == null) return false;

        // Perküsif vuruş: machine_4 içindeki en güçlü transient'ten ~180ms
        hitSlice  = SliceAtLoudestTransient(machineHit ?? machineTonalA, 0.18f, "OstinatoHit");

        // Tik: machine_1 transient'i, daha kısa ve hafif
        tickSlice = SliceAtLoudestTransient(machineTonalB ?? machineHit ?? machineTonalA, 0.09f, "OstinatoTick");

        // Steam nefesi: kaydın ortasından 2.5s, yumuşak zarf
        steamSlice = steam != null
            ? SliceWithFades(steam, steam.length * 0.3f, 2.5f, 0.4f, 1.2f, "SteamBreath")
            : null;

        // Eno tape loop'ları: uzun kayıtlardan farklı bölgelerden 8-10s dilimler
        var tapes = new List<AudioClip>();
        AudioClip longSrc = machineTonalA ?? machineTonalB;
        if (longSrc != null)
        {
            float len = longSrc.length;
            AddIfValid(tapes, SliceWithFades(longSrc, len * 0.05f, Mathf.Min(9.5f, len * 0.4f), 2.5f, 3.0f, "Tape_A"));
            AddIfValid(tapes, SliceWithFades(longSrc, len * 0.45f, Mathf.Min(8.0f, len * 0.4f), 2.5f, 3.0f, "Tape_B"));
        }
        if (machineTonalB != null && machineTonalA != null)
        {
            float len = machineTonalB.length;
            AddIfValid(tapes, SliceWithFades(machineTonalB, len * 0.15f, Mathf.Min(10f, len * 0.5f), 2.5f, 3.0f, "Tape_C"));
            AddIfValid(tapes, SliceWithFades(machineTonalB, len * 0.55f, Mathf.Min(7.5f, len * 0.4f), 2.5f, 3.0f, "Tape_D"));
        }
        tapeSlices = tapes.ToArray();

        return hitSlice != null && tapeSlices.Length > 0;
    }

    private static void AddIfValid(List<AudioClip> list, AudioClip clip)
    {
        if (clip != null) list.Add(clip);
    }

    /// <summary>Kayıttaki en yüksek enerjili anı bulur, hemen öncesinden kısa bir vuruş keser.</summary>
    private AudioClip SliceAtLoudestTransient(AudioClip src, float duration, string name)
    {
        if (src == null) return null;
        float[] data = new float[src.samples * src.channels];
        if (!src.GetData(data, 0)) return null;

        // 10ms pencerelerle RMS taraması (interleaved index → frame'e çevrilir)
        int ch  = src.channels;
        int win = Mathf.Max(1, src.frequency / 100) * ch;
        int bestStart = 0;
        float bestEnergy = -1f;
        for (int i = 0; i + win < data.Length; i += win)
        {
            float e = 0f;
            for (int j = 0; j < win; j++) e += data[i + j] * data[i + j];
            if (e > bestEnergy) { bestEnergy = e; bestStart = i; }
        }

        // Atak kaybolmasın diye 5ms geriden başla
        int attackPad = src.frequency / 200;
        float startSec = Mathf.Max(0f, (bestStart / ch - attackPad) / (float)src.frequency);
        return SliceWithFades(src, startSec, duration, 0.003f, duration * 0.6f, name);
    }

    /// <summary>Kayıttan dilim keser, kenarlara fade uygular — dijital bant makası.</summary>
    private AudioClip SliceWithFades(AudioClip src, float startSec, float durationSec,
                                     float fadeIn, float fadeOut, string name)
    {
        if (src == null) return null;
        int freq  = src.frequency;
        int start = Mathf.Clamp((int)(startSec * freq), 0, src.samples - 1);
        int count = Mathf.Min((int)(durationSec * freq), src.samples - start);
        if (count <= freq / 100) return null;

        float[] slice = new float[count];
        float[] full  = new float[src.samples * src.channels];
        if (!src.GetData(full, 0)) return null;

        // Mono varsayımı (importer forceToMono: 1) — yine de kanal 0'ı al
        int ch = src.channels;
        for (int i = 0; i < count; i++) slice[i] = full[(start + i) * ch];

        int fadeInSamples  = Mathf.Min((int)(fadeIn * freq), count / 2);
        int fadeOutSamples = Mathf.Min((int)(fadeOut * freq), count / 2);
        for (int i = 0; i < fadeInSamples; i++)
            slice[i] *= i / (float)fadeInSamples;
        for (int i = 0; i < fadeOutSamples; i++)
            slice[count - 1 - i] *= i / (float)fadeOutSamples;

        var clip = AudioClip.Create(name, count, 1, freq, false);
        clip.SetData(slice, 0);
        return clip;
    }

    // ── Onay / hata geri bildirim tonları ───────────────────────────────

    /// <summary>
    /// Kutu geri bildirimi için iki ayrı prosedürel klip üretir.
    /// Makine gürültüsü diliminin pitch'i algılanamadığından onay ile hata
    /// birbirinden ayırt edilemiyordu — net duyulan tonlar şart.
    /// </summary>
    private void GenerateFeedbackTones()
    {
        confirmTone = GenerateHarmonicTone(440f, 0.5f, "ConfirmTone");
        errorBuzz   = GenerateBuzzTone(110f, 0.22f, "ErrorBuzz");
    }

    /// <summary>Yumuşak zarlı, harmonik (sinüs + üst ton) onay tonu.</summary>
    private static AudioClip GenerateHarmonicTone(float freq, float duration, string name)
    {
        int rate  = AudioSettings.outputSampleRate;
        int count = (int)(rate * duration);
        float[] s = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t   = i / (float)rate;
            float env = Mathf.Exp(-6f * t / duration);     // üstel sönüm — çan benzeri
            float w   = 2f * Mathf.PI * freq * t;
            s[i] = (Mathf.Sin(w) + 0.35f * Mathf.Sin(2f * w) + 0.15f * Mathf.Sin(3f * w))
                   * env * 0.6f;
        }
        var clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(s, 0);
        return clip;
    }

    /// <summary>Sert, testere dişli hata vızıltısı — hafif detune ile kalınlaştırılmış.</summary>
    private static AudioClip GenerateBuzzTone(float freq, float duration, string name)
    {
        int rate  = AudioSettings.outputSampleRate;
        int count = (int)(rate * duration);
        float[] s = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t   = i / (float)rate;
            // Kenarlarda kısa fade — tıklama (click) önler
            float env = Mathf.Min(1f, Mathf.Min(t / 0.005f, (duration - t) / 0.03f));
            float p1  = t * freq;          // testere dişi 1
            float p2  = t * freq * 1.01f;  // hafif detune — daha "arızalı" tını
            float saw1 = 2f * (p1 - Mathf.Floor(p1 + 0.5f));
            float saw2 = 2f * (p2 - Mathf.Floor(p2 + 0.5f));
            s[i] = (saw1 + saw2) * 0.5f * env * 0.55f;
        }
        var clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(s, 0);
        return clip;
    }
}
