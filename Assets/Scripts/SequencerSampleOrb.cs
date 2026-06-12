using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

/// <summary>
/// A floating, grabbable sound sample for the Scene 4 radial sequencer.
///
/// Idle: bobs gently around its home position. Held: plays a quiet looping
/// preview of its tone so the player knows what they are placing. Released
/// near an empty slot: snaps in and becomes part of the loop. Released
/// anywhere else: drifts back home. Grabbing a slotted orb frees its slot.
///
/// No gravity, no free physics — the orb is kinematic except while held
/// (XRGrabInteractable VelocityTracking manages that), so it can never be
/// lost under the terrain. Quest-friendly: one small sphere, no lights.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class SequencerSampleOrb : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Index into the sequencer's timbre/note table. Set by the setup tool.")]
    public int orbIndex;

    [Header("Placement")]
    [Tooltip("Max distance from an empty slot at which a released orb snaps in")]
    public float snapRadius = 0.35f;
    [Tooltip("Seconds to glide into a slot or back home")]
    public float glideDuration = 0.5f;

    [Header("Idle Float")]
    public float bobAmplitude = 0.05f;
    public float bobSpeed = 0.8f;

    // Assigned at runtime
    [System.NonSerialized] public AudioClip toneClip;
    [System.NonSerialized] public Color orbColor;

    private enum State { Floating, Held, Slotted, Gliding }
    private State state = State.Floating;

    private RadialSequencer sequencer;
    private XRGrabInteractable grab;
    private Rigidbody rb;
    private AudioSource previewSource;
    private Renderer rend;
    private Vector3 homePosition;
    private int slotIndex = -1;
    private float bobPhase;
    private Coroutine glideCoroutine;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();

        rb.useGravity = false;
        rb.isKinematic = true;
        grab.throwOnDetach = false;
        grab.forceGravityOnDetach = false;

        // Quiet looping preview while held — the player hears what they hold
        previewSource = gameObject.AddComponent<AudioSource>();
        previewSource.playOnAwake = false;
        previewSource.loop = true;
        previewSource.spatialBlend = 1f;
        previewSource.rolloffMode = AudioRolloffMode.Linear;
        previewSource.minDistance = 0.5f;
        previewSource.maxDistance = 8f;
        previewSource.dopplerLevel = 0f;
        previewSource.volume = 0.3f;

        bobPhase = orbIndex * 1.7f; // desync the bobbing between orbs
    }

    private void Start()
    {
        // After RadialSequencer.Awake has ground-snapped the rig (-100 exec order),
        // this position is final — cache it as home.
        homePosition = transform.position;
        sequencer = FindFirstObjectByType<RadialSequencer>();

        toneClip = SequencerSound.CreateOrbTone(orbIndex);
        orbColor = SequencerSound.TimbreColor(orbIndex);
        previewSource.clip = toneClip;

        // Tint the orb with its timbre color, softly emissive
        if (rend != null)
        {
            var block = new MaterialPropertyBlock();
            rend.GetPropertyBlock(block);
            block.SetColor("_BaseColor", orbColor);
            block.SetColor("_Color", orbColor);
            block.SetColor("_EmissionColor", orbColor * 0.5f);
            rend.SetPropertyBlock(block);
        }
    }

    private void OnEnable()
    {
        GetComponent<XRGrabInteractable>().selectEntered.AddListener(OnGrab);
        GetComponent<XRGrabInteractable>().selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        GetComponent<XRGrabInteractable>().selectEntered.RemoveListener(OnGrab);
        GetComponent<XRGrabInteractable>().selectExited.RemoveListener(OnRelease);
    }

    private void Update()
    {
        if (state == State.Held)
        {
            // Invite the player: empty slots glow, brightest within snap range
            if (sequencer != null)
                sequencer.HighlightEmptySlots(transform.position, snapRadius);
            return;
        }
        if (state != State.Floating) return;
        // Gentle idle bob around home
        float y = Mathf.Sin(Time.time * bobSpeed * 2f * Mathf.PI * 0.2f + bobPhase) * bobAmplitude;
        transform.position = homePosition + Vector3.up * y;
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (glideCoroutine != null) { StopCoroutine(glideCoroutine); glideCoroutine = null; }

        if (state == State.Slotted && sequencer != null && slotIndex >= 0)
        {
            sequencer.FreeSlot(slotIndex);
            slotIndex = -1;
        }
        state = State.Held;
        previewSource.Play();
        SendHaptic(args.interactorObject, 0.3f, 0.05f);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        previewSource.Stop();
        if (sequencer != null) sequencer.ClearSlotHighlights();

        // Back under our control — no free physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        int slot = sequencer != null ? sequencer.NearestEmptySlot(transform.position, snapRadius) : -1;
        // FillSlot can refuse if another orb claimed the slot this same frame
        if (slot >= 0 && sequencer.FillSlot(slot, this))
        {
            slotIndex = slot;
            SendHaptic(args.interactorObject, 0.5f, 0.08f);
            glideCoroutine = StartCoroutine(GlideTo(sequencer.GetSlotAnchor(slot), State.Slotted));
        }
        else
        {
            glideCoroutine = StartCoroutine(GlideTo(null, State.Floating));
        }
    }

    /// <summary>Short haptic pulse on the controller holding/releasing this orb.</summary>
    private static void SendHaptic(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor,
                                   float amplitude, float duration)
    {
        if (interactor == null || interactor.transform == null) return;
        var player = interactor.transform
            .GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics.HapticImpulsePlayer>();
        if (player != null) player.SendHapticImpulse(amplitude, duration);
    }

    /// <summary>Glide to a slot anchor (target != null) or back home (target == null).</summary>
    private IEnumerator GlideTo(Transform target, State endState)
    {
        state = State.Gliding;
        Vector3 from = transform.position;
        float t = 0f;
        while (t < glideDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / glideDuration);
            Vector3 to = target != null ? target.position : homePosition;
            transform.position = Vector3.Lerp(from, to, k);
            yield return null;
        }
        if (target != null) transform.position = target.position;
        state = endState;
        glideCoroutine = null;
    }

    /// <summary>
    /// Reveal hook: the orb materializes — rises from the stage floor to its
    /// home position while scaling up, glowing in. Called by
    /// SequencerAppearDirector after the PA announcement. Not grabbable
    /// until the rise completes.
    /// </summary>
    public void PlayAppearAnimation(float delay, float duration = 1.2f)
    {
        StartCoroutine(AppearRoutine(delay, duration));
    }

    private IEnumerator AppearRoutine(float delay, float duration)
    {
        state = State.Gliding;            // Update won't fight the animation
        grab.enabled = false;
        transform.localScale = Vector3.zero; // ilk frame'de görünmesin

        // Start() bir frame sonra çalışır ve homePosition'ı cache'ler — onu bekle
        yield return null;

        Vector3 home = homePosition;
        Vector3 from = home + Vector3.down * 1.2f;
        transform.position = from;

        yield return new WaitForSeconds(delay);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            transform.position = Vector3.Lerp(from, home, k);
            // Slight overshoot pop at the end
            float s = Mathf.Min(1.08f, k * 1.2f);
            transform.localScale = Vector3.one * (0.12f * s);
            yield return null;
        }
        transform.position = home;
        transform.localScale = Vector3.one * 0.12f;
        grab.enabled = true;
        state = State.Floating;
    }

    /// <summary>
    /// Finale hook: the orb stops being a toy. Color drains to clock-marker
    /// gray, the glow dies, it shrinks toward an hour-marker dot, and it can
    /// no longer be picked up. p = 0..1.
    /// </summary>
    public void ApplyClockMode(float p)
    {
        if (rend != null)
        {
            var block = new MaterialPropertyBlock();
            rend.GetPropertyBlock(block);
            Color c = Color.Lerp(orbColor, new Color(0.08f, 0.08f, 0.08f), p);
            block.SetColor("_BaseColor", c);
            block.SetColor("_Color", c);
            block.SetColor("_EmissionColor", Color.Lerp(orbColor * 0.5f, Color.black, p));
            rend.SetPropertyBlock(block);
        }
        transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.05f, p);
        if (p >= 0.999f && grab.enabled) grab.enabled = false;
    }
}
