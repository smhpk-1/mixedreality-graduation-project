using UnityEngine;

namespace MusicSpace
{
    /// <summary>
    /// Placed outside the elevator. Checks every frame whether the player's head
    /// (Camera.main) is within the trigger radius. When it enters, opens the elevator
    /// doors via ElevatorSceneTransition.OpenDoors().
    /// Distance-based detection is used because OnTriggerEnter is unreliable with
    /// XR Origin rigs in VR (tag/layer mismatches).
    /// </summary>
    public class ElevatorProximityTrigger : MonoBehaviour
    {
        [Tooltip("How close the player's head must be (metres) to trigger door open.")]
        public float triggerRadius = 2.5f;

        private bool triggered = false;
        private ElevatorSceneTransition transition;

        private void Start()
        {
            Transform elevator = transform.parent;
            if (elevator != null)
                transition = elevator.GetComponentInChildren<ElevatorSceneTransition>();

            if (transition == null)
                transition = FindFirstObjectByType<ElevatorSceneTransition>();
        }

        private void Update()
        {
            if (triggered) return;
            if (transition == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            if (Vector3.Distance(cam.transform.position, transform.position) <= triggerRadius)
            {
                triggered = true;
                transition.OpenDoors();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
            Gizmos.DrawSphere(transform.position, triggerRadius);
        }
    }
}
