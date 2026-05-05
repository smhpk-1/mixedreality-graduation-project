using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class TrashItemSetup
{
    private static readonly string[] trashNames = new[]
    {
        "Prefab_WaterBottle",
        "Prefab_CoffeeCup",
        "Prefab_BotteCap",
        "Prefab_SodaBottle",
        "Prefab_SodaCan",
        "Prefab_SodaCup",
        "Mesh_WatterBottle",
    };

    // ── 1) Çöp objelerini kur ────────────────────────────────────────────────
    [MenuItem("Tools/Trash/1 - Setup Trash Items")]
    public static void SetupAll()
    {
        int count = 0;

        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (!IsTrash(go.name)) continue;

            Undo.RecordObject(go, "Setup Trash Item");

            if (go.GetComponent<TrashItem>() == null)
                Undo.AddComponent<TrashItem>(go);

            var existingRb = go.GetComponent<Rigidbody>();
            if (existingRb == null)
                existingRb = Undo.AddComponent<Rigidbody>(go);
            Undo.RecordObject(existingRb, "Setup Trash Rigidbody");
            existingRb.mass           = 0.2f;
            existingRb.linearDamping  = 1f;
            existingRb.angularDamping = 1f;
            existingRb.isKinematic    = false;   // Fizik aktif — bırakınca düşer
            existingRb.useGravity     = true;

            // Root'ta collider yoksa BoxCollider ekle (XRGrabInteractable için şart)
            if (go.GetComponent<Collider>() == null)
                Undo.AddComponent<BoxCollider>(go);

            if (go.GetComponent<XRGrabInteractable>() == null)
            {
                var grab = Undo.AddComponent<XRGrabInteractable>(go);
                grab.movementType    = XRBaseInteractable.MovementType.VelocityTracking; // Fırlatılabilir
                grab.throwOnDetach   = true;
            }

            count++;
        }

        Debug.Log($"[Trash] {count} çöp objesi kuruldu.");
        EditorUtility.DisplayDialog("Setup Trash Items", $"{count} objeye bileşenler eklendi.", "Tamam");
    }

    // ── 2) Fizik aktif mevcut çöpleri güncelle ──────────────────────────────
    [MenuItem("Tools/Trash/2 - Fizik Güncelle (Gravity On)")]
    public static void FixRigidbodies()
    {
        int count = 0;
        foreach (var trash in Object.FindObjectsByType<TrashItem>(FindObjectsSortMode.None))
        {
            var rb = trash.GetComponent<Rigidbody>();
            if (rb == null) continue;
            Undo.RecordObject(rb, "Fix Trash Physics");
            rb.isKinematic     = false;
            rb.useGravity      = true;
            rb.linearDamping   = 1f;
            rb.angularDamping  = 1f;
            // XRGrabInteractable movementType güncelle
            var grab = trash.GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                Undo.RecordObject(grab, "Fix Grab Type");
                grab.movementType  = XRBaseInteractable.MovementType.VelocityTracking;
                grab.throwOnDetach = true;
            }
            count++;
        }
        EditorUtility.DisplayDialog("Fizik Güncellendi", $"{count} çöp objesi güncellendi. Artık fırlatılabilir.", "Tamam");
    }

    // ── 3) TrashCart kur ─────────────────────────────────────────────────────
    [MenuItem("Tools/Trash/3 - Setup TrashCart + UI")]
    public static void SetupTrashCart()
    {
        GameObject cart = GameObject.Find("Prefab_TrashCart");
        if (cart == null)
        {
            EditorUtility.DisplayDialog("Hata", "Sahnede 'Prefab_TrashCart' bulunamadı.", "Tamam");
            return;
        }

        // Eski TrashCounter_Canvas'ı kaldır
        Transform oldCanvas = cart.transform.Find("TrashCounter_Canvas");
        if (oldCanvas != null)
            Undo.DestroyObjectImmediate(oldCanvas.gameObject);

        // Trigger collider — sepet ağzını kapsayacak boyut
        BoxCollider trigger = cart.GetComponent<BoxCollider>();
        if (trigger == null)
            trigger = Undo.AddComponent<BoxCollider>(cart);
        Undo.RecordObject(trigger, "Setup Cart Trigger");
        trigger.isTrigger = true;
        trigger.size      = new Vector3(1.5f, 2f, 1.5f);
        trigger.center    = new Vector3(0f, 1f, 0f);

        // TrashCollector
        TrashCollector collector = cart.GetComponent<TrashCollector>();
        if (collector == null)
            collector = Undo.AddComponent<TrashCollector>(cart);

        int total = Object.FindObjectsByType<TrashItem>(FindObjectsSortMode.None).Length;
        collector.totalTrash = total > 0 ? total : 17;

        // ── Camera HUD Canvas (XR kamerasına bağlı world-space) ──────────────
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            EditorUtility.DisplayDialog("Uyarı",
                "Main Camera bulunamadı. Canvas manuel oluşturulacak.\nTrashCollector'a counterText alanını kendin bağla.", "Tamam");
            return;
        }

        // Varsa eski HUD'u temizle
        Transform oldHud = mainCam.transform.Find("TrashHUD_Canvas");
        if (oldHud != null)
            Undo.DestroyObjectImmediate(oldHud.gameObject);

        // Kameraya bağlı world-space canvas (VR'da Screen Overlay çalışmaz)
        GameObject canvasGO = new GameObject("TrashHUD_Canvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create TrashHUD");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        canvasGO.transform.SetParent(mainCam.transform, false);
        // Kameranın 1.5m önünde, sağ üst köşe
        canvasGO.transform.localPosition = new Vector3(0.25f, 0.15f, 1.5f);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale    = Vector3.one * 0.001f;

        RectTransform crt = canvasGO.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(400f, 80f);

        // TextMeshPro
        GameObject textGO = new GameObject("CounterText");
        Undo.RegisterCreatedObjectUndo(textGO, "Create CounterText");
        textGO.transform.SetParent(canvasGO.transform, false);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "0 / " + collector.totalTrash + " trash collected";
        tmp.fontSize  = 48;
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.color     = Color.white;

        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        // Collector'a text bağla
        SerializedObject so = new SerializedObject(collector);
        so.FindProperty("counterText").objectReferenceValue = tmp;
        so.ApplyModifiedProperties();

        EditorUtility.DisplayDialog(
            "TrashCart Kuruldu",
            $"Trigger collider + TrashCollector eklendi.\nToplam çöp: {collector.totalTrash}\nHUD sayaç kameraya bağlandı (sağ üst).",
            "Tamam");
    }

    private static bool IsTrash(string name)
    {
        foreach (var keyword in trashNames)
            if (name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        return false;
    }
}
