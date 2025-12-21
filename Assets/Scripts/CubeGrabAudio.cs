using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class CubeGrabAudio : MonoBehaviour
{
    [SerializeField] private AudioClip blueCubeClip;
    [SerializeField] private AudioClip redCubeClip;

    private AudioSource audioSource;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        audioSource.spatialBlend = 1.0f;
        audioSource.playOnAwake = false;
        if (blueCubeClip == null)
            blueCubeClip = Resources.Load<AudioClip>("cubegrabbing_1");
        if (redCubeClip == null)
            redCubeClip = Resources.Load<AudioClip>("cubegrabbing_1");
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrab);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (gameObject.name == "BlueCube" && blueCubeClip != null)
        {
            audioSource.PlayOneShot(blueCubeClip);
        }
        else if (gameObject.name == "RedCube" && redCubeClip != null)
        {
            audioSource.PlayOneShot(redCubeClip);
        }
        else if (blueCubeClip != null)
        {
            audioSource.PlayOneShot(blueCubeClip); // fallback
        }
    }
}
