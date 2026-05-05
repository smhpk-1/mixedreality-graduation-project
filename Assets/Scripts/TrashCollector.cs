using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Prefab_TrashCart üzerine eklenir.
/// İçine giren TrashItem'ları sayar, UI günceller,
/// hepsi toplandığında OnAllCollected eventi tetiklenir.
/// </summary>
public class TrashCollector : MonoBehaviour
{
    [Header("Görev")]
    [Tooltip("Sahnede yerleştirilen toplam çöp sayısı")]
    public int totalTrash = 15;

    [Header("UI")]
    [Tooltip("World-space veya Screen-space TextMeshPro alanı\n(örn: '5 / 17 çöp toplandı' yazar)")]
    public TextMeshProUGUI counterText;

    [Header("Tamamlandığında")]
    public UnityEvent OnAllCollected;

    // ── Dahili ──────────────────────────────────────────────────────────────
    private int collected = 0;
    private bool completed = false;

    private void Start()
    {
        UpdateUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed) return;

        // Çöp objesi mi?
        TrashItem trash = other.GetComponent<TrashItem>();
        if (trash == null)
            trash = other.GetComponentInParent<TrashItem>();

        if (trash == null) return;

        collected++;
        UpdateUI();

        // Çöpü sepette dondur (fizik kapan, görünür kalsın)
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            rb.linearVelocity    = Vector3.zero;
            rb.angularVelocity   = Vector3.zero;
            rb.isKinematic       = true;
        }

        // XRGrabInteractable'ı devre dışı bırak (tekrar alınamasın)
        var grab = other.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null)
            grab = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
            grab.enabled = false;

        if (collected >= totalTrash)
        {
            completed = true;
            UpdateUI();
            OnAllCollected?.Invoke();
        }
    }

    private void UpdateUI()
    {
        if (counterText == null) return;

        if (completed)
            counterText.text = "All trash collected!";
        else
            counterText.text = $"{collected} / {totalTrash} trash collected";
    }
}
