using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A floating circular step sequencer for Scene 4 — and, secretly, a clock.
///
/// 12 slots are spaced evenly around a ring (exactly like the hour positions of
/// the Scene 1 factory WallClock). A playhead sweeps around the circle; when it
/// passes a filled slot, that slot's sound is triggered, scheduled on the audio
/// DSP clock for stable timing. The player fills slots by placing grabbable
/// SequencerSampleOrb objects into them.
///
/// The clock reveal (deceleration to 1-second steps, tick takeover, color
/// drain) is driven externally by SequencerFinaleDirector through the public
/// API here: stepInterval, tickDominance, ApplyClockLook, ShowNumbers, etc.
///
/// Visuals are built procedurally from primitives (project convention — see
/// DrumKitGenerator, WallClock.GenerateVisuals). No realtime lights, no
/// particles; emission color changes only. Quest 3S friendly.
/// </summary>
[DefaultExecutionOrder(-100)]
public class RadialSequencer : MonoBehaviour
{
    // ── Layout ──────────────────────────────────────────────────────────
    [Header("Layout")]
    [Tooltip("Number of slots around the ring. 12 = a clock face in disguise. The finale assumes 12.")]
    public int slotCount = 12;

    [Tooltip("Ring radius in meters")]
    public float radius = 0.8f;

    [Tooltip("Height of the ring center above the ground (set on Awake via ground raycast)")]
    public float heightAboveGround = 1.4f;

    [Tooltip("Snap the rig to the terrain/ground surface on Awake (Scene 4 uses a terrain)")]
    public bool snapToGround = true;

    // ── Timing ──────────────────────────────────────────────────────────
    [Header("Timing")]
    [Tooltip("Seconds per step. 0.25 = playful 12-step loop in 3s. The finale slides this to 1.0 — one tick per second.")]
    public float stepInterval = 0.25f;

    [Tooltip("0 = every filled slot plays its own tone. 1 = every step plays a mechanical tick instead. The finale raises this to 1.")]
    [Range(0f, 1f)] public float tickDominance = 0f;

    // ── Audio ───────────────────────────────────────────────────────────
    [Header("Audio")]
    [Range(0f, 1f)] public float stepVolume = 0.9f;
    [Tooltip("Distance where slot sounds play at full volume")]
    public float minDistance = 1.5f;
    [Tooltip("Distance where slot sounds become inaudible")]
    public float maxDistance = 25f;

    // ── Events ──────────────────────────────────────────────────────────
    /// <summary>Fired each time the playhead wraps past slot 0 (one full revolution).</summary>
    public System.Action OnLoopWrapped;
    /// <summary>Fired when a slot becomes filled (slot index).</summary>
    public System.Action<int> OnSlotFilled;
    /// <summary>Fired when a slot becomes empty again (slot index).</summary>
    public System.Action<int> OnSlotFreed;

    // ── Slot state ──────────────────────────────────────────────────────
    public class Slot
    {
        public Transform anchor;            // world anchor on the ring
        public Renderer pad;                // visual pad renderer
        public SequencerSampleOrb occupant; // null = empty
    }
    private Slot[] slots;
    public int FilledCount { get; private set; }

    // ── Internals ───────────────────────────────────────────────────────
    private const double Lookahead = 0.35; // seconds of DSP schedule-ahead
    private double lastStepDsp;
    private int    nextStepIndex;
    private bool   running;

    private AudioClip tickClip, tockClip;
    private readonly List<AudioSource> pool = new List<AudioSource>();
    private int poolCursor;
    private const int PoolSize = 8;
    private System.Random rng = new System.Random(900); // 09:00 — shift start

    // Visual part references (found by name so they survive editor-built scenes)
    private Transform playheadPivot;
    private Transform playheadVisual;
    private Renderer faceR, rimR, hubR, playheadR;
    private Transform hourHand, minuteHand;
    private readonly List<TextMesh> numberTexts = new List<TextMesh>();

    private void Awake()
    {
        if (snapToGround) SnapRigToGround();

        if (transform.Find("RingCenter") == null) BuildVisuals();
        CacheParts();
        CollectSlots();

        tickClip = SequencerSound.CreateTick(false);
        tockClip = SequencerSound.CreateTick(true);
        BuildSourcePool();

        lastStepDsp   = AudioSettings.dspTime + 0.2;
        nextStepIndex = 0;
        running       = true;
    }

    private void Update()
    {
        if (!running) return;
        double now = AudioSettings.dspTime;

        // Schedule upcoming steps inside the lookahead window
        while (lastStepDsp + stepInterval < now + Lookahead)
        {
            lastStepDsp += stepInterval;
            ScheduleStep(nextStepIndex, lastStepDsp);
            nextStepIndex = (nextStepIndex + 1) % slotCount;
            if (nextStepIndex == 0) OnLoopWrapped?.Invoke();
        }

        // Playhead locked to the DSP clock — audio and visual never drift apart.
        // Negative angle = clockwise, matching WallClock's hand convention.
        double stepsElapsed = nextStepIndex - 1 + (now - lastStepDsp) / stepInterval;
        float angle = (float)((stepsElapsed / slotCount) % 1.0) * 360f;
        if (playheadPivot != null)
            playheadPivot.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    // ── Step scheduling ─────────────────────────────────────────────────

    private void ScheduleStep(int index, double dspTime)
    {
        var slot = slots[index];
        bool filled = slot.occupant != null;

        // Once tickDominance rises, even empty slots start ticking —
        // the clock asserts itself over the music.
        bool playTick = rng.NextDouble() < tickDominance;
        AudioClip clip = null;
        if (playTick)        clip = (index % 2 == 0) ? tickClip : tockClip;
        else if (filled)     clip = slot.occupant.toneClip;
        if (clip == null) return;

        var src = NextPooledSource();
        src.transform.position = slot.anchor.position;
        src.clip   = clip;
        src.volume = stepVolume;
        src.pitch  = 1f;
        src.PlayScheduled(dspTime);

        // Visual pulse on the pad, delayed to match the scheduled audio
        float delay = Mathf.Max(0f, (float)(dspTime - AudioSettings.dspTime));
        StartCoroutine(PulsePad(slot, delay, playTick));
    }

    private IEnumerator PulsePad(Slot slot, float delay, bool tick)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (slot.pad == null) yield break;

        Color flash = tick ? new Color(0.9f, 0.9f, 0.9f)
                           : (slot.occupant != null ? slot.occupant.orbColor : Color.white);
        var block = new MaterialPropertyBlock();
        float life = 0.18f, t = 0f;
        while (t < life)
        {
            t += Time.deltaTime;
            float k = 1f - t / life;
            slot.pad.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", flash * (k * 1.2f));
            slot.pad.SetPropertyBlock(block);
            yield return null;
        }
        slot.pad.GetPropertyBlock(block);
        block.SetColor("_EmissionColor", Color.black);
        slot.pad.SetPropertyBlock(block);
    }

    private AudioSource NextPooledSource()
    {
        var src = pool[poolCursor];
        poolCursor = (poolCursor + 1) % pool.Count;
        return src;
    }

    private void BuildSourcePool()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            var go = new GameObject("SeqVoice_" + i);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.loop         = false;
            src.spatialBlend = 1f;
            src.rolloffMode  = AudioRolloffMode.Linear;
            src.minDistance  = minDistance;
            src.maxDistance  = maxDistance;
            src.dopplerLevel = 0f;
            pool.Add(src);
        }
    }

    // ── Slot API (used by SequencerSampleOrb) ───────────────────────────

    /// <summary>Nearest empty slot anchor within maxRange of a world position, or -1.</summary>
    public int NearestEmptySlot(Vector3 worldPos, float maxRange)
    {
        int best = -1;
        float bestDist = maxRange;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].occupant != null) continue;
            float d = Vector3.Distance(worldPos, slots[i].anchor.position);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    public Transform GetSlotAnchor(int index) => slots[index].anchor;

    /// <summary>
    /// Claim a slot for an orb. Returns false if another orb got there first
    /// (e.g. two orbs released near the same slot in the same frame) — the
    /// caller should send its orb back home in that case.
    /// </summary>
    public bool FillSlot(int index, SequencerSampleOrb orb)
    {
        if (slots[index].occupant != null) return false;
        slots[index].occupant = orb;
        FilledCount++;
        OnSlotFilled?.Invoke(index);
        PlayPlacementTone(slots[index]);
        return true;
    }

    public void FreeSlot(int index)
    {
        if (slots[index].occupant == null) return;
        slots[index].occupant = null;
        FilledCount--;
        OnSlotFreed?.Invoke(index);
    }

    /// <summary>
    /// Instant confirmation: the newly placed orb's tone plays once on the
    /// next grid step, so placement feels musical instead of waiting up to a
    /// full revolution for the playhead to arrive.
    /// </summary>
    private void PlayPlacementTone(Slot slot)
    {
        if (!running || slot.occupant == null || slot.occupant.toneClip == null) return;

        double now = AudioSettings.dspTime;
        double t = lastStepDsp + stepInterval; // next unscheduled grid point
        while (t < now + 0.05) t += stepInterval;

        var src = NextPooledSource();
        src.transform.position = slot.anchor.position;
        src.clip   = slot.occupant.toneClip;
        src.volume = stepVolume;
        src.pitch  = 1f;
        src.PlayScheduled(t);
        StartCoroutine(PulsePad(slot, (float)(t - now), false));
    }

    // ── Slot highlighting (while the player holds an orb) ───────────────

    /// <summary>
    /// Glow the empty slots while an orb is held: a soft invitation on all of
    /// them, full brightness within snap range of the held orb.
    /// </summary>
    public void HighlightEmptySlots(Vector3 holderPos, float snapRadius)
    {
        if (tickDominance > 0.01f) return; // the clock no longer invites

        var block = new MaterialPropertyBlock();
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s.pad == null || s.occupant != null) continue;
            float d = Vector3.Distance(holderPos, s.anchor.position);
            float strength = d <= snapRadius
                ? 1f
                : 0.25f * Mathf.Clamp01(1.5f - d * 0.4f);
            s.pad.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", new Color(0.7f, 0.7f, 0.9f) * (0.15f + strength * 0.8f));
            s.pad.SetPropertyBlock(block);
        }
    }

    /// <summary>Turn off the empty-slot glow (orb released).</summary>
    public void ClearSlotHighlights()
    {
        var block = new MaterialPropertyBlock();
        foreach (var s in slots)
        {
            if (s.pad == null || s.occupant != null) continue;
            s.pad.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", Color.black);
            s.pad.SetPropertyBlock(block);
        }
    }

    /// <summary>All orbs currently sitting in slots (for the finale to freeze/desaturate).</summary>
    public List<SequencerSampleOrb> GetSlottedOrbs()
    {
        var list = new List<SequencerSampleOrb>();
        foreach (var s in slots)
            if (s.occupant != null) list.Add(s.occupant);
        return list;
    }

    // ── Clock reveal API (used by SequencerFinaleDirector) ──────────────

    /// <summary>
    /// Blend the sequencer's look toward the Scene 1 WallClock: white face,
    /// black rim and hub, slim red second hand, gray pads. p = 0..1.
    /// </summary>
    public void ApplyClockLook(float p)
    {
        LerpColor(faceR, new Color(0.12f, 0.12f, 0.16f), Color.white, p);
        LerpColor(rimR,  new Color(0.25f, 0.2f, 0.45f),  Color.black, p);
        LerpColor(hubR,  new Color(0.3f, 0.3f, 0.4f),    Color.black, p);
        LerpColor(playheadR, new Color(0.2f, 0.9f, 0.8f), Color.red,  p);
        foreach (var s in slots)
            LerpColor(s.pad, new Color(0.35f, 0.35f, 0.45f), new Color(0.2f, 0.2f, 0.2f), p);

        // The wide glowing playhead thins into a second hand
        if (playheadVisual != null)
        {
            Vector3 sc = playheadVisual.localScale;
            sc.x = Mathf.Lerp(0.06f, 0.02f, p);
            playheadVisual.localScale = sc;
        }
    }

    private static void LerpColor(Renderer r, Color from, Color to, float p)
    {
        if (r == null) return;
        var block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        Color c = Color.Lerp(from, to, p);
        block.SetColor("_BaseColor", c);
        block.SetColor("_Color", c);
        r.SetPropertyBlock(block);
    }

    /// <summary>Fade in the 1-12 clock numbers. alpha = 0..1.</summary>
    public void ShowNumbers(float alpha)
    {
        foreach (var tm in numberTexts)
        {
            if (!tm.gameObject.activeSelf && alpha > 0f) tm.gameObject.SetActive(true);
            Color c = tm.color; c.a = alpha; tm.color = c;
        }
    }

    /// <summary>Reveal the frozen hour/minute hands, set to 9:00 — shift start.</summary>
    public void ShowHands(float scale)
    {
        SetHand(hourHand,   -270f, scale); // 9 o'clock
        SetHand(minuteHand,    0f, scale); // :00
    }

    private static void SetHand(Transform hand, float angle, float scale)
    {
        if (hand == null) return;
        if (!hand.gameObject.activeSelf) hand.gameObject.SetActive(true);
        hand.localRotation = Quaternion.Euler(0f, 0f, angle);
        hand.localScale = Vector3.one * Mathf.Clamp01(scale);
    }

    // ── Construction ────────────────────────────────────────────────────

    private void SnapRigToGround()
    {
        Vector3 origin = transform.position + Vector3.up * 60f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f))
        {
            Vector3 p = transform.position;
            p.y = hit.point.y;
            transform.position = p;
        }
    }

    /// <summary>Builds the full ring from primitives. Idempotent — clears old children first.</summary>
    [ContextMenu("Build Visuals")]
    public void BuildVisuals()
    {
        // Clear previous build (keep audio pool objects out — they're created at runtime after this)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        float d = radius * 2f;
        var center = new GameObject("RingCenter").transform;
        center.SetParent(transform, false);
        center.localPosition = Vector3.up * heightAboveGround;

        // Face — flat cylinder facing ±Z, dark and translucent-looking at first
        var face = Primitive(PrimitiveType.Cylinder, "Face", center,
            Vector3.zero, new Vector3(d * 1.02f, 0.015f, d * 1.02f),
            new Color(0.12f, 0.12f, 0.16f));
        face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        SafeDestroy(face.GetComponent<Collider>());

        // Rim — slightly larger ring behind the face
        var rim = Primitive(PrimitiveType.Cylinder, "Rim", center,
            new Vector3(0f, 0f, 0.012f), new Vector3(d * 1.12f, 0.012f, d * 1.12f),
            new Color(0.25f, 0.2f, 0.45f));
        rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        SafeDestroy(rim.GetComponent<Collider>());

        // Hub — small center disc in front
        var hub = Primitive(PrimitiveType.Cylinder, "Hub", center,
            new Vector3(0f, 0f, -0.05f), new Vector3(0.08f, 0.02f, 0.08f),
            new Color(0.3f, 0.3f, 0.4f));
        hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        SafeDestroy(hub.GetComponent<Collider>());

        // Playhead — pivot at center, visual arm pointing outward (WallClock hand pattern)
        var pivot = new GameObject("PlayheadPivot").transform;
        pivot.SetParent(center, false);
        pivot.localPosition = new Vector3(0f, 0f, -0.04f);
        var arm = Primitive(PrimitiveType.Cube, "PlayheadArm", pivot,
            new Vector3(0f, radius * 0.45f, 0f), new Vector3(0.06f, radius * 0.9f, 0.015f),
            new Color(0.2f, 0.9f, 0.8f));
        SafeDestroy(arm.GetComponent<Collider>());

        // Slots — 12 pads at the hour positions, slot 0 at the top (12 o'clock)
        var slotsRoot = new GameObject("Slots").transform;
        slotsRoot.SetParent(center, false);
        for (int i = 0; i < slotCount; i++)
        {
            float ang = i * (360f / slotCount) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Sin(ang), Mathf.Cos(ang), 0f) * (radius * 0.86f);
            pos.z = -0.03f;

            var anchor = new GameObject("SlotAnchor_" + i).transform;
            anchor.SetParent(slotsRoot, false);
            anchor.localPosition = pos;

            var pad = Primitive(PrimitiveType.Cylinder, "Pad", anchor,
                Vector3.zero, new Vector3(0.16f, 0.01f, 0.16f),
                new Color(0.35f, 0.35f, 0.45f));
            pad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            SafeDestroy(pad.GetComponent<Collider>());
        }

        // Clock numbers — hidden until the reveal
        var numbersRoot = new GameObject("Numbers").transform;
        numbersRoot.SetParent(center, false);
        for (int i = 1; i <= 12; i++)
        {
            var numObj = new GameObject("Num_" + i);
            numObj.transform.SetParent(numbersRoot, false);
            float ang = i * 30f * Mathf.Deg2Rad;
            numObj.transform.localPosition = new Vector3(
                Mathf.Sin(ang) * radius * 0.86f, Mathf.Cos(ang) * radius * 0.86f, -0.045f);
            // Text faces the player side (-Z front)
            numObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var tm = numObj.AddComponent<TextMesh>();
            tm.text = i.ToString();
            tm.color = new Color(0f, 0f, 0f, 0f);
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 60;
            tm.characterSize = 0.045f;
            numObj.SetActive(false);
        }

        // Frozen clock hands for the reveal — hidden until then
        hourHand   = BuildHand(center, "HourHand",   new Vector3(0.07f, radius * 0.45f, 0.02f), -0.06f);
        minuteHand = BuildHand(center, "MinuteHand", new Vector3(0.05f, radius * 0.65f, 0.02f), -0.07f);
        hourHand.gameObject.SetActive(false);
        minuteHand.gameObject.SetActive(false);
    }

    private Transform BuildHand(Transform parent, string name, Vector3 size, float zOffset)
    {
        var pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = new Vector3(0f, 0f, zOffset);
        var visual = Primitive(PrimitiveType.Cube, "Visual", pivot,
            new Vector3(0f, size.y / 2f, 0f), size, Color.black);
        SafeDestroy(visual.GetComponent<Collider>());
        return pivot;
    }

    /// <summary>Destroy that works in both play mode and editor (setup tool builds in edit mode).</summary>
    private static void SafeDestroy(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent,
                                        Vector3 localPos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.EnableKeyword("_EMISSION");
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private void CacheParts()
    {
        var center = transform.Find("RingCenter");
        if (center == null) return;
        faceR = center.Find("Face")?.GetComponent<Renderer>();
        rimR  = center.Find("Rim")?.GetComponent<Renderer>();
        hubR  = center.Find("Hub")?.GetComponent<Renderer>();
        playheadPivot = center.Find("PlayheadPivot");
        if (playheadPivot != null)
        {
            playheadVisual = playheadPivot.Find("PlayheadArm");
            playheadR = playheadVisual?.GetComponent<Renderer>();
        }
        hourHand   = center.Find("HourHand");
        minuteHand = center.Find("MinuteHand");

        numberTexts.Clear();
        var numbersRoot = center.Find("Numbers");
        if (numbersRoot != null)
            foreach (Transform child in numbersRoot)
            {
                var tm = child.GetComponent<TextMesh>();
                if (tm != null) numberTexts.Add(tm);
            }
    }

    private void CollectSlots()
    {
        var slotsRoot = transform.Find("RingCenter/Slots");
        slots = new Slot[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            var anchor = slotsRoot.Find("SlotAnchor_" + i);
            slots[i] = new Slot
            {
                anchor = anchor,
                pad = anchor.Find("Pad")?.GetComponent<Renderer>()
            };
        }
        FilledCount = 0;
    }
}

/// <summary>
/// Procedural audio for the sequencer: six small synth timbres in A minor
/// pentatonic for the orbs, plus the mechanical tick/tock the clock collapses
/// into. Everything is generated at load — no audio assets required.
/// </summary>
public static class SequencerSound
{
    // A minor pentatonic over two octaves, in semitones above A3 (220 Hz)
    private static readonly int[] Pentatonic = { 0, 3, 5, 7, 10, 12, 15, 17, 19, 22, 24 };

    public const int TimbreCount = 6;

    /// <summary>Create the tone for orb #index — timbre and pitch both derive from the index.</summary>
    public static AudioClip CreateOrbTone(int index)
    {
        int semis = Pentatonic[(index * 2) % Pentatonic.Length];
        float freq = 220f * Mathf.Pow(2f, semis / 12f);
        switch (index % TimbreCount)
        {
            case 0:  return Render("Orb_Bell_" + index,  1.1f, t => Bell(t, freq));
            case 1:  return Render("Orb_Pluck_" + index, 0.4f, t => Pluck(t, freq));
            case 2:  return Render("Orb_Saw_" + index,   0.5f, t => SawStab(t, freq));
            case 3:  return Render("Orb_Pad_" + index,   0.9f, t => Pad(t, freq));
            case 4:  return Render("Orb_Sub_" + index,   0.6f, t => Sub(t, freq * 0.5f));
            default: return Render("Orb_Chime_" + index, 0.7f, t => Chime(t, freq * 2f));
        }
    }

    /// <summary>A color per timbre family, used for orb body + slot pulse.</summary>
    public static Color TimbreColor(int index)
    {
        switch (index % TimbreCount)
        {
            case 0:  return new Color(1.0f, 0.75f, 0.2f); // bell — amber
            case 1:  return new Color(0.3f, 0.85f, 1.0f); // pluck — cyan
            case 2:  return new Color(1.0f, 0.35f, 0.45f); // saw — red-pink
            case 3:  return new Color(0.55f, 0.4f, 1.0f); // pad — violet
            case 4:  return new Color(0.35f, 1.0f, 0.55f); // sub — green
            default: return new Color(1.0f, 1.0f, 0.85f); // chime — warm white
        }
    }

    /// <summary>Short mechanical tick (tock = lower-pitched variant). The sound of the trap.</summary>
    public static AudioClip CreateTick(bool tock)
    {
        float thump = tock ? 70f : 95f;
        return Render(tock ? "Tock" : "Tick", 0.09f, t =>
        {
            // 6ms noise click (escapement) + decaying low square (pendulum body)
            float click = (t < 0.006f) ? (Hash(t) * 2f - 1f) * (1f - t / 0.006f) * 0.7f : 0f;
            float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * thump * t))
                       * Mathf.Exp(-t * 55f) * 0.8f;
            return click + sq;
        });
    }

    // ── Timbres ─────────────────────────────────────────────────────────

    private static float Bell(float t, float f) =>
        (Mathf.Sin(2f * Mathf.PI * f * t) + 0.45f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t))
        * Mathf.Exp(-t * 4.5f);

    private static float Pluck(float t, float f) =>
        (Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t)) * 0.6f + Tri(t, f) * 0.4f)
        * Mathf.Exp(-t * 14f);

    private static float SawStab(float t, float f) =>
        (Saw(t, f) + Saw(t, f * 1.008f)) * 0.5f * Mathf.Exp(-t * 9f);

    private static float Pad(float t, float f) =>
        Tri(t, f) * Mathf.Min(t / 0.12f, 1f) * Mathf.Exp(-t * 3.5f);

    private static float Sub(float t, float f) =>
        Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 6f);

    private static float Chime(float t, float f) =>
        (Mathf.Sin(2f * Mathf.PI * f * t) * 0.7f + Hash(t) * 0.1f) * Mathf.Exp(-t * 8f);

    private static float Saw(float t, float f) { float p = t * f; return 2f * (p - Mathf.Floor(p + 0.5f)); }
    private static float Tri(float t, float f) { float p = t * f; return 4f * Mathf.Abs(p - Mathf.Floor(p + 0.5f)) - 1f; }
    private static float Hash(float t) { return Mathf.Abs(Mathf.Sin(t * 12345.678f + 0.123f) * 43758.5453f % 1f); }

    /// <summary>Render a waveform function to a normalized mono AudioClip.</summary>
    private static AudioClip Render(string name, float duration, System.Func<float, float> wave)
    {
        int rate = AudioSettings.outputSampleRate;
        int count = (int)(rate * duration);
        float[] s = new float[count];
        float peak = 0.0001f;
        for (int i = 0; i < count; i++)
        {
            s[i] = wave(i / (float)rate);
            peak = Mathf.Max(peak, Mathf.Abs(s[i]));
        }
        // Normalize so all timbres sit at the same loudness, then add a fade-out tail
        float gain = 0.7f / peak;
        int fade = Mathf.Min(count / 8, rate / 20);
        for (int i = 0; i < count; i++)
        {
            s[i] *= gain;
            if (i > count - fade) s[i] *= (count - i) / (float)fade;
        }
        var clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(s, 0);
        return clip;
    }
}
