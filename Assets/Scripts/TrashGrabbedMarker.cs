using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// İşaretçi component — bir çöp ilk kez grab edildiğinde otomatik olarak eklenir.
/// TrashCart bu marker'a bakarak "kullanıcı bu çöple etkileşti mi?" anlar.
/// Aksi halde cart yerdeki sabit çöplerin üzerinden geçince hepsini toplamış olur.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class TrashGrabbedMarker : MonoBehaviour
{
    // Bu component sadece bir flag — özellik yok, sadece varlığı yeterli.
    // Eklenme mantığı TrashGrabAutoMarker tarafından yönetilir.
}

/// <summary>
/// Her çöp objesine eklenir. Grab edildiğinde TrashGrabbedMarker'ı ekler ve kendini siler.
/// Sahnedeki tüm çöplere FixTrashItemsForVR script'i otomatik ekler.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class TrashGrabAutoMarker : MonoBehaviour
{
    private void Awake()
    {
        var grab = GetComponent<XRGrabInteractable>();
        if (grab == null) return;

        grab.selectEntered.AddListener(_ =>
        {
            if (gameObject.GetComponent<TrashGrabbedMarker>() == null)
                gameObject.AddComponent<TrashGrabbedMarker>();
        });
    }
}
