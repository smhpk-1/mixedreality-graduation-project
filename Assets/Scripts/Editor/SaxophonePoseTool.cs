using UnityEngine;
using UnityEditor;

/// <summary>
/// Seçili humanoid NPC'yi "saksofon çalan" statik pozuna getirir.
/// Kollar aşağı + dirsekler bükük + eller önde (saksofonu tutar) + baş hafif öne.
///
/// Editor-time çalışır: bone'ları bir kez döndürür, runtime'da hiçbir script
/// gerekmez → VR'da %100 kararlı (prosedürel animasyon yok, poz bozulmaz).
///
/// Kullanım:
///   1) Hierarchy'de saksofon çalacak NPC'yi seç
///   2) Tools → Pose → Saxophone Player
///   (NPCScene3Wanderer / NPCIdlePose / NPCTrainPassenger varsa kaldırılır,
///    Animator devre dışı bırakılır ki poz sabit kalsın.)
/// </summary>
public class SaxophonePoseTool
{
    [MenuItem("Tools/Pose/Saxophone Player")]
    public static void ApplyPose()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Saxophone Pose", "Önce Hierarchy'de bir NPC seç.", "OK");
            return;
        }

        var animator = go.GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman)
        {
            EditorUtility.DisplayDialog("Saxophone Pose",
                "Seçili objede humanoid Animator bulunamadı.\n" +
                "NPC'nin Rig → Animation Type → Humanoid olmalı.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(go, "Saxophone Pose");

        // Çakışan prosedürel animasyon script'lerini kaldır (dekoratif NPC, hareketsiz)
        RemoveIfExists(go, "NPCScene3Wanderer");
        RemoveIfExists(go, "NPCIdlePose");
        RemoveIfExists(go, "NPCTrainPassenger");

        // Bone'ları yakala
        Transform lUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        Transform lLower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        Transform lHand  = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform rUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        Transform rLower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        Transform rHand  = animator.GetBoneTransform(HumanBodyBones.RightHand);
        Transform head   = animator.GetBoneTransform(HumanBodyBones.Head);
        Transform spine  = animator.GetBoneTransform(HumanBodyBones.Spine);

        Transform root = animator.transform; // yön referansı

        // ── Sol kol: üst kol aşağı-öne, ön kol (dirsek) öne-yukarı-MERKEZE
        //    Eller saksofon gövdesinin önünde, ortada buluşmalı.
        //    LeftHand NPC'nin solunda → merkeze gelmek için +root.right (içe).
        AlignBone(lUpper, lLower, (Vector3.down * 1.0f + root.forward * 0.40f + root.right * 0.10f), 0.95f);
        AlignBone(lLower, lHand,  (root.forward * 0.80f + Vector3.up * 0.20f + root.right * 0.30f), 0.95f);

        // ── Sağ kol: simetrik. RightHand NPC'nin sağında → merkeze -root.right (içe).
        AlignBone(rUpper, rLower, (Vector3.down * 1.0f + root.forward * 0.40f - root.right * 0.10f), 0.95f);
        AlignBone(rLower, rHand,  (root.forward * 0.80f + Vector3.up * 0.20f - root.right * 0.30f), 0.95f);

        // ── Baş: ağızlığa doğru hafif öne-aşağı eğ
        if (head != null)
            RotateBoneWorld(head, root.right, 14f);

        // ── Gövde: çok hafif öne (çalma duruşu)
        if (spine != null)
            RotateBoneWorld(spine, root.right, 5f);

        // Animator'ı devre dışı bırak — poz sabit kalsın (Controller yoksa zaten dokunmaz,
        // ama garanti olsun; runtime'da T-pose'a dönmesin)
        animator.enabled = false;

        EditorUtility.SetDirty(go);
        Debug.Log($"[SaxophonePoseTool] {go.name} saksofon çalma pozuna getirildi. " +
                  "Animator devre dışı bırakıldı, prosedürel script'ler kaldırıldı.");
        EditorUtility.DisplayDialog("Saxophone Pose",
            $"{go.name} saksofon pozuna getirildi.\n\n" +
            "• Kollar aşağı + dirsekler bükük\n" +
            "• Baş hafif öne\n" +
            "• Animator devre dışı (poz sabit)\n\n" +
            "Şimdi saksofonu NPC'nin eline (LeftHand bone'una) child yapıp konumlandır.",
            "OK");
    }

    /// <summary>
    /// Seçili Saksofon objesini, sahnedeki NPC'nin LeftHand bone'una child yapar.
    /// İki obje de seçili olmalı (NPC + Saxophone) VEYA önce NPC, sonra bu çalışınca
    /// otomatik "Saxophone" isimli objeyi bulur.
    /// </summary>
    [MenuItem("Tools/Pose/Attach Saxophone To NPC Hand")]
    public static void AttachSaxophone()
    {
        // NPC bul (Animator'ı humanoid olan)
        Animator npcAnimator = null;
        GameObject saxophone = null;

        foreach (var obj in Selection.gameObjects)
        {
            var anim = obj.GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman) npcAnimator = anim;
            if (obj.name.ToLower().Contains("saxophone")) saxophone = obj;
        }

        // Saksofon seçilmemişse sahnede "Saxophone" ara
        if (saxophone == null)
        {
            var sax = GameObject.Find("Saxophone");
            if (sax != null) saxophone = sax;
        }

        if (npcAnimator == null)
        {
            EditorUtility.DisplayDialog("Attach Saxophone",
                "Humanoid NPC seçili değil. Hierarchy'de NPC'yi seç " +
                "(istersen Cmd+click ile Saxophone'u da ekle).", "OK");
            return;
        }
        if (saxophone == null)
        {
            EditorUtility.DisplayDialog("Attach Saxophone",
                "Saxophone objesi bulunamadı. Sahnede 'Saxophone' isimli obje olmalı " +
                "veya onu da seç.", "OK");
            return;
        }

        Transform leftHand = npcAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
        if (leftHand == null)
        {
            EditorUtility.DisplayDialog("Attach Saxophone", "LeftHand bone bulunamadı.", "OK");
            return;
        }

        Undo.SetTransformParent(saxophone.transform, leftHand, "Attach Saxophone");
        // Elin önünde, gövde aşağı, ağızlık yukarı olacak makul başlangıç konumu
        saxophone.transform.localPosition = new Vector3(0.05f, -0.02f, 0.08f);
        saxophone.transform.localRotation = Quaternion.Euler(-10f, 95f, 10f);

        Selection.activeGameObject = saxophone;
        Debug.Log($"[SaxophonePoseTool] Saxophone, {npcAnimator.name} LeftHand bone'una eklendi. " +
                  "Position/Rotation'ı Scene view'da ince ayarla.");
        EditorUtility.DisplayDialog("Attach Saxophone",
            "Saksofon NPC'nin sol eline eklendi.\n\n" +
            "Scene view'da Saxophone seçiliyken Move/Rotate ile ince ayar yap " +
            "(gövde ellerin önünde, ağızlık yüze yakın olsun).",
            "OK");
    }

    // ── Yardımcı: segment'i hedef yöne döndür (rig-agnostic) ─────────────
    private static void AlignBone(Transform bone, Transform child, Vector3 targetDir, float weight)
    {
        if (bone == null || child == null) return;
        Vector3 current = (child.position - bone.position);
        if (current.sqrMagnitude < 1e-6f || targetDir.sqrMagnitude < 1e-6f) return;

        current = current.normalized;
        Vector3 target = Vector3.Slerp(current, targetDir.normalized, Mathf.Clamp01(weight));
        Quaternion delta = Quaternion.FromToRotation(current, target);
        bone.rotation = delta * bone.rotation;
    }

    private static void RotateBoneWorld(Transform bone, Vector3 axis, float degrees)
    {
        if (bone == null) return;
        bone.rotation = Quaternion.AngleAxis(degrees, axis.normalized) * bone.rotation;
    }

    private static void RemoveIfExists(GameObject go, string componentTypeName)
    {
        var comp = go.GetComponent(componentTypeName);
        if (comp != null)
        {
            Object.DestroyImmediate(comp);
            Debug.Log($"[SaxophonePoseTool] {componentTypeName} kaldırıldı ({go.name}).");
        }
    }
}
