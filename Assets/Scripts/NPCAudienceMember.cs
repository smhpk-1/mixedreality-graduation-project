using UnityEngine;

/// <summary>
/// A concert audience member for Scene 4. Same humanoid bone-driving
/// approach as NPCMusicianPerformer — no Animator Controller, pure
/// LateUpdate transforms, Quest-friendly.
///
/// Each member gets a randomized personality (groove tempo, sway depth,
/// how often they raise their arms) so the crowd never moves in unison:
///   • bounce on the beat (hips dip and rise)
///   • body sway and head bob
///   • occasional both-arms-up moments held for a few seconds
///
/// Narrative arc, in line with the radial sequencer finale:
///   1. While the concert plays, they dance facing the stage.
///   2. As the player's sequencer replaces the band (concert ducks), the
///      crowd keeps dancing — but slowly TURNS toward the sequencer and
///      the player. The audience is now watching YOU.
///   3. When the clock reveals itself (tick takeover), the crowd freezes
///      mid-pose with the band — an audience of stopped workers.
/// </summary>
public class NPCAudienceMember : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("What this person faces while the band plays (stage center). Set by the setup tool.")]
    public Vector3 stageFocus;

    [Header("Personality (randomized per member by the setup tool seed)")]
    [Tooltip("Groove tempo in bounces per second")]
    public float groove = 2f;
    [Tooltip("0-1: how energetically this person dances")]
    public float enthusiasm = 0.8f;
    [Tooltip("Average seconds between arms-up moments")]
    public float armsUpEvery = 14f;

    // ── Internals ───────────────────────────────────────────────────────
    private Animator anim;
    private ConcertAudioDirector concert;
    private RadialSequencer sequencer;
    private float energy = 1f;
    private float clock, seed;
    private float armsUpTimer, armsUpLeft;
    private bool frozen;

    private Transform hips, spine, head;
    private Transform lUp, lLo, lHand, rUp, rLo, rHand;
    private Quaternion headBase, spineBase;
    private Vector3 hipsBasePos;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim == null || !anim.isHuman)
        {
            Debug.LogWarning($"[NPCAudienceMember] {name}: humanoid Animator required.");
            enabled = false;
            return;
        }
        anim.runtimeAnimatorController = null;

        // Asset pack's low-LOD meshes are rig-broken (appear horizontal/airborne)
        // — same device bug fixed for the Scene 3 wanderers. Pin to LOD0.
        foreach (var lod in GetComponentsInChildren<LODGroup>(true))
            if (lod != null) lod.ForceLOD(0);

        hips  = anim.GetBoneTransform(HumanBodyBones.Hips);
        spine = anim.GetBoneTransform(HumanBodyBones.Spine);
        head  = anim.GetBoneTransform(HumanBodyBones.Head);
        lUp   = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        lLo   = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        lHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        rUp   = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rLo   = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
        rHand = anim.GetBoneTransform(HumanBodyBones.RightHand);

        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down,
                            out RaycastHit hit, 50f))
        {
            Vector3 p = transform.position;
            p.y = hit.point.y;
            transform.position = p;
        }

        if (head  != null) headBase  = head.localRotation;
        if (spine != null) spineBase = spine.localRotation;
        if (hips  != null) hipsBasePos = hips.position;

        concert   = FindFirstObjectByType<ConcertAudioDirector>();
        sequencer = FindFirstObjectByType<RadialSequencer>();

        seed = (GetInstanceID() & 0xFF) * 0.13f;
        clock = seed;
        armsUpTimer = Random.Range(2f, armsUpEvery);

        // Face the stage
        FaceTowards(stageFocus, 1f);
    }

    private void LateUpdate()
    {
        if (hips == null) return;

        float concertLevel = concert != null ? concert.PerformanceLevel : 1f;

        // The clock reveal freezes everyone — an audience of stopped workers
        if (!frozen && sequencer != null && sequencer.tickDominance > 0.2f)
            frozen = true;

        // While the player's loop takes over they still dance a little —
        // the music never fully stops until the clock claims it
        float target = frozen ? 0f : Mathf.Max(concertLevel, 0.35f);
        energy = Mathf.MoveTowards(energy, target, Time.deltaTime * 0.4f);

        float amp = energy * enthusiasm;
        clock += Time.deltaTime * Mathf.Lerp(0.3f, 1f, energy);
        float beat = clock * groove * Mathf.PI * 2f;

        // As the concert dies, turn toward the sequencer: the player is the show now
        if (!frozen && sequencer != null && concertLevel < 0.5f)
        {
            float turn = (0.5f - concertLevel) * 2f; // 0..1
            FaceTowards(sequencer.transform.position, turn * Time.deltaTime * 0.8f);
        }

        if (head  != null) head.localRotation  = headBase;
        if (spine != null) spine.localRotation = spineBase;

        Transform root = transform;

        // Bounce: hips dip on the beat
        float bounce = Mathf.Abs(Mathf.Sin(beat)) * 0.05f * amp;
        hips.position = hipsBasePos + Vector3.down * bounce;

        // Sway + head bob
        RotateAbs(spine, root.forward, Mathf.Sin(clock * 1.1f + seed) * 5f * amp);
        RotateAbs(head, root.right, Mathf.Max(0f, Mathf.Sin(beat)) * 9f * amp);

        // Arms: down by default, up during arms-up moments
        if (!frozen)
        {
            armsUpTimer -= Time.deltaTime;
            if (armsUpTimer <= 0f && energy > 0.4f)
            {
                armsUpLeft = Random.Range(2f, 4.5f);
                armsUpTimer = armsUpEvery * Random.Range(0.7f, 1.5f);
            }
            if (armsUpLeft > 0f) armsUpLeft -= Time.deltaTime;
        }

        if (armsUpLeft > 0f && amp > 0.05f)
        {
            // Both arms up, hands waving with the beat
            float wave = Mathf.Sin(beat) * 0.15f * amp;
            AlignBone(lUp, lLo, Vector3.up + root.forward * 0.15f - root.right * (0.25f + wave));
            AlignBone(rUp, rLo, Vector3.up + root.forward * 0.15f + root.right * (0.25f - wave));
            AlignBone(lLo, lHand, Vector3.up - root.right * wave);
            AlignBone(rLo, rHand, Vector3.up + root.right * wave);
        }
        else
        {
            // Arms relaxed at the sides
            AlignBone(lUp, lLo, Vector3.down - root.right * 0.2f);
            AlignBone(rUp, rLo, Vector3.down + root.right * 0.2f);
            AlignBone(lLo, lHand, Vector3.down + root.forward * 0.15f);
            AlignBone(rLo, rHand, Vector3.down + root.forward * 0.15f);
        }
    }

    private void FaceTowards(Vector3 worldPoint, float t)
    {
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion want = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, Mathf.Clamp01(t));
    }

    private static void AlignBone(Transform bone, Transform child, Vector3 targetDir)
    {
        if (bone == null || child == null) return;
        Vector3 current = (child.position - bone.position).normalized;
        Vector3 target = Vector3.Slerp(current, targetDir.normalized, 0.95f).normalized;
        bone.rotation = Quaternion.FromToRotation(current, target) * bone.rotation;
    }

    private static void RotateAbs(Transform bone, Vector3 worldAxis, float degrees)
    {
        if (bone == null) return;
        bone.rotation = Quaternion.AngleAxis(degrees, worldAxis) * bone.rotation;
    }
}
