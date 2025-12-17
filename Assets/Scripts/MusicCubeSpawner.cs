using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

namespace MusicSpace
{
    /// <summary>
    /// Spawns and manages the 5 music cubes in Scene 2.
    /// </summary>
    public class MusicCubeSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Reference to XR Origin to spawn cubes near player")]
        public Transform xrOrigin;
        [Tooltip("Offset from player position (X=left/right, Y=up/down, Z=forward)")]
        public Vector3 spawnOffsetFromPlayer = new Vector3(0, 1.0f, 0.6f);
        public float cubeSpacing = 0.3f;
        public float cubeSize = 0.12f;

        [Header("Cube Colors")]
        public Color[] cubeColors = new Color[]
        {
            new Color(1f, 0.2f, 0.2f),    // Red
            new Color(0.2f, 0.6f, 1f),    // Blue
            new Color(0.2f, 1f, 0.4f),    // Green
            new Color(1f, 0.9f, 0.2f),    // Yellow
            new Color(0.8f, 0.3f, 1f)     // Purple
        };

        [Header("Cube Names")]
        public string[] cubeNames = new string[]
        {
            "RedCube",
            "BlueCube", 
            "GreenCube",
            "YellowCube",
            "PurpleCube"
        };

        [Header("Audio Clips (Assign Your Sounds Here)")]
        public AudioClip[] cubeSounds = new AudioClip[5];

        [Header("Physics")]
        public float cubeMass = 0.5f;
        public float cubeAngularDrag = 0.5f;

        private List<MusicCube> spawnedCubes = new List<MusicCube>();
        private Transform cubeContainer;

        private void Start()
        {
            // Create container for organization
            cubeContainer = new GameObject("MusicCubes").transform;
            cubeContainer.parent = transform;
            cubeContainer.localPosition = Vector3.zero;

            // Try to find XR Origin if not assigned
            if (xrOrigin == null)
            {
                GameObject xrOriginObj = GameObject.Find("XR Origin (XR Rig)");
                if (xrOriginObj != null)
                {
                    xrOrigin = xrOriginObj.transform;
                }
            }

            SpawnAllCubes();
        }

        /// <summary>
        /// Get spawn center position based on player location
        /// </summary>
        private Vector3 GetSpawnCenter()
        {
            if (xrOrigin != null)
            {
                // Spawn relative to player position
                return xrOrigin.position + xrOrigin.TransformDirection(spawnOffsetFromPlayer);
            }
            else
            {
                // Fallback to world position
                return spawnOffsetFromPlayer;
            }
        }

        [ContextMenu("Spawn All Cubes")]
        public void SpawnAllCubes()
        {
            // Clear existing cubes
            foreach (var cube in spawnedCubes)
            {
                if (cube != null)
                {
                    Destroy(cube.gameObject);
                }
            }
            spawnedCubes.Clear();

            Vector3 spawnCenter = GetSpawnCenter();

            // Spawn 5 cubes in an arc in front of player
            for (int i = 0; i < 5; i++)
            {
                // Position cubes in a slight arc
                float xOffset = (i - 2) * cubeSpacing;
                Vector3 position = spawnCenter + new Vector3(xOffset, 0, 0);
                SpawnCube(i, position);
            }
        }

        private void SpawnCube(int index, Vector3 position)
        {
            // Create cube primitive
            GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeObj.name = cubeNames[index];
            cubeObj.transform.parent = cubeContainer;
            cubeObj.transform.position = position;
            cubeObj.transform.localScale = Vector3.one * cubeSize;

            // Setup material - use default cube material and set color
            MeshRenderer renderer = cubeObj.GetComponent<MeshRenderer>();
            Material mat = renderer.material; // Get the default material instance
            mat.color = cubeColors[index];
            
            // Try to enable emission if shader supports it
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", cubeColors[index] * 0.5f);
            }

            // Add Rigidbody - start with gravity OFF (floating)
            Rigidbody rb = cubeObj.AddComponent<Rigidbody>();
            rb.mass = cubeMass;
            rb.angularDamping = cubeAngularDrag;
            rb.linearDamping = 10f; // High drag for floating
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.useGravity = false; // Floating in air

            // Add XR Grab Interactable
            XRGrabInteractable grabInteractable = cubeObj.AddComponent<XRGrabInteractable>();
            grabInteractable.throwOnDetach = true;
            grabInteractable.throwSmoothingDuration = 0.1f;
            grabInteractable.throwVelocityScale = 1.5f;
            grabInteractable.throwAngularVelocityScale = 1f;
            grabInteractable.useDynamicAttach = true;
            grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;

            // Add AudioSource
            AudioSource audioSource = cubeObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;

            // Add MusicCube script
            MusicCube musicCube = cubeObj.AddComponent<MusicCube>();
            musicCube.cubeColor = cubeColors[index];
            musicCube.cubeName = cubeNames[index];
            musicCube.spawnPosition = position;
            
            // Assign sound if available
            if (index < cubeSounds.Length && cubeSounds[index] != null)
            {
                musicCube.assignedSound = cubeSounds[index];
            }

            spawnedCubes.Add(musicCube);
        }

        public void RespawnCube(MusicCube cube)
        {
            // The cube handles its own reset, this is for any additional logic
            Debug.Log($"Respawning {cube.cubeName} at {cube.spawnPosition}");
        }

        // Editor helper to visualize spawn positions
        private void OnDrawGizmosSelected()
        {
            Vector3 center = GetSpawnCenter();
            
            for (int i = 0; i < 5; i++)
            {
                Gizmos.color = cubeColors[i];
                Vector3 pos = center + new Vector3((i - 2) * cubeSpacing, 0, 0);
                Gizmos.DrawWireCube(pos, Vector3.one * cubeSize);
                Gizmos.DrawSphere(pos, cubeSize * 0.3f);
            }
        }
    }
}
