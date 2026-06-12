using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Scene 3 fizik onarıcısı — sahne yüklenince kendini çalıştırır.
///
/// SORUN: Subway Vol2 asset pack'inin HİÇBİR prefab'ında collider yok; sahnedeki
/// 216 box collider elle eklenmiş ve eksik. Sonuçları (cihazda görüldü):
///   • NPC'ler ve oyuncu duvarlardan geçiyor (engel raycast'leri boşa düşüyor,
///     NavMesh zemin plakaları duvarların altından devam ediyor)
///   • Bırakılan çöpler zemine düşemiyor / blob gölgeler zemini bulamıyor
///   • Bazı çöplerin collider'ı yok (tutulamıyor) ya da grab/rigidbody şişenin
///     kendisinde değil KAPAĞINDA → kapağı tutuyorsun, şişe yerinde kalıyor
///
/// YAPILANLAR:
///   1. Yapı parçalarına (duvar, zemin, kolon, bank, otomat...) renderer
///      bounds'undan BoxCollider eklenir (zaten collider'ı olanlara dokunulmaz).
///   2. Duvar/mobilya objelerine carve'lı NavMeshObstacle eklenir → NavMesh
///      ajanları artık duvarların/bankların içinden yürüyemez.
///   3. Çöp objeleri normalize edilir: grab + rigidbody SADECE kök objede,
///      tüm parçalara collider, XRI collider listesi yeniden kurulup manager'a
///      yeniden kayıt edilir, TrashGrabVRConfig uygulanır.
/// </summary>
public static class Scene3PhysicsBootstrap
{
    // Collider eklenecek yapı parçası isim önekleri (Subway Vol2)
    private static readonly string[] SolidPrefixes =
    {
        "WallA", "WallB", "Floor_Main", "Floor_Plane", "RailFloor", "Track",
        "Stair", "PillarA", "PillarB", "VendingMachine", "NewsPaperStand",
        "Metalbench", "MetalTrashCan"
    };

    // NavMesh'te carve edilecekler (ajanlar içinden geçmesin)
    private static readonly string[] CarvePrefixes =
    {
        "WallA", "WallB", "PillarA", "PillarB", "VendingMachine",
        "NewsPaperStand", "Metalbench", "MetalTrashCan"
    };

    // Çöp kökü sayılacak isim parçaları (URP_WasteOvergrowth_SA adlandırması)
    private static readonly string[] TrashNameBits =
    {
        "bottle", "botte", "watter", "water", "soda", "coffee",
        "cup", "can_", "cap", "trash"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        TryRun(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded += (scene, mode) => TryRun(scene);
    }

    private static void TryRun(Scene scene)
    {
        if (scene.name != "Scene 3") return;

        int solids = AddStructureColliders();
        int carves = AddNavMeshCarvers();
        int trash  = NormalizeTrashItems();
        Debug.Log($"[Scene3PhysicsBootstrap] {solids} yapı collider'ı, {carves} NavMesh carve, {trash} çöp normalize edildi.");
    }

    // ── 1. Yapı collider'ları ───────────────────────────────────────────
    private static int AddStructureColliders()
    {
        int added = 0;
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!NameStartsWithAny(mr.gameObject.name, SolidPrefixes)) continue;
            if (mr.GetComponent<Collider>() != null) continue;

            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var bc = mr.gameObject.AddComponent<BoxCollider>();
            bc.center = mf.sharedMesh.bounds.center;
            bc.size   = mf.sharedMesh.bounds.size;
            added++;
        }
        return added;
    }

    // ── 2. NavMesh carve engelleri ──────────────────────────────────────
    private static int AddNavMeshCarvers()
    {
        int added = 0;
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!NameStartsWithAny(mr.gameObject.name, CarvePrefixes)) continue;
            if (mr.GetComponent<NavMeshObstacle>() != null) continue;

            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var ob = mr.gameObject.AddComponent<NavMeshObstacle>();
            ob.shape   = NavMeshObstacleShape.Box;
            ob.center  = mf.sharedMesh.bounds.center;
            ob.size    = mf.sharedMesh.bounds.size;
            ob.carving = true;
            ob.carveOnlyStationary = true; // statik — bir kez carve edilir
            added++;
        }
        return added;
    }

    // ── 3. Çöp normalizasyonu ───────────────────────────────────────────
    private static int NormalizeTrashItems()
    {
        // Çöp köklerini topla: TrashItem/GrabbableTrash taşıyan objelerden yukarı,
        // ismi "çöp gibi" olan en üst ataya tırman (script kapağa eklenmiş olabilir)
        var roots = new HashSet<GameObject>();
        foreach (var t in Object.FindObjectsByType<TrashItem>(FindObjectsSortMode.None))
            roots.Add(TrashRootOf(t.transform).gameObject);
        foreach (var g in Object.FindObjectsByType<GrabbableTrash>(FindObjectsSortMode.None))
            roots.Add(TrashRootOf(g.transform).gameObject);

        foreach (var root in roots)
            NormalizeOne(root);

        return roots.Count;
    }

    private static Transform TrashRootOf(Transform t)
    {
        Transform best = t;
        Transform cur = t;
        while (cur.parent != null && NameLooksTrashy(cur.parent.name))
        {
            cur = cur.parent;
            best = cur;
        }
        return best;
    }

    private static void NormalizeOne(GameObject root)
    {
        // Çocuklardaki yabancı grab/rigidbody'leri kaldır (kapak grab'leri vs.)
        // DestroyImmediate: XRI manager kaydı hemen silinsin ki kökün yeniden
        // kaydı çakışmasın
        foreach (var g in root.GetComponentsInChildren<XRGrabInteractable>(true))
            if (g.gameObject != root) Object.DestroyImmediate(g);
        foreach (var r in root.GetComponentsInChildren<Rigidbody>(true))
            if (r.gameObject != root) Object.DestroyImmediate(r);

        // Çocuklardaki kopya çöp scriptlerini etkisizleştir (kökte olacaklar)
        foreach (var ti in root.GetComponentsInChildren<TrashItem>(true))
            if (ti.gameObject != root) ti.enabled = false;
        foreach (var gt in root.GetComponentsInChildren<GrabbableTrash>(true))
            if (gt.gameObject != root) gt.enabled = false;

        // Her görünür parçaya collider garanti et (collider'sız şişe tutulamıyor)
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (mr.GetComponent<Collider>() != null) continue;
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            var bc = mr.gameObject.AddComponent<BoxCollider>();
            bc.center = mf.sharedMesh.bounds.center;
            bc.size   = mf.sharedMesh.bounds.size;
        }

        // Kökte rigidbody + grab + TrashItem garanti et (sıra önemli:
        // TrashItem.Awake rigidbody ve grab'i arar)
        var rb = root.GetComponent<Rigidbody>();
        if (rb == null) rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        var grab = root.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = root.AddComponent<XRGrabInteractable>();

        if (root.GetComponent<TrashItem>() == null && root.GetComponent<GrabbableTrash>() == null)
            root.AddComponent<TrashItem>();

        // Cart sadece marker'lı (en az bir kez tutulmuş) çöpleri sayar — auto marker
        // editor tool'una bağlı kalmasın, kökte garanti olsun
        if (root.GetComponent<TrashGrabbedMarker>() == null &&
            root.GetComponent<TrashGrabAutoMarker>() == null)
            root.AddComponent<TrashGrabAutoMarker>();

        TrashGrabVRConfig.Apply(root);

        // XRI collider listesini yeniden kur ve manager'a yeniden kaydet
        grab.enabled = false;
        grab.colliders.Clear();
        foreach (var col in root.GetComponentsInChildren<Collider>(true))
            if (!col.isTrigger) grab.colliders.Add(col);
        grab.enabled = true;
    }

    // ── Yardımcılar ─────────────────────────────────────────────────────
    private static bool NameStartsWithAny(string name, string[] prefixes)
    {
        for (int i = 0; i < prefixes.Length; i++)
            if (name.StartsWith(prefixes[i], System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static bool NameLooksTrashy(string name)
    {
        string n = name.ToLowerInvariant();
        for (int i = 0; i < TrashNameBits.Length; i++)
            if (n.Contains(TrashNameBits[i]))
                return true;
        return false;
    }
}
