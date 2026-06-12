using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Çöp objesine eklenen bileşen.
/// - Oyun başlayınca kinematic kalır (yerinden oynamaz).
/// - Grab edilince fizik açılır, bırakılınca yerde kalır.
/// - TrashCollector bu script varlığına bakarak çöpü tanır.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TrashItem : MonoBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Başlangıçta dondur — fizik yok, yerinden oynamaz
        rb.isKinematic = true;
        rb.useGravity  = false;

        // Grab ayarlarını runtime'da garantiye al (elde titreşim fix'i)
        TrashGrabVRConfig.Apply(gameObject);

        // XRGrabInteractable varsa grab/release olaylarına bağlan
        var grab = GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            grab.selectEntered.AddListener(_ => OnGrabbed());
            grab.selectExited.AddListener(_ => OnReleased());
        }
    }

    private void OnGrabbed()
    {
        rb.isKinematic = false;
        rb.useGravity  = true;
        // Hızlı fırlatmada cart trigger'ını tünelleyip kaçmasın
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void OnReleased()
    {
        // Bırakılınca fizik açık kalır — çöp düşer, yerde kalır
        rb.isKinematic = false;
        rb.useGravity  = true;

        // XRI'ın bırakış sırasındaki rigidbody state restore'una karşı garanti:
        // bir frame sonra tekrar zorla (çöp havada asılı kalmasın)
        if (isActiveAndEnabled)
            StartCoroutine(EnforceDropNextFrame());
    }

    private System.Collections.IEnumerator EnforceDropNextFrame()
    {
        yield return null;
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity  = true;
        }
    }
}
