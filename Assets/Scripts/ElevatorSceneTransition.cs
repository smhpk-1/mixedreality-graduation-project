using UnityEngine;
using UnityEngine.SceneManagement;

namespace MusicSpace
{
    /// <summary>
    /// Attached to the inside trigger of the elevator.
    /// When the player enters: closes doors, waits 5 seconds, transitions to Scene 3.
    /// OpenDoors() is called by ElevatorCallButton when the player presses the call button.
    /// </summary>
    public class ElevatorSceneTransition : MonoBehaviour
    {
        [Header("Settings")]
        public string targetScene = "Scene 3";
        public float doorOpenDuration = 1.5f;
        public float doorCloseDuration = 1.5f;
        public float waitAfterClose = 5f;

        [Tooltip("Radius inside elevator that triggers door close + scene transition.")]
        public float insideRadius = 2.5f;

        private bool doorsOpen = false;
        private bool closing = false;
        private Transform leftDoor;
        private Transform rightDoor;
        private Transform doorSeam;
        private float elevWidth;
        private float doorHalfWidth;

        private bool playerInside = false;

        private void Start()
        {
            Transform elevator = transform.parent;
            if (elevator != null)
            {
                leftDoor  = elevator.Find("LeftDoor");
                rightDoor = elevator.Find("RightDoor");
                doorSeam  = elevator.Find("DoorSeam");
                if (leftDoor != null)
                {
                    doorHalfWidth = leftDoor.localScale.x;
                    elevWidth     = doorHalfWidth * 2f;
                }
            }
        }

        public void OpenDoors()
        {
            if (doorsOpen || closing) return;
            doorsOpen = true;
            StartCoroutine(DoorOpenSequence());
        }

        private void Update()
        {
            // Allow transition even if doors were never explicitly "opened"
            // (proximity trigger may have failed in VR) — physical doors block entry anyway.
            if (closing) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            bool nowInside = Vector3.Distance(cam.transform.position, transform.position) <= insideRadius;

            if (nowInside && !playerInside)
            {
                playerInside = true;
                closing = true;
                // If doors are somehow still closed, open first then close
                if (!doorsOpen)
                {
                    doorsOpen = true;
                    StartCoroutine(OpenThenClose());
                }
                else
                {
                    StartCoroutine(DoorCloseAndTransition());
                }
            }

            if (!nowInside)
                playerInside = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, insideRadius);
        }

        private System.Collections.IEnumerator OpenThenClose()
        {
            yield return StartCoroutine(DoorOpenSequence());
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(DoorCloseAndTransition());
        }

        private System.Collections.IEnumerator DoorOpenSequence()
        {
            if (leftDoor == null || rightDoor == null) yield break;

            // Hide seam while doors are open
            if (doorSeam != null) doorSeam.gameObject.SetActive(false);

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

            // Show seam again once doors are fully closed
            if (doorSeam != null) doorSeam.gameObject.SetActive(true);

            // Wait 5 seconds, then transition
            yield return new WaitForSeconds(waitAfterClose);
            SceneManager.LoadScene(targetScene);
        }
    }
}
