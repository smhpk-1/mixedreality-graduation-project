using UnityEngine;

namespace ConveyorShift
{
    /// <summary>
    /// Applies a constant force to rigidbodies that rest on the belt in order to drag them
    /// toward the player. Also scrolls the material offset for a simple movement illusion.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ConveyorBelt : MonoBehaviour
    {
        [SerializeField] private float beltForce = 8f;
        [SerializeField] private Vector3 localDirection = Vector3.forward;
        [SerializeField] private Renderer beltRenderer;
        [SerializeField] private float textureScrollSpeed = 2f;

        private bool isActive = true;
        private Material beltMaterial;
        private Vector2 currentOffset = Vector2.zero;

        private void Awake()
        {
            // Material instance oluştur ki scroll diğer objeleri etkilemesin
            if (beltRenderer != null)
            {
                beltMaterial = beltRenderer.material; // Bu otomatik olarak instance oluşturur
            }
        }

        private void Reset()
        {
            Collider colliderComponent = GetComponent<Collider>();
            colliderComponent.sharedMaterial = null;
            colliderComponent.isTrigger = false;
        }

        private void Update()
        {
            if (!isActive || beltMaterial == null)
            {
                return;
            }

            // Texture offset'i sürekli scroll et (bandın hareket illüzyonu)
            currentOffset.y += textureScrollSpeed * Time.deltaTime;
            
            // Offset'i sıfırla (çok büyük olmasın)
            if (currentOffset.y > 1f)
            {
                currentOffset.y -= 1f;
            }
            
            beltMaterial.mainTextureOffset = currentOffset;
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!isActive || collision.rigidbody == null)
            {
                return;
            }

            Vector3 worldDirection = transform.TransformDirection(localDirection).normalized;
            collision.rigidbody.AddForce(worldDirection * beltForce, ForceMode.Acceleration);
        }

        public void StartBelt()
        {
            isActive = true;
        }

        public void StopBelt()
        {
            isActive = false;
        }

        private void OnDestroy()
        {
            // Material instance'ı temizle (memory leak önlemek için)
            if (beltMaterial != null)
            {
                Destroy(beltMaterial);
            }
        }
    }
}


