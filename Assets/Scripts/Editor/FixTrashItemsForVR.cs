using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Editor utility: Sahnedeki tüm çöp objelerini VR için doğru ayarlara getirir.
/// Tools → VR Helpers → Fix All Trash Items menüsünden çağrılır.
///
/// Yapılanlar (her TrashItem / GrabbableTrash içeren obje için):
///   • Rigidbody: Continuous collision detection, interpolate, kütle düzelt
///   • XRGrabInteractable: MovementType = Instantaneous, throwOnDetach, gravityOnDetach
///   • Collider varsa: Trigger DEĞİL (çöp fizik objesi, trigger olmaz)
/// </summary>
public class FixTrashItemsForVR
{
    [MenuItem("Tools/VR Helpers/Fix All Trash Items For VR")]
    public static void FixAll()
    {
        int fixedCount = 0;

        // Sahnedeki tüm TrashItem ve GrabbableTrash component'lerini bul
        TrashItem[]      trashItems   = Object.FindObjectsByType<TrashItem>(FindObjectsSortMode.None);
        GrabbableTrash[] grabbables   = Object.FindObjectsByType<GrabbableTrash>(FindObjectsSortMode.None);

        var allTrash = new System.Collections.Generic.HashSet<GameObject>();
        foreach (var t in trashItems)   allTrash.Add(t.gameObject);
        foreach (var g in grabbables)   allTrash.Add(g.gameObject);

        foreach (var go in allTrash)
        {
            ConfigureTrash(go);
            fixedCount++;
        }

        Debug.Log($"[FixTrashItemsForVR] {fixedCount} çöp objesi VR için yapılandırıldı.");
        EditorUtility.DisplayDialog("Trash VR Fix",
            $"{fixedCount} çöp objesi VR için doğru ayarlandı.\n\n" +
            "• Rigidbody: Continuous collision\n" +
            "• XRGrabInteractable: Instantaneous grab\n" +
            "• Throw on detach: aktif",
            "OK");
    }

    private static void ConfigureTrash(GameObject go)
    {
        Undo.RegisterCompleteObjectUndo(go, "Fix Trash For VR");

        // 1) Rigidbody
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody>(go);

        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        if (rb.mass < 0.05f) rb.mass = 0.3f;   // Çok hafif olunca tutarsız fizik
        rb.linearDamping  = 0.5f;
        rb.angularDamping = 0.5f;

        // 2) XRGrabInteractable
        XRGrabInteractable grab = go.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = Undo.AddComponent<XRGrabInteractable>(go);

        // Instantaneous: çöp hand'e anında "yapışır", çarpışmalardan etkilenmez
        grab.movementType         = XRGrabInteractable.MovementType.Instantaneous;
        grab.trackPosition        = true;
        grab.trackRotation        = true;
        grab.throwOnDetach        = true;
        grab.forceGravityOnDetach = true;
        grab.attachEaseInTime     = 0.15f;
        grab.useDynamicAttach     = true; // El'in tam pozisyonunda yakala

        // "Grab edildi mi?" işaretleyicisini ekle (cart sabit çöpleri toplamasın diye)
        if (go.GetComponent<TrashGrabAutoMarker>() == null)
            Undo.AddComponent<TrashGrabAutoMarker>(go);

        // Collider'ları grab'ın listesine ekle — AMA başka bir grab interactable'a ait olanları atla
        var colliders = go.GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col == null) continue;

            // Bu collider başka bir XRGrabInteractable'ın subtree'sinde mi?
            // (örn. su şişesi içinde "kapak" ayrı grab interactable ise — onun collider'ını alma)
            var ownerGrab = col.GetComponentInParent<XRGrabInteractable>();
            if (ownerGrab != null && ownerGrab != grab) continue; // başka grab'ın

            // Trigger DEĞİL olmalı (fizik çarpışması için)
            if (col.isTrigger)
            {
                Debug.LogWarning($"[FixTrashItemsForVR] {go.name}: '{col.name}' trigger idi, kapatıldı.");
                col.isTrigger = false;
            }
            if (!grab.colliders.Contains(col)) grab.colliders.Add(col);
        }

        EditorUtility.SetDirty(go);
    }

    [MenuItem("Tools/VR Helpers/Diagnose Trash Cart")]
    public static void DiagnoseCart()
    {
        var cart = Object.FindFirstObjectByType<TrashCart>();
        if (cart == null)
        {
            EditorUtility.DisplayDialog("Trash Cart Diagnose",
                "Sahnede TrashCart component'i olan bir obje bulunamadı.\n" +
                "Prefab_TrashCart'a TrashCart component'i ekledin mi?",
                "OK");
            return;
        }

        var box = cart.GetComponentsInChildren<Collider>();
        int triggerCount = 0, solidCount = 0;
        foreach (var b in box)
        {
            if (b.isTrigger) triggerCount++;
            else solidCount++;
        }

        string msg = $"Cart: {cart.name}\n" +
                     $"• Trigger collider sayısı: {triggerCount}\n" +
                     $"• Solid collider sayısı: {solidCount}\n" +
                     $"• Player atanmış mı: {(cart.player != null ? "Evet" : "Hayır (auto-find devrede)")}\n" +
                     $"• Target Count: {cart.targetCount}\n" +
                     $"• Next Scene: {cart.nextSceneName}\n\n" +
                     (triggerCount == 0 ? "⚠ HİÇ TRIGGER COLLIDER YOK! Cart çöpleri yakalayamaz.\n" : "") +
                     (solidCount > 0 ? "ℹ Solid collider'lar var — çöp bunlara çarparsa sekebilir.\n" : "");

        EditorUtility.DisplayDialog("Trash Cart Diagnose", msg, "OK");
    }
}
