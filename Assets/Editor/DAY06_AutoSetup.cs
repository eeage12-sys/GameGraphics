#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[InitializeOnLoad]
public static class DAY06_AutoSetup
{
    private const string ShaderPath = "Assets/SG_VertexWave.shader";
    private const string MaterialPath = "Assets/Materials/Mat_VertexWave.mat";
    private const string MeshPath = "Assets/DAY06_WaterWave/AnimatedSurfaceMesh.asset";
    private const string GridTexturePath = "Assets/DAY06_WaterWave/VertexWave_TestGrid.png";
    private const string WaterTexturePath = "Assets/DAY06_WaterWave/WaterSurface_Albedo.png";
    private const string ScenePath = "Assets/Scenes/DAY06_VertexWave.unity";
    private const string SessionKey = "DAY06_VertexWave_AutoSetup_v4_OfficialResources";

    static DAY06_AutoSetup()
    {
        EditorApplication.delayCall += TrySetupSilently;
    }

    [MenuItem("Tools/DAY06/Finalize Vertex Wave Demo")]
    public static void SetupFromMenu()
    {
        SessionState.EraseBool(SessionKey);
        if (BuildOrUpdateAssetsAndScene(true))
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Tools/DAY06/Open DAY06 Vertex Wave Scene")]
    public static void OpenDemoScene()
    {
        if (!File.Exists(ScenePath))
            BuildOrUpdateAssetsAndScene(false);
        if (File.Exists(ScenePath))
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Tools/DAY06/Use Test Grid Texture")]
    public static void UseGridTexture()
    {
        ApplyTexture(GridTexturePath, new Vector4(1f, 1f, 0f, 0f));
    }

    [MenuItem("Tools/DAY06/Use Water Texture")]
    public static void UseWaterTexture()
    {
        ApplyTexture(WaterTexturePath, new Vector4(1f, 1f, 0f, 0f));
    }

    private static void TrySetupSilently()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        if (BuildOrUpdateAssetsAndScene(false))
        {
            SessionState.SetBool(SessionKey, true);
            Debug.Log("DAY06 resources updated: water texture + test grid + vertex wave + UV flow.");
        }
        else
        {
            EditorApplication.delayCall += TrySetupSilently;
        }
    }

    private static bool BuildOrUpdateAssetsAndScene(bool showLog)
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        Texture2D gridTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GridTexturePath);
        Texture2D waterTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(WaterTexturePath);

        if (shader == null || gridTexture == null || waterTexture == null)
            return false;

        Directory.CreateDirectory("Assets/Materials");
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/DAY06_WaterWave");

        // DAY06 문서의 Import 설정
        ConfigureTexture(GridTexturePath, FilterMode.Point);
        ConfigureTexture(WaterTexturePath, FilterMode.Bilinear);

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh == null)
        {
            mesh = CreateGridMesh(64, 10f);
            mesh.name = "AnimatedSurfaceMesh";
            AssetDatabase.CreateAsset(mesh, MeshPath);
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(shader) { name = "Mat_VertexWave" };
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        else
        {
            mat.shader = shader;
        }

        SetMaterialDefaults(mat, waterTexture);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        // 현재 씬을 바꾸지 않고 DAY06 씬만 생성/갱신한다.
        Scene current = SceneManager.GetActiveScene();
        bool day06IsCurrent = current.path == ScenePath;
        Scene demoScene;
        bool openedAdditively = false;

        if (day06IsCurrent)
        {
            demoScene = current;
        }
        else if (File.Exists(ScenePath))
        {
            demoScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            openedAdditively = true;
        }
        else
        {
            demoScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            demoScene.name = "DAY06_VertexWave";
            openedAdditively = true;
        }

        SetupDemoScene(demoScene, mesh, mat);
        EditorSceneManager.SaveScene(demoScene, ScenePath);

        if (openedAdditively)
            EditorSceneManager.CloseScene(demoScene, true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showLog)
            Debug.Log("DAY06 complete: WaterSurface_Albedo + VertexWave_TestGrid + vertex wave + UV flow.");
        return true;
    }

    private static void SetupDemoScene(Scene scene, Mesh mesh, Material mat)
    {
        GameObject surface = FindRoot(scene, "AnimatedSurface");
        if (surface == null)
        {
            surface = new GameObject("AnimatedSurface");
            SceneManager.MoveGameObjectToScene(surface, scene);
        }

        MeshFilter mf = surface.GetComponent<MeshFilter>();
        if (mf == null) mf = surface.AddComponent<MeshFilter>();
        MeshRenderer mr = surface.GetComponent<MeshRenderer>();
        if (mr == null) mr = surface.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = mat;
        surface.transform.position = Vector3.zero;
        surface.transform.rotation = Quaternion.identity;
        surface.transform.localScale = Vector3.one;

        GameObject cameraGO = FindRoot(scene, "Main Camera");
        if (cameraGO == null)
        {
            cameraGO = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraGO, scene);
        }
        cameraGO.tag = "MainCamera";
        Camera cam = cameraGO.GetComponent<Camera>();
        if (cam == null) cam = cameraGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cameraGO.transform.position = new Vector3(0f, 3.2f, -7.3f);
        LookAt(cameraGO.transform, new Vector3(0f, 0f, 0.8f));

        GameObject lightGO = FindRoot(scene, "Directional Light");
        if (lightGO == null)
        {
            lightGO = new GameObject("Directional Light");
            SceneManager.MoveGameObjectToScene(lightGO, scene);
        }
        Light light = lightGO.GetComponent<Light>();
        if (light == null) light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        GameObject marker = FindRoot(scene, "DAY06_VertexWaveDemo");
        if (marker == null)
        {
            marker = new GameObject("DAY06_VertexWaveDemo");
            SceneManager.MoveGameObjectToScene(marker, scene);
        }
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }

    private static void SetMaterialDefaults(Material mat, Texture2D waterTexture)
    {
        if (mat.HasProperty("_WaveTexture")) mat.SetTexture("_WaveTexture", waterTexture);
        if (mat.HasProperty("_Amplitude")) mat.SetFloat("_Amplitude", 0.15f);
        if (mat.HasProperty("_WaveFrequency")) mat.SetFloat("_WaveFrequency", 2f);
        if (mat.HasProperty("_WaveSpeed")) mat.SetFloat("_WaveSpeed", 1.5f);
        if (mat.HasProperty("_UvTiling")) mat.SetVector("_UvTiling", new Vector4(1f, 1f, 0f, 0f));
        if (mat.HasProperty("_UvFlowDirection")) mat.SetVector("_UvFlowDirection", new Vector4(0.03f, 0.08f, 0f, 0f));
        if (mat.HasProperty("_UvFlowSpeed")) mat.SetFloat("_UvFlowSpeed", 0.2f);
        if (mat.HasProperty("_CrossWaveFrequency")) mat.SetFloat("_CrossWaveFrequency", 1.6f);
        if (mat.HasProperty("_CrossWaveSpeed")) mat.SetFloat("_CrossWaveSpeed", 1.1f);
        if (mat.HasProperty("_CrossWaveStrength")) mat.SetFloat("_CrossWaveStrength", 0.5f);
    }

    private static void ApplyTexture(string texturePath, Vector4 tiling)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (mat == null || tex == null)
        {
            Debug.LogWarning("DAY06: Material/texture not ready. Use Tools > DAY06 > Finalize Vertex Wave Demo.");
            return;
        }

        if (mat.HasProperty("_WaveTexture")) mat.SetTexture("_WaveTexture", tex);
        if (mat.HasProperty("_UvTiling")) mat.SetVector("_UvTiling", tiling);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
        Debug.Log("DAY06 texture changed to: " + texturePath);
    }

    private static void ConfigureTexture(string path, FilterMode filter)
    {
        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            bool dirty = false;
            if (importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                dirty = true;
            }
            if (importer.filterMode != filter)
            {
                importer.filterMode = filter;
                dirty = true;
            }
            if (dirty) importer.SaveAndReimport();
        }
    }

    private static Mesh CreateGridMesh(int resolution, float size)
    {
        int vertsPerSide = resolution + 1;
        Vector3[] vertices = new Vector3[vertsPerSide * vertsPerSide];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[resolution * resolution * 6];

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int i = z * vertsPerSide + x;
                float fx = (float)x / resolution;
                float fz = (float)z / resolution;
                vertices[i] = new Vector3((fx - 0.5f) * size, 0f, (fz - 0.5f) * size);
                normals[i] = Vector3.up;
                uvs[i] = new Vector2(fx, fz);
            }
        }

        int t = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = z * vertsPerSide + x;
                triangles[t++] = i;
                triangles[t++] = i + vertsPerSide;
                triangles[t++] = i + 1;
                triangles[t++] = i + 1;
                triangles[t++] = i + vertsPerSide;
                triangles[t++] = i + vertsPerSide + 1;
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void LookAt(Transform t, Vector3 target)
    {
        Vector3 dir = target - t.position;
        if (dir.sqrMagnitude > 0.0001f)
            t.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}
#endif
