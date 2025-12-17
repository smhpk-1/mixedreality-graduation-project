using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

namespace MusicSpace
{
    /// <summary>
    /// Spawns music cubes from prefabs at fixed world positions.
    /// Prefab-based approach for reliable Quest deployment.
    /// </summary>
    public class MusicCubeSpawner : MonoBehaviour
    {
        [Header("Prefab (Create in Unity Editor)")]
        [Tooltip("Drag your cube prefab here")]
        public GameObject cubePrefab;

        [Header("Spawn Settings")]
        [Tooltip("Fixed world position - NOT attached to player")]
        public Vector3 spawnCenter = new Vector3(0, 1.2f, 2f);
        public float cubeSpacing = 0.2f;

        [Header("Audio Clips")]
        public AudioClip[] cubeSounds = new AudioClip[5];

        // Colors for the 5 cubes
        private readonly Color[] cubeColors = new Color[]
        {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow,
            new Color(0.8f, 0.2f, 0.8f) // Purple
        };

        private readonly string[] cubeNames = new string[]
        {
            "RedCube", "BlueCube", "GreenCube", "YellowCube", "PurpleCube"
        };

        private List<GameObject> spawnedCubes = new List<GameObject>();

        private void Start()
        {
            // Wait a frame to ensure scene is loaded
            Invoke(nameof(SpawnAllCubes), 0.1f);
        }

        [ContextMenu("Spawn All Cubes")]
        public void SpawnAllCubes()
        {
            // Clear existing
            foreach (var cube in spawnedCubes)
            {
                if (cube != null) Destroy(cube);
            }
            spawnedCubes.Clear();

            // Check if prefab exists
            if (cubePrefab == null)
            {
                Debug.LogError("MusicCubeSpawner: No prefab assigned! Creating basic cubes...");
                SpawnBasicCubes();
                return;
            }

            // Spawn from prefab
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos = spawnCenter + new Vector3((i - 2) * cubeSpacing, 0, 0);
                GameObject cube = Instantiate(cubePrefab, pos, Quaternion.identity);
                cube.name = cubeNames[i];
                
                // Set color on renderer
                Renderer rend = cube.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = cubeColors[i];
                }

                // Setup MusicCube component
                MusicCube mc = cube.GetComponent<MusicCube>();
                if (mc != null)
                {
                    mc.cubeColor = cubeColors[i];
                    mc.cubeName = cubeNames[i];
                    mc.spawnPosition = pos;
                    if (i < cubeSounds.Length) mc.assignedSound = cubeSounds[i];
                }

                spawnedCubes.Add(cube);
            }

            Debug.Log($"Spawned 5 cubes at world position {spawnCenter}");
        }

        /// <summary>
        /// Fallback: Create basic cubes without prefab
        /// </summary>
        private void SpawnBasicCubes()
        {
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos = spawnCenter + new Vector3((i - 2) * cubeSpacing, 0, 0);
                
                // Create cube
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = cubeNames[i];
                cube.transform.position = pos;
                cube.transform.localScale = Vector3.one * 0.1f;

                // Create a proper opaque material
                Renderer rend = cube.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Standard"));
                mat.SetFloat("_Mode", 0); // Opaque mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = -1;
                mat.color = cubeColors[i];
                rend.material = mat;

                // Add rigidbody - floating
                Rigidbody rb = cube.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.mass = 0.5f;
                rb.linearDamping = 3f;
                rb.angularDamping = 3f;

                // Add XR Grab
                XRGrabInteractable grab = cube.AddComponent<XRGrabInteractable>();
                grab.throwOnDetach = true;
                grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

                // Add audio
                cube.AddComponent<AudioSource>();

                // Add MusicCube
                MusicCube mc = cube.AddComponent<MusicCube>();
                mc.cubeColor = cubeColors[i];
                mc.cubeName = cubeNames[i];
                mc.spawnPosition = pos;
                if (i < cubeSounds.Length) mc.assignedSound = cubeSounds[i];

                spawnedCubes.Add(cube);
            }
        }

        private void OnDrawGizmosSelected()
        {
            for (int i = 0; i < 5; i++)
            {
                Gizmos.color = cubeColors[i];
                Vector3 pos = spawnCenter + new Vector3((i - 2) * cubeSpacing, 0, 0);
                Gizmos.DrawWireCube(pos, Vector3.one * 0.1f);
            }
        }
    }
}
