using UnityEditor;
using UnityEngine;

/// <summary>
/// Train_Prefab altında iki kanatlı kayan kapı oluşturur.
/// Tools → Train → Build Single Door
/// </summary>
public static class TrainDoorBuilder
{
    [MenuItem("Tools/Train/Build Single Door")]
    public static void BuildDoor()
    {
        GameObject train = GameObject.Find("Train_Prefab");
        if (train == null)
        {
            EditorUtility.DisplayDialog("Hata", "Sahnede 'Train_Prefab' bulunamadı.", "Tamam");
            return;
        }

        // Sadece peron tarafındaki (front) kapıları devre dışı bırak
        // Back kapılar (trenin diğer tarafı) dokunulmadan kalır
        string[] oldDoors = { "sidedoor_front_l", "sidedoor_front_r" };
        foreach (string doorName in oldDoors)
        {
            Transform d = train.transform.Find(doorName);
            if (d != null)
            {
                Undo.RecordObject(d.gameObject, "Disable Old Door");
                d.gameObject.SetActive(false);
            }
        }

        // Mevcut NewSlideDoor varsa pozisyon/rotasyon/scale'i kaydet, sonra sil
        Vector3    savedPos   = Vector3.zero;
        Quaternion savedRot   = Quaternion.identity;
        Vector3    savedScale = new Vector3(2f, 2.5f, 0.05f);

        Transform existing = train.transform.Find("NewSlideDoor");
        if (existing != null)
        {
            savedPos   = existing.localPosition;
            savedRot   = existing.localRotation;
            savedScale = existing.localScale;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // ── Parent boş obje ─────────────────────────────────────────────────
        GameObject doorRoot = new GameObject("NewSlideDoor");
        Undo.RegisterCreatedObjectUndo(doorRoot, "Create Double Slide Door");
        doorRoot.transform.SetParent(train.transform, false);
        doorRoot.transform.localPosition = savedPos;
        doorRoot.transform.localRotation = savedRot;
        doorRoot.transform.localScale    = Vector3.one;

        // Kapı materyali
        string matPath = "Assets/materials/TrainDoorMat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.05f, 0.08f, 0.25f);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        // Panel boyutları — eski kapının yarısı genişliğinde
        float panelWidth  = savedScale.x / 2f;
        float panelHeight = savedScale.y;
        float panelDepth  = savedScale.z;
        float halfGap     = panelWidth / 2f;

        // ── Sol kanat ───────────────────────────────────────────────────────
        GameObject panelL = CreatePanel("DoorPanel_L", doorRoot, mat,
            localPos: new Vector3(0f, 0f, -halfGap),
            scale:    new Vector3(panelWidth, panelHeight, panelDepth));

        // ── Sağ kanat ───────────────────────────────────────────────────────
        GameObject panelR = CreatePanel("DoorPanel_R", doorRoot, mat,
            localPos: new Vector3(0f, 0f, halfGap),
            scale:    new Vector3(panelWidth, panelHeight, panelDepth));

        // ── SubwayTrainController'a otomatik ata ────────────────────────────
        SubwayTrainController ctrl = train.GetComponent<SubwayTrainController>();
        if (ctrl != null)
        {
            Undo.RecordObject(ctrl, "Assign Door Panels");
            ctrl.leftDoors      = new Transform[] { panelL.transform };
            ctrl.rightDoors     = new Transform[] { panelR.transform };
            ctrl.slideDistance  = panelWidth;
            ctrl.slideLocalAxis = new Vector3(0f, 0f, 1f);
        }

        Selection.activeGameObject = doorRoot;
        SceneView.FrameLastActiveSceneView();

        EditorUtility.DisplayDialog(
            "Çift Kanatlı Kapı Oluşturuldu",
            "DoorPanel_L ve DoorPanel_R oluşturuldu.\n" +
            (ctrl != null
                ? "SubwayTrainController'a otomatik atandı.\nPlay'e bas ve test et.\nYanlış yönde kayarsa Slide Local Axis'i (1,0,0) dene."
                : "SubwayTrainController bulunamadı — panelleri manuel ata."),
            "Tamam");
    }

    private static GameObject CreatePanel(string name, GameObject parent, Material mat,
                                          Vector3 localPos, Vector3 scale)
    {
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = name;
        Undo.RegisterCreatedObjectUndo(panel, "Create Door Panel");
        panel.transform.SetParent(parent.transform, false);
        panel.transform.localPosition = localPos;
        panel.transform.localScale    = scale;
        panel.GetComponent<Renderer>().sharedMaterial = mat;
        return panel;
    }
}
