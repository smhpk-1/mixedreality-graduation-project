using UnityEngine;
using UnityEngine.SceneManagement;

namespace MusicSpace
{
    /// <summary>
    /// Attached to the inside trigger of the elevator.
    /// When the player enters: closes doors, waits 5 seconds, transitions to Scene 3.
    /// Also provides OpenDoors() method called by ElevatorProximityTrigger.
    /// </summary>
    public class ElevatorSceneTransition : MonoBehaviour
    {
        [Header("Settings")]
        public string targetScene = "Scene 3";
        public float doorOpenDuration = 1.5f;
        public float doorCloseDuration = 1.5f;
        public float waitAfterClose = 5f;

        private bool doorsOpen = false;
        private bool closing = false;
        private Transform leftDoor;
        private Transform rightDoor;
        private float elevWidth;
        private float doorHalfWidth;

        private void Start()
        {
            Transform elevator = transform.parent;
            if (elevator != null)
            {
                leftDoor = elevator.Find("LeftDoor");
                rightDoor = elevator.Find("RightDoor");
                if (leftDoor != null)
                {
                    doorHalfWidth = leftDoor.localScale.x;
                    elevWidth = doorHalfWidth * 2f;
                }
            }
        }

        public void OpenDoors()
        {
            if (doorsOpen || closing) return;
            doorsOpen = true;
            StartCoroutine(DoorOpenSequence());
        }

        private void OnTriggerEnter(Collider other)
        {
            if (closing) return;
            if (!doorsOpen) return;

            if (other.CompareTag("Player") || other.CompareTag("MainCamera") ||
                other.GetComponentInParent<Camera>() != null)
            {
                closing = true;
                StartCoroutine(DoorCloseAndTransition());
            }
        }

        private System.Collections.IEnumerator DoorOpenSequence()
        {
            if (leftDoor == null || rightDoor == null) yield break;

            Vector3 leftStart = leftDoor.localPosition;
            Vector3 rightStart = rightDoor.localPosition;

            // Open position: push doors to sides
            Vector3 leftTarget = new Vector3(-elevWidth / 2f - doorHalfWidth / 2f + 0.05f, leftStart.y, leftStart.z);
            Vector3 rightTarget = new Vector3(elevWidth / 2f + doorHalfWidth / 2f - 0.05f, rightStart.y, rightStart.z);

            float elapsed = 0f;
            while (elapsed < doorOpenDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / doorOpenDuration);
                t = t * t * (3f - 2f * t);
                leftDoor.localPosition = Vector3.Lerp(leftStart, leftTarget, t);
                rightDoor.localPosition = Vector3.Lerp(rightStart, rightTarget, t);
                yield return null;
            }
            leftDoor.localPosition = leftTarget;
            rightDoor.localPosition = rightTarget;
        }

        private System.Collections.IEnumerator DoorCloseAndTransition()
        {
            if (leftDoor == null || rightDoor == null)
            {
                yield return new WaitForSeconds(waitAfterClose);
                SceneManager.LoadScene(targetScene);
                yield break;
            }

            // Small pause before closing
            yield return new WaitForSeconds(0.3f);

            Vector3 leftStart = leftDoor.localPosition;
            Vector3 rightStart = rightDoor.localPosition;

            // Closed position: doors meet at center
            Vector3 leftTarget = new Vector3(-doorHalfWidth / 2f, leftStart.y, leftStart.z);
            Vector3 rightTarget = new Vector3(doorHalfWidth / 2f, rightStart.y, rightStart.z);

            float elapsed = 0f;
            while (elapsed < doorCloseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / doorCloseDuration);
                t = t * t * (3f - 2f * t);
                leftDoor.localPosition = Vector3.Lerp(leftStart, leftTarget, t);
                rightDoor.localPosition = Vector3.Lerp(rightStart, rightTarget, t);
                yield return null;
            }
            leftDoor.localPosition = leftTarget;
            rightDoor.localPosition = rightTarget;

            // Wait 5 seconds, then transition
            yield return new WaitForSeconds(waitAfterClose);
            SceneManager.LoadScene(targetScene);
        }
    }
}
