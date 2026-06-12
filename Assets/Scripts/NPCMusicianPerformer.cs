using UnityEngine;

/// <summary>
/// Makes a humanoid NPC visibly PLAY an instrument on the Scene 4 stage.
///
/// Same techniques as the project's other NPC scripts: no Animator
/// Controller, bones are driven directly each LateUpdate using the
/// direction-based AlignBone approach from SaxophonePoseTool/NPCIdlePose
/// (rig-independent). Each role has a held base pose plus a rhythmic
/// playing motion:
///   Drummer     — seated, alternating arm strikes, head nods on the beat
///   Guitarist   — guitar held on the body, right arm strums, slow head-bang
///   Bassist     — like guitarist, slower and heavier
///   Keyboardist — hands over the keys, alternating dips, gentle sway
///
/// Narrative tie-in: motion intensity follows ConcertAudioDirector's
/// PerformanceLevel. As the player fills the radial sequencer and the
/// concert ducks, the band winds down — and when the player's loop fully
/// replaces the music, the musicians freeze mid-pose, standing motionless
/// like workers at a stopped line.
///
/// Quest 3S: pure transform math, no physics, no extra draw calls.
/// </summary>
public class NPCMusicianPerformer : MonoBehaviour
{
    public enum Role { Drummer, Guitarist, Bassist, Keyboardist }

    [Header("Role")]
    public Role role = Role.Guitarist;

    [Tooltip("The instrument object. Guitars get held by the NPC; drums/synth stay in place.")]
    public Transform instrument;

    [Header("Motion")]
    [Tooltip("Playing actions per second at full energy (strikes, strums, key hits)")]
    public float tempo = 2.1f;

    [Header("Guitar Hold (Guitarist/Bassist only)")]
    [Tooltip("Extra world-space offset for the held guitar — tweak per rig if needed")]
    public Vector3 holdOffset = Vector3.zero;
    [Tooltip("Roll of the guitar neck, degrees (negative = neck up to the NPC's left)")]
    public float holdTilt = -50f;

    // ── Internals ───────────────────────────────────────────────────────
    private Animator anim;
    private ConcertAudioDirector concert;
    private float energy = 1f;  // smoothed performance level
    private float clock;        // own time — slows down as the band dies
    private float seed;

    private Transform hips, spine, chest, head;
    private Transform lUp, lLo, lHand, rUp, rLo, rHand;
    private Transform lUpLeg, lLoLeg, lFoot, rUpLeg, rLoLeg, rFoot;
    private Quaternion headBase, spineBase;
    private Vector3 hipsBasePos;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim == null || !anim.isHuman)
        {
            Debug.LogWarning($"[NPCMusicianPerformer] {name}: humanoid Animator required.");
            enabled = false;
            return;
        }

        // Kill any animation but keep the humanoid bone map alive
        anim.runtimeAnimatorController = null;

        hips   = anim.GetBoneTransform(HumanBodyBones.Hips);
        spine  = anim.GetBoneTransform(HumanBodyBones.Spine);
        chest  = anim.GetBoneTransform(HumanBodyBones.Chest);
        head   = anim.GetBoneTransform(HumanBodyBones.Head);
        lUp    = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        lLo    = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        lHand  = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        rUp    = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rLo    = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
        rHand  = anim.GetBoneTransform(HumanBodyBones.RightHand);
        lUpLeg = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        lLoLeg = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        lFoot  = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        rUpLeg = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        rLoLeg = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        rFoot  = anim.GetBoneTransform(HumanBodyBones.RightFoot);

        // Ground-snap (Scene 4 stands on a terrain)
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

        concert = FindFirstObjectByType<ConcertAudioDirector>();
        seed = (GetInstanceID() & 0xFF) * 0.07f; // desync musicians from each other
        clock = seed;
    }

    private void LateUpdate()
    {
        if (hips == null) return;

        // The band plays only as long as its music does. Ducked to silence
        // by the player's sequencer → musicians freeze where they stand.
        float level = concert != null ? concert.PerformanceLevel : 1f;
        energy = Mathf.MoveTowards(energy, level, Time.deltaTime * 0.5f);
        float amp = energy;
        clock += Time.deltaTime * Mathf.Lerp(0.35f, 1f, energy);
        float beat = clock * tempo * Mathf.PI * 2f;

        Transform root = transform;

        // Reset oscillating bones to base, then apply absolute offsets
        if (head  != null) head.localRotation  = headBase;
        if (spine != null) spine.localRotation = spineBase;

        switch (role)
        {
            case Role.Drummer:     PoseDrummer(root, beat, amp);     break;
            case Role.Guitarist:   PoseGuitarist(root, beat, amp, false); break;
            case Role.Bassist:     PoseGuitarist(root, beat, amp, true);  break;
            case Role.Keyboardist: PoseKeyboardist(root, beat, amp); break;
        }

        HoldGuitar(root);
    }

    // ── Role poses ──────────────────────────────────────────────────────

    private void PoseDrummer(Transform root, float beat, float amp)
    {
        // Seated: hips drop, thighs forward, shins down
        hips.position = hipsBasePos + Vector3.down * 0.34f;
        AlignBone(lUpLeg, lLoLeg, root.forward * 0.9f + Vector3.down * 0.35f - root.right * 0.15f);
        AlignBone(rUpLeg, rLoLeg, root.forward * 0.9f + Vector3.down * 0.35f + root.right * 0.15f);
        AlignBone(lLoLeg, lFoot, Vector3.down + root.forward * 0.25f);
        AlignBone(rLoLeg, rFoot, Vector3.down + root.forward * 0.25f);

        // Alternating strikes: forearm swings between raised-ready and hit
        float strikeR = Mathf.Max(0f, Mathf.Sin(beat));
        float strikeL = Mathf.Max(0f, Mathf.Sin(beat + Mathf.PI));
        AlignBone(rUp, rLo, Vector3.down * 0.7f + root.forward * 0.5f + root.right * 0.2f);
        AlignBone(lUp, lLo, Vector3.down * 0.7f + root.forward * 0.5f - root.right * 0.2f);
        AlignBone(rLo, rHand, root.forward + Vector3.up * Mathf.Lerp(0.45f, -0.35f, strikeR * amp));
        AlignBone(lLo, lHand, root.forward + Vector3.up * Mathf.Lerp(0.45f, -0.35f, strikeL * amp));

        // Head nods with the right hand, torso bounces slightly
        RotateAbs(head, root.right, 7f * strikeR * amp);
        RotateAbs(spine, root.right, 4f + 2f * strikeR * amp);
    }

    private void PoseGuitarist(Transform root, float beat, float amp, bool bass)
    {
        float pace = bass ? 0.6f : 1f; // bassist plays slower, heavier

        // Left arm reaches out along the neck (up-left-forward)
        AlignBone(lUp, lLo, -root.right * 0.7f + root.forward * 0.4f + Vector3.down * 0.25f);
        AlignBone(lLo, lHand, -root.right * 0.8f + root.forward * 0.3f + Vector3.up * 0.35f);

        // Right arm strums over the body
        float strum = Mathf.Sin(beat * pace) * 0.22f * amp;
        AlignBone(rUp, rLo, Vector3.down * 0.85f + root.forward * 0.3f);
        AlignBone(rLo, rHand, root.forward * 0.6f + Vector3.down * 0.45f + Vector3.up * strum);

        // Slow head-bang + body sway
        RotateAbs(head, root.right, (6f + Mathf.Sin(beat * pace * 0.5f) * 8f) * amp);
        RotateAbs(spine, root.forward, Mathf.Sin(clock * 0.8f + seed) * 4f * amp);
        RotateAbs(spine, root.right, 4f * amp);
    }

    private void PoseKeyboardist(Transform root, float beat, float amp)
    {
        // Both arms forward-down over the keys, elbows bent
        AlignBone(lUp, lLo, Vector3.down * 0.75f + root.forward * 0.45f - root.right * 0.1f);
        AlignBone(rUp, rLo, Vector3.down * 0.75f + root.forward * 0.45f + root.right * 0.1f);

        // Hands alternate small dips — left and right out of phase
        float dipL = Mathf.Max(0f, Mathf.Sin(beat * 2f)) * 0.14f * amp;
        float dipR = Mathf.Max(0f, Mathf.Sin(beat * 2f + Mathf.PI)) * 0.14f * amp;
        AlignBone(lLo, lHand, root.forward * 0.9f + Vector3.down * (0.2f + dipL) - root.right * 0.15f);
        AlignBone(rLo, rHand, root.forward * 0.9f + Vector3.down * (0.2f + dipR) + root.right * 0.15f);

        // Looks down at the keys, sways gently
        RotateAbs(head, root.right, 12f * amp);
        RotateAbs(spine, root.forward, Mathf.Sin(clock * 0.6f + seed) * 3f * amp);
    }

    // ── Guitar holding ──────────────────────────────────────────────────

    /// <summary>
    /// Guitars are held on the body: positioned at the hip/chest every frame
    /// in world space (rig-independent), neck tilted up to the NPC's left.
    /// </summary>
    private void HoldGuitar(Transform root)
    {
        if (instrument == null || (role != Role.Guitarist && role != Role.Bassist)) return;
        if (chest == null) return;

        instrument.position = chest.position
                              + root.forward * 0.28f
                              + Vector3.down * 0.35f
                              + root.right * 0.05f
                              + holdOffset;
        // Generators build guitars standing up (+Y = neck). Face the audience,
        // roll the neck up-left like a strap-worn guitar.
        instrument.rotation = Quaternion.LookRotation(root.forward, Vector3.up)
                              * Quaternion.Euler(0f, 0f, holdTilt);
    }

    // ── Bone math (project convention — see SaxophonePoseTool) ──────────

    /// <summary>Rotate a bone so the bone→child direction matches targetDir. Absolute, no drift.</summary>
    private static void AlignBone(Transform bone, Transform child, Vector3 targetDir)
    {
        if (bone == null || child == null) return;
        Vector3 current = (child.position - bone.position).normalized;
        Vector3 target = Vector3.Slerp(current, targetDir.normalized, 0.95f).normalized;
        bone.rotation = Quaternion.FromToRotation(current, target) * bone.rotation;
    }

    /// <summary>World-axis rotation applied on top of the bone's captured base pose.</summary>
    private static void RotateAbs(Transform bone, Vector3 worldAxis, float degrees)
    {
        if (bone == null) return;
        bone.rotation = Quaternion.AngleAxis(degrees, worldAxis) * bone.rotation;
    }
}
