using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ConveyorShift
{
    /// <summary>
    /// High level state controller that advances the scenario through the
    /// requested phases: Work -> Glitch -> Transition.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            WorkState,
            GlitchState,
            TransitionState,
            EmptyRoomState // New state for the cleared room
        }

        [Header("Scene References")]
        [SerializeField] private ConveyorBelt conveyorBelt;
        [SerializeField] private ObjectSpawner objectSpawner;
        [SerializeField] private AnomalyMovement anomalyMovement;
        // [SerializeField] private FloatingMessController floatingMessController; // Removed

        [Header("Cleanup Targets")]
        [SerializeField] private GameObject messRoot; // Assign "Generated_Mess"
        [SerializeField] private GameObject binsRoot; // Assign "SortingBins"
        [SerializeField] private GameObject conveyorRoot; // Assign "conveyorbelt"
        [SerializeField] private GameObject dispenserRoot; // Assign "Cube_Dispenser"

        [Header("Timing")]
        // [SerializeField] private float workDurationSeconds = 100f; // Removed
        // [SerializeField] private float daydreamDurationSeconds = 20f; // Removed
        [SerializeField] private float glitchOverlayFadeTime = 2f;
        [SerializeField] private float blackoutFadeTime = 0.75f;

        [Header("Overlays")]
        [SerializeField] private CanvasGroup glitchOverlayCanvas;
        [SerializeField] private CanvasGroup blackoutCanvas;
        [SerializeField] private AnimationCurve overlayFadeCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Scene Management")]
        [SerializeField] private string nextSceneName = "Scene 2";

        private GameState currentState = GameState.WorkState;
        private Coroutine activeRoutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (glitchOverlayCanvas != null)
            {
                glitchOverlayCanvas.alpha = 0f;
                glitchOverlayCanvas.gameObject.SetActive(false);
            }

            if (blackoutCanvas != null)
            {
                blackoutCanvas.alpha = 0f;
                blackoutCanvas.gameObject.SetActive(false);
            }

            if (anomalyMovement != null)
            {
                anomalyMovement.OnAnomalyComplete += HandleAnomalyComplete;
            }
        }

        private void OnDestroy()
        {
            // Check if the object is still alive before accessing it to prevent MissingReferenceException on quit
            if (anomalyMovement != null && anomalyMovement.gameObject != null)
            {
                anomalyMovement.OnAnomalyComplete -= HandleAnomalyComplete;
            }
        }

        private void Start()
        {
            SwitchState(GameState.WorkState);
        }

        public void TriggerAnomalyCleanup()
        {
            Debug.Log("Anomaly Triggered! Clearing the room...");
            SwitchState(GameState.EmptyRoomState);
        }

        private void SwitchState(GameState nextState)
        {
            currentState = nextState;

            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }

            switch (currentState)
            {
                case GameState.WorkState:
                    activeRoutine = StartCoroutine(WorkRoutine());
                    break;
                case GameState.GlitchState:
                    activeRoutine = StartCoroutine(GlitchRoutine());
                    break;
                case GameState.TransitionState:
                    activeRoutine = StartCoroutine(TransitionRoutine());
                    break;
                case GameState.EmptyRoomState:
                    activeRoutine = StartCoroutine(EmptyRoomRoutine());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IEnumerator EmptyRoomRoutine()
        {
            // 1. Stop Spawning
            objectSpawner?.StopSpawning();
            
            // 2. Stop Belt
            conveyorBelt?.StopBelt();

            // 3. Destroy Props
            if (messRoot == null) messRoot = GameObject.Find("Generated_Mess");
            if (messRoot != null) Destroy(messRoot);

            if (binsRoot == null) binsRoot = GameObject.Find("SortingBins"); // Adjust name if needed
            if (binsRoot != null) Destroy(binsRoot);

            if (conveyorRoot == null) conveyorRoot = GameObject.Find("conveyorbelt");
            if (conveyorRoot != null) Destroy(conveyorRoot);

            if (dispenserRoot == null) dispenserRoot = GameObject.Find("Cube_Dispenser");
            if (dispenserRoot != null) Destroy(dispenserRoot);

            // 4. Destroy all spawned cubes (Red/Blue/Green)
            // We can find them by tag or type if they are not parented.
            // Assuming they have XRGrabInteractable or Rigidbody
            var interactables = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None);
            foreach (var interactable in interactables)
            {
                Destroy(interactable.gameObject);
            }

            Debug.Log("Room Cleared. Only the room geometry remains.");
            yield return null;
        }

        private IEnumerator WorkRoutine()
        {
            conveyorBelt?.StartBelt();
            objectSpawner?.StartSpawning();
            anomalyMovement?.ResetAnomaly();

            // Removed time-based progression. Game stays in WorkState indefinitely until external trigger.
            yield return null; 
        }

        private IEnumerator GlitchRoutine()
        {
            // Phase 1: The Daydream Begins (Floating Mess)
            // floatingMessController.StartFloating(); // Removed

            // Optional: Slow down belt instead of stopping immediately?
            // For now, let's keep the belt running during the daydream for the "Loop" effect
            // conveyorBelt?.StopBelt(); 
            
            // Wait for the daydream duration (20 seconds)
            // yield return new WaitForSeconds(daydreamDurationSeconds); // Removed

            // Phase 2: The Glitch / End of Scene
            objectSpawner?.StopSpawning();
            conveyorBelt?.StopBelt();

            if (glitchOverlayCanvas != null)
            {
                glitchOverlayCanvas.gameObject.SetActive(true);
                yield return FadeCanvas(glitchOverlayCanvas, 1f, glitchOverlayFadeTime);
            }

            anomalyMovement?.RevealAnomaly();
            anomalyMovement?.BeginApproach();
        }

        private IEnumerator TransitionRoutine()
        {
            if (blackoutCanvas != null)
            {
                blackoutCanvas.gameObject.SetActive(true);
                yield return FadeCanvas(blackoutCanvas, 1f, blackoutFadeTime);
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(nextSceneName);

            if (loadOperation == null)
            {
                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }
        }

        private void HandleAnomalyComplete()
        {
            if (currentState != GameState.GlitchState)
            {
                return;
            }

            SwitchState(GameState.TransitionState);
        }

        private IEnumerator FadeCanvas(CanvasGroup canvas, float targetAlpha, float duration)
        {
            float startAlpha = canvas.alpha;
            float time = 0f;
            duration = Mathf.Max(0.01f, duration);

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = overlayFadeCurve.Evaluate(time / duration);
                canvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvas.alpha = targetAlpha;
            canvas.gameObject.SetActive(targetAlpha > 0f);
        }
    }
}

