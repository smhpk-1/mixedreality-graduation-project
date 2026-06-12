using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Çöp objesine eklenir.
/// Oyun başlayınca yerinden oynamaz (kinematic).
/// Grab edilince fizik açılır, bırakılınca yerde kalır.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class GrabbableTrash : MonoBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // Grab ayarlarını runtime'da garantiye al (elde titreşim fix'i)
        TrashGrabVRConfig.Apply(gameObject);

        var grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(_ => EnablePhysics());
    }

    private void EnablePhysics()
    {
        rb.isKinematic = false;
        rb.useGravity  = true;
        // Hızlı fırlatmada cart trigger'ını tünelleyip kaçmasın
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
}
