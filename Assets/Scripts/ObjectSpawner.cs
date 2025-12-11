using System.Collections;
using UnityEngine;


namespace ConveyorShift
{
    /// <summary>
    /// Spawns coloured cube interactables onto the conveyor in random intervals.
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject[] redPrefabs;
        [SerializeField] private GameObject[] bluePrefabs;
        [SerializeField] private GameObject greenAnomalyPrefab; // New Green Cube Prefab
        [SerializeField] private Material redMaterial;
        [SerializeField] private Material blueMaterial;

        [Header("Timing")]
        [SerializeField] private float minSpawnDelay = 2.0f;
        [SerializeField] private float maxSpawnDelay = 4.0f;

        [Header("Physics")]
        [SerializeField] private float initialDropForce = 0.1f;
        [SerializeField] private Vector3 randomTorqueRange = new Vector3(0.5f, 0.5f, 0.5f);
        [SerializeField] private float rigidbodyDrag = 5f;
        [SerializeField] private bool autoStart = false;

        private bool isRunning;
        private Coroutine spawnRoutine;
        private int spawnCount = 0; // Counter
        private const int TARGET_SPAWN_COUNT = 30; // Switch after 30

        private Transform SpawnRoot => spawnPoint != null ? spawnPoint : transform;

        private void Start()
        {
            if (autoStart)
            {
                StartSpawning();
            }
        }

        public void StartSpawning()
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;
            spawnRoutine = StartCoroutine(SpawnLoop());
        }

        public void StopSpawning()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;

            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }
        }

        private IEnumerator SpawnLoop()
        {
            while (isRunning)
            {
                if (spawnCount < TARGET_SPAWN_COUNT)
                {
                    // Normal Phase: Red/Blue Cubes
                    SpawnObject();
                    spawnCount++;
                    float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
                    yield return new WaitForSeconds(delay);
                }
                else
                {
                    // Anomaly Phase: Spawn 3 Green Cubes then STOP
                    Debug.Log("Target spawn count reached. Spawning Anomalies...");
                    
                    for (int i = 0; i < 3; i++)
                    {
                        SpawnAnomalyCube();
                        yield return new WaitForSeconds(1.0f); // Short delay between anomalies
                    }
                    
                    StopSpawning(); // Done.
                }
            }
        }

        private void SpawnAnomalyCube()
        {
            if (greenAnomalyPrefab == null)
            {
                Debug.LogError("Green Anomaly Prefab is not assigned!");
                return;
            }

            GameObject instance = Instantiate(greenAnomalyPrefab, SpawnRoot.position, SpawnRoot.rotation);
            ConfigureInteractable(instance);
            
            // Ensure it has the AnomalyCube script
            if (instance.GetComponent<AnomalyCube>() == null)
            {
                instance.AddComponent<AnomalyCube>();
            }
        }

        private void SpawnObject()
        {
            bool spawnRed = Random.value > 0.5f;
            GameObject prefab = ChoosePrefab(spawnRed);
            GameObject instance = InstantiateObject(prefab, spawnRed);
            if (instance == null)
            {
                return;
            }

            ConfigureInteractable(instance);
        }

        private GameObject ChoosePrefab(bool spawnRed)
        {
            GameObject[] pool = spawnRed ? redPrefabs : bluePrefabs;

            if (pool == null || pool.Length == 0)
            {
                return null;
            }

            int index = Random.Range(0, pool.Length);
            return pool[index];
        }

        private GameObject InstantiateObject(GameObject prefab, bool isRed)
        {
            if (prefab != null)
            {
                return Instantiate(prefab, SpawnRoot.position, SpawnRoot.rotation);
            }

            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.name = isRed ? "RedCube" : "BlueCube"; // Name it for identification
            primitive.transform.SetPositionAndRotation(SpawnRoot.position, SpawnRoot.rotation);
            // Adjusted size for hand interaction (approx 15-20cm) with slight randomness
            primitive.transform.localScale = Vector3.one * Random.Range(0.15f, 0.2f);

            Material runtimeMaterial = isRed ? redMaterial : blueMaterial;
            ApplyMaterial(primitive, runtimeMaterial, isRed);
            return primitive;
        }

        private static void ApplyMaterial(GameObject primitive, Material material, bool isRed)
        {
            Renderer renderer = primitive.GetComponent<Renderer>();

            if (renderer == null)
            {
                return;
            }

            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            else
            {
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = isRed ? Color.red : Color.blue
                };
            }
        }

        private void ConfigureInteractable(GameObject instance)
        {
            Rigidbody rigidbody = instance.GetComponent<Rigidbody>();

            if (rigidbody == null)
            {
                rigidbody = instance.AddComponent<Rigidbody>();
            }

            // Rigidbody settings
            rigidbody.linearDamping = rigidbodyDrag;
            rigidbody.angularDamping = 5f;
            rigidbody.mass = 0.5f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            // Add Grab Interaction
            var grab = instance.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            if (grab == null)
            {
                grab = instance.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            }

            // Configure Grab settings for best feel
            grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;
            grab.forceGravityOnDetach = true;
            
            // FIX: Ensure the object is interactable by Everything (Layer -1)
            // This fixes issues where the controller might be looking for a specific layer
            grab.interactionLayers = -1; 

            // FIX: Explicitly tell the interactable which collider to use
            Collider col = instance.GetComponent<Collider>();
            if (col != null && !grab.colliders.Contains(col))
            {
                grab.colliders.Add(col);
            }

            // Initial drop force
            if (initialDropForce > 0f)
            {
                rigidbody.AddForce(-SpawnRoot.up * initialDropForce, ForceMode.Impulse);
            }

            // Random torque removed to ensure objects land flat on the conveyor
            /* 
            if (randomTorqueRange.magnitude > 0f)
            {
                Vector3 torque = new Vector3(
                    Random.Range(-randomTorqueRange.x, randomTorqueRange.x),
                    Random.Range(-randomTorqueRange.y, randomTorqueRange.y),
                    Random.Range(-randomTorqueRange.z, randomTorqueRange.z));

                rigidbody.AddTorque(torque, ForceMode.Impulse);
            }
            */
        }
    }
}

