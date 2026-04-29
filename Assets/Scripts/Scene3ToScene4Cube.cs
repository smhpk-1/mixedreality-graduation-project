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
    [Header("Renk")]
    public Color cubeColor = new Color(0.6f, 0.2f, 0.9f, 1f);

    private void Start()
    {
        var grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(_ => LoadScene4());

        var rend = GetComponent<Renderer>();
        if (rend != null && rend.sharedMaterial != null)
        {
            // Shared material'i değiştirmemek için instance al
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            rend.GetPropertyBlock(block);
            block.SetColor("_BaseColor", cubeColor);
            block.SetColor("_Color", cubeColor);
            rend.SetPropertyBlock(block);
        }
    }

    private void LoadScene4()
    {
        SceneManager.LoadScene("Scene 4");
    }
}
