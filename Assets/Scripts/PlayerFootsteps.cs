using UnityEngine;

/// <summary>
/// Oyuncu adım sesleri — VR'da smooth locomotion ile yürürken beton üzerinde
/// adım sesi verir. Kameranın yatay hareketini izler; her ~0.72 m'de bir,
/// ayak hizasından (sol/sağ dönüşümlü) prosedürel bir adım çalar.
///
/// Teleport guard: kare başına 1.5 m üstü sıçramalar (teleport/scene snap)
/// adım saymaz. Ses StreetAmbienceDirector'ın prosedürel bankosundan gelir —
/// dosya gerekmez. Quest: 2 kaynaklı mini havuz, frame başına tek mesafe hesabı.
/// </summary>
public class PlayerFootsteps : MonoBehaviour
{
    [Tooltip("İki adım arasındaki yatay mesafe (metre)")]
    public float strideLength = 0.72f;
    [Tooltip("Bu hızın altında adım üretme (m/s) — başın ufak salınımı adım değildir")]
    public float minSpeed = 0.35f;
    [Range(0f, 1f)] public float volume = 0.22f;

    private Transform cam;
    private Vector3 lastPos;
    private float accumulated;
    private bool leftFoot;
    private float speedSmoothed;

    private AudioClip[] stepClips;
    private AudioSource[] pool;
    private int poolCursor;

    private void Start()
    {
        stepClips = new[]
        {
            ProceduralStreet.Footstep(0),
            ProceduralStreet.Footstep(1),
            ProceduralStreet.Footstep(2),
        };

        pool = new AudioSource[2];
        for (int i = 0; i < pool.Length; i++)
        {
            var go = new GameObject("FootstepVoice_" + i);
            go.transform.SetParent(transform, false);
            pool[i] = go.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
            pool[i].loop = false;
            pool[i].spatialBlend = 1f;
            pool[i].rolloffMode = AudioRolloffMode.Linear;
            pool[i].minDistance = 0.5f;
            pool[i].maxDistance = 6f;
            pool[i].dopplerLevel = 0f;
        }
    }

    private void Update()
    {
        if (cam == null)
        {
            var main = Camera.main;
            if (main == null) return;
            cam = main.transform;
            lastPos = cam.position;
            return;
        }

        Vector3 pos = cam.position;
        Vector3 delta = pos - lastPos;
        delta.y = 0f;
        lastPos = pos;

        float dist = delta.magnitude;

        // Teleport / sahne snap'i — adım sayma, sayacı sıfırla
        if (dist > 1.5f)
        {
            accumulated = 0f;
            return;
        }

        float speed = dist / Mathf.Max(0.0001f, Time.deltaTime);
        speedSmoothed = Mathf.Lerp(speedSmoothed, speed, 10f * Time.deltaTime);
        if (speedSmoothed < minSpeed)
        {
            // Duraksamada birikimi yavaşça boşalt — tekrar yürüyünce ilk adım hemen gelsin
            accumulated = Mathf.Min(accumulated, strideLength * 0.6f);
            return;
        }

        accumulated += dist;
        if (accumulated < strideLength) return;
        accumulated -= strideLength;

        PlayStep();
    }

    private void PlayStep()
    {
        leftFoot = !leftFoot;

        // Ayak hizası: kameranın altı, hafif sol/sağ — adımlar dönüşümlü hissedilir
        Vector3 side = cam.right * (leftFoot ? -0.14f : 0.14f);
        Vector3 footPos = cam.position + Vector3.down * 1.5f + side;

        var src = pool[poolCursor];
        poolCursor = (poolCursor + 1) % pool.Length;
        src.transform.position = footPos;
        src.pitch = Random.Range(0.93f, 1.08f);
        // Hızlı yürüyüş bir tık daha gür
        float v = volume * Mathf.Lerp(0.8f, 1.15f, Mathf.InverseLerp(0.5f, 2f, speedSmoothed));
        src.PlayOneShot(stepClips[Random.Range(0, stepClips.Length)], v);
    }
}
