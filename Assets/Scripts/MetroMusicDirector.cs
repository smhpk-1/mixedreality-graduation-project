using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene 3 (Metro istasyonu) jeneratif ambiyans + müzik motoru — FactoryMusicDirector'ın kardeşi.
///
/// Brian Eno yaklaşımı: istasyonun kendisi enstrümandır. Her obje kendi
/// ostinato kalıbını çalar, hepsi ortak DSP saatine ve ortak moda (A minör
/// pentatonik) kilitlidir → "her obje kendi paterninde ama hepsi armonide".
///
///   1. RAY OSTINATO   — Track objelerinden derin metalik vuruşlar, 16-step grid.
///   2. BANK PEDLERİ   — Her Metalbench kendi EŞÖLÇÜLEMEZ periyodunda (19.7s,
///      23.3s, 28.9s...) pentatonik bir dyad mırıldanır. Music for Airports
///      tape loop tekniği: katmanlar hiçbir zaman aynı hizada buluşmaz.
///   3. LAMBA VINILTISI— Aydınlatma objelerinden flüoresan/elektrik hum'ı:
///      root + beşli + oktav akor sesleri, sürekli drone.
///   4. ÇÖP KUTULARI   — Her MetalTrashCan'in kendine ait seyrek 2-step'lik
///      tık kalıbı (indeksten türetilir), pentatonik düşük register.
///   5. NPC SESLERİ    — Her NPC kendi periyodunda mırıldanır: pentatonik hum
///      notası / anlaşılmaz konuşma mırıltısı (Scene2 ses kayıtlarından
///      lowpass'lenmiş dilimler) / fısıltı (zarf × gürültü sentezi).
///      Ses NPC'nin üstündeki AudioSource'tan çıkar → NPC'yle yürür.
///   6. ANONS SİSTEMİ  — Platform tavanına yerleştirilen sanal PA hoparlörleri:
///      çan (A-D-E) + TTS anonsları ("The next train departs to West Coast...").
///      Highpass+lowpass+distortion+echo ile gerçekçi istasyon PA tınısı.
///      Tren olaylarına bağlı: kapı kapanışı, varış, kalkış anonsları.
///   7. TÜNEL UĞULTUSU — Ray uçlarından kahverengi gürültü rumble bed'i.
///   8. ÇÖP TOPLAMA    — TrashCart'a atılan her çöp kuantize pentatonik bir
///      nota yürütür (Scene 1'deki bin notalarının akrabası); hedefe ulaşınca
///      küçük bir arpej.
///
/// Sahneye elle eklemek gerekmez: "Scene 3" yüklendiğinde kendini bootstrap eder.
/// </summary>
public class MetroMusicDirector : MonoBehaviour
{
    public static MetroMusicDirector Instance { get; private set; }

    [Header("Tempo / Ton")]
    [Tooltip("İstasyon temposu (BPM) — fabrikadan daha sakin, bekleyiş hissi")]
    public float bpm = 58f;

    [Tooltip("Pattern uzunluğu (16'lık adım sayısı)")]
    public int stepsPerBar = 16;

    [Header("Mix Seviyeleri")]
    [Range(0f, 1f)] public float droneVolume    = 0.07f; // lamba hum'ları (kaynak başına)
    [Range(0f, 1f)] public float padVolume      = 0.22f; // bank pedleri
    [Range(0f, 1f)] public float railVolume     = 0.16f; // ray ostinato
    [Range(0f, 1f)] public float canVolume      = 0.10f; // çöp kutusu tıkları
    [Range(0f, 1f)] public float npcVoiceVolume = 0.38f; // NPC hum/mırıltı/fısıltı
    [Range(0f, 1f)] public float paVolume       = 0.85f; // anonslar
    [Range(0f, 1f)] public float rumbleVolume   = 0.28f; // tünel uğultusu
    [Range(0f, 1f)] public float hissVolume     = 0.45f; // tren fren hışırtısı
    [Range(0f, 1f)] public float cartNoteVolume = 0.55f; // çöp toplama notaları

    [Header("Mekansal Ses (3D)")]
    public float layerMinDistance = 2f;
    public float layerMaxDistance = 25f;
    [Tooltip("PA hoparlörleri platformun her yerinden duyulsun")]
    public float paMaxDistance = 45f;

    [Header("Anons Zamanlaması")]
    [Tooltip("Boşta (idle) anonslar arası min/max süre (saniye)")]
    public float announceIntervalMin = 50f;
    public float announceIntervalMax = 85f;

    // ── Mod: A minör pentatonik (yarım ton offsetleri) ───────────────────
    private static readonly int[] Pentatonic = { 0, 3, 5, 7, 10 };

    // Bank tape loop periyotları (saniye) — kasıtlı olarak eşölçülemez
    private static readonly float[] BenchPeriods =
        { 19.7f, 23.3f, 28.9f, 31.7f, 37.1f, 41.3f, 43.9f, 47.7f, 53.3f, 59.3f };

    // Bank dyad'ları (A3'e göre yarım ton çiftleri) — hepsi pentatonik içi
    private static readonly int[,] BenchDyads =
        { { 0, 7 }, { 3, 12 }, { 5, 12 }, { 7, 15 }, { 10, 19 } };

    // NPC ses periyotları (saniye) — yine eşölçülemez, NPC başına offsetlenir
    private static readonly float[] NpcPeriods = { 11.3f, 13.7f, 17.9f, 19.3f, 23.7f, 29.3f };

    // Lamba drone akoru: root(A2) + beşli(E3) + oktav(A3), 110Hz hum'a göre
    private static readonly int[] DroneChord = { 0, 7, 12, 7 };

    private const double ScheduleLookahead = 0.6; // saniye — DSP planlama penceresi

    // ── Sentezlenen "enstrümanlar" ───────────────────────────────────────
    private AudioClip humLoop;    // 110Hz flüoresan vınıltısı (kusursuz loop)
    private AudioClip padClip;    // 220Hz vokalimsi "ahh" hum notası (yumuşak zarf)
    private AudioClip bellClip;   // 440Hz inharmonik çan — chime + cart notaları
    private AudioClip tickClip;   // kısa metalik tık — ray + çöp kutuları
    private AudioClip rumbleLoop; // kahverengi gürültü tünel uğultusu (loop)
    private AudioClip hissClip;   // tren fren / hava hışırtısı

    // Ses kayıtlarından kesilen dokular
    private readonly List<AudioClip> murmurs  = new List<AudioClip>(); // anlaşılmaz konuşma
    private readonly List<AudioClip> whispers = new List<AudioClip>(); // fısıltılar
    private readonly Dictionary<string, AudioClip> announcements = new Dictionary<string, AudioClip>();

    // ── Mekansal çapalar ─────────────────────────────────────────────────
    private readonly List<Transform> benches = new List<Transform>();
    private readonly List<Transform> cans    = new List<Transform>();
    private readonly List<Transform> lamps   = new List<Transform>();
    private readonly List<Transform> tracks  = new List<Transform>();
    private NPCScene3Wanderer[] npcs = new NPCScene3Wanderer[0];
    private Transform railAnchor; // platform ortasına en yakın track

    private readonly List<AudioSource> paSources  = new List<AudioSource>();
    private AudioSource[] npcSources = new AudioSource[0];

    // ── Sequencer durumu ────────────────────────────────────────────────
    private double dspStartTime;
    private double stepDuration;
    private long   nextStepIndex;
    private double[] benchNextTimes;
    private double[] npcNextTimes;
    private double nextIdleAnnounceTime;
    private double paBusyUntil;
    private int idleAnnounceIndex;

    private static readonly string[] IdleAnnouncements =
        { "announce_westcoast", "announce_security", "announce_clean",
          "announce_gap", "announce_depart" };

    // One-shot kaynak havuzu
    private readonly List<AudioSource> pool = new List<AudioSource>();
    private int poolCursor;
    private const int PoolSize = 24;

    // Çöp toplama melodisi durumu
    private int melodyIndex;
    private System.Random rng = new System.Random(1978); // Music for Airports yılı

    // ── Bootstrap: Scene 3 yüklenince kendini kurar ──────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        TrySpawnForScene(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded += (scene, mode) => TrySpawnForScene(scene);
    }

    private static void TrySpawnForScene(Scene scene)
    {
        if (scene.name != "Scene 3") return;
        if (Instance != null) return;
        var go = new GameObject("MetroMusicDirector");
        go.AddComponent<MetroMusicDirector>();
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
        SynthesizeInstruments();
        LoadVoicesAndAnnouncements();
        ScanAnchors();
        BuildSourcePool();
        BuildDrones();
        BuildRumbleBed();
        BuildPASpeakers();
        AttachNpcSources();
        HookTrainEvents();
        HookTrashCart();

        stepDuration = 60.0 / bpm / 4.0; // 16'lık süre
        dspStartTime = AudioSettings.dspTime + 0.5;
        nextStepIndex = 0;

        benchNextTimes = new double[benches.Count];
        for (int i = 0; i < benches.Count; i++)
            benchNextTimes[i] = dspStartTime + 2.0 + i * 2.9; // kademeli giriş

        npcNextTimes = new double[npcs.Length];
        for (int i = 0; i < npcs.Length; i++)
            npcNextTimes[i] = dspStartTime + 4.0 + i * 1.7;

        // İlk idle anons biraz bekletilir — önce ortam otursun
        nextIdleAnnounceTime = dspStartTime + 18.0;
    }

    private void Update()
    {
        double horizon = AudioSettings.dspTime + ScheduleLookahead;

        // 1. Grid adımları: ray ostinato + çöp kutusu kalıpları
        while (StepTime(nextStepIndex) < horizon)
        {
            ScheduleStep(nextStepIndex, StepTime(nextStepIndex));
            nextStepIndex++;
        }

        // 2. Bank tape loop'ları — her bank kendi periyodunda kendi dyad'ını çalar
        for (int i = 0; i < benches.Count; i++)
        {
            while (benchNextTimes[i] < horizon)
            {
                int d = i % BenchDyads.GetLength(0);
                ScheduleOneShot(padClip, benchNextTimes[i], SemitoneToPitch(BenchDyads[d, 0]),
                                padVolume, benches[i].position, true, layerMinDistance, layerMaxDistance);
                ScheduleOneShot(padClip, benchNextTimes[i] + stepDuration * 2,
                                SemitoneToPitch(BenchDyads[d, 1]),
                                padVolume * 0.7f, benches[i].position, true, layerMinDistance, layerMaxDistance);
                benchNextTimes[i] += BenchPeriods[i % BenchPeriods.Length];
            }
        }

        // 3. NPC sesleri — her NPC kendi periyodunda, kendi gövdesinden
        for (int i = 0; i < npcs.Length; i++)
        {
            while (npcNextTimes[i] < horizon)
            {
                ScheduleNpcVoice(i, QuantizeUp(npcNextTimes[i], 1));
                npcNextTimes[i] += NpcPeriods[i % NpcPeriods.Length] + i * 0.37f;
            }
        }

        // 4. Boşta PA anonsları
        if (AudioSettings.dspTime >= nextIdleAnnounceTime && AudioSettings.dspTime > paBusyUntil)
        {
            string key = IdleAnnouncements[idleAnnounceIndex % IdleAnnouncements.Length];
            idleAnnounceIndex++;
            PlayAnnouncement(key, withChime: true);
            // Anons yüklenememiş olsa bile zamanlayıcı ilerlesin (her frame retry olmasın)
            nextIdleAnnounceTime = System.Math.Max(paBusyUntil, AudioSettings.dspTime) +
                Random.Range(announceIntervalMin, announceIntervalMax);
        }
    }

    private double StepTime(long step) => dspStartTime + step * stepDuration;

    // ── Grid kalıbı: ray + çöp kutuları ─────────────────────────────────
    private void ScheduleStep(long step, double time)
    {
        int stepInBar = (int)(step % stepsPerBar);
        long bar      = step / stepsPerBar;

        // Ray ostinato: 1'de root (A2), 9'da beşli (E2 üstü) — ağır, derin
        if (railAnchor != null)
        {
            if (stepInBar == 0)
                ScheduleAnchored(tickClip, time, SemitoneToPitch(-24), railVolume, railAnchor);
            else if (stepInBar == 8)
                ScheduleAnchored(tickClip, time, SemitoneToPitch(-17), railVolume * 0.8f, railAnchor);
            // Bar kapanışında seyrek oktav aksanı
            else if (stepInBar == stepsPerBar - 2 && bar % 4 == 3)
                ScheduleAnchored(tickClip, time, SemitoneToPitch(-12), railVolume * 0.5f, railAnchor);
        }

        // Çöp kutuları: her kutunun indeksten türeyen 2 step'lik kendi kalıbı.
        // Seyrek (her bar %45 şans) → platforma dağılmış metalik parıltı.
        for (int i = 0; i < cans.Count; i++)
        {
            int sA = (i * 3 + 1) % stepsPerBar;
            int sB = (i * 5 + 9) % stepsPerBar;
            if (stepInBar != sA && stepInBar != sB) continue;
            if (rng.NextDouble() > 0.45) continue;

            int degree = Pentatonic[i % Pentatonic.Length];
            ScheduleAnchored(tickClip, time, SemitoneToPitch(degree - 12),
                             canVolume, cans[i]);
        }
    }

    // ── NPC sesi: hum notası / mırıltı / fısıltı ────────────────────────
    private void ScheduleNpcVoice(int i, double time)
    {
        var npc = npcs[i];
        if (npc == null || !npc.gameObject.activeInHierarchy) return;
        if (npc.transform.position.y < -100f) return; // hidden state (tren döngüsü)

        AudioSource src = npcSources[i];
        if (src == null) return;

        double roll = rng.NextDouble();
        if (roll < 0.5)
        {
            // Pentatonik hum — NPC kendi kendine mırıldanıyor, modun içinde
            int degree = Pentatonic[rng.Next(Pentatonic.Length)];
            int octave = rng.NextDouble() < 0.3 ? 12 : 0;
            src.clip   = padClip;
            src.pitch  = SemitoneToPitch(degree + octave - 12); // A2-A3 bandı: insan hum'ı
            src.volume = npcVoiceVolume * 0.8f;
        }
        else if (roll < 0.8 && murmurs.Count > 0)
        {
            // Anlaşılmaz konuşma mırıltısı
            src.clip   = murmurs[rng.Next(murmurs.Count)];
            src.pitch  = SemitoneToPitch(rng.Next(-3, 2));
            src.volume = npcVoiceVolume;
        }
        else if (whispers.Count > 0)
        {
            // Fısıltı
            src.clip   = whispers[rng.Next(whispers.Count)];
            src.pitch  = 1f;
            src.volume = npcVoiceVolume * 1.1f;
        }
        else return;

        src.PlayScheduled(time);
    }

    // ── PA anons sistemi ────────────────────────────────────────────────

    /// <summary>Anonsu (istenirse çan girişiyle) tüm PA hoparlörlerinden planlar.</summary>
    public void PlayAnnouncement(string key, bool withChime)
    {
        if (!announcements.TryGetValue(key, out AudioClip speech) || speech == null) return;
        if (paSources.Count == 0) return;

        double t = QuantizeUp(AudioSettings.dspTime + 0.15, withChime ? 8 : 2);
        double speechTime = t;

        if (withChime)
        {
            // Çan: A4 - D5 - E5 (pentatonik içi), 8'lik aralıklarla, hoparlörlerden
            int[] chime = { 0, 5, 7 };
            for (int n = 0; n < chime.Length; n++)
                foreach (var pa in paSources)
                    ScheduleOneShot(bellClip, t + n * stepDuration * 2,
                                    SemitoneToPitch(chime[n]), paVolume * 0.35f,
                                    pa.transform.position, true, 3f, paMaxDistance);
            speechTime = t + 3 * stepDuration * 2 + 0.25;
        }

        foreach (var pa in paSources)
        {
            pa.clip   = speech;
            pa.volume = paVolume / Mathf.Sqrt(paSources.Count); // çok hoparlör = tek tek kısık
            pa.PlayScheduled(speechTime);
        }

        paBusyUntil = speechTime + speech.length + 1.0;
    }

    // ── Tren olayları ───────────────────────────────────────────────────
    private void HookTrainEvents()
    {
        foreach (var train in FindObjectsByType<SubwayTrainController>(FindObjectsSortMode.None))
        {
            var t = train; // closure kopyası
            t.OnArrivedAtStop += stopCount =>
            {
                PlayBrakeHiss(t.transform.position);
                if (stopCount <= 1)
                    PlayAnnouncement("announce_arrival", withChime: false);
                else if (rng.NextDouble() < 0.5)
                    PlayAnnouncement("announce_gap", withChime: false);
            };
            t.OnDoorsClosing += () => PlayAnnouncement("announce_doors", withChime: false);
            t.OnDeparted += () =>
            {
                PlayBrakeHiss(t.transform.position);
                if (rng.NextDouble() < 0.4)
                    PlayAnnouncement("announce_depart", withChime: true);
            };
        }
    }

    private void PlayBrakeHiss(Vector3 pos)
    {
        ScheduleOneShot(hissClip, AudioSettings.dspTime + 0.05, 1f, hissVolume,
                        pos, true, 3f, 35f);
    }

    // ── Çöp toplama melodisi ────────────────────────────────────────────
    private void HookTrashCart()
    {
        foreach (var cart in FindObjectsByType<TrashCart>(FindObjectsSortMode.None))
        {
            var c = cart;
            c.OnTrashCollected += (current, target) => PlayCartNote(c, current, target);
            c.OnGoalReached += () => PlayCartArpeggio(c);
        }
    }

    private void PlayCartNote(TrashCart cart, int current, int target)
    {
        // Scene 1 bin melodisinin akrabası: pentatonik yürüyüş, ilerledikçe register yükselir
        melodyIndex += rng.NextDouble() < 0.75 ? (rng.NextDouble() < 0.5 ? 1 : -1)
                                               : (rng.NextDouble() < 0.5 ? 2 : -2);
        melodyIndex = Mathf.Clamp(melodyIndex, 0, Pentatonic.Length * 2 - 1);

        int rise   = Mathf.RoundToInt(Mathf.Clamp01(current / (float)Mathf.Max(1, target)) * 7f);
        int degree = Pentatonic[melodyIndex % Pentatonic.Length];
        int octave = (melodyIndex / Pentatonic.Length) * 12;

        int semis = degree + octave + rise;
        while (semis > 19) semis -= 12;

        double t = QuantizeUp(AudioSettings.dspTime + 0.05, 1);
        ScheduleOneShot(bellClip, t, SemitoneToPitch(semis), cartNoteVolume,
                        cart.transform.position, true, 2f, 25f);

        // Her 5. çöpte beşli paralel onay notası
        if (current % 5 == 0)
            ScheduleOneShot(bellClip, t + stepDuration * 2, SemitoneToPitch(semis + 7),
                            cartNoteVolume * 0.7f, cart.transform.position, true, 2f, 25f);
    }

    private void PlayCartArpeggio(TrashCart cart)
    {
        int[] arp = { 0, 7, 12, 19 };
        double t = QuantizeUp(AudioSettings.dspTime + 0.05, 2);
        for (int n = 0; n < arp.Length; n++)
            ScheduleOneShot(bellClip, t + n * stepDuration * 2, SemitoneToPitch(arp[n]),
                            cartNoteVolume, cart.transform.position, true, 2f, 30f);
    }

    // ── Kurulum: çapalar, drone'lar, PA, NPC kaynakları ─────────────────

    private void ScanAnchors()
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            string n = t.name;
            if      (n.StartsWith("Metalbench"))    benches.Add(t);
            else if (n.StartsWith("MetalTrashCan")) cans.Add(t);
            else if (n.StartsWith("Lights"))        lamps.Add(t);
            else if (n.StartsWith("Track"))         tracks.Add(t);
        }

        npcs = FindObjectsByType<NPCScene3Wanderer>(FindObjectsSortMode.None);

        // Çok obje varsa eşit aralıklarla seyrelt — kaynak sayısı patlamasın
        Thin(benches, BenchPeriods.Length);
        Thin(cans, 8);
        Thin(lamps, 8);

        // Lamba pozisyonları mantıklı mı? (grup pivot'u origin'de kalmış olabilir)
        Vector3 centroid = Centroid(benches);
        lamps.RemoveAll(l => Vector3.Distance(l.position, centroid) > 80f);

        // Ray çapası: bank merkezine en yakın track
        float best = float.MaxValue;
        foreach (var t in tracks)
        {
            float d = Vector3.Distance(t.position, centroid);
            if (d < best) { best = d; railAnchor = t; }
        }
    }

    private static void Thin(List<Transform> list, int max)
    {
        if (list.Count <= max) return;
        var kept = new List<Transform>(max);
        for (int i = 0; i < max; i++)
            kept.Add(list[i * list.Count / max]);
        list.Clear();
        list.AddRange(kept);
    }

    private static Vector3 Centroid(List<Transform> list)
    {
        if (list.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (var t in list) sum += t.position;
        return sum / list.Count;
    }

    /// <summary>Lamba objelerine flüoresan hum drone'ları — root/beşli/oktav akoru.</summary>
    private void BuildDrones()
    {
        for (int i = 0; i < lamps.Count; i++)
        {
            var src = NewChildSource($"Drone_{i:00}", lamps[i].position);
            src.clip         = humLoop;
            src.loop         = true;
            src.pitch        = SemitoneToPitch(DroneChord[i % DroneChord.Length]);
            src.volume       = droneVolume;
            src.minDistance  = 1.5f;
            src.maxDistance  = 12f;
            src.Play();
        }
    }

    /// <summary>Ray uçlarına tünel uğultusu bed'i.</summary>
    private void BuildRumbleBed()
    {
        if (tracks.Count == 0) return;

        // En uç iki track'i bul (birbirinden en uzak çift yaklaşımı: min/max x+z)
        Transform a = tracks[0], b = tracks[0];
        foreach (var t in tracks)
        {
            if (t.position.x + t.position.z < a.position.x + a.position.z) a = t;
            if (t.position.x + t.position.z > b.position.x + b.position.z) b = t;
        }

        foreach (var anchor in new[] { a, b })
        {
            var src = NewChildSource($"Rumble_{anchor.name}", anchor.position);
            src.clip        = rumbleLoop;
            src.loop        = true;
            src.volume      = rumbleVolume;
            src.minDistance = 4f;
            src.maxDistance = 45f;
            src.Play();
            if (a == b) break;
        }
    }

    /// <summary>Platform tavanına 3 sanal PA hoparlörü kurar (filtre zinciriyle).</summary>
    private void BuildPASpeakers()
    {
        Vector3 centroid = Centroid(benches);
        if (benches.Count == 0) centroid = transform.position;

        // Platformun ana eksenini bul: bankların en geniş yayıldığı eksen
        Vector3 min = centroid, max = centroid;
        foreach (var bn in benches)
        {
            min = Vector3.Min(min, bn.position);
            max = Vector3.Max(max, bn.position);
        }
        Vector3 extent = max - min;
        Vector3 axis = Mathf.Abs(extent.x) > Mathf.Abs(extent.z) ? Vector3.right : Vector3.forward;
        float span = Mathf.Max(Mathf.Abs(extent.x), Mathf.Abs(extent.z));

        for (int i = -1; i <= 1; i++)
        {
            Vector3 pos = centroid + axis * (i * span * 0.33f) + Vector3.up * 3f;
            var src = NewChildSource($"PASpeaker_{i + 1}", pos);
            src.minDistance  = 3f;
            src.maxDistance  = paMaxDistance;

            // PA tınısı: telefon bandı + hafif kırılma + istasyon yankısı
            var go = src.gameObject;
            go.AddComponent<AudioHighPassFilter>().cutoffFrequency = 500f;
            go.AddComponent<AudioLowPassFilter>().cutoffFrequency  = 4500f;
            go.AddComponent<AudioDistortionFilter>().distortionLevel = 0.12f;
            var echo = go.AddComponent<AudioEchoFilter>();
            echo.delay      = 170f;
            echo.decayRatio = 0.25f;
            echo.wetMix     = 0.35f;

            paSources.Add(src);
        }
    }

    /// <summary>Her NPC'ye gövdesini takip eden bir ses kaynağı asar (ağız hizası).</summary>
    private void AttachNpcSources()
    {
        npcSources = new AudioSource[npcs.Length];
        for (int i = 0; i < npcs.Length; i++)
        {
            if (npcs[i] == null) continue;
            var go = new GameObject("NPCVoice");
            go.transform.SetParent(npcs[i].transform, false);
            go.transform.localPosition = Vector3.up * 1.55f;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.spatialBlend = 1f;
            src.dopplerLevel = 0f;
            src.rolloffMode  = AudioRolloffMode.Linear;
            src.minDistance  = 1f;
            src.maxDistance  = 12f;
            npcSources[i] = src;
        }
    }

    // ── Ses planlama altyapısı ──────────────────────────────────────────

    private void ScheduleAnchored(AudioClip clip, double dspTime, float pitch,
                                  float volume, Transform anchor)
    {
        if (anchor == null) return;
        ScheduleOneShot(clip, dspTime, pitch, volume, anchor.position, true,
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
        poolCursor = (poolCursor + 1) % pool.Count;
        return pool[poolCursor];
    }

    private void BuildSourcePool()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            var src = NewChildSource($"Voice_{i:00}", transform.position);
            pool.Add(src);
        }
    }

    private AudioSource NewChildSource(string name, Vector3 pos)
    {
        var child = new GameObject(name);
        child.transform.SetParent(transform, false);
        child.transform.position = pos;
        var src = child.AddComponent<AudioSource>();
        src.playOnAwake  = false;
        src.spatialBlend = 1f;
        src.dopplerLevel = 0f;
        src.rolloffMode  = AudioRolloffMode.Linear;
        src.minDistance  = 2f;
        src.maxDistance  = 25f;
        return src;
    }

    /// <summary>Bir sonraki grid noktası (quantizeTo: kaç 16'lıkta bir).</summary>
    private double QuantizeUp(double dspTime, int quantizeTo)
    {
        double span = stepDuration * quantizeTo;
        long   n    = (long)System.Math.Ceiling((dspTime - dspStartTime) / span);
        return dspStartTime + n * span;
    }

    private static float SemitoneToPitch(double semitones)
        => (float)System.Math.Pow(2.0, semitones / 12.0);

    // ── Sentez atölyesi ─────────────────────────────────────────────────

    private const int SynthRate = 44100;

    private void SynthesizeInstruments()
    {
        humLoop    = SynthHumLoop(110f, 2f);
        padClip    = SynthPad(220f, 3.2f);
        bellClip   = SynthBell(440f, 1.8f, 0.5f);
        tickClip   = SynthBell(440f, 0.55f, 0.12f);
        rumbleLoop = SynthRumble(8f);
        hissClip   = SynthHiss(1.6f);
    }

    /// <summary>Flüoresan vınıltısı: temel + harmonikler, tam periyot sayısı → kusursuz loop.</summary>
    private AudioClip SynthHumLoop(float freq, float seconds)
    {
        // Loop'un tıklamasız olması için süreyi tam döngü sayısına yuvarla
        int cycles  = Mathf.RoundToInt(freq * seconds);
        int samples = Mathf.RoundToInt(cycles / freq * SynthRate);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            double ph = 2.0 * System.Math.PI * freq * i / SynthRate;
            data[i] = (float)(0.55 * System.Math.Sin(ph)
                            + 0.30 * System.Math.Sin(ph * 2)
                            + 0.10 * System.Math.Sin(ph * 3)
                            + 0.05 * System.Math.Sin(ph * 4.02)); // hafif detune titreşimi
        }
        return MakeClip("MetroHum", data, true);
    }

    /// <summary>Vokalimsi hum notası: harmonik seri + vibrato, yumuşak zarf.</summary>
    private AudioClip SynthPad(float freq, float seconds)
    {
        int samples = (int)(seconds * SynthRate);
        float[] data = new float[samples];
        int attack  = (int)(0.8f * SynthRate);
        int release = (int)(1.2f * SynthRate);
        for (int i = 0; i < samples; i++)
        {
            double t  = (double)i / SynthRate;
            double vib = 1.0 + 0.003 * System.Math.Sin(2 * System.Math.PI * 5.0 * t);
            double ph = 2.0 * System.Math.PI * freq * vib * t;
            float s = (float)(0.60 * System.Math.Sin(ph)
                            + 0.25 * System.Math.Sin(ph * 2)
                            + 0.10 * System.Math.Sin(ph * 3)
                            + 0.05 * System.Math.Sin(ph * 4));
            float env = 1f;
            if (i < attack) env = i / (float)attack;
            else if (i > samples - release) env = (samples - i) / (float)release;
            data[i] = s * env * env; // karesel zarf — daha yumuşak giriş/çıkış
        }
        return MakeClip("MetroPad", data, false);
    }

    /// <summary>İnharmonik çan/metal tınısı — istasyon chime'ı ve metalik tıklar.</summary>
    private AudioClip SynthBell(float freq, float seconds, float tau)
    {
        int samples = (int)(seconds * SynthRate);
        float[] data = new float[samples];
        double[] partials = { 1.0, 2.76, 5.40, 8.93 };
        double[] amps     = { 1.0, 0.40, 0.18, 0.08 };
        for (int i = 0; i < samples; i++)
        {
            double t = (double)i / SynthRate;
            double s = 0;
            for (int p = 0; p < partials.Length; p++)
                s += amps[p] * System.Math.Sin(2 * System.Math.PI * freq * partials[p] * t)
                             * System.Math.Exp(-t / (tau * (1.0 - p * 0.18)));
            // İlk 3ms fade-in (tık önleme)
            float env = i < SynthRate * 3 / 1000 ? i / (SynthRate * 0.003f) : 1f;
            data[i] = (float)(s * 0.55) * env;
        }
        return MakeClip("MetroBell", data, false);
    }

    /// <summary>Tünel uğultusu: kahverengi gürültü + lowpass, dairesel crossfade ile loop.</summary>
    private AudioClip SynthRumble(float seconds)
    {
        int samples = (int)(seconds * SynthRate);
        float[] data = new float[samples];
        var noiseRng = new System.Random(42);
        float brown = 0f, lp = 0f;
        float lpCoef = 1f - Mathf.Exp(-2f * Mathf.PI * 150f / SynthRate);
        for (int i = 0; i < samples; i++)
        {
            float white = (float)(noiseRng.NextDouble() * 2.0 - 1.0);
            brown = brown * 0.995f + white * 0.02f;
            lp += lpCoef * (brown - lp);
            data[i] = lp * 18f;
        }
        // Dairesel crossfade: son 0.5s, başlangıçla harmanlanır → tıklamasız loop
        int xfade = (int)(0.5f * SynthRate);
        for (int i = 0; i < xfade; i++)
        {
            float w = i / (float)xfade;
            data[samples - xfade + i] = data[samples - xfade + i] * (1f - w) + data[i] * w;
        }
        return MakeClip("TunnelRumble", data, true);
    }

    /// <summary>Tren fren/hava hışırtısı: highpass'li gürültü, hızlı atak + uzun sönüş.</summary>
    private AudioClip SynthHiss(float seconds)
    {
        int samples = (int)(seconds * SynthRate);
        float[] data = new float[samples];
        var noiseRng = new System.Random(7);
        float lp = 0f;
        float lpCoef = 1f - Mathf.Exp(-2f * Mathf.PI * 800f / SynthRate);
        int attack = (int)(0.05f * SynthRate);
        for (int i = 0; i < samples; i++)
        {
            float white = (float)(noiseRng.NextDouble() * 2.0 - 1.0);
            lp += lpCoef * (white - lp);
            float hp = white - lp; // highpass: tiz hışırtı bandı
            float env = i < attack ? i / (float)attack
                                   : Mathf.Exp(-(i - attack) / (0.45f * SynthRate));
            data[i] = hp * env * 0.5f;
        }
        return MakeClip("BrakeHiss", data, false);
    }

    private static AudioClip MakeClip(string name, float[] data, bool _)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SynthRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ── Ses kayıtları: anonslar + mırıltı/fısıltı dokuları ──────────────

    private void LoadVoicesAndAnnouncements()
    {
        AudioClip[] all = Resources.LoadAll<AudioClip>("scene_3_sound_design");
        var voices = new List<AudioClip>();

        foreach (var c in all)
        {
            if (c == null) continue;
            if (c.name.StartsWith("announce_")) announcements[c.name] = c;
            else if (c.name.StartsWith("voice_")) voices.Add(c);
        }

        // Her ses kaydından: 2 mırıltı dilimi + 1 fısıltı dilimi
        foreach (var v in voices)
        {
            float len = v.length;
            AddIfValid(murmurs,  MakeMurmur(v, len * 0.18f, Mathf.Min(2.4f, len * 0.4f)));
            AddIfValid(murmurs,  MakeMurmur(v, len * 0.55f, Mathf.Min(2.0f, len * 0.35f)));
            AddIfValid(whispers, MakeWhisper(v, len * 0.35f, Mathf.Min(1.8f, len * 0.35f)));
        }

        if (announcements.Count == 0)
            Debug.LogWarning("[MetroMusicDirector] Anons kayıtları bulunamadı (Resources/scene_3_sound_design).");
    }

    private static void AddIfValid(List<AudioClip> list, AudioClip clip)
    {
        if (clip != null) list.Add(clip);
    }

    /// <summary>Konuşma kaydından anlaşılmaz mırıltı: çift lowpass + normalize + fade.</summary>
    private AudioClip MakeMurmur(AudioClip src, float startSec, float durSec)
    {
        float[] slice = ExtractMono(src, startSec, durSec);
        if (slice == null) return null;

        // İki geçiş one-pole lowpass (~800Hz) — kelimeler erir, ton kalır
        float coef = 1f - Mathf.Exp(-2f * Mathf.PI * 800f / src.frequency);
        for (int pass = 0; pass < 2; pass++)
        {
            float lp = 0f;
            for (int i = 0; i < slice.Length; i++)
            {
                lp += coef * (slice[i] - lp);
                slice[i] = lp;
            }
        }

        Normalize(slice, 0.8f);
        Fade(slice, src.frequency, 0.15f, 0.4f);
        var clip = AudioClip.Create("Murmur_" + src.name, slice.Length, 1, src.frequency, false);
        clip.SetData(slice, 0);
        return clip;
    }

    /// <summary>Fısıltı sentezi: konuşmanın genlik zarfı × beyaz gürültü, highpass bandı.</summary>
    private AudioClip MakeWhisper(AudioClip src, float startSec, float durSec)
    {
        float[] slice = ExtractMono(src, startSec, durSec);
        if (slice == null) return null;

        var noiseRng = new System.Random(src.name.GetHashCode());
        float envCoef = 1f - Mathf.Exp(-2f * Mathf.PI * 25f / src.frequency);   // zarf takibi
        float hpCoef  = 1f - Mathf.Exp(-2f * Mathf.PI * 1400f / src.frequency); // tiz bant
        float env = 0f, lp = 0f;
        for (int i = 0; i < slice.Length; i++)
        {
            env += envCoef * (Mathf.Abs(slice[i]) - env);
            float white = (float)(noiseRng.NextDouble() * 2.0 - 1.0);
            lp += hpCoef * (white - lp);
            slice[i] = (white - lp) * env * 2.2f;
        }

        Normalize(slice, 0.5f);
        Fade(slice, src.frequency, 0.2f, 0.35f);
        var clip = AudioClip.Create("Whisper_" + src.name, slice.Length, 1, src.frequency, false);
        clip.SetData(slice, 0);
        return clip;
    }

    /// <summary>Kayıttan mono dilim çıkarır (kanal 0).</summary>
    private static float[] ExtractMono(AudioClip src, float startSec, float durSec)
    {
        if (src == null) return null;
        int freq  = src.frequency;
        int start = Mathf.Clamp((int)(startSec * freq), 0, src.samples - 1);
        int count = Mathf.Min((int)(durSec * freq), src.samples - start);
        if (count <= freq / 10) return null;

        float[] full = new float[src.samples * src.channels];
        if (!src.GetData(full, 0)) return null;

        int ch = src.channels;
        float[] slice = new float[count];
        for (int i = 0; i < count; i++) slice[i] = full[(start + i) * ch];
        return slice;
    }

    private static void Normalize(float[] data, float peak)
    {
        float max = 0f;
        for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
        if (max < 0.0001f) return;
        float g = peak / max;
        for (int i = 0; i < data.Length; i++) data[i] *= g;
    }

    private static void Fade(float[] data, int freq, float fadeIn, float fadeOut)
    {
        int fi = Mathf.Min((int)(fadeIn * freq), data.Length / 2);
        int fo = Mathf.Min((int)(fadeOut * freq), data.Length / 2);
        for (int i = 0; i < fi; i++) data[i] *= i / (float)fi;
        for (int i = 0; i < fo; i++) data[data.Length - 1 - i] *= i / (float)fo;
    }
}
