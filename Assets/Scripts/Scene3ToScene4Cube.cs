using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Scene 3 içindeki bir küpe ekle.
/// Oyuncu küpü grab ettiğinde Scene 4'e geçer.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class Scene3ToScene4Cube : MonoBehaviour
{
    private void Start()
    {
        var grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(_ => LoadScene4());

        // Küpü mor renge boyar
        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = rend.material; // mevcut materyali kullan
            Color purple = new Color(0.6f, 0.2f, 0.9f, 1f);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", purple);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", purple);
            mat.color = purple;
        }
    }

    private void LoadScene4()
    {
        SceneManager.LoadScene("Scene 4");
    }
}
