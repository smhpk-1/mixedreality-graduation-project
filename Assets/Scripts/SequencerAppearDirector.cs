using UnityEngine;
using System.Collections;

/// <summary>
/// The summons. The radial sequencer does not exist at the start of the
/// concert — partway in, the METRO PA voice from Scene 3 follows the player
/// into the daydream:
///
///     "Dear passenger. Please come to the stage."
///
/// (clip: Resources/scene_4_sound_design/announce_stage — same TTS voice
/// family as the Scene 3 platform announcements; the system that schedules
/// trains now schedules the player.)
///
/// Then the instrument materializes on the stage: the ring spins up from
/// nothing while a rising pentatonic arpeggio plays, the twelve slot pads
/// flash in sequence around the circle — a clock face being inscribed,
/// though no one notices yet — and the sample orbs rise from the stage
/// floor one by one.
///
/// Quest-safe: transform/scale animation, MaterialPropertyBlock pulses and
/// DSP-scheduled one-shots. No particles, no lights.
/// </summary>
[RequireComponent(typeof(RadialSequencer))]
public class SequencerAppearDirector : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Seconds of concert before the PA announcement summons the player")]
    public float announceDelay = 45f;
    [Tooltip("Pause between the announcement ending and the ring materializing")]
    public float revealPause = 1.5f;
    [Tooltip("Seconds for the ring to spin/scale into existence")]
    public float ringGrowDuration = 2.5f;

    [Header("Audio")]
    [Range(0f, 1f)] public float announceVolume = 0.85f;
    [Range(0f, 1f)] public float arpeggioVolume = 0.5f;

    private RadialSequencer sequencer;
    private Transform ringCenter;
    private SequencerSampleOrb[] orbs;

    private void Awake()
    {
        sequencer = GetComponent<RadialSequencer>();
        // RadialSequencer (exec order -100) has already built the visuals
        ringCenter = transform.Find("RingCenter");

        orbs = FindObjectsByType<SequencerSampleOrb>(FindObjectsSortMode.None);

        // Hide everything until the summons
        if (ringCenter != null) ringCenter.gameObject.SetActive(false);
        foreach (var orb in orbs) orb.gameObject.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(SummonsRoutine());
    }

    private IEnumerator SummonsRoutine()
    {
        yield return new WaitForSeconds(announceDelay);

        // ── The PA voice from the metro finds the player ────────────────
        AudioClip announce = Resources.Load<AudioClip>("scene_4_sound_design/announce_stage");
        AudioClip chime = SequencerSound.PaChime();

        var pa = BuildPaSource();
        pa.PlayOneShot(chime, announceVolume);
        yield return new WaitForSeconds(1.1f);
        if (announce != null)
        {
            pa.PlayOneShot(announce, announceVolume);
            yield return new WaitForSeconds(announce.length);
        }
        else
        {
            Debug.LogWarning("[SequencerAppear] announce_stage clip not found in Resources/scene_4_sound_design");
        }

        yield return new WaitForSeconds(revealPause);

        // ── The instrument materializes ─────────────────────────────────
        if (ringCenter != null)
        {
            ringCenter.gameObject.SetActive(true);
            StartCoroutine(GrowRing());
        }

        // Rising pentatonic arpeggio — the ring announces its musical language
        StartCoroutine(PlayAppearArpeggio());

        // Slot pads flash in sequence: a clock face inscribing itself
        yield return new WaitForSeconds(ringGrowDuration * 0.4f);
        sequencer.FlashSlotsSequential(0.09f);

        // Orbs rise from the stage floor, staggered
        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] == null) continue;
            orbs[i].gameObject.SetActive(true); // Start() runs now, caches home
            orbs[i].PlayAppearAnimation(0.15f * i, 1.2f);
        }
    }

    private IEnumerator GrowRing()
    {
        Vector3 finalScale = ringCenter.localScale;
        Quaternion finalRot = ringCenter.localRotation;
        float t = 0f;
        while (t < ringGrowDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / ringGrowDuration);
            // Ease-out with a slight overshoot pop
            float s = p < 0.85f
                ? Mathf.SmoothStep(0f, 1.06f, p / 0.85f)
                : Mathf.Lerp(1.06f, 1f, (p - 0.85f) / 0.15f);
            ringCenter.localScale = finalScale * s;
            // Spins in like a dropped coin settling
            ringCenter.localRotation = finalRot * Quaternion.Euler(0f, (1f - p) * 540f, 0f);
            yield return null;
        }
        ringCenter.localScale = finalScale;
        ringCenter.localRotation = finalRot;
    }

    private IEnumerator PlayAppearArpeggio()
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 2f;
        src.maxDistance = 30f;
        src.dopplerLevel = 0f;
        src.transform.position = transform.position;

        for (int i = 0; i < 12; i++)
        {
            src.PlayOneShot(SequencerSound.CreateOrbTone(i), arpeggioVolume);
            yield return new WaitForSeconds(0.16f);
        }
        Destroy(src, 3f);
    }

    /// <summary>
    /// PA source: prefers a generated stage speaker so the voice comes from
    /// the venue's own system; falls back to the sequencer's position.
    /// </summary>
    private AudioSource BuildPaSource()
    {
        Transform anchor = transform;
        var speaker = FindFirstObjectByType<StageSpeakerGenerator>();
        if (speaker != null) anchor = speaker.transform;

        var go = new GameObject("PA_Announcement");
        go.transform.position = anchor.position + Vector3.up * 2f;
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 6f;   // PA: geniş alanda net duyulur
        src.maxDistance = 80f;
        src.dopplerLevel = 0f;
        Destroy(go, 12f);
        return src;
    }
}
