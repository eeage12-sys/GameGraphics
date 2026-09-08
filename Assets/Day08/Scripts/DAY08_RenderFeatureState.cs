using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class DAY08_RenderFeatureState : MonoBehaviour
{
    public ScriptableRendererData rendererData;
    public bool outlineShellPass;
    public bool screenOutline;

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        if (isActiveAndEnabled)
            Apply();
    }

    public void Apply()
    {
        if (rendererData == null || rendererData.rendererFeatures == null)
            return;

        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature == null) continue;

            if (feature.name == "DAY08 Outline Shell Pass")
                feature.SetActive(outlineShellPass);
            else if (feature.name == "DAY08 Screen Outline")
                feature.SetActive(screenOutline);
        }
    }
}
