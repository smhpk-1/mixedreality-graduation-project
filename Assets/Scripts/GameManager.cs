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
        public enum GameState
        {
            WorkState,
            GlitchState,
            TransitionState
        }

        [Header("Scene References")]
        [SerializeField] private ConveyorBelt conveyorBelt;
        [SerializeField] private ObjectSpawner objectSpawner;
        [SerializeField] private AnomalyMovement anomalyMovement;
        [SerializeField] private FloatingMessController floatingMessController; // Added reference

        [Header("Timing")]
        [SerializeField] private float workDurationSeconds = 100f; // Reduced to 100s
        [SerializeField] private float daydreamDurationSeconds = 20f; // Added 20s for daydream
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
            if (anomalyMovement != null)
            {
                anomalyMovement.OnAnomalyComplete -= HandleAnomalyComplete;
            }
        }

        private void Start()
        {
            SwitchState(GameState.WorkState);
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IEnumerator WorkRoutine()
        {
            conveyorBelt?.StartBelt();
            objectSpawner?.StartSpawning();
            anomalyMovement?.ResetAnomaly();

            yield return new WaitForSeconds(workDurationSeconds);
            SwitchState(GameState.GlitchState);
        }

        private IEnumerator GlitchRoutine()
        {
            // Phase 1: The Daydream Begins (Floating Mess)
            if (floatingMessController != null)
            {
                floatingMessController.StartFloating();
            }

            // Optional: Slow down belt instead of stopping immediately?
            // For now, let's keep the belt running during the daydream for the "Loop" effect
            // conveyorBelt?.StopBelt(); 
            
            // Wait for the daydream duration (20 seconds)
            yield return new WaitForSeconds(daydreamDurationSeconds);

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

