using UnityEngine;

/// <summary>
/// Animator Controller olmadan NPC'nin kollarını T-pose'dan aşağı indirir
/// ve nefes + hafif sallanma ile canlı bir idle görünümü sağlar.
///
/// KURULUM:
/// 1. NPC prefab'ının root objesine ekle (Animator olan obje).
/// 2. Rig: Humanoid olmalı (FBX import ayarında Rig → Animation Type → Humanoid).
/// 3. Animator Controller atamana gerek yok — bu script animasyonsuz çalışır.
/// </summary>
[RequireComponent(typeof(Animator))]
public class NPCIdlePose : MonoBehaviour
{
    [Header("Kol Pozisyonu")]
    [Tooltip("T-pose'dan kolları kaç derece aşağı indir (varsayılan 70 çoğu rig için çalışır)")]
    [Range(0f, 90f)] public float armLowerAngle = 70f;

    [Header("Nefes")]
    [Range(0.2f, 2f)] public float breathSpeed  = 0.7f;
    [Range(0f, 0.02f)] public float breathAmount = 0.008f;

    [Header("Hafif Sallanma")]
    [Range(0f, 2f)] public float swaySpeed  = 0.35f;
    [Range(0f, 2f)] public float swayAmount = 0.4f;   // derece

    // ── Dahili ────────────────────────────────────────────────────────────────
    private Animator  anim;
    private Transform leftUpperArm, rightUpperArm, leftLowerArm, rightLowerArm, chest;
    private float     seed;

    private void Start()
    {
        anim = GetComponent<Animator>();
        seed = Random.Range(0f, 100f);

        if (anim == null || !anim.isHuman)
        {
            Debug.LogWarning($"[NPCIdlePose] {name}: Humanoid rig gerekli.");
            enabled = false;
            return;
        }

        leftUpperArm  = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightUpperArm = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        leftLowerArm  = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        rightLowerArm = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
        chest         = anim.GetBoneTransform(HumanBodyBones.Chest);
    }

    private void LateUpdate()
    {
        float t = Time.time + seed;

        // ── Kolları world-space'te aşağı döndür (rig'den bağımsız) ────────
        RotateArmDown(leftUpperArm,  leftLowerArm,  -1f);   // -1 = sol (dışa)
        RotateArmDown(rightUpperArm, rightLowerArm,  1f);   // +1 = sağ (dışa)

        // ── Nefes ─────────────────────────────────────────────────────────
        if (chest != null)
        {
            float breathe = 1f + Mathf.Sin(t * breathSpeed * Mathf.PI) * breathAmount;
            chest.localScale = new Vector3(breathe, breathe, breathe);
        }

        // ── Hafif sallanma ─────────────────────────────────────────────────
        float sway = Mathf.Sin(t * swaySpeed) * swayAmount;
        Vector3 euler = transform.localEulerAngles;
        euler.z = sway;
        transform.localEulerAngles = euler;
    }

    /// <summary>
    /// upperArm'ı, lowerArm yönünü hedef olarak kullanarak aşağı döndürür.
    /// Rig'in local eksenleri ne olursa olsun doğru çalışır.
    /// </summary>
    private void RotateArmDown(Transform upperArm, Transform lowerArm, float side)
    {
        if (upperArm == null || lowerArm == null) return;

        Vector3 currentDir = (lowerArm.position - upperArm.position).normalized;

        // Aşağı + karakterin yanına hafif açılı (side: -1 sol, +1 sağ)
        Vector3 targetDir = (Vector3.down + transform.right * side * 0.25f).normalized;

        float blendFactor = armLowerAngle / 90f;
        Vector3 blendedDir = Vector3.Slerp(currentDir, targetDir, blendFactor).normalized;

        Quaternion rot = Quaternion.FromToRotation(currentDir, blendedDir);
        upperArm.rotation = rot * upperArm.rotation;
    }
}
