using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// The trap. Watches the radial sequencer; when the player has filled all 12
/// slots and enjoyed their creation for a few loops, the reveal begins:
///
/// 1. DECELERATION — the loop slows until one step lasts exactly one second.
///    The concert stems drain away underneath it.
/// 2. COLLAPSE — step by step, the player's chosen tones are replaced by a
///    mechanical tick. The ring's colors drain into the Scene 1 WallClock
///    palette (white face, black rim, red second hand). Numbers 1-12 fade in
///    over the very slots the player filled. The orbs gray out and shrink
///    into hour markers. Frozen hands appear at 9:00 — shift start.
/// 3. TICKING — for a few seconds the player stands before the clock they
///    built themselves, hearing only tick, tock.
/// 4. RETURN — an opaque black sphere grows around the camera (cheap VR
///    blackout, no transparent shaders) and Assets/scene1.unity loads.
///    Factory → daydream → creation → clock → factory.
///
/// All effects are material color lerps, one scale animation and DSP-clock
/// audio — no particles, no realtime lights. Quest 3S safe. The optional
/// fallback toggles below can strip it down even further.
/// </summary>
[RequireComponent(typeof(RadialSequencer))]
public class SequencerFinaleDirector : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("Full revolutions the completed loop plays before the reveal starts — let them enjoy it first")]
    public int revealAfterLoops = 2;

    [Header("Reveal Timing")]
    [Tooltip("Seconds over which the tempo decays to 1 step/second and colors drain")]
    public float decelDuration = 14f;
    [Tooltip("Seconds of pure ticking in front of the finished clock")]
    public float tickingHold = 6f;
    [Tooltip("Seconds for the blackout sphere to swallow the view")]
    public float blackoutDuration = 4f;

    [Header("Destination")]
    [Tooltip("Scene loaded after the blackout. Scene file lives at Assets/scene1.unity")]
    public string returnSceneName = "scene1";

    [Header("Fallback Toggles (disable to lighten the finale)")]
    public bool showNumbers = true;
    public bool showFrozenHands = true;
    public bool useBlackout = true;

    private RadialSequencer sequencer;
    private ConcertAudioDirector concert;
    private int fullLoopsPlayed;
    private bool revealStarted;

    private void Awake()
    {
        sequencer = GetComponent<RadialSequencer>();
        concert = FindFirstObjectByType<ConcertAudioDirector>();
    }

    private void OnEnable()
    {
        var seq = GetComponent<RadialSequencer>();
        seq.OnLoopWrapped += HandleLoopWrapped;
        seq.OnSlotFilled  += HandleSlotChanged;
        seq.OnSlotFreed   += HandleSlotChanged;
    }

    private void OnDisable()
    {
        var seq = GetComponent<RadialSequencer>();
        seq.OnLoopWrapped -= HandleLoopWrapped;
        seq.OnSlotFilled  -= HandleSlotChanged;
        seq.OnSlotFreed   -= HandleSlotChanged;
    }

    /// <summary>
    /// Each filled slot ducks the concert by 1/12 — the player's loop
    /// gradually replaces the band. With all 12 slots filled, only the
    /// player's own creation remains... and that's when the trap closes.
    /// </summary>
    private void HandleSlotChanged(int _)
    {
        if (revealStarted || concert == null) return;
        concert.SetDuckTarget(1f - sequencer.FilledCount / (float)sequencer.slotCount);
    }

    private void HandleLoopWrapped()
    {
        if (revealStarted) return;

        // Only revolutions with ALL slots filled count toward the reveal
        if (sequencer.FilledCount >= sequencer.slotCount)
        {
            fullLoopsPlayed++;
            if (fullLoopsPlayed >= revealAfterLoops)
            {
                revealStarted = true;
                StartCoroutine(RevealClock());
            }
        }
        else
        {
            fullLoopsPlayed = 0; // player removed an orb — the trap resets
        }
    }

    private IEnumerator RevealClock()
    {
        Debug.Log("[SequencerFinale] Loop complete. The clock reveals itself.");

        // The loop no longer belongs to the player — orbs can't be removed
        foreach (var orb in sequencer.GetSlottedOrbs())
        {
            var grab = orb.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab != null) grab.enabled = false;
        }

        // Concert dies as the clock takes over
        if (concert != null) concert.FadeOutAll(decelDuration);

        // 1+2. Decelerate to 1s steps while everything drains toward the clock
        float startInterval = sequencer.stepInterval;
        float t = 0f;
        while (t < decelDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / decelDuration);
            float eased = p * p; // the slowdown creeps in, then dominates

            sequencer.stepInterval = Mathf.Lerp(startInterval, 1f, eased);
            sequencer.tickDominance = eased;
            sequencer.ApplyClockLook(p);

            foreach (var orb in sequencer.GetSlottedOrbs())
                orb.ApplyClockMode(p);

            if (showNumbers)
                sequencer.ShowNumbers(Mathf.Clamp01((p - 0.5f) * 2f)); // fade in during 2nd half
            if (showFrozenHands && p > 0.7f)
                sequencer.ShowHands((p - 0.7f) / 0.3f);

            yield return null;
        }

        // Lock the final clock state
        sequencer.stepInterval = 1f;
        sequencer.tickDominance = 1f;
        sequencer.ApplyClockLook(1f);
        foreach (var orb in sequencer.GetSlottedOrbs()) orb.ApplyClockMode(1f);
        if (showNumbers) sequencer.ShowNumbers(1f);
        if (showFrozenHands) sequencer.ShowHands(1f);

        // 3. Stand before it. Tick. Tock.
        yield return new WaitForSeconds(tickingHold);

        // 4. The return — and Scene 1 will remember this happened
        FactoryMusicDirector.SecondShift = true;

        if (useBlackout) yield return StartCoroutine(GrowBlackout());
        else yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(returnSceneName);
    }

    /// <summary>
    /// VR-safe blackout without transparent shaders: an inside-out opaque
    /// black sphere grows around the camera until it swallows the view.
    /// </summary>
    private IEnumerator GrowBlackout()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "FinaleBlackout";
        Destroy(sphere.GetComponent<Collider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        mat.color = Color.black;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.black);
        sphere.GetComponent<Renderer>().sharedMaterial = mat;

        float t = 0f;
        while (t < blackoutDuration)
        {
            t += Time.deltaTime;
            float k = t / blackoutDuration;
            sphere.transform.position = cam.transform.position;
            // Negative scale flips the winding so the inside is visible.
            // Shrinks toward the camera but stays above the near plane,
            // so the view ends fully black instead of popping back.
            float r = Mathf.Lerp(12f, 0.18f, k * k);
            sphere.transform.localScale = new Vector3(-r, r, r);
            yield return null;
        }
    }
}
