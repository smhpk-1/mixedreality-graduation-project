using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Çöp objelerinin XR grab ayarlarını RUNTIME'da garantiye alır.
///
/// SORUN: FixTrashItemsForVR editor tool'u elle çalıştırılması gereken bir araç —
/// tool'dan sonra sahneye eklenen çöpler default VelocityTracking grab'le kalıyor.
/// VelocityTracking elde tutulurken collider'lara takılıp TİTRİYOR (cihazda 72Hz'de
/// belirgin). Instantaneous ise ele anında yapışır, çarpışmadan etkilenmez.
///
/// TrashItem ve GrabbableTrash Awake'lerinden çağrılır → her çöp, editor tool
/// çalıştırılmış olsun olmasın, doğru ayarlarla başlar.
/// </summary>
public static class TrashGrabVRConfig
{
    public static void Apply(GameObject go)
    {
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation          = RigidbodyInterpolation.Interpolate;
            if (rb.mass < 0.05f) rb.mass = 0.3f; // çok hafif olunca tutarsız fizik
            rb.linearDamping  = 0.5f;
            rb.angularDamping = 0.5f;
        }

        XRGrabInteractable grab = go.GetComponent<XRGrabInteractable>();
        if (grab == null) return;

        grab.movementType         = XRGrabInteractable.MovementType.Instantaneous;
        grab.trackPosition        = true;
        grab.trackRotation        = true;
        grab.throwOnDetach        = true;
        grab.forceGravityOnDetach = true;
        grab.attachEaseInTime     = 0f;    // anında — ease yok, titreşim azalır
        grab.useDynamicAttach     = false; // hand merkezine gelsin, ray ucunda kalmasın
        grab.smoothPosition       = true;
        grab.smoothRotation       = true;
        grab.smoothPositionAmount = 8f;
        grab.smoothRotationAmount = 8f;
        grab.tightenPosition      = 0.5f;
        grab.tightenRotation      = 0.5f;
    }
}
