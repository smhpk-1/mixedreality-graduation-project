using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

namespace MusicSpace
{
    /// <summary>
    /// Spawns and manages the 5 music cubes in Scene 2.
    /// Simple and reliable version for Quest 3S.
    /// </summary>
    public class MusicCubeSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Fixed world position where cubes spawn")]
        public Vector3 spawnCenter = new Vector3(0, 1.2f, 1.5f);
        public float cubeSpacing = 0.25f;
        public float cubeSize = 0.1f;

        [Header("Audio Clips (Assign Your Sounds Here)")]
        public AudioClip[] cubeSounds = new AudioClip[5];

        [Header("Physics")]
        public float cubeMass = 0.3f;

        // Fixed colors - bright and saturated
        private readonly Color[] cubeColors = new Color[]
        {
            new Color(1f, 0.1f, 0.1f),    // Bright Red
            new Color(0.1f, 0.4f, 1f),    // Bright Blue
            new Color(0.1f, 1f, 0.3f),    // Bright Green
            new Color(1f, 0.95f, 0.1f),   // Bright Yellow
            new Color(0.8f, 0.1f, 1f)     // Bright Purple
        };

        private readonly string[] cubeNames = new string[]
        {
            "RedCube",
            "BlueCube", 
            "GreenCube",
            "YellowCube",
            "PurpleCube"
        };

        private List<MusicCube> spawnedCubes = new List<MusicCube>();
        private Transform cubeContainer;

        private void Start()
        {
            // Create container for organization
            cubeContainer = new GameObject("MusicCubes").transform;
            cubeContainer.parent = transform;
            cubeContainer.localPosition = Vector3.zero;

            SpawnAllCubes();
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

            // Spawn 5 cubes in a row at fixed position
            for (int i = 0; i < 5; i++)
            {
                float xOffset = (i - 2) * cubeSpacing;
                Vector3 position = spawnCenter + new Vector3(xOffset, 0, 0);
                SpawnCube(i, position);
            }
            
            Debug.Log($"Spawned 5 music cubes at {spawnCenter}");
        }

        private void SpawnCube(int index, Vector3 position)
        {
            // Create cube
            GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeObj.name = cubeNames[index];
            cubeObj.transform.parent = cubeContainer;
            cubeObj.transform.position = position;
            cubeObj.transform.localScale = Vector3.one * cubeSize;

            // Create a simple unlit colored material (works on all platforms)
            MeshRenderer renderer = cubeObj.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat.shader.name == "Hidden/InternalErrorShader")
            {
                // Fallback if URP not found
                mat = new Material(Shader.Find("Unlit/Color"));
            }
            mat.color = cubeColors[index];
            renderer.material = mat;

            // Add Rigidbody - NO GRAVITY (floating)
            Rigidbody rb = cubeObj.AddComponent<Rigidbody>();
            rb.mass = cubeMass;
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearDamping = 5f;
            rb.angularDamping = 5f;

            // Add XR Grab Interactable - Quest optimized settings
            XRGrabInteractable grab = cubeObj.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;
            grab.throwSmoothingDuration = 0.25f;
            grab.throwVelocityScale = 1.5f;
            grab.throwAngularVelocityScale = 1.0f;
            grab.useDynamicAttach = true;
            grab.matchAttachPosition = true;
            grab.matchAttachRotation = true;
            grab.snapToColliderVolume = false;
            grab.retainTransformParent = true;

            // Add AudioSource
            AudioSource audioSource = cubeObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 0.5f;
            audioSource.maxDistance = 15f;

            // Add MusicCube script LAST (after all components)
            MusicCube musicCube = cubeObj.AddComponent<MusicCube>();
            musicCube.cubeColor = cubeColors[index];
            musicCube.cubeName = cubeNames[index];
            musicCube.spawnPosition = position;
            
            // Force apply color immediately
            musicCube.ApplyColor();

            // Assign sound if available
            if (index < cubeSounds.Length && cubeSounds[index] != null)
            {
                musicCube.assignedSound = cubeSounds[index];
            }

            spawnedCubes.Add(musicCube);
        }

        public void RespawnCube(MusicCube cube)
        {
            Debug.Log($"Respawning {cube.cubeName} at {cube.spawnPosition}");
        }

        // Editor visualization
        private void OnDrawGizmosSelected()
        {
            for (int i = 0; i < 5; i++)
            {
                Gizmos.color = cubeColors[i];
                Vector3 pos = spawnCenter + new Vector3((i - 2) * cubeSpacing, 0, 0);
                Gizmos.DrawCube(pos, Vector3.one * cubeSize);
            }
        }
    }
}
