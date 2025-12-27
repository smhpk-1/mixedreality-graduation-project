using UnityEngine;

// Bu script, küplere otomatik olarak "Cube" tag'ini atar
public class CubeTagger : MonoBehaviour
{
    void Awake()
    {
        gameObject.tag = "Cube";
    }
}