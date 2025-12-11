using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ConveyorShift
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class AnomalyCube : MonoBehaviour
    {
        private XRGrabInteractable interactable;
        private bool triggered = false;

        private void Awake()
        {
            interactable = GetComponent<XRGrabInteractable>();
        }

        private void OnEnable()
        {
            interactable.selectEntered.AddListener(OnGrab);
        }

        private void OnDisable()
        {
            interactable.selectEntered.RemoveListener(OnGrab);
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            if (triggered) return;
            triggered = true;

            Debug.Log("Anomaly Cube Touched! Clearing Room...");
            
            // Trigger the cleanup via GameManager
            GameManager.Instance.TriggerAnomalyCleanup();
        }
    }
}
