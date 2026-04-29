using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Water_Volume underwater effect disabled for Unity 6 / URP RenderGraph compatibility.
// The water surface shader still works normally.
public class Water_Volume : ScriptableRendererFeature
{
    public override void Create() { }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) { }
}



