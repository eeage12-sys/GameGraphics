using UnityEngine;

public sealed class PortfolioHUD : MonoBehaviour
{
    [SerializeField] private PortfolioEffectController controller;

    public void Configure(PortfolioEffectController effectController)
    {
        controller = effectController;
    }

    private void OnGUI()
    {
        const float width = 430f;
        const float height = 150f;

        GUI.Box(new Rect(14f, 14f, width, height), "Game Graphics Portfolio");
        GUI.Label(new Rect(28f, 42f, width - 24f, 22f), "LMB on Impact Pad : FX_HitSpark once");
        GUI.Label(new Rect(28f, 64f, width - 24f, 22f), "1 / 2 / 3 : VFX SpawnRate 20 / 200 / 80");
        GUI.Label(new Rect(28f, 86f, width - 24f, 22f), "Shader : Fresnel + Emission + Time pulse + Alpha");
        GUI.Label(new Rect(28f, 108f, width - 24f, 22f), "Particles : HitSpark + HealGlow | PBR comparison");

        string state = controller != null && controller.SpawnRatePropertyReady
            ? $"SpawnRate exposed | Current {controller.CurrentSpawnRate:0}"
            : "SpawnRate property needs final VFX Graph connection";

        GUI.Label(new Rect(28f, 130f, width - 24f, 22f), state);
    }
}
