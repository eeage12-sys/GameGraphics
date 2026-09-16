#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

public static class PortfolioGraphicsBuilder
{
    private const string Root = "Assets/PortfolioGraphics";
    private const string ShaderFolder = Root + "/ShaderGraphs";
    private const string MaterialFolder = Root + "/Materials";
    private const string PrefabFolder = Root + "/Prefabs";
    private const string TextureFolder = Root + "/Textures";
    private const string SceneFolder = Root + "/Scenes";
    private const string SettingsFolder = Root + "/Settings";
    private const string DocumentationFolder = Root + "/Documentation";

    private const string ScenePath = SceneFolder + "/GraphicsPortfolio_MagicTrainingGround.unity";
    private const string ShaderGraphPath = ShaderFolder + "/SG_MagicShield.shadergraph";
    private const string VfxPath = "Assets/DAY12/VFX/VFX_GpuSpark.vfx";
    private const string VolumeProfilePath = SettingsFolder + "/VP_MagicTraining.asset";
    private const string SoftParticleTexturePath = TextureFolder + "/T_SoftParticle.asset";

    private const string ShieldMaterialPath = MaterialFolder + "/Mat_MagicShield.mat";
    private const string FloorMaterialPath = MaterialFolder + "/Mat_Floor.mat";
    private const string StoneMaterialPath = MaterialFolder + "/Mat_Stone.mat";
    private const string AccentMaterialPath = MaterialFolder + "/Mat_Accent.mat";
    private const string DarkMaterialPath = MaterialFolder + "/Mat_DummyDark.mat";
    private const string MattePbrMaterialPath = MaterialFolder + "/Mat_PBR_MatteStone.mat";
    private const string MetalPbrMaterialPath = MaterialFolder + "/Mat_PBR_ArcaneMetal.mat";
    private const string HitParticleMaterialPath = MaterialFolder + "/Mat_FX_HitSpark.mat";
    private const string HealParticleMaterialPath = MaterialFolder + "/Mat_FX_HealGlow.mat";

    private const string HitPrefabPath = PrefabFolder + "/FX_HitSpark.prefab";
    private const string HealPrefabPath = PrefabFolder + "/FX_HealGlow.prefab";

    [MenuItem("Tools/Game Graphics Portfolio/1 Build Complete Scene")]
    public static void BuildCompleteScene()
    {
        try
        {
            EnsureFolders();
            AssetDatabase.ImportAsset(
                ShaderGraphPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            Shader shieldShader = LoadShaderFromGraph(ShaderGraphPath);
            if (shieldShader == null)
                throw new Exception("SG_MagicShield.shadergraph를 Shader로 불러오지 못했습니다. Unity 임포트가 끝난 뒤 다시 실행하세요.");

            Shader litShader = FindShader(
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Standard");

            if (litShader == null)
                throw new Exception("URP Lit Shader를 찾지 못했습니다. URP 프로젝트인지 확인하세요.");

            Shader particleShader = FindShader(
                "Universal Render Pipeline/Particles/Unlit",
                "Sprites/Default",
                "Legacy Shaders/Particles/Alpha Blended");

            if (particleShader == null)
                throw new Exception("Particle용 Shader를 찾지 못했습니다.");

            Texture2D softTexture = CreateSoftParticleTexture();
            CreateMaterials(shieldShader, litShader, particleShader, softTexture);
            CreateParticlePrefabs();

            VisualEffectAsset vfxAsset = GetOrCreateVfxAsset();
            if (vfxAsset == null)
            {
                throw new Exception(
                    "VFX_GpuSpark.vfx 생성에 실패했습니다.\n" +
                    "Package Manager에서 Visual Effect Graph를 설치한 뒤\n" +
                    "Tools > DAY12 FINAL > Rebuild Complete를 먼저 실행하세요.");
            }

            Material floorMat = LoadMaterial(FloorMaterialPath);
            Material stoneMat = LoadMaterial(StoneMaterialPath);
            Material accentMat = LoadMaterial(AccentMaterialPath);
            Material darkMat = LoadMaterial(DarkMaterialPath);
            Material shieldMat = LoadMaterial(ShieldMaterialPath);
            Material mattePbrMat = LoadMaterial(MattePbrMaterialPath);
            Material metalPbrMat = LoadMaterial(MetalPbrMaterialPath);
            GameObject hitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HitPrefabPath);
            GameObject healPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealPrefabPath);

            if (new[] { floorMat, stoneMat, accentMat, darkMat, shieldMat, mattePbrMat, metalPbrMat }.Any(x => x == null) ||
                hitPrefab == null || healPrefab == null)
            {
                throw new Exception("Material 또는 Particle Prefab 생성에 실패했습니다.");
            }

            int impactLayer = EnsureLayer("PortfolioImpact");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = new GameObject("GraphicsPortfolio_MagicTrainingGround");
            Transform environment = NewParent("Environment", root.transform);
            Transform shaderTargets = NewParent("ShaderTargets", root.transform);
            Transform pbrComparison = NewParent("PBRComparison", root.transform);
            Transform particleEffects = NewParent("ParticleEffects", root.transform);
            Transform vfxEffects = NewParent("VFXEffects", root.transform);
            Transform gameplayLink = NewParent("GameplayLink", root.transform);
            Transform verification = NewParent("Verification", root.transform);

            Camera camera = CreateCamera(environment);
            CreateDirectionalLight(environment);
            CreateGlobalVolume(environment);
            BuildEnvironment(environment, floorMat, stoneMat, accentMat);
            BuildShieldTarget(shaderTargets, stoneMat, accentMat, darkMat, shieldMat);
            BuildPbrComparison(pbrComparison, stoneMat, mattePbrMat, metalPbrMat);

            BuildHealingZone(particleEffects, stoneMat, accentMat, healPrefab);
            BuildImpactZone(particleEffects, stoneMat, accentMat, impactLayer);

            VisualEffect visualEffect = BuildManaFountain(
                vfxEffects,
                stoneMat,
                accentMat,
                vfxAsset);

            ParticleSystem hitParticle = hitPrefab.GetComponent<ParticleSystem>() ??
                                         hitPrefab.GetComponentInChildren<ParticleSystem>(true);

            PortfolioEffectController controller = gameplayLink.gameObject.AddComponent<PortfolioEffectController>();
            controller.Configure(camera, 1 << impactLayer, hitParticle, visualEffect);

            PortfolioHUD hud = gameplayLink.gameObject.AddComponent<PortfolioHUD>();
            hud.Configure(controller);

            BuildVerificationHierarchy(verification);
            EditorSceneManager.SaveScene(scene, ScenePath);
            WriteRuntimeSettingsRecord(visualEffect);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string report = ValidateInternal();
            Debug.Log("========== GAME GRAPHICS PORTFOLIO VALIDATION ==========\n" + report);

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorGUIUtility.PingObject(Selection.activeObject);

            bool spawnRateReady = visualEffect.HasFloat("SpawnRate");
            string vfxMessage = spawnRateReady
                ? "VFX SpawnRate Exposed Property 확인 완료"
                : "VFX Graph에서 SpawnRate Float를 Exposed로 만들고 Constant Spawn Rate에 연결해야 합니다";

            EditorUtility.DisplayDialog(
                "그래픽 포트폴리오 씬 생성 완료",
                "씬: " + ScenePath + "\n\n" +
                "Play 확인\n" +
                "- Impact Pad 좌클릭: HitSpark 한 번\n" +
                "- 숫자 1 / 2 / 3: VFX 20 / 200 / 80\n" +
                "- 중앙 Shader Graph 보호막\n" +
                "- PBR Matte / Metal 비교\n" +
                "- HealGlow와 VFX Graph\n\n" +
                vfxMessage + "\n\n" +
                "Tools > Game Graphics Portfolio > 2 Validate에서 최종 확인하세요.",
                "확인");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("그래픽 포트폴리오 생성 오류", exception.Message, "확인");
        }
    }

    [MenuItem("Tools/Game Graphics Portfolio/2 Validate")]
    public static void ValidatePortfolio()
    {
        string report = ValidateInternal();
        Debug.Log("========== GAME GRAPHICS PORTFOLIO VALIDATION ==========\n" + report);
        EditorUtility.DisplayDialog(
            report.Contains("FAIL") ? "확인할 항목이 있습니다" : "필수 파일 검증 통과",
            report,
            "확인");
    }

    [MenuItem("Tools/Game Graphics Portfolio/3 Open VFX Graph")]
    public static void OpenVfxGraph()
    {
        VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VfxPath);
        if (asset == null)
        {
            EditorUtility.DisplayDialog("VFX Graph 없음", "먼저 1 Build Complete Scene을 실행하세요.", "확인");
            return;
        }

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        AssetDatabase.OpenAsset(asset);
    }

    [MenuItem("Tools/Game Graphics Portfolio/4 Select Documentation")]
    public static void SelectDocumentation()
    {
        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
            DocumentationFolder + "/Implementation_Description.md");

        if (asset != null)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }

    private static void CreateMaterials(
        Shader shieldShader,
        Shader litShader,
        Shader particleShader,
        Texture2D softTexture)
    {
        ReplaceAsset(ShieldMaterialPath);
        Material shield = new Material(shieldShader) { name = "Mat_MagicShield" };
        SetColorIfPresent(shield, "_ShieldColor", new Color(0.15f, 0.65f, 1f, 1f));
        SetFloatIfPresent(shield, "_RimPower", 3f);
        SetFloatIfPresent(shield, "_EmissionStrength", 2f);
        SetFloatIfPresent(shield, "_AlphaStrength", 0.45f);
        SetFloatIfPresent(shield, "_PulseSpeed", 2f);
        AssetDatabase.CreateAsset(shield, ShieldMaterialPath);

        CreateLitMaterial(
            FloorMaterialPath, litShader, new Color(0.045f, 0.060f, 0.085f, 1f),
            0.08f, 0.30f, Color.black);

        CreateLitMaterial(
            StoneMaterialPath, litShader, new Color(0.12f, 0.15f, 0.20f, 1f),
            0.12f, 0.42f, Color.black);

        CreateLitMaterial(
            AccentMaterialPath, litShader, new Color(0.02f, 0.20f, 0.24f, 1f),
            0.35f, 0.68f, new Color(0f, 1.4f, 1.8f, 1f));

        CreateLitMaterial(
            DarkMaterialPath, litShader, new Color(0.025f, 0.030f, 0.040f, 1f),
            0.05f, 0.22f, Color.black);

        CreateLitMaterial(
            MattePbrMaterialPath, litShader, new Color(0.30f, 0.32f, 0.35f, 1f),
            0.05f, 0.15f, Color.black);

        CreateLitMaterial(
            MetalPbrMaterialPath, litShader, new Color(0.04f, 0.19f, 0.24f, 1f),
            0.85f, 0.80f, new Color(0f, 1.25f, 1.65f, 1f));

        CreateParticleMaterial(
            HitParticleMaterialPath,
            particleShader,
            softTexture,
            new Color(1f, 0.42f, 0.04f, 1f));

        CreateParticleMaterial(
            HealParticleMaterialPath,
            particleShader,
            softTexture,
            new Color(0.12f, 1f, 0.72f, 0.82f));
    }

    private static void CreateLitMaterial(
        string path,
        Shader shader,
        Color baseColor,
        float metallic,
        float smoothness,
        Color emission)
    {
        ReplaceAsset(path);
        Material material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
        SetColorIfPresent(material, "_BaseColor", baseColor);
        SetColorIfPresent(material, "_Color", baseColor);
        SetFloatIfPresent(material, "_Metallic", metallic);
        SetFloatIfPresent(material, "_Smoothness", smoothness);

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", emission);
            if (emission.maxColorComponent > 0.001f)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }

        AssetDatabase.CreateAsset(material, path);
    }

    private static void CreateParticleMaterial(
        string path,
        Shader shader,
        Texture2D texture,
        Color tint)
    {
        ReplaceAsset(path);
        Material material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
        SetColorIfPresent(material, "_BaseColor", tint);
        SetColorIfPresent(material, "_Color", tint);
        SetTextureIfPresent(material, "_BaseMap", texture);
        SetTextureIfPresent(material, "_MainTex", texture);
        SetFloatIfPresent(material, "_Surface", 1f);
        SetFloatIfPresent(material, "_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        AssetDatabase.CreateAsset(material, path);
    }

    private static Texture2D CreateSoftParticleTexture()
    {
        ReplaceAsset(SoftParticleTexturePath);
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "T_SoftParticle",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - normalizedDistance);
                alpha = alpha * alpha * (3f - 2f * alpha);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        AssetDatabase.CreateAsset(texture, SoftParticleTexturePath);
        return texture;
    }

    private static void CreateParticlePrefabs()
    {
        Material hitMaterial = LoadMaterial(HitParticleMaterialPath);
        Material healMaterial = LoadMaterial(HealParticleMaterialPath);
        CreateHitSparkPrefab(hitMaterial);
        CreateHealGlowPrefab(healMaterial);
    }

    private static void CreateHitSparkPrefab(Material material)
    {
        ReplaceAsset(HitPrefabPath);
        GameObject root = new GameObject("FX_HitSpark");
        ParticleSystem system = root.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.35f;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 4.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.90f, 0.25f, 1f),
            new Color(1f, 0.18f, 0.02f, 1f));
        main.maxParticles = 64;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.08f;
        shape.randomDirectionAmount = 0.35f;

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(CreateFadeGradient(
            new Color(1f, 1f, 0.45f, 1f),
            new Color(1f, 0.12f, 0.01f, 0f)));

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.18f;
        renderer.lengthScale = 1.4f;
        renderer.sharedMaterial = material;

        PrefabUtility.SaveAsPrefabAsset(root, HitPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateHealGlowPrefab(Material material)
    {
        ReplaceAsset(HealPrefabPath);
        GameObject root = new GameObject("FX_HealGlow");
        ParticleSystem system = root.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = system.main;
        main.duration = 2f;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.90f, 1.50f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.15f, 1f, 0.78f, 0.85f),
            new Color(0.12f, 0.65f, 1f, 0.70f));
        main.maxParticles = 96;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 24f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.4f, 0.05f, 1.4f);

        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.35f, 0.80f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.18f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.20f;

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(CreateFadeGradient(
            new Color(0.30f, 1f, 0.78f, 0f),
            new Color(0.08f, 0.55f, 1f, 0f),
            0.75f));

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1f));

        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;

        PrefabUtility.SaveAsPrefabAsset(root, HealPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static Gradient CreateFadeGradient(Color start, Color end, float middleAlpha = 1f)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(start, 0f),
                new GradientColorKey(end, 1f)
            },
            new[]
            {
                new GradientAlphaKey(start.a, 0f),
                new GradientAlphaKey(middleAlpha, 0.18f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private static void BuildEnvironment(
        Transform parent,
        Material floor,
        Material stone,
        Material accent)
    {
        CreatePrimitive(
            PrimitiveType.Plane,
            "TrainingGround_Floor",
            parent,
            Vector3.zero,
            new Vector3(1.45f, 1f, 1.18f),
            floor);

        CreatePrimitive(
            PrimitiveType.Cube,
            "BackWall",
            parent,
            new Vector3(0f, 1.7f, 5.2f),
            new Vector3(10.8f, 3.4f, 0.30f),
            stone);

        CreatePrimitive(
            PrimitiveType.Cube,
            "LeftWall",
            parent,
            new Vector3(-5.35f, 0.65f, 1.2f),
            new Vector3(0.30f, 1.3f, 8.3f),
            stone);

        CreatePrimitive(
            PrimitiveType.Cube,
            "RightWall",
            parent,
            new Vector3(5.35f, 0.65f, 1.2f),
            new Vector3(0.30f, 1.3f, 8.3f),
            stone);

        Vector3[] positions =
        {
            new Vector3(-4.45f, 1.25f, 4.45f),
            new Vector3(4.45f, 1.25f, 4.45f),
            new Vector3(-4.45f, 1.25f, -2.0f),
            new Vector3(4.45f, 1.25f, -2.0f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            CreatePrimitive(
                PrimitiveType.Cube,
                "TrainingPillar_" + (i + 1),
                parent,
                positions[i],
                new Vector3(0.55f, 2.5f, 0.55f),
                stone);

            CreatePrimitive(
                PrimitiveType.Cube,
                "PillarAccent_" + (i + 1),
                parent,
                positions[i] + Vector3.up * 0.78f,
                new Vector3(0.63f, 0.10f, 0.63f),
                accent);
        }
    }

    private static void BuildShieldTarget(
        Transform parent,
        Material stone,
        Material accent,
        Material dark,
        Material shield)
    {
        Transform zone = NewParent("01_Central_Shield_Target", parent);

        CreatePrimitive(
            PrimitiveType.Cylinder,
            "ShieldPedestal",
            zone,
            new Vector3(0f, 0.17f, 0.65f),
            new Vector3(1.25f, 0.17f, 1.25f),
            stone);

        CreatePrimitive(
            PrimitiveType.Cylinder,
            "ShieldEnergyRing",
            zone,
            new Vector3(0f, 0.30f, 0.65f),
            new Vector3(0.98f, 0.025f, 0.98f),
            accent);

        CreatePrimitive(
            PrimitiveType.Capsule,
            "TrainingDummy",
            zone,
            new Vector3(0f, 1.25f, 0.65f),
            new Vector3(0.62f, 0.95f, 0.62f),
            dark);

        CreatePrimitive(
            PrimitiveType.Sphere,
            "MagicShield_Fresnel_Emission_Time_Alpha",
            zone,
            new Vector3(0f, 1.25f, 0.65f),
            Vector3.one * 2.1f,
            shield);
    }

    private static void BuildPbrComparison(
        Transform parent,
        Material pedestal,
        Material matte,
        Material metal)
    {
        CreatePbrSample(
            "PBR_A_MatteStone_Metallic005_Smoothness015",
            parent,
            new Vector3(-3.40f, 0.95f, -1.25f),
            pedestal,
            matte);

        CreatePbrSample(
            "PBR_B_ArcaneMetal_Metallic085_Smoothness080_Emission",
            parent,
            new Vector3(3.40f, 0.95f, -1.25f),
            pedestal,
            metal);
    }

    private static void CreatePbrSample(
        string name,
        Transform parent,
        Vector3 position,
        Material pedestal,
        Material sample)
    {
        Transform root = NewParent(name, parent);
        CreatePrimitive(
            PrimitiveType.Cylinder,
            name + "_Pedestal",
            root,
            new Vector3(position.x, 0.16f, position.z),
            new Vector3(0.82f, 0.16f, 0.82f),
            pedestal);

        CreatePrimitive(
            PrimitiveType.Sphere,
            name + "_Sphere",
            root,
            position,
            Vector3.one * 1.35f,
            sample);
    }

    private static void BuildHealingZone(
        Transform parent,
        Material stone,
        Material accent,
        GameObject healPrefab)
    {
        Transform zone = NewParent("02_Healing_Zone", parent);
        CreatePrimitive(
            PrimitiveType.Cylinder,
            "HealingPad",
            zone,
            new Vector3(-3.05f, 0.14f, 1.15f),
            new Vector3(1.0f, 0.14f, 1.0f),
            stone);

        CreatePrimitive(
            PrimitiveType.Cylinder,
            "HealingEnergyRing",
            zone,
            new Vector3(-3.05f, 0.26f, 1.15f),
            new Vector3(0.82f, 0.025f, 0.82f),
            accent);

        GameObject instance = PrefabUtility.InstantiatePrefab(healPrefab) as GameObject;
        if (instance == null)
            throw new Exception("FX_HealGlow Prefab 인스턴스 생성 실패");

        instance.name = "FX_HealGlow_Looping";
        instance.transform.SetParent(zone);
        instance.transform.position = new Vector3(-3.05f, 0.32f, 1.15f);
        instance.transform.localScale = Vector3.one;
    }

    private static void BuildImpactZone(
        Transform parent,
        Material stone,
        Material accent,
        int impactLayer)
    {
        Transform zone = NewParent("03_Impact_Test_Zone", parent);
        GameObject pad = CreatePrimitive(
            PrimitiveType.Cube,
            "ImpactPad_ClickTarget",
            zone,
            new Vector3(3.05f, 0.12f, 1.15f),
            new Vector3(2.15f, 0.22f, 2.15f),
            stone);
        pad.layer = impactLayer;

        CreatePrimitive(
            PrimitiveType.Cylinder,
            "ImpactPad_EnergyMark",
            zone,
            new Vector3(3.05f, 0.25f, 1.15f),
            new Vector3(0.74f, 0.025f, 0.74f),
            accent);
    }

    private static VisualEffect BuildManaFountain(
        Transform parent,
        Material stone,
        Material accent,
        VisualEffectAsset vfxAsset)
    {
        Transform zone = NewParent("04_Mana_Fountain", parent);
        CreatePrimitive(
            PrimitiveType.Cylinder,
            "ManaFountainPedestal",
            zone,
            new Vector3(0f, 0.17f, 3.55f),
            new Vector3(1.20f, 0.17f, 1.20f),
            stone);

        CreatePrimitive(
            PrimitiveType.Cylinder,
            "ManaFountainEnergyRing",
            zone,
            new Vector3(0f, 0.30f, 3.55f),
            new Vector3(0.95f, 0.025f, 0.95f),
            accent);

        GameObject vfxObject = new GameObject("VFX_GpuSpark_ManaFountain");
        vfxObject.transform.SetParent(zone);
        vfxObject.transform.position = new Vector3(0f, 0.35f, 3.55f);
        VisualEffect visualEffect = vfxObject.AddComponent<VisualEffect>();
        visualEffect.visualEffectAsset = vfxAsset;
        visualEffect.playRate = 1f;

        if (visualEffect.HasFloat("SpawnRate"))
            visualEffect.SetFloat("SpawnRate", 80f);

        GameObject lightObject = new GameObject("ManaFountainLight");
        lightObject.transform.SetParent(zone);
        lightObject.transform.position = new Vector3(0f, 1.2f, 3.55f);
        Light pointLight = lightObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(0.10f, 0.85f, 1f, 1f);
        pointLight.intensity = 2.1f;
        pointLight.range = 4.2f;
        return visualEffect;
    }

    private static Camera CreateCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent);
        cameraObject.transform.position = new Vector3(0f, 4.4f, -10.5f);
        cameraObject.transform.LookAt(new Vector3(0f, 1.05f, 1.25f));
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 50f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.016f, 0.023f, 0.038f, 1f);
        return camera;
    }

    private static void CreateDirectionalLight(Transform parent)
    {
        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(parent);
        lightObject.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.82f, 0.90f, 1f, 1f);
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
    }

    private static void CreateGlobalVolume(Transform parent)
    {
        ReplaceAsset(VolumeProfilePath);
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "VP_MagicTraining";
        AssetDatabase.CreateAsset(profile, VolumeProfilePath);

        GameObject volumeObject = new GameObject("Global Volume");
        volumeObject.transform.SetParent(parent);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.sharedProfile = profile;
    }

    private static VisualEffectAsset GetOrCreateVfxAsset()
    {
        VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VfxPath);
        if (asset != null)
            return asset;

        Type builderType = FindType("DAY12_VFXBuilder");
        MethodInfo rebuild = builderType?.GetMethod(
            "RebuildEverything",
            BindingFlags.Public | BindingFlags.Static);

        if (rebuild != null)
        {
            rebuild.Invoke(null, null);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VfxPath);
        }

        return asset;
    }

    private static void BuildVerificationHierarchy(Transform parent)
    {
        string[] names =
        {
            "Requirement_ShaderGraph_Fresnel_Emission_Time_Alpha",
            "Requirement_PBR_Metallic_Smoothness_Emission",
            "Requirement_ParticlePrefab_FX_HitSpark",
            "Requirement_ParticlePrefab_FX_HealGlow",
            "Requirement_VFXGraph_Spawn_Initialize_Update_Output",
            "Requirement_CodeEvent_Click_To_HitSpark",
            "Requirement_NoDuplicate_FrameAndCooldownGuard",
            "Requirement_VFX_ExposedProperty_SpawnRate",
            "Requirement_Documentation_And_PlayMode_Record"
        };

        foreach (string name in names)
            NewParent(name, parent);
    }

    private static string ValidateInternal()
    {
        List<string> lines = new List<string>();
        lines.Add(GraphicsSettings.currentRenderPipeline != null
            ? "PASS Unity Render Pipeline assigned"
            : "FAIL Render Pipeline is not assigned");

        lines.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
            ? "PASS Portfolio Scene"
            : "FAIL Portfolio Scene");

        lines.Add(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ShaderGraphPath) != null
            ? "PASS Shader Graph asset"
            : "FAIL Shader Graph asset");

        lines.Add(LoadMaterial(ShieldMaterialPath) != null
            ? "PASS Shader Graph material"
            : "FAIL Shader Graph material");

        Material matte = LoadMaterial(MattePbrMaterialPath);
        Material metal = LoadMaterial(MetalPbrMaterialPath);
        lines.Add(matte != null && metal != null
            ? "PASS PBR comparison materials"
            : "FAIL PBR comparison materials");

        GameObject hit = AssetDatabase.LoadAssetAtPath<GameObject>(HitPrefabPath);
        GameObject heal = AssetDatabase.LoadAssetAtPath<GameObject>(HealPrefabPath);
        lines.Add(
            hit != null && heal != null &&
            hit.GetComponentInChildren<ParticleSystem>(true) != null &&
            heal.GetComponentInChildren<ParticleSystem>(true) != null
                ? "PASS Particle System Prefab 2+"
                : "FAIL Particle System Prefab 2+");

        VisualEffectAsset vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VfxPath);
        lines.Add(vfxAsset != null ? "PASS VFX Graph asset" : "FAIL VFX Graph asset");

        if (SceneManager.GetActiveScene().path == ScenePath)
        {
            GameObject pbrRoot = GameObject.Find("PBRComparison");
            lines.Add(pbrRoot != null && pbrRoot.transform.childCount >= 2
                ? "PASS PBR comparison in Scene"
                : "FAIL PBR comparison in Scene");

            GameObject vfxObject = GameObject.Find("VFX_GpuSpark_ManaFountain");
            VisualEffect vfx = vfxObject != null ? vfxObject.GetComponent<VisualEffect>() : null;
            lines.Add(vfx != null && vfx.visualEffectAsset == vfxAsset
                ? "PASS VFX Graph in Scene"
                : "FAIL VFX Graph in Scene");
            lines.Add(vfx != null && vfx.HasFloat("SpawnRate")
                ? "PASS VFX SpawnRate Exposed Property"
                : "FAIL VFX SpawnRate must be Exposed and connected");

            PortfolioEffectController controller =
                UnityEngine.Object.FindFirstObjectByType<PortfolioEffectController>();
            lines.Add(controller != null
                ? "PASS Input event and duplicate guard script"
                : "FAIL PortfolioEffectController");

            GameObject impactPad = GameObject.Find("ImpactPad_ClickTarget");
            lines.Add(impactPad != null && impactPad.layer == LayerMask.NameToLayer("PortfolioImpact")
                ? "PASS Impact Pad collision layer"
                : "FAIL Impact Pad collision layer");
        }
        else
        {
            lines.Add("INFO Open the generated Scene for Hierarchy and Play Mode validation");
        }

        bool documentationReady =
            File.Exists(DocumentationFolder + "/Implementation_Description.md") &&
            File.Exists(DocumentationFolder + "/Verification_Checklist.md") &&
            File.Exists(DocumentationFolder + "/Runtime_Settings_Record.md");

        lines.Add(documentationReady ? "PASS Documentation" : "FAIL Documentation");
        return string.Join("\n", lines);
    }

    private static void WriteRuntimeSettingsRecord(VisualEffect visualEffect)
    {
        RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
        int qualityIndex = QualitySettings.GetQualityLevel();
        string qualityName = QualitySettings.names.Length > qualityIndex
            ? QualitySettings.names[qualityIndex]
            : qualityIndex.ToString();

        StringBuilder text = new StringBuilder();
        text.AppendLine("# Runtime Settings Record");
        text.AppendLine();
        text.AppendLine("Generated by PortfolioGraphicsBuilder on this project.");
        text.AppendLine();
        text.AppendLine("| Item | Recorded value |");
        text.AppendLine("|---|---|");
        text.AppendLine("| Unity version | `" + Application.unityVersion + "` |");
        text.AppendLine("| Render Pipeline Asset | `" + (pipeline != null ? pipeline.name : "None") + "` |");
        text.AppendLine("| Quality level | `" + qualityName + "` |");
        text.AppendLine("| VSync Count | `" + QualitySettings.vSyncCount + "` |");
        text.AppendLine("| Anti Aliasing | `" + QualitySettings.antiAliasing + "` |");
        text.AppendLine("| Shadow Distance | `" + QualitySettings.shadowDistance + "` |");
        text.AppendLine("| VFX SpawnRate exposed | `" + (visualEffect.HasFloat("SpawnRate") ? "Yes" : "No") + "` |");
        text.AppendLine("| Scene | `" + ScenePath + "` |");
        text.AppendLine();
        text.AppendLine("Use the verification checklist for Inspector, Graph, and Play Mode screenshots.");

        string path = DocumentationFolder + "/Runtime_Settings_Record.md";
        File.WriteAllText(path, text.ToString(), Encoding.UTF8);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private static GameObject CreatePrimitive(
        PrimitiveType type,
        string name,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        gameObject.transform.SetParent(parent);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        return gameObject;
    }

    private static Transform NewParent(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent);
        return gameObject.transform;
    }

    private static void EnsureFolders()
    {
        foreach (string path in new[]
                 {
                     Root, ShaderFolder, MaterialFolder, PrefabFolder, TextureFolder,
                     SceneFolder, SettingsFolder, DocumentationFolder, Root + "/Scripts", Root + "/Editor"
                 })
        {
            EnsureFolder(path);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
            return existing;

        UnityEngine.Object tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")
            .FirstOrDefault();
        if (tagManager == null)
            throw new Exception("TagManager.asset을 찾지 못했습니다.");

        SerializedObject serialized = new SerializedObject(tagManager);
        SerializedProperty layers = serialized.FindProperty("layers");

        for (int i = 8; i < 32; i++)
        {
            SerializedProperty element = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(element.stringValue))
                continue;

            element.stringValue = layerName;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return i;
        }

        throw new Exception("사용 가능한 User Layer 슬롯이 없습니다.");
    }

    private static Shader LoadShaderFromGraph(string path)
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
        return shader ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<Shader>().FirstOrDefault();
    }

    private static Shader FindShader(params string[] names)
    {
        foreach (string name in names)
        {
            Shader shader = Shader.Find(name);
            if (shader != null)
                return shader;
        }

        return null;
    }

    private static Type FindType(string typeName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type type = assembly.GetTypes().FirstOrDefault(candidate => candidate.Name == typeName);
                if (type != null)
                    return type;
            }
            catch (ReflectionTypeLoadException exception)
            {
                Type type = exception.Types.Where(candidate => candidate != null)
                    .FirstOrDefault(candidate => candidate.Name == typeName);
                if (type != null)
                    return type;
            }
        }

        return null;
    }

    private static Material LoadMaterial(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    private static void ReplaceAsset(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    private static void SetColorIfPresent(Material material, string name, Color value)
    {
        if (material.HasProperty(name))
            material.SetColor(name, value);
    }

    private static void SetFloatIfPresent(Material material, string name, float value)
    {
        if (material.HasProperty(name))
            material.SetFloat(name, value);
    }

    private static void SetTextureIfPresent(Material material, string name, Texture value)
    {
        if (material.HasProperty(name))
            material.SetTexture(name, value);
    }
}
#endif
