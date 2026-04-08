using UnityEngine;
using System.Collections;

namespace MusicSpace
{
    /// <summary>
    /// Forces the XR Origin to spawn inside the room and moves cubes there too.
    /// Attach this to XR Origin (XR Rig).
    /// </summary>
    public class SpawnInRoom : MonoBehaviour
    {
        [Tooltip("Drag the RoomGeometry object here, or leave empty to auto-find")]
        public Transform roomTarget;

        [Tooltip("Height offset above the room position")]
        public float heightOffset = 1.0f;

        private void Start()
        {
            StartCoroutine(ForceSpawnPosition());
        }

        private IEnumerator ForceSpawnPosition()
        {
            // Wait for room generation and XR init
            yield return null;
            yield return null;

            // Auto-find RoomGeometry if not assigned
            if (roomTarget == null)
            {
                GameObject roomGeo = GameObject.Find("RoomGeometry");
                if (roomGeo != null)
                    roomTarget = roomGeo.transform;
            }

            if (roomTarget != null)
            {
                Vector3 roomPos = roomTarget.position;
                Vector3 target = new Vector3(roomPos.x, roomPos.y + heightOffset, roomPos.z);

                // Force XR Origin position multiple frames
                for (int i = 0; i < 10; i++)
                {
                    transform.position = target;
                    yield return null;
                }

                // Also move the cubes generator into the room
                GameObject cubesObj = GameObject.Find("Scene2TwentyColoredCubes");
                if (cubesObj != null)
                {
                    cubesObj.transform.position = new Vector3(roomPos.x, roomPos.y, roomPos.z);
                }
            }
        }
    }
}
