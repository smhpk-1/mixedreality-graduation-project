using System.Collections.Generic;
using UnityEngine;

namespace ConveyorShift
{
    /// <summary>
    /// Procedurally generates industrial machines for the factory environment.
    /// Machines can be used as visual props and as future audio sources.
    /// </summary>
    public class MachineGenerator : MonoBehaviour
    {
        [Header("Machine Settings")]
        public int machineCount = 4;
        public Vector3 areaCenter = new Vector3(0, 0, 0);
        public Vector3 areaSize = new Vector3(8, 0, 8);
        public Vector2 machineSizeRange = new Vector2(0.7f, 2.0f);
        public Material machineMaterial;

        private List<GameObject> machines = new List<GameObject>();

        [ContextMenu("Generate Machines")]
        public void GenerateMachines()
        {
            ClearMachines();
            for (int i = 0; i < machineCount; i++)
            {
                Vector3 pos = areaCenter + new Vector3(
                    Random.Range(-areaSize.x / 2, areaSize.x / 2),
                    0,
                    Random.Range(-areaSize.z / 2, areaSize.z / 2)
                );
                float baseSize = Random.Range(machineSizeRange.x, machineSizeRange.y);
                GameObject machine = CreateMachine(pos, baseSize);
                machines.Add(machine);
            }
        }

        private GameObject CreateMachine(Vector3 position, float baseSize)
        {
            GameObject root = new GameObject("FactoryMachine");
            root.transform.position = position;

            // Main body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(baseSize, baseSize * 1.2f, baseSize * 0.7f);
            ApplyMaterial(body);

            // Chimney
            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chimney.transform.SetParent(root.transform);
            chimney.transform.localPosition = new Vector3(0, baseSize * 0.8f, 0);
            chimney.transform.localScale = new Vector3(baseSize * 0.2f, baseSize * 0.6f, baseSize * 0.2f);
            ApplyMaterial(chimney);

            // Control panel
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.transform.SetParent(root.transform);
            panel.transform.localPosition = new Vector3(baseSize * 0.4f, baseSize * 0.2f, baseSize * 0.4f);
            panel.transform.localScale = new Vector3(baseSize * 0.3f, baseSize * 0.15f, baseSize * 0.1f);
            ApplyMaterial(panel);

            // Pipes
            for (int i = 0; i < 2; i++)
            {
                GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pipe.transform.SetParent(root.transform);
                pipe.transform.localPosition = new Vector3(
                    baseSize * 0.5f * (i == 0 ? 1 : -1),
                    baseSize * 0.4f,
                    -baseSize * 0.3f
                );
                pipe.transform.localScale = new Vector3(baseSize * 0.07f, baseSize * 0.5f, baseSize * 0.07f);
                pipe.transform.localRotation = Quaternion.Euler(90, 0, 0);
                ApplyMaterial(pipe);
            }

            root.AddComponent<BoxCollider>();
            root.isStatic = true;
            return root;
        }

        private void ApplyMaterial(GameObject obj)
        {
            if (machineMaterial != null)
            {
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = machineMaterial;
                }
            }
        }

        [ContextMenu("Clear Machines")]
        public void ClearMachines()
        {
            foreach (var m in machines)
            {
                if (m != null) DestroyImmediate(m);
            }
            machines.Clear();
        }
    }
}
