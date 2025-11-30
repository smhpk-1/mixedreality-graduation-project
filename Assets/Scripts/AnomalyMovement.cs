using System;
using System.Collections;
using UnityEngine;


namespace ConveyorShift
{
    /// <summary>
    /// Controls the green glitch cube behaviour that becomes intangible and approaches the player.
    /// </summary>
    public class AnomalyMovement : MonoBehaviour
    {
        [SerializeField] private Transform targetCamera;
        [SerializeField] private float travelDuration = 5f;
        [SerializeField] private float stopDistance = 0.2f;
        [SerializeField] private AnimationCurve approachCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Optional FX")]
        [SerializeField] private ParticleSystem glitchParticles;

        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
        private Collider anomalyCollider;
        private Coroutine movementRoutine;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        public event Action OnAnomalyComplete;

        private void Awake()
        {
            grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            anomalyCollider = GetComponent<Collider>();
            CacheInitialTransform();
            EnsureIntangible();
        }

        private void CacheInitialTransform()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        public void ResetAnomaly()
        {
            StopMovement();
            gameObject.SetActive(false);
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            EnsureIntangible();
        }

        public void RevealAnomaly()
        {
            gameObject.SetActive(true);
            CacheInitialTransform();
            EnsureIntangible();

            if (glitchParticles != null && !glitchParticles.isPlaying)
            {
                glitchParticles.Play();
            }
        }

        public void BeginApproach()
        {
            StopMovement();
            movementRoutine = StartCoroutine(MoveTowardCamera());
        }

        private IEnumerator MoveTowardCamera()
        {
            Transform cameraTransform = ResolveCameraTarget();

            if (cameraTransform == null)
            {
                yield break;
            }

            Vector3 start = transform.position;
            Quaternion startRotation = transform.rotation;
            Vector3 end = cameraTransform.position + cameraTransform.forward * stopDistance;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, travelDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float curved = approachCurve.Evaluate(normalized);
                transform.position = Vector3.Lerp(start, end, curved);
                transform.rotation = Quaternion.Slerp(startRotation, Quaternion.LookRotation(cameraTransform.forward, Vector3.up), curved);
                yield return null;
            }

            transform.position = end;
            OnAnomalyComplete?.Invoke();
        }

        private Transform ResolveCameraTarget()
        {
            if (targetCamera != null)
            {
                return targetCamera;
            }

            Camera mainCamera = Camera.main;
            return mainCamera != null ? mainCamera.transform : null;
        }

        private void StopMovement()
        {
            if (movementRoutine != null)
            {
                StopCoroutine(movementRoutine);
                movementRoutine = null;
            }
        }

        private void EnsureIntangible()
        {
            if (grabInteractable != null)
            {
                grabInteractable.enabled = false;
            }

            if (anomalyCollider != null)
            {
                anomalyCollider.isTrigger = true;
            }
        }
    }
}

