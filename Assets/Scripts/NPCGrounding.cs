using UnityEngine;

/// <summary>
/// Scene 3 NPC'leri için TEK yetkili zemin sorgusu.
///
/// "Havada yüzen NPC" sorununun kökü: hiçbir kod NPC'yi gerçek zemine
/// oturtmuyordu. NavMesh snap'i navmesh DÜZLEMİNİ bulur (görsel zeminin
/// birkaç cm-on cm üstünde durabilir), waypoint yürüyüşü waypoint Y'sini
/// aynen takip eder, manuel mod ise 0.7m üstü düşüşleri reddeder (peron
/// kenarı koruması ilk inişi de engelliyordu).
///
/// Bu utility görsel zemini doğrudan ölçer: NPC'lerin kendi collider'larını,
/// diğer NPC'leri, trigger'ları, fiziksel objeleri (çöpler) ve oyuncunun
/// CharacterController'ını yok sayarak aşağı ray atar, origin altındaki en
/// yüksek geçerli yüzeyi döndürür.
/// </summary>
public static class NPCGrounding
{
    private static readonly RaycastHit[] Hits = new RaycastHit[16];

    /// <summary>
    /// position'ın altındaki gerçek zemin Y'sini bulur. self ve NPC'ler hariç.
    /// </summary>
    public static bool TryGetGroundY(Vector3 position, Transform self, out float groundY,
                                     float probeUp = 3f, float probeDown = 30f)
    {
        Vector3 origin = position + Vector3.up * probeUp;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, Hits,
                                            probeUp + probeDown, ~0,
                                            QueryTriggerInteraction.Ignore);
        groundY = 0f;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            var h = Hits[i];
            if (h.collider == null) continue;

            // Kendi gövdemiz / diğer NPC'ler zemin değildir
            if (self != null && h.transform.IsChildOf(self)) continue;
            if (IsNpc(h.transform)) continue;

            // Oyuncu ve serbest fizik objeleri (çöpler vb.) zemin değildir
            if (h.collider is CharacterController) continue;
            if (h.rigidbody != null && !h.rigidbody.isKinematic) continue;

            // Yürünebilir eğim olsun
            if (h.normal.y < 0.4f) continue;

            // Origin altındaki EN YÜKSEK yüzeyi al (peron > ray yatağı)
            if (!found || h.point.y > groundY)
            {
                groundY = h.point.y;
                found = true;
            }
        }
        return found;
    }

    /// <summary>
    /// Root'u doğrudan zemine oturtur (ilk yerleşim / cycle reset için).
    /// Zemin bulunamazsa konuma dokunmaz, false döner.
    /// </summary>
    public static bool SnapToGround(Transform root, float footOffset = 0.02f,
                                    float probeUp = 3f, float probeDown = 30f)
    {
        if (root == null) return false;
        if (!TryGetGroundY(root.position, root, out float y, probeUp, probeDown))
            return false;

        Vector3 p = root.position;
        p.y = y + footOffset;
        root.position = p;
        return true;
    }

    private static bool IsNpc(Transform t)
    {
        return t.GetComponentInParent<NPCScene3Wanderer>() != null
            || t.GetComponentInParent<NPCTrainPassenger>() != null;
    }
}
