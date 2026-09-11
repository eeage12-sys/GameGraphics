using UnityEngine;

public class DAY14PortfolioHUD : MonoBehaviour
{
    [SerializeField] private bool show = true;

    private void OnGUI()
    {
        if (!show) return;
        GUI.Box(new Rect(12, 12, 390, 132), "DAY14 Graphics Portfolio");
        GUI.Label(new Rect(24, 38, 360, 22), "LMB Ground : FX_HitSpark");
        GUI.Label(new Rect(24, 58, 360, 22), "1 : VFX Low (SpawnRate 20)");
        GUI.Label(new Rect(24, 78, 360, 22), "2 : VFX High (SpawnRate 200)");
        GUI.Label(new Rect(24, 98, 360, 22), "Shader : Shield / Fresnel + Emission");
        GUI.Label(new Rect(24, 118, 360, 22), "Particle : HitSpark + HealGlow / VFX : GpuSpark");
    }
}
