#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DAY05_AutoSetup
{
    private const string ShaderPath = "Assets/SG_Shield.shadergraph";
    private const string MaterialPath = "Assets/Materials/Mat_Shield.mat";
    private const string ScenePath = "Assets/Scenes/DAY05_Shield.unity";
    private const string SessionKey = "DAY05_Shield_AutoSetup_v1";

    static DAY05_AutoSetup()
    {
        EditorApplication.delayCall += TrySetup;
    }

    [MenuItem("Tools/DAY05/Finalize Shield Demo")]
    public static void SetupFromMenu()
    {
        SessionState.EraseBool(SessionKey);
        TrySetup();
    }

    private static void TrySetup()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
        {
            // Shader Graph may still be importing on first project open.
            EditorApplication.delayCall += TrySetup;
            return;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null && GameObject.Find("DAY05_ShieldDemo") != null)
        {
            SessionState.SetBool(SessionKey, true);
            return;
        }

        if (mat == null)
        {
            mat = new Material(shader) { name = "Mat_Shield" };
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        else
        {
            mat.shader = shader;
        }

        if (mat.HasProperty("_ShieldColor"))
            mat.SetColor("_ShieldColor", new Color(0.15f, 0.65f, 1.0f, 1.0f));
        if (mat.HasProperty("_RimPower"))
            mat.SetFloat("_RimPower", 3.0f);
        if (mat.HasProperty("_EmissionStrength"))
            mat.SetFloat("_EmissionStrength", 2.0f);
        if (mat.HasProperty("_AlphaStrength"))
            mat.SetFloat("_AlphaStrength", 0.45f);
        if (mat.HasProperty("_PulseSpeed"))
            mat.SetFloat("_PulseSpeed", 2.0f);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject demo = GameObject.Find("DAY05_ShieldDemo");
        if (demo == null)
        {
            demo = GameObject.Find("Sphere (3)");
            if (demo == null)
                demo = GameObject.Find("Sphere");
        }

        if (demo == null)
        {
            demo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            demo.transform.position = new Vector3(0.07f, 1.0f, 3.68f);
        }

        demo.name = "DAY05_ShieldDemo";
        Renderer renderer = demo.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = mat;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        SessionState.SetBool(SessionKey, true);
        Debug.Log("DAY05 setup complete: SG_Shield + Mat_Shield + DAY05_ShieldDemo");
    }
}
#endif
