using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace MusicSpace
{
    /// <summary>
    /// On-screen debug logger for VR testing.
    /// Displays debug messages on a world-space canvas visible in the headset.
    /// </summary>
    public class VRDebugLogger : MonoBehaviour
    {
        [Header("Settings")]
        public int maxMessages = 15;
        public float messageLifetime = 10f;
        public Vector3 canvasOffset = new Vector3(0, 2f, 3f);
        
        private static VRDebugLogger instance;
        private Canvas canvas;
        private TextMeshProUGUI textDisplay;
        private List<string> messages = new List<string>();
        private List<float> messageTimes = new List<float>();

        private void Awake()
        {
            instance = this;
            CreateDebugCanvas();
            
            // Hook into Unity's log system
            Application.logMessageReceived += HandleLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void CreateDebugCanvas()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("VRDebugCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Position canvas in front of player
            canvasObj.transform.localPosition = canvasOffset;
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = Vector3.one * 0.005f; // Scale down for world space
            
            // Add CanvasScaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            
            // Create background panel
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            Image panel = panelObj.AddComponent<Image>();
            panel.color = new Color(0, 0, 0, 0.7f);
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(800, 500);
            
            // Create text
            GameObject textObj = new GameObject("DebugText");
            textObj.transform.SetParent(panelObj.transform, false);
            
            // Try to use TextMeshPro if available
            textDisplay = textObj.AddComponent<TextMeshProUGUI>();
            textDisplay.fontSize = 24;
            textDisplay.color = Color.white;
            textDisplay.alignment = TextAlignmentOptions.TopLeft;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
            
            // Add title
            Log("<color=yellow>=== VR Debug Logger ===</color>");
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            string color = type switch
            {
                LogType.Error => "red",
                LogType.Warning => "orange",
                _ => "white"
            };
            
            // Only show ColorReactiveWall related messages
            if (logString.Contains("ColorReactiveWall") || logString.Contains("Wall") || logString.Contains("Cube"))
            {
                Log($"<color={color}>{logString}</color>");
            }
        }

        public static void Log(string message)
        {
            if (instance != null)
            {
                instance.AddMessage(message);
            }
        }

        private void AddMessage(string message)
        {
            messages.Add(message);
            messageTimes.Add(Time.time);
            
            // Remove old messages
            while (messages.Count > maxMessages)
            {
                messages.RemoveAt(0);
                messageTimes.RemoveAt(0);
            }
            
            UpdateDisplay();
        }

        private void Update()
        {
            // Remove expired messages
            bool changed = false;
            for (int i = messageTimes.Count - 1; i >= 0; i--)
            {
                if (Time.time - messageTimes[i] > messageLifetime)
                {
                    messages.RemoveAt(i);
                    messageTimes.RemoveAt(i);
                    changed = true;
                }
            }
            
            if (changed)
            {
                UpdateDisplay();
            }
            
            // Make canvas face the camera
            if (Camera.main != null)
            {
                canvas.transform.LookAt(Camera.main.transform);
                canvas.transform.Rotate(0, 180, 0);
            }
        }

        private void UpdateDisplay()
        {
            if (textDisplay != null)
            {
                textDisplay.text = string.Join("\n", messages);
            }
        }
    }
}
