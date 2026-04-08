using UnityEngine;

namespace MusicSpace
{
    /// <summary>
    /// Attached to a trigger zone outside the elevator.
    /// When the player approaches, it tells ElevatorSceneTransition to open the doors.
    /// </summary>
    public class ElevatorProximityTrigger : MonoBehaviour
    {
        private bool triggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;

            if (other.CompareTag("Player") || other.CompareTag("MainCamera") ||
                other.GetComponentInParent<Camera>() != null)
            {
                triggered = true;

                // Find ElevatorSceneTransition on the sibling InsideTrigger
                Transform elevator = transform.parent;
                if (elevator != null)
                {
                    ElevatorSceneTransition transition = elevator.GetComponentInChildren<ElevatorSceneTransition>();
                    if (transition != null)
                    {
                        transition.OpenDoors();
                    }
                }
            }
        }
    }
}
