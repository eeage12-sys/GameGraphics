using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

public static class DAY14_PortfolioIntegrationV3
{
    const string ROOT = "Assets/DAY14";
    const string SHADERS = ROOT + "/Shaders";
    const string MATERIALS = ROOT + "/Materials";
    const string SETTINGS = ROOT + "/Settings";
    const string SCENES = ROOT + "/Scenes";
    const string DOCS = ROOT + "/Documentation";

    const string PORTFOLIO_GRAPH = SHADERS + "/SG_Portfolio_Final.shadergraph";
    const string PORTFOLIO_MATERIAL = MATERIALS + "/Mat_Portfolio_Final.mat";
    const string VOLUME_PROFILE = SETTINGS + "/VP_DAY14_Final.asset";
    const string FINAL_SCENE = SCENES + "/GraphicsPortfolio.unity";

    const string DAY12_VFX = "Assets/DAY12/VFX/VFX_GpuSpark.vfx";

    [MenuItem("Tools/DAY14 FINAL/1 - Build From Existing Work v3")]
    public static void Build()
    {
        try
        {
            EnsureFolders();

            string graphicsLab = FindGraphicsLabScene();
            if (string.IsNullOrEmpty(graphicsLab))
                throw new Exception(
                    "GraphicsLab 씬을 찾지 못했습니다.\n" +
                    "DAY14 원문은 기존 GraphicsLab을 GraphicsPortfolio로 Save As 해서 통합합니다.\n" +
                    "기존 GraphicsLab 씬이 남아 있는지 확인하세요.");

            string shaderGraph = FindBestExistingShaderGraph();
            if (string.IsNullOrEmpty(shaderGraph))
                throw new Exception(
                    "DAY05~DAY08에서 사용할 Shader Graph를 찾지 못했습니다.");

            Shader sourceShader = LoadShaderFromGraph(shaderGraph);
            if (sourceShader == null)
                throw new Exception(
                    "선택한 Shader Graph의 Shader를 불러오지 못했습니다:\n" + shaderGraph);

            Material sourceMaterial = FindMaterialUsingShader(sourceShader);

            GameObject hitPrefab = FindPrefabExact("FX_HitSpark", "Assets/DAY10");
            GameObject healPrefab = FindPrefabExact("FX_HealGlow", "Assets/DAY10");
            GameObject explosionPrefab = FindPrefabExact("FX_ExplosionSmall", "Assets/DAY10");

            if (hitPrefab == null)
                throw new Exception("DAY10 FX_HitSpark Prefab을 찾지 못했습니다.");

            if (healPrefab == null && explosionPrefab == null)
                throw new Exception(
                    "DAY10 Particle Prefab 2번째 항목을 찾지 못했습니다.\n" +
                    "FX_HealGlow 또는 FX_ExplosionSmall이 필요합니다.");

            VisualEffectAsset vfxAsset =
                AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(DAY12_VFX);

            if (vfxAsset == null)
                throw new Exception(
                    "DAY12 VFX_GpuSpark.vfx를 찾지 못했습니다.");

            InputActionAsset inputAsset = FindInputAssetWithRequiredActions();
            if (inputAsset == null)
                throw new Exception(
                    "Point / Click / LowIntensity / HighIntensity를 모두 가진 Input Actions Asset을 찾지 못했습니다.\n" +
                    "DAY13 Input Actions를 확인하세요.");

            // PlayerInput은 Editor에서 actions 프로퍼티를 통해 넣을 때
            // 내부 복제/재생성 로직을 수행할 수 있으므로 이름/경로를 먼저 보관합니다.
            string inputAssetName = inputAsset.name;
            string inputAssetPath = AssetDatabase.GetAssetPath(inputAsset);

            Type clickSpawnerType = FindTypeByName("ClickEffectSpawner");
            Type intensityType = FindTypeByName("VfxIntensityController");

            if (clickSpawnerType == null)
                throw new Exception("DAY11 ClickEffectSpawner.cs를 찾지 못했습니다.");

            if (intensityType == null)
                throw new Exception("DAY13 VfxIntensityController.cs를 찾지 못했습니다.");

            CreatePortfolioGraphAndMaterial(
                shaderGraph,
                sourceShader,
                sourceMaterial);

            Material portfolioMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(PORTFOLIO_MATERIAL);

            if (portfolioMaterial == null)
                throw new Exception("DAY14 Portfolio Material 생성 실패");

            // DAY14 원문대로 원본 씬은 보존하고 복제본에서만 작업.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FINAL_SCENE) != null)
                AssetDatabase.DeleteAsset(FINAL_SCENE);

            if (!AssetDatabase.CopyAsset(graphicsLab, FINAL_SCENE))
                throw new Exception("GraphicsLab -> GraphicsPortfolio 씬 복제 실패");

            AssetDatabase.ImportAsset(
                FINAL_SCENE,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            Scene scene = EditorSceneManager.OpenScene(
                FINAL_SCENE,
                OpenSceneMode.Single);

            // 복제된 기존 씬의 원래 루트들을 먼저 보존.
            GameObject[] originalRoots = scene.GetRootGameObjects();

            GameObject environment = new GameObject("Environment");
            GameObject shaderTargets = new GameObject("ShaderTargets");
            GameObject particleEffects = new GameObject("ParticleEffects");
            GameObject vfxEffects = new GameObject("VfxEffects");
            GameObject effectInput = new GameObject("EffectInput");

            foreach (GameObject root in originalRoots)
            {
                if (root == null)
                    continue;

                root.transform.SetParent(environment.transform, true);
            }

            Camera mainCamera = FindExistingCamera();
            if (mainCamera == null)
                throw new Exception(
                    "GraphicsLab 복제 씬에서 Camera를 찾지 못했습니다.");

            EnsureMainCameraTag(mainCamera);

            EnsureVolume(environment.transform);

            GameObject ground = FindExistingGround();
            if (ground == null)
                throw new Exception(
                    "GraphicsLab 복제 씬에서 바닥 Plane/Floor/Ground Collider를 찾지 못했습니다.\n" +
                    "새 바닥을 임의로 만들지 않고 기존 씬을 사용하도록 설계했기 때문에 여기서 중단합니다.");

            int groundLayer = EnsureLayer("Ground");
            ground.layer = groundLayer;

            Renderer shaderTarget = FindExistingShaderTarget(ground);
            if (shaderTarget == null)
                throw new Exception(
                    "GraphicsLab 안에서 Shader Material을 적용할 기존 Mesh Renderer를 찾지 못했습니다.");

            shaderTarget.transform.SetParent(shaderTargets.transform, true);
            ApplyMaterialToFirstSlot(shaderTarget, portfolioMaterial);

            Vector3 anchor = shaderTarget.bounds.center;
            float spacing = Mathf.Max(
                1.2f,
                Mathf.Max(shaderTarget.bounds.extents.x, shaderTarget.bounds.extents.z) * 2.2f);

            // Particle System 2종: 기존 Prefab을 그대로 인스턴스화.
            GameObject hitInstance = InstantiatePrefab(
                hitPrefab,
                particleEffects.transform,
                "FX_HitSpark_Portfolio",
                anchor + new Vector3(-spacing, 0.25f, 0f));

            GameObject secondPrefab = healPrefab != null ? healPrefab : explosionPrefab;
            GameObject secondInstance = InstantiatePrefab(
                secondPrefab,
                particleEffects.transform,
                secondPrefab.name + "_Portfolio",
                anchor + new Vector3(spacing, 0.25f, 0f));

            // 원본 Prefab 설정은 건드리지 않는다.
            // 클릭 이벤트로 HitSpark가 따로 생성되므로 scene instance는 비교/검증용.
            ParticleSystem hitPS = GetParticleSystem(hitPrefab);

            GameObject vfxGO = new GameObject("VFX_GpuSpark");
            vfxGO.transform.SetParent(vfxEffects.transform);
            vfxGO.transform.position =
                anchor + new Vector3(0f, 0.25f, spacing * 0.9f);

            VisualEffect visualEffect = vfxGO.AddComponent<VisualEffect>();
            visualEffect.visualEffectAsset = vfxAsset;
            visualEffect.playRate = 1f;

            if (visualEffect.HasFloat("SpawnRate"))
                visualEffect.SetFloat("SpawnRate", 80f);

            PlayerInput playerInput = effectInput.AddComponent<PlayerInput>();

            // IMPORTANT (Unity 6 / Input System):
            // In Edit Mode, assigning PlayerInput.actions through the public property
            // can trigger PlayerInput's runtime copy lifecycle. That can leave the
            // imported InputActionAsset reference destroyed and cause MissingReferenceException.
            // Write the serialized editor fields directly instead.
            SerializedObject playerInputSO = new SerializedObject(playerInput);
            SerializedProperty actionsProp = playerInputSO.FindProperty("m_Actions");
            SerializedProperty defaultMapProp = playerInputSO.FindProperty("m_DefaultActionMap");
            SerializedProperty behaviorProp = playerInputSO.FindProperty("m_NotificationBehavior");

            if (actionsProp == null || defaultMapProp == null || behaviorProp == null)
                throw new Exception(
                    "현재 Input System 버전의 PlayerInput 직렬화 필드를 찾지 못했습니다.");

            // Reload a fresh AssetDatabase reference immediately before serialization.
            InputActionAsset freshInputAsset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputAssetPath);

            if (freshInputAsset == null)
                throw new Exception("Input Actions Asset을 다시 불러오지 못했습니다.");

            actionsProp.objectReferenceValue = freshInputAsset;
            defaultMapProp.stringValue = "Gameplay";
            behaviorProp.enumValueIndex = (int)PlayerNotifications.SendMessages;
            playerInputSO.ApplyModifiedPropertiesWithoutUndo();

            Component clickSpawner = effectInput.AddComponent(clickSpawnerType);
            WireClickSpawner(
                clickSpawner,
                mainCamera,
                hitPS,
                groundLayer);

            Component intensityController =
                effectInput.AddComponent(intensityType);

            WireIntensityController(
                intensityController,
                visualEffect);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, FINAL_SCENE);

            WriteDocumentation(
                graphicsLab,
                shaderGraph,
                shaderTarget.gameObject.name,
                hitPrefab.name,
                secondPrefab.name,
                ground.name,
                inputAssetName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(FINAL_SCENE);

            string report = ValidateInternal();
            Debug.Log("========== DAY14 FINAL VALIDATION ==========\n" + report);

            EditorUtility.DisplayDialog(
                "DAY14 최종 통합 완료",
                "이번 버전은 새 훈련장/더미를 임의로 만들지 않았습니다.\n\n" +
                "기존 GraphicsLab 씬을 복제하고,\n" +
                "DAY05~08 Shader Graph / DAY10 Particle / DAY11 코드 / DAY12 VFX / DAY13 제어를\n" +
                "그 씬 안에 통합했습니다.\n\n" +
                "생성 씬:\n" + FINAL_SCENE + "\n\n" +
                "Play 확인:\n" +
                "• Shader 대상 표현\n" +
                "• Particle 2종\n" +
                "• Ground 좌클릭 -> HitSpark\n" +
                "• 숫자 1 -> SpawnRate 20\n" +
                "• 숫자 2 -> SpawnRate 200\n\n" +
                "마지막으로 Tools > DAY14 FINAL > 2 - Validate 를 누르세요.",
                "확인");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog(
                "DAY14 생성 오류",
                e.Message,
                "확인");
        }
    }

    [MenuItem("Tools/DAY14 FINAL/2 - Validate")]
    public static void Validate()
    {
        string report = ValidateInternal();

        Debug.Log(
            "========== DAY14 FINAL VALIDATION ==========\n" +
            report);

        EditorUtility.DisplayDialog(
            report.Contains("FAIL")
                ? "DAY14 확인 필요"
                : "DAY14 검증 통과",
            report,
            "확인");
    }

    [MenuItem("Tools/DAY14 FINAL/3 - Select Final Scene")]
    public static void SelectFinalScene()
    {
        UnityEngine.Object scene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(FINAL_SCENE);

        if (scene != null)
        {
            Selection.activeObject = scene;
            EditorGUIUtility.PingObject(scene);
        }
    }

    static void EnsureFolders()
    {
        EnsureFolder(ROOT);
        EnsureFolder(SHADERS);
        EnsureFolder(MATERIALS);
        EnsureFolder(SETTINGS);
        EnsureFolder(SCENES);
        EnsureFolder(DOCS);
    }

    static string FindGraphicsLabScene()
    {
        string[] guids =
            AssetDatabase.FindAssets("GraphicsLab t:Scene");

        List<string> paths = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p =>
                Path.GetFileNameWithoutExtension(p)
                    .Equals("GraphicsLab", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Length)
            .ToList();

        return paths.FirstOrDefault() ?? "";
    }

    static string FindBestExistingShaderGraph()
    {
        string[] preferred =
        {
            "SG_Shield",
            "SG_ToonRim",
            "SG_ColorPulse",
            "SG_ToonBand",
            "SG_OutlineShell"
        };

        List<string> candidates = new List<string>();

        for (int day = 4; day <= 8; day++)
        {
            string root = "Assets/DAY0" + day;
            if (!Directory.Exists(root))
                continue;

            candidates.AddRange(
                Directory.GetFiles(
                    root,
                    "*.shadergraph",
                    SearchOption.AllDirectories)
                .Select(p => p.Replace("\\", "/")));
        }

        if (candidates.Count == 0)
            return "";

        foreach (string name in preferred)
        {
            string match = candidates.FirstOrDefault(p =>
                Path.GetFileNameWithoutExtension(p)
                    .Equals(name, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(match))
                return match;
        }

        return candidates[0];
    }

    static Shader LoadShaderFromGraph(string path)
    {
        Shader shader =
            AssetDatabase.LoadAssetAtPath<Shader>(path);

        if (shader != null)
            return shader;

        return AssetDatabase
            .LoadAllAssetsAtPath(path)
            .OfType<Shader>()
            .FirstOrDefault();
    }

    static Material FindMaterialUsingShader(Shader shader)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[]
                {
                    "Assets/DAY04",
                    "Assets/DAY05",
                    "Assets/DAY06",
                    "Assets/DAY07",
                    "Assets/DAY08"
                });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.shader == shader)
                return mat;
        }

        return null;
    }

    static void CreatePortfolioGraphAndMaterial(
        string sourceGraph,
        Shader sourceShader,
        Material sourceMaterial)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PORTFOLIO_GRAPH) != null)
            AssetDatabase.DeleteAsset(PORTFOLIO_GRAPH);

        if (!AssetDatabase.CopyAsset(sourceGraph, PORTFOLIO_GRAPH))
            throw new Exception("Portfolio Shader Graph 복제 실패");

        AssetDatabase.ImportAsset(
            PORTFOLIO_GRAPH,
            ImportAssetOptions.ForceSynchronousImport |
            ImportAssetOptions.ForceUpdate);

        Shader copiedShader = LoadShaderFromGraph(PORTFOLIO_GRAPH);
        if (copiedShader == null)
            copiedShader = sourceShader;

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PORTFOLIO_MATERIAL) != null)
            AssetDatabase.DeleteAsset(PORTFOLIO_MATERIAL);

        Material portfolio =
            sourceMaterial != null
                ? new Material(sourceMaterial)
                : new Material(copiedShader);

        portfolio.name = "Mat_Portfolio_Final";
        portfolio.shader = copiedShader;

        AssetDatabase.CreateAsset(
            portfolio,
            PORTFOLIO_MATERIAL);

        EditorUtility.SetDirty(portfolio);
    }

    static Camera FindExistingCamera()
    {
        Camera main = Camera.main;
        if (main != null)
            return main;

        Camera[] cameras =
            UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        return cameras.FirstOrDefault();
    }

    static void EnsureMainCameraTag(Camera cam)
    {
        if (cam != null && cam.gameObject.tag != "MainCamera")
            cam.gameObject.tag = "MainCamera";
    }

    static void EnsureVolume(UnityEngine.Transform environment)
    {
        Volume[] volumes =
            UnityEngine.Object.FindObjectsByType<Volume>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        if (volumes.Length > 0)
            return;

        GameObject go = new GameObject("Global Volume");
        go.transform.SetParent(environment);

        Volume volume = go.AddComponent<Volume>();
        volume.isGlobal = true;

        VolumeProfile profile =
            AssetDatabase.LoadAssetAtPath<VolumeProfile>(VOLUME_PROFILE);

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VOLUME_PROFILE);
        }

        volume.sharedProfile = profile;
    }

    static GameObject FindExistingGround()
    {
        Collider[] colliders =
            UnityEngine.Object.FindObjectsByType<Collider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        string[] nameHints =
        {
            "ground",
            "floor",
            "plane"
        };

        foreach (string hint in nameHints)
        {
            Collider match = colliders.FirstOrDefault(c =>
                c != null &&
                c.gameObject.activeInHierarchy &&
                c.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0);

            if (match != null)
                return match.gameObject;
        }

        // 이름이 달라도 넓고 납작한 기존 Collider를 바닥 후보로 사용.
        Collider flat = colliders
            .Where(c => c != null && c.gameObject.activeInHierarchy)
            .OrderByDescending(c =>
                c.bounds.size.x * c.bounds.size.z)
            .FirstOrDefault(c =>
                c.bounds.size.y <=
                Mathf.Max(0.5f, Mathf.Min(c.bounds.size.x, c.bounds.size.z) * 0.25f));

        return flat != null ? flat.gameObject : null;
    }

    static Renderer FindExistingShaderTarget(GameObject ground)
    {
        Renderer[] renderers =
            UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        string[] preferred =
        {
            "sphere",
            "capsule",
            "cube"
        };

        foreach (string hint in preferred)
        {
            Renderer match = renderers.FirstOrDefault(r =>
                r != null &&
                r.gameObject.activeInHierarchy &&
                r.gameObject != ground &&
                r.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0);

            if (match != null)
                return match;
        }

        return renderers.FirstOrDefault(r =>
            r != null &&
            r.gameObject.activeInHierarchy &&
            r.gameObject != ground);
    }

    static void ApplyMaterialToFirstSlot(
        Renderer renderer,
        Material material)
    {
        Material[] mats = renderer.sharedMaterials;

        if (mats == null || mats.Length == 0)
        {
            renderer.sharedMaterial = material;
            return;
        }

        mats[0] = material;
        renderer.sharedMaterials = mats;
    }

    static GameObject InstantiatePrefab(
        GameObject prefab,
        UnityEngine.Transform parent,
        string name,
        Vector3 position)
    {
        GameObject go =
            PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (go == null)
            throw new Exception(prefab.name + " Prefab 인스턴스 생성 실패");

        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = Vector3.one;

        return go;
    }

    static ParticleSystem GetParticleSystem(GameObject prefab)
    {
        ParticleSystem ps =
            prefab.GetComponent<ParticleSystem>() ??
            prefab.GetComponentInChildren<ParticleSystem>(true);

        if (ps == null)
            throw new Exception(
                prefab.name + " 안에 ParticleSystem이 없습니다.");

        return ps;
    }

    static GameObject FindPrefabExact(
        string name,
        string root)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                name + " t:Prefab",
                new[] { root });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null &&
                prefab.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return prefab;
        }

        return null;
    }

    static InputActionAsset FindInputAssetWithRequiredActions()
    {
        string[] guids =
            AssetDatabase.FindAssets("t:InputActionAsset");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            InputActionAsset asset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);

            if (asset == null)
                continue;

            InputActionMap map =
                asset.FindActionMap("Gameplay", false);

            if (map == null)
                continue;

            bool ok =
                map.FindAction("Point", false) != null &&
                map.FindAction("Click", false) != null &&
                map.FindAction("LowIntensity", false) != null &&
                map.FindAction("HighIntensity", false) != null;

            if (ok)
                return asset;
        }

        return null;
    }

    static void WireClickSpawner(
        Component component,
        Camera camera,
        ParticleSystem hitPrefab,
        int groundLayer)
    {
        SerializedObject so = new SerializedObject(component);

        SerializedProperty targetCamera = so.FindProperty("targetCamera");
        SerializedProperty effectPrefab = so.FindProperty("effectPrefab");
        SerializedProperty groundMask = so.FindProperty("groundMask");

        if (targetCamera == null ||
            effectPrefab == null ||
            groundMask == null)
        {
            throw new Exception(
                "DAY11 ClickEffectSpawner 필드 구조가 수업 코드와 다릅니다.");
        }

        targetCamera.objectReferenceValue = camera;
        effectPrefab.objectReferenceValue = hitPrefab;
        groundMask.intValue = 1 << groundLayer;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void WireIntensityController(
        Component component,
        VisualEffect visualEffect)
    {
        SerializedObject so = new SerializedObject(component);

        SetObjectRef(so, "visualEffect", visualEffect);
        SetString(so, "spawnRateName", "SpawnRate");
        SetFloat(so, "defaultRate", 80f);
        SetFloat(so, "lowRate", 20f);
        SetFloat(so, "highRate", 200f);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetObjectRef(
        SerializedObject so,
        string name,
        UnityEngine.Object value)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p != null)
            p.objectReferenceValue = value;
    }

    static void SetString(
        SerializedObject so,
        string name,
        string value)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p != null)
            p.stringValue = value;
    }

    static void SetFloat(
        SerializedObject so,
        string name,
        float value)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p != null)
            p.floatValue = value;
    }

    static string ValidateInternal()
    {
        List<string> lines = new List<string>();

        lines.Add(
            GraphicsSettings.currentRenderPipeline != null
                ? "PASS URP/Scriptable Render Pipeline active"
                : "FAIL Render Pipeline Asset 없음");

        SceneAsset scene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(FINAL_SCENE);

        lines.Add(
            scene != null
                ? "PASS GraphicsPortfolio Scene"
                : "FAIL GraphicsPortfolio Scene");

        UnityEngine.Object graph =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PORTFOLIO_GRAPH);

        lines.Add(
            graph != null
                ? "PASS Portfolio Shader Graph copy"
                : "FAIL Portfolio Shader Graph copy");

        Material mat =
            AssetDatabase.LoadAssetAtPath<Material>(PORTFOLIO_MATERIAL);

        lines.Add(
            mat != null
                ? "PASS Portfolio Material"
                : "FAIL Portfolio Material");

        VisualEffectAsset vfxAsset =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(DAY12_VFX);

        lines.Add(
            vfxAsset != null
                ? "PASS DAY12 VFX_GpuSpark"
                : "FAIL DAY12 VFX_GpuSpark");

        InputActionAsset input = null;

        if (SceneManager.GetActiveScene().path == FINAL_SCENE)
        {
            GameObject effectInputForAsset = GameObject.Find("EffectInput");
            PlayerInput existingPI =
                effectInputForAsset != null
                    ? effectInputForAsset.GetComponent<PlayerInput>()
                    : null;

            if (existingPI != null)
            {
                SerializedObject piSO = new SerializedObject(existingPI);
                SerializedProperty a = piSO.FindProperty("m_Actions");
                input = a != null
                    ? a.objectReferenceValue as InputActionAsset
                    : null;
            }
        }

        if (input == null)
            input = FindInputAssetWithRequiredActions();

        lines.Add(
            input != null
                ? "PASS Input Actions Point/Click/Low/High"
                : "FAIL Input Actions");

        if (SceneManager.GetActiveScene().path == FINAL_SCENE)
        {
            string[] groups =
            {
                "Environment",
                "ShaderTargets",
                "ParticleEffects",
                "VfxEffects",
                "EffectInput"
            };

            foreach (string group in groups)
            {
                lines.Add(
                    GameObject.Find(group) != null
                        ? "PASS Hierarchy " + group
                        : "FAIL Hierarchy " + group);
            }

            Renderer[] shaderRenderers =
                GameObject.Find("ShaderTargets")
                    ?.GetComponentsInChildren<Renderer>(true);

            lines.Add(
                shaderRenderers != null &&
                shaderRenderers.Any(r =>
                    r.sharedMaterials.Contains(mat))
                    ? "PASS Shader Material assigned to existing scene mesh"
                    : "FAIL Shader Material assignment");

            ParticleSystem[] particles =
                GameObject.Find("ParticleEffects")
                    ?.GetComponentsInChildren<ParticleSystem>(true);

            lines.Add(
                particles != null &&
                particles.Length >= 2
                    ? "PASS Particle System 2+"
                    : "FAIL Particle System 2+");

            VisualEffect vfx =
                GameObject.Find("VFX_GpuSpark")
                    ?.GetComponent<VisualEffect>();

            lines.Add(
                vfx != null &&
                vfx.visualEffectAsset == vfxAsset
                    ? "PASS Visual Effect Graph instance"
                    : "FAIL Visual Effect Graph instance");

            if (vfx != null)
            {
                lines.Add(
                    vfx.HasFloat("SpawnRate")
                        ? "PASS SpawnRate Exposed"
                        : "FAIL SpawnRate Exposed");
            }

            GameObject inputGO =
                GameObject.Find("EffectInput");

            PlayerInput pi =
                inputGO != null
                    ? inputGO.GetComponent<PlayerInput>()
                    : null;

            bool playerInputOK = false;

            if (pi != null)
            {
                SerializedObject piSO = new SerializedObject(pi);
                SerializedProperty actionsP = piSO.FindProperty("m_Actions");
                SerializedProperty mapP = piSO.FindProperty("m_DefaultActionMap");
                SerializedProperty behaviorP = piSO.FindProperty("m_NotificationBehavior");

                playerInputOK =
                    actionsP != null &&
                    actionsP.objectReferenceValue != null &&
                    mapP != null &&
                    mapP.stringValue == "Gameplay" &&
                    behaviorP != null &&
                    behaviorP.enumValueIndex == (int)PlayerNotifications.SendMessages;
            }

            lines.Add(
                playerInputOK
                    ? "PASS EffectInput / PlayerInput Send Messages"
                    : "FAIL PlayerInput");

            Type clickType = FindTypeByName("ClickEffectSpawner");
            Type intensityType = FindTypeByName("VfxIntensityController");

            lines.Add(
                inputGO != null &&
                clickType != null &&
                inputGO.GetComponent(clickType) != null
                    ? "PASS Ground Click -> HitSpark code"
                    : "FAIL ClickEffectSpawner");

            lines.Add(
                inputGO != null &&
                intensityType != null &&
                inputGO.GetComponent(intensityType) != null
                    ? "PASS Key 1/2 -> SpawnRate code"
                    : "FAIL VfxIntensityController");

            Camera camera = FindExistingCamera();
            lines.Add(
                camera != null
                    ? "PASS Main Camera"
                    : "FAIL Main Camera");

            Volume[] volumes =
                UnityEngine.Object.FindObjectsByType<Volume>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            lines.Add(
                volumes.Length > 0
                    ? "PASS Volume"
                    : "FAIL Volume");
        }

        lines.Add(
            File.Exists(DOCS + "/DAY14_Verification_Record.md") &&
            File.Exists(DOCS + "/DAY14_Spatial_Placement.md")
                ? "PASS Verification / Spatial documentation"
                : "FAIL Documentation");

        return string.Join("\n", lines);
    }

    static void WriteDocumentation(
        string sourceScene,
        string sourceGraph,
        string targetName,
        string hitPrefab,
        string secondPrefab,
        string groundName,
        string inputName)
    {
        string verification =
@"# DAY14 최종 통합 검증 기록

## 통합 원칙
- 새 프로토타입 맵을 만들지 않고 기존 `GraphicsLab`을 복제해 `GraphicsPortfolio`로 사용.
- DAY05~08 Shader Graph 원본은 보존하고 포트폴리오용 복제본만 사용.
- DAY10 Particle Prefab, DAY11 Click 코드, DAY12 VFX Graph, DAY13 SpawnRate 제어를 그대로 재사용.

## 출처
- Base Scene: `" + sourceScene + @"`
- Source Shader Graph: `" + sourceGraph + @"`
- Portfolio Shader Graph: `SG_Portfolio_Final`
- Portfolio Material: `Mat_Portfolio_Final`
- Shader Target: `" + targetName + @"`
- Particle 1: `" + hitPrefab + @"`
- Particle 2: `" + secondPrefab + @"`
- VFX Graph: `VFX_GpuSpark`
- Ground: `" + groundName + @"`
- Input Actions: `" + inputName + @"`

## 최종 요구사항
| 항목 | 구현 |
|---|---|
| Shader Graph 1개 이상 | Portfolio Graph 복제본 |
| 표면 표현 | 원본 Graph의 Fresnel/Emission/기타 기존 연결을 보존 |
| Particle System 2개 이상 | DAY10 Prefab 2종 |
| VFX Graph 1개 이상 | DAY12 VFX_GpuSpark |
| 코드 연동 | Ground 클릭 -> FX_HitSpark |
| 상태 제어 | 1 -> SpawnRate 20 / 2 -> 200 |
| 공간 배치 | `DAY14_Spatial_Placement.md` |
| 플레이 규칙 | 한 번의 Ground Click -> HitSpark 한 번 |
| 검증 | Inspector / Graph / Play Mode |

## Play Mode 체크
1. ShaderTargets의 기존 Mesh에 Portfolio Material이 적용되어 보이는지 확인.
2. ParticleEffects 아래 DAY10 Prefab 2종 확인.
3. Ground를 좌클릭했을 때 클릭 위치에 HitSpark가 한 번 생성되는지 확인.
4. `1`을 눌렀을 때 VFX 밀도가 낮아지는지 확인.
5. `2`를 눌렀을 때 VFX 밀도가 높아지는지 확인.
6. VFX Graph의 Spawn / Initialize / Update / Output 연결 확인.
7. Console에 빨간 Error가 없는지 확인.

## 성능 설명
- 프레임 저하: SpawnRate 우선 감소.
- 너무 오래 남음: Lifetime 감소.
- 화면 밖 계산: Bounds 확인.
- 너무 밝음: Output Color / Alpha 조절.
- 저사양: VFX 강도 감소 또는 비활성화.
";

        File.WriteAllText(
            DOCS + "/DAY14_Verification_Record.md",
            verification);

        string spatial =
@"# DAY14 공간 배치 분석

## FX_HitSpark
| 항목 | 기록 |
|---|---|
| 발생 위치 | 기존 GraphicsPortfolio Ground의 Raycast Hit Point |
| 방향 | DAY10 Prefab의 기존 Shape/방향 사용 |
| 크기 | Prefab 원본 설정, Scene Instance Scale `(1,1,1)` |
| 카메라 거리 | 기존 GraphicsLab Camera 기준 |
| 지속 시간 | DAY10 HitSpark 원본 Lifetime/Duration |
| 게임 규칙 | Ground 클릭이 실제 확정된 한 번에만 한 번 생성 |

## 두 번째 Particle Prefab
- 기존 DAY10 Prefab을 그대로 배치.
- 원본의 Looping / Lifetime / Renderer Material을 변경하지 않음.
- 기존 씬 카메라에서 역할을 비교할 수 있는 위치에 배치.

## VFX_GpuSpark
| 항목 | 기록 |
|---|---|
| 발생 위치 | 기존 Shader Target 근처 |
| 방향 | DAY12에서 설정한 위로 솟고 중력으로 떨어지는 흐름 |
| 크기 | DAY12 Size Random 설정 |
| 지속 시간 | DAY12 Lifetime Random |
| 강도 | SpawnRate 80 기본, DAY13 키 입력으로 20 / 200 |
| 성능 | SpawnRate / Lifetime / Bounds 우선 점검 |

## Shader Target
- 기존 GraphicsLab의 Mesh를 재사용.
- 새 Sphere/Capsule 훈련장을 임의로 만들지 않음.
- Portfolio Material만 첫 번째 Material Slot에 적용.
";

        File.WriteAllText(
            DOCS + "/DAY14_Spatial_Placement.md",
            spatial);

        AssetDatabase.ImportAsset(
            DOCS + "/DAY14_Verification_Record.md",
            ImportAssetOptions.ForceUpdate);

        AssetDatabase.ImportAsset(
            DOCS + "/DAY14_Spatial_Placement.md",
            ImportAssetOptions.ForceUpdate);
    }

    static Type FindTypeByName(string typeName)
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type type = asm.GetTypes()
                    .FirstOrDefault(t =>
                        t != null &&
                        t.Name == typeName);

                if (type != null)
                    return type;
            }
            catch (ReflectionTypeLoadException e)
            {
                Type type = e.Types
                    .Where(t => t != null)
                    .FirstOrDefault(t =>
                        t.Name == typeName);

                if (type != null)
                    return type;
            }
        }

        return null;
    }

    static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0)
            return existing;

        UnityEngine.Object tagManagerAsset =
            AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset")
            .FirstOrDefault();

        if (tagManagerAsset == null)
            throw new Exception("TagManager.asset 없음");

        SerializedObject tagManager =
            new SerializedObject(tagManagerAsset);

        SerializedProperty layers =
            tagManager.FindProperty("layers");

        for (int i = 8; i < 32; i++)
        {
            SerializedProperty layer =
                layers.GetArrayElementAtIndex(i);

            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = name;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        throw new Exception("Ground Layer를 만들 빈 Layer Slot이 없습니다.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent =
            Path.GetDirectoryName(path).Replace("\\", "/");

        string name = Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }
}
