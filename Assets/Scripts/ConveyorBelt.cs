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
        [SerializeField] private float targetSpeed = 1.5f;
        [SerializeField] private float acceleration = 15f;
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 worldDirection = transform.TransformDirection(localDirection).normalized;
            Gizmos.DrawLine(transform.position, transform.position + worldDirection * 2f);
            Gizmos.DrawSphere(transform.position + worldDirection * 2f, 0.1f);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!isActive || collision.rigidbody == null)
            {
                return;
            }

            // 1. Stop Rolling: Heavily dampen angular velocity while on the belt
            collision.rigidbody.angularVelocity = Vector3.Lerp(collision.rigidbody.angularVelocity, Vector3.zero, Time.deltaTime * 20f);

            // 2. Move: Accelerate towards target speed instead of adding raw force
            Vector3 worldDirection = transform.TransformDirection(localDirection).normalized;
            Vector3 targetVelocity = worldDirection * targetSpeed;
            
            // Use linearVelocity for Unity 6 compatibility (matches ObjectSpawner's linearDamping)
            Vector3 currentVelocity = collision.rigidbody.linearVelocity;
            
            // Calculate speed difference (ignoring gravity/Y axis)
            Vector3 velocityDiff = targetVelocity - currentVelocity;
            velocityDiff.y = 0;

            // Apply acceleration to match speed
            collision.rigidbody.AddForce(velocityDiff * acceleration, ForceMode.Acceleration);
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


