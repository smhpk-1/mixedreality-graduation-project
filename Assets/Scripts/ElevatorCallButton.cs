using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MusicSpace
{
    /// <summary>
    /// Attach to the call button GameObject outside the elevator.
    /// When the player selects it with an XR controller (ray or direct),
    /// the elevator doors slide open. After the player enters, ElevatorSceneTransition
    /// closes the doors and loads Scene 3 after 5 seconds.
    /// </summary>
    public class ElevatorCallButton : MonoBehaviour
    {
        private bool pressed = false;
        private Renderer btnRenderer;
        private Color originalEmission;

        private void Start()
        {
            btnRenderer = GetComponent<Renderer>();

            // XRSimpleInteractable lets both ray interactors (pointer) and
            // direct/hand interactors (touch) select this object.
            var interactable = gameObject.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnSelected);
        }

        private void OnSelected(SelectEnterEventArgs args)
        {
            if (pressed) return;
            pressed = true;

            // Visual feedback: button turns bright white
            if (btnRenderer != null)
            {
                Material mat = btnRenderer.material;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.white * 4f);
            }

            // Find ElevatorSceneTransition via the shared elevator root parent
            ElevatorSceneTransition transition = null;
            Transform elevRoot = transform.parent;
            if (elevRoot != null)
                transition = elevRoot.GetComponentInChildren<ElevatorSceneTransition>();

            // Fallback: search the whole scene
            if (transition == null)
                transition = FindFirstObjectByType<ElevatorSceneTransition>();

            if (transition != null)
                transition.OpenDoors();
            else
                Debug.LogWarning("[ElevatorCallButton] Could not find ElevatorSceneTransition in scene.");
        }
    }
}
