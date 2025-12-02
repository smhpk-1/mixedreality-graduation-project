using UnityEngine;

public class FactoryFloorGenerator : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 floorSize = new Vector3(10f, 0.1f, 15f); // Genişlik, Kalınlık, Uzunluk
    public Color concreteColor = new Color(0.25f, 0.25f, 0.28f); // Koyu, kirli beton rengi

    [ContextMenu("Generate Dirty Floor")]
    public void GenerateFloor()
    {
        // Eski zemin varsa temizle
        GameObject oldFloor = GameObject.Find("FactoryFloor");
        if (oldFloor != null) DestroyImmediate(oldFloor);

        // Yeni zemin oluştur
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "FactoryFloor";
        
        // Konum: Yüzeyi tam 0 noktasında olacak şekilde hafif aşağıda
        floor.transform.position = new Vector3(0, -floorSize.y / 2, 0); 
        floor.transform.localScale = floorSize;

        // Materyal oluştur (Kirli Beton)
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.name = "DirtyConcreteMat";
        mat.color = concreteColor;
        mat.SetFloat("_Smoothness", 0.1f); // Mat, parlak olmayan yüzey
        
        floor.GetComponent<Renderer>().sharedMaterial = mat;
        
        Debug.Log("Kirli zemin oluşturuldu! Şimdi MessGenerator scriptine bu zemini tanıtmayı unutma.");
    }
}
