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

public static class DAY14_MagicTrainingGroundBuilder
{
    const string ROOT = "Assets/DAY14";
    const string SHADERS = ROOT + "/Shaders";
    const string MATERIALS = ROOT + "/Materials";
    const string SETTINGS = ROOT + "/Settings";
    const string SCENES = ROOT + "/Scenes";
    const string DOCS = ROOT + "/Documentation";

    const string FINAL_SCENE =
        SCENES + "/GraphicsPortfolio_MagicTrainingGround.unity";

    const string PORTFOLIO_GRAPH =
        SHADERS + "/SG_Portfolio_MagicShield.shadergraph";

    const string SHIELD_MAT =
        MATERIALS + "/Mat_Portfolio_MagicShield.mat";

    const string FLOOR_MAT =
        MATERIALS + "/Mat_TrainingGround_Floor.mat";

    const string STONE_MAT =
        MATERIALS + "/Mat_TrainingGround_Stone.mat";

    const string ACCENT_MAT =
        MATERIALS + "/Mat_TrainingGround_Accent.mat";

    const string DARK_MAT =
        MATERIALS + "/Mat_TrainingDummy_Dark.mat";

    const string VOLUME_PROFILE =
        SETTINGS + "/VP_DAY14_MagicTraining.asset";

    const string DAY12_VFX =
        "Assets/DAY12/VFX/VFX_GpuSpark.vfx";

    [MenuItem("Tools/DAY14 MAGIC TRAINING/1 - Build Final Scene")]
    public static void BuildFinalScene()
    {
        try
        {
            EnsureFolders();

            string graphicsLab = FindGraphicsLabScene();
            if (string.IsNullOrEmpty(graphicsLab))
                throw new Exception(
                    "GraphicsLab 씬을 찾지 못했습니다.\n" +
                    "DAY14는 기존 GraphicsLab을 복제해 GraphicsPortfolio로 만드는 과정입니다.");

            string shieldGraph = FindFileByName(
                "SG_Shield",
                ".shadergraph",
                "Assets/DAY05");

            if (string.IsNullOrEmpty(shieldGraph))
                throw new Exception(
                    "DAY05 SG_Shield.shadergraph를 찾지 못했습니다.");

            Shader sourceShieldShader = LoadShaderFromGraph(shieldGraph);

            if (sourceShieldShader == null)
                throw new Exception(
                    "DAY05 SG_Shield Shader를 불러오지 못했습니다.");

            Material sourceShieldMaterial =
                FindMaterialUsingShader(
                    sourceShieldShader,
                    new[] { "Assets/DAY05" });

            GameObject hitPrefab =
                FindPrefabExact("FX_HitSpark", "Assets/DAY10");

            GameObject healPrefab =
                FindPrefabExact("FX_HealGlow", "Assets/DAY10");

            if (hitPrefab == null || healPrefab == null)
                throw new Exception(
                    "DAY10 FX_HitSpark / FX_HealGlow Prefab을 찾지 못했습니다.");

            ParticleSystem hitParticle =
                GetParticleSystem(hitPrefab);

            VisualEffectAsset vfxAsset =
                AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(DAY12_VFX);

            if (vfxAsset == null)
                throw new Exception(
                    "DAY12 VFX_GpuSpark.vfx를 찾지 못했습니다.");

            InputActionAsset inputAsset =
                FindInputAssetWithRequiredActions();

            if (inputAsset == null)
                throw new Exception(
                    "Gameplay에 Point / Click / LowIntensity / HighIntensity가 모두 있는 Input Actions Asset을 찾지 못했습니다.");

            string inputAssetPath =
                AssetDatabase.GetAssetPath(inputAsset);

            Type clickSpawnerType =
                FindTypeByName("ClickEffectSpawner");

            Type intensityType =
                FindTypeByName("VfxIntensityController");

            if (clickSpawnerType == null)
                throw new Exception(
                    "DAY11 ClickEffectSpawner.cs를 찾지 못했습니다.");

            if (intensityType == null)
                throw new Exception(
                    "DAY13 VfxIntensityController.cs를 찾지 못했습니다.");

            CreatePortfolioShield(
                shieldGraph,
                sourceShieldShader,
                sourceShieldMaterial);

            CreateEnvironmentMaterials();

            // DAY14 source: preserve previous scene by copying GraphicsLab.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FINAL_SCENE) != null)
                AssetDatabase.DeleteAsset(FINAL_SCENE);

            if (!AssetDatabase.CopyAsset(graphicsLab, FINAL_SCENE))
                throw new Exception(
                    "GraphicsLab -> DAY14 final scene 복제 실패");

            AssetDatabase.ImportAsset(
                FINAL_SCENE,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            Scene scene =
                EditorSceneManager.OpenScene(
                    FINAL_SCENE,
                    OpenSceneMode.Single);

            // Keep the old practice scene for reference but remove its clutter from presentation.
            GameObject[] originalRoots =
                scene.GetRootGameObjects();

            GameObject portfolioRoot =
                new GameObject("DAY14_MagicTrainingGround");

            GameObject environment =
                NewParent("Environment", portfolioRoot.transform);

            GameObject shaderTargets =
                NewParent("ShaderTargets", portfolioRoot.transform);

            GameObject particleEffects =
                NewParent("ParticleEffects", portfolioRoot.transform);

            GameObject vfxEffects =
                NewParent("VfxEffects", portfolioRoot.transform);

            GameObject effectInput =
                NewParent("EffectInput", portfolioRoot.transform);

            GameObject verification =
                NewParent("Verification", portfolioRoot.transform);

            GameObject legacy =
                NewParent("LegacyReference_Disabled", environment.transform);

            // Preserve existing camera/light/volume before disabling old practice samples.
            Camera camera = FindAnyCamera();
            Light directional = FindDirectionalLight();
            Volume existingVolume = FindAnyVolume();

            if (camera != null)
                camera.transform.SetParent(environment.transform, true);

            if (directional != null)
                directional.transform.SetParent(environment.transform, true);

            if (existingVolume != null)
                existingVolume.transform.SetParent(environment.transform, true);

            foreach (GameObject root in originalRoots)
            {
                if (root == null)
                    continue;

                if (root.transform.IsChildOf(portfolioRoot.transform))
                    continue;

                // Camera/Light/Volume may have been moved out already.
                if (root == camera?.gameObject ||
                    root == directional?.gameObject ||
                    root == existingVolume?.gameObject)
                    continue;

                root.transform.SetParent(legacy.transform, true);
                root.SetActive(false);
            }

            // Existing scene provides the base, but final presentation is curated.
            if (camera == null)
                camera = CreateCamera(environment.transform);

            SetupCamera(camera);

            if (directional == null)
                directional = CreateDirectionalLight(environment.transform);
            else
                SetupDirectionalLight(directional);

            Volume volume =
                existingVolume != null
                    ? existingVolume
                    : CreateVolume(environment.transform);

            int groundLayer = EnsureLayer("Ground");

            Material floorMat =
                AssetDatabase.LoadAssetAtPath<Material>(FLOOR_MAT);

            Material stoneMat =
                AssetDatabase.LoadAssetAtPath<Material>(STONE_MAT);

            Material accentMat =
                AssetDatabase.LoadAssetAtPath<Material>(ACCENT_MAT);

            Material darkMat =
                AssetDatabase.LoadAssetAtPath<Material>(DARK_MAT);

            Material shieldMat =
                AssetDatabase.LoadAssetAtPath<Material>(SHIELD_MAT);

            if (floorMat == null ||
                stoneMat == null ||
                accentMat == null ||
                darkMat == null ||
                shieldMat == null)
            {
                throw new Exception(
                    "DAY14 Material 생성에 실패했습니다.");
            }

            BuildEnvironment(
                environment.transform,
                floorMat,
                stoneMat,
                accentMat);

            // =========================
            // CENTRAL SHIELD TRAINING
            // =========================
            GameObject centralZone =
                NewParent(
                    "01_Central_Shield_Training",
                    shaderTargets.transform);

            GameObject centralPedestal =
                CreateCylinder(
                    "Shield_Pedestal",
                    centralZone.transform,
                    new Vector3(0f, 0.18f, 0.2f),
                    new Vector3(1.35f, 0.18f, 1.35f),
                    stoneMat);

            GameObject centralRing =
                CreateCylinder(
                    "Shield_Energy_Ring",
                    centralZone.transform,
                    new Vector3(0f, 0.31f, 0.2f),
                    new Vector3(1.05f, 0.03f, 1.05f),
                    accentMat);

            GameObject dummy =
                GameObject.CreatePrimitive(PrimitiveType.Capsule);

            dummy.name = "TrainingDummy";
            dummy.transform.SetParent(centralZone.transform);
            dummy.transform.position =
                new Vector3(0f, 1.25f, 0.2f);
            dummy.transform.localScale =
                new Vector3(0.62f, 0.95f, 0.62f);

            dummy.GetComponent<Renderer>().sharedMaterial =
                darkMat;

            GameObject shield =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);

            shield.name =
                "MagicShield_Fresnel_Emission";

            shield.transform.SetParent(centralZone.transform);
            shield.transform.position =
                new Vector3(0f, 1.25f, 0.2f);
            shield.transform.localScale =
                Vector3.one * 2.15f;

            shield.GetComponent<Renderer>().sharedMaterial =
                shieldMat;

            // =========================
            // LEFT HEALING ZONE
            // =========================
            GameObject healZone =
                NewParent(
                    "02_Healing_Zone",
                    particleEffects.transform);

            CreateCylinder(
                "Healing_Pad",
                healZone.transform,
                new Vector3(-3.1f, 0.14f, 0.7f),
                new Vector3(1.0f, 0.14f, 1.0f),
                stoneMat);

            CreateCylinder(
                "Healing_Energy_Ring",
                healZone.transform,
                new Vector3(-3.1f, 0.26f, 0.7f),
                new Vector3(0.82f, 0.025f, 0.82f),
                accentMat);

            GameObject healInstance =
                InstantiatePrefab(
                    healPrefab,
                    healZone.transform,
                    "FX_HealGlow_Zone",
                    new Vector3(-3.1f, 0.35f, 0.7f));

            ConfigureHealInstance(healInstance);

            // =========================
            // RIGHT IMPACT TEST ZONE
            // =========================
            GameObject impactZone =
                NewParent(
                    "03_Impact_Test_Zone",
                    particleEffects.transform);

            GameObject impactPad =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            impactPad.name = "ImpactPad_Ground";
            impactPad.transform.SetParent(impactZone.transform);
            impactPad.transform.position =
                new Vector3(3.1f, 0.11f, 0.7f);
            impactPad.transform.localScale =
                new Vector3(2.1f, 0.20f, 2.1f);
            impactPad.layer = groundLayer;
            impactPad.GetComponent<Renderer>().sharedMaterial =
                stoneMat;

            GameObject impactMark =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            impactMark.name = "ImpactPad_EnergyMark";
            impactMark.transform.SetParent(impactZone.transform);
            impactMark.transform.position =
                new Vector3(3.1f, 0.23f, 0.7f);
            impactMark.transform.localScale =
                new Vector3(0.72f, 0.025f, 0.72f);
            impactMark.GetComponent<Renderer>().sharedMaterial =
                accentMat;

            GameObject hitReference =
                InstantiatePrefab(
                    hitPrefab,
                    impactZone.transform,
                    "FX_HitSpark_PrefabReference",
                    new Vector3(3.1f, 0.40f, 0.7f));

            ConfigureHitReference(hitReference);

            // =========================
            // BACK ENERGY FOUNTAIN
            // =========================
            GameObject energyZone =
                NewParent(
                    "04_Mana_Fountain",
                    vfxEffects.transform);

            CreateCylinder(
                "ManaFountain_Pedestal",
                energyZone.transform,
                new Vector3(0f, 0.18f, 3.25f),
                new Vector3(1.35f, 0.18f, 1.35f),
                stoneMat);

            CreateCylinder(
                "ManaFountain_EnergyRing",
                energyZone.transform,
                new Vector3(0f, 0.31f, 3.25f),
                new Vector3(1.08f, 0.025f, 1.08f),
                accentMat);

            GameObject vfxGO =
                new GameObject("VFX_GpuSpark_ManaFountain");

            vfxGO.transform.SetParent(energyZone.transform);
            vfxGO.transform.position =
                new Vector3(0f, 0.34f, 3.25f);

            VisualEffect visualEffect =
                vfxGO.AddComponent<VisualEffect>();

            visualEffect.visualEffectAsset =
                vfxAsset;

            visualEffect.playRate = 1f;

            if (visualEffect.HasFloat("SpawnRate"))
                visualEffect.SetFloat("SpawnRate", 80f);

            // Small local light so the back device reads as one themed prop.
            GameObject manaLightGO =
                new GameObject("ManaFountain_Light");

            manaLightGO.transform.SetParent(energyZone.transform);
            manaLightGO.transform.position =
                new Vector3(0f, 1.2f, 3.25f);

            Light manaLight =
                manaLightGO.AddComponent<Light>();

            manaLight.type = LightType.Point;
            manaLight.color =
                new Color(0.15f, 0.95f, 1.0f, 1f);
            manaLight.intensity = 2.2f;
            manaLight.range = 4.2f;

            // =========================
            // INPUT / GAME RULE
            // =========================
            InputActionAsset freshInputAsset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    inputAssetPath);

            if (freshInputAsset == null)
                throw new Exception(
                    "Input Actions Asset을 다시 불러오지 못했습니다.");

            PlayerInput playerInput =
                effectInput.AddComponent<PlayerInput>();

            WirePlayerInputSerialized(
                playerInput,
                freshInputAsset);

            Component clickSpawner =
                effectInput.AddComponent(clickSpawnerType);

            WireClickSpawner(
                clickSpawner,
                camera,
                hitParticle,
                groundLayer);

            Component intensity =
                effectInput.AddComponent(intensityType);

            WireIntensityController(
                intensity,
                visualEffect);

            DAY14MagicTrainingHUD hud =
                effectInput.AddComponent<DAY14MagicTrainingHUD>();

            SerializedObject hudSO =
                new SerializedObject(hud);

            SerializedProperty hudVfx =
                hudSO.FindProperty("energyVfx");

            if (hudVfx != null)
                hudVfx.objectReferenceValue =
                    visualEffect;

            hudSO.ApplyModifiedPropertiesWithoutUndo();

            // =========================
            // VERIFICATION HIERARCHY
            // =========================
            AddNote(
                verification.transform,
                "ShaderGraph_SG_Portfolio_MagicShield");

            AddNote(
                verification.transform,
                "Surface_Fresnel_Emission");

            AddNote(
                verification.transform,
                "Particle_01_FX_HitSpark");

            AddNote(
                verification.transform,
                "Particle_02_FX_HealGlow");

            AddNote(
                verification.transform,
                "VFXGraph_VFX_GpuSpark");

            AddNote(
                verification.transform,
                "Code_Click_ImpactPad_HitSpark");

            AddNote(
                verification.transform,
                "Code_Key1_Key2_SpawnRate");

            AddNote(
                verification.transform,
                "PlayRule_OneClick_OneHitEffect");

            AddNote(
                verification.transform,
                "Spatial_Analysis_Documentation");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(
                scene,
                FINAL_SCENE);

            WriteDocumentation(
                graphicsLab,
                shieldGraph,
                freshInputAsset.name);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    FINAL_SCENE);

            string report =
                ValidateInternal();

            Debug.Log(
                "========== DAY14 MAGIC TRAINING VALIDATION ==========\n" +
                report);

            EditorUtility.DisplayDialog(
                "DAY14 마법 훈련장 완성",
                "DAY14 문서 기준으로 최종 씬을 다시 구성했습니다.\n\n" +
                "이번 버전의 핵심:\n" +
                "• 기존 GraphicsLab은 복제 후 LegacyReference로 보존\n" +
                "• 전시장식 일렬 배치 제거\n" +
                "• 중앙 보호막 훈련 대상\n" +
                "• 왼쪽 회복 구역\n" +
                "• 오른쪽 타격 시험 패드\n" +
                "• 뒤쪽 마력 분수 VFX\n" +
                "• 어두운 청회색 + 청록 Accent로 장면 통일\n" +
                "• Impact Pad 클릭 -> HitSpark 1회\n" +
                "• 숫자 1 / 2 -> SpawnRate 20 / 200\n" +
                "• 공간 배치 / 플레이 규칙 / 검증 기록 생성\n\n" +
                "씬:\n" + FINAL_SCENE + "\n\n" +
                "Play 후 Game View에서 확인하세요.",
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

    [MenuItem("Tools/DAY14 MAGIC TRAINING/2 - Validate Final Scene")]
    public static void ValidateFinalScene()
    {
        string report =
            ValidateInternal();

        Debug.Log(
            "========== DAY14 MAGIC TRAINING VALIDATION ==========\n" +
            report);

        EditorUtility.DisplayDialog(
            report.Contains("FAIL")
                ? "DAY14 확인 필요"
                : "DAY14 검증 통과",
            report,
            "확인");
    }

    [MenuItem("Tools/DAY14 MAGIC TRAINING/3 - Select Verification Record")]
    public static void SelectVerificationRecord()
    {
        UnityEngine.Object doc =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                DOCS + "/DAY14_Verification_Record.md");

        if (doc != null)
        {
            Selection.activeObject = doc;
            EditorGUIUtility.PingObject(doc);
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
        EnsureFolder(ROOT + "/Scripts");
    }

    static void CreatePortfolioShield(
        string sourceGraph,
        Shader sourceShader,
        Material sourceMaterial)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                PORTFOLIO_GRAPH) != null)
        {
            AssetDatabase.DeleteAsset(
                PORTFOLIO_GRAPH);
        }

        if (!AssetDatabase.CopyAsset(
                sourceGraph,
                PORTFOLIO_GRAPH))
        {
            throw new Exception(
                "SG_Shield 포트폴리오 복제 실패");
        }

        AssetDatabase.ImportAsset(
            PORTFOLIO_GRAPH,
            ImportAssetOptions.ForceSynchronousImport |
            ImportAssetOptions.ForceUpdate);

        Shader copiedShader =
            LoadShaderFromGraph(
                PORTFOLIO_GRAPH);

        if (copiedShader == null)
            copiedShader = sourceShader;

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                SHIELD_MAT) != null)
        {
            AssetDatabase.DeleteAsset(
                SHIELD_MAT);
        }

        Material mat =
            sourceMaterial != null
                ? new Material(sourceMaterial)
                : new Material(copiedShader);

        mat.name =
            "Mat_Portfolio_MagicShield";

        mat.shader =
            copiedShader;

        AssetDatabase.CreateAsset(
            mat,
            SHIELD_MAT);

        EditorUtility.SetDirty(mat);
    }

    static void CreateEnvironmentMaterials()
    {
        Shader lit = Shader.Find(
            "Universal Render Pipeline/Lit");

        if (lit == null)
            lit = Shader.Find(
                "Universal Render Pipeline/Simple Lit");

        if (lit == null)
            lit = Shader.Find("Standard");

        if (lit == null)
            throw new Exception(
                "환경 Material용 Lit Shader를 찾지 못했습니다.");

        CreateOrReplaceMaterial(
            FLOOR_MAT,
            lit,
            new Color(0.055f, 0.075f, 0.105f, 1f),
            Color.black,
            0.15f,
            0.65f);

        CreateOrReplaceMaterial(
            STONE_MAT,
            lit,
            new Color(0.12f, 0.15f, 0.20f, 1f),
            Color.black,
            0.10f,
            0.55f);

        CreateOrReplaceMaterial(
            ACCENT_MAT,
            lit,
            new Color(0.03f, 0.28f, 0.32f, 1f),
            new Color(0.0f, 2.0f, 2.2f, 1f),
            0.20f,
            0.35f);

        CreateOrReplaceMaterial(
            DARK_MAT,
            lit,
            new Color(0.025f, 0.030f, 0.040f, 1f),
            Color.black,
            0.05f,
            0.30f);
    }

    static void CreateOrReplaceMaterial(
        string path,
        Shader shader,
        Color baseColor,
        Color emissionColor,
        float metallic,
        float smoothness)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        Material mat =
            new Material(shader);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor(
                "_BaseColor",
                baseColor);

        if (mat.HasProperty("_Color"))
            mat.SetColor(
                "_Color",
                baseColor);

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat(
                "_Metallic",
                metallic);

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat(
                "_Smoothness",
                smoothness);

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor(
                "_EmissionColor",
                emissionColor);

            if (emissionColor.maxColorComponent > 0.001f)
                mat.EnableKeyword("_EMISSION");
        }

        AssetDatabase.CreateAsset(
            mat,
            path);
    }

    static void BuildEnvironment(
        Transform parent,
        Material floorMat,
        Material stoneMat,
        Material accentMat)
    {
        GameObject floor =
            GameObject.CreatePrimitive(
                PrimitiveType.Plane);

        floor.name =
            "TrainingGround_Floor";

        floor.transform.SetParent(parent);
        floor.transform.position =
            Vector3.zero;

        floor.transform.localScale =
            new Vector3(1.55f, 1f, 1.25f);

        floor.GetComponent<Renderer>().sharedMaterial =
            floorMat;

        // Back wall.
        CreateCube(
            "BackWall",
            parent,
            new Vector3(0f, 1.6f, 5.2f),
            new Vector3(10.8f, 3.2f, 0.35f),
            stoneMat);

        // Low side walls frame the scene without blocking the camera.
        CreateCube(
            "LeftWall",
            parent,
            new Vector3(-5.35f, 0.65f, 1.1f),
            new Vector3(0.35f, 1.3f, 8.5f),
            stoneMat);

        CreateCube(
            "RightWall",
            parent,
            new Vector3(5.35f, 0.65f, 1.1f),
            new Vector3(0.35f, 1.3f, 8.5f),
            stoneMat);

        Vector3[] pillarPositions =
        {
            new Vector3(-4.3f, 1.3f, 4.4f),
            new Vector3( 4.3f, 1.3f, 4.4f),
            new Vector3(-4.3f, 1.3f,-1.8f),
            new Vector3( 4.3f, 1.3f,-1.8f)
        };

        for (int i = 0; i < pillarPositions.Length; i++)
        {
            CreateCube(
                "TrainingPillar_" + (i + 1),
                parent,
                pillarPositions[i],
                new Vector3(0.60f, 2.6f, 0.60f),
                stoneMat);

            CreateCube(
                "TrainingPillar_Accent_" + (i + 1),
                parent,
                pillarPositions[i] + new Vector3(0f, 0.85f, 0f),
                new Vector3(0.68f, 0.12f, 0.68f),
                accentMat);
        }

        // Simple accent strips make the space read as one designed room.
        CreateCube(
            "CenterGuide_Left",
            parent,
            new Vector3(-1.55f, 0.04f, 1.7f),
            new Vector3(2.0f, 0.04f, 0.08f),
            accentMat);

        CreateCube(
            "CenterGuide_Right",
            parent,
            new Vector3(1.55f, 0.04f, 1.7f),
            new Vector3(2.0f, 0.04f, 0.08f),
            accentMat);
    }

    static Camera CreateCamera(Transform parent)
    {
        GameObject go =
            new GameObject("Main Camera");

        go.tag = "MainCamera";
        go.transform.SetParent(parent);

        Camera cam =
            go.AddComponent<Camera>();

        return cam;
    }

    static void SetupCamera(Camera camera)
    {
        camera.gameObject.tag =
            "MainCamera";

        camera.transform.position =
            new Vector3(0f, 4.5f, -9.6f);

        camera.transform.LookAt(
            new Vector3(0f, 1.05f, 1.25f));

        camera.fieldOfView =
            50f;

        camera.clearFlags =
            CameraClearFlags.SolidColor;

        camera.backgroundColor =
            new Color(
                0.018f,
                0.025f,
                0.040f,
                1f);
    }

    static Light CreateDirectionalLight(
        Transform parent)
    {
        GameObject go =
            new GameObject("Directional Light");

        go.transform.SetParent(parent);

        Light light =
            go.AddComponent<Light>();

        light.type =
            LightType.Directional;

        SetupDirectionalLight(light);

        return light;
    }

    static void SetupDirectionalLight(
        Light light)
    {
        light.transform.rotation =
            Quaternion.Euler(
                48f,
                -30f,
                0f);

        light.color =
            new Color(
                0.76f,
                0.84f,
                1.0f,
                1f);

        light.intensity =
            1.05f;
    }

    static Volume CreateVolume(
        Transform parent)
    {
        GameObject go =
            new GameObject(
                "Global Volume");

        go.transform.SetParent(parent);

        Volume volume =
            go.AddComponent<Volume>();

        volume.isGlobal =
            true;

        VolumeProfile profile =
            AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                VOLUME_PROFILE);

        if (profile == null)
        {
            profile =
                ScriptableObject.CreateInstance<VolumeProfile>();

            AssetDatabase.CreateAsset(
                profile,
                VOLUME_PROFILE);
        }

        volume.sharedProfile =
            profile;

        return volume;
    }

    static Camera FindAnyCamera()
    {
        Camera[] cameras =
            UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        return cameras.FirstOrDefault();
    }

    static Light FindDirectionalLight()
    {
        Light[] lights =
            UnityEngine.Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        return lights.FirstOrDefault(
            l => l.type == LightType.Directional);
    }

    static Volume FindAnyVolume()
    {
        Volume[] volumes =
            UnityEngine.Object.FindObjectsByType<Volume>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        return volumes.FirstOrDefault();
    }

    static GameObject CreateCube(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Material material)
    {
        GameObject go =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube);

        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;

        go.GetComponent<Renderer>().sharedMaterial =
            material;

        return go;
    }

    static GameObject CreateCylinder(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Material material)
    {
        GameObject go =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder);

        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;

        go.GetComponent<Renderer>().sharedMaterial =
            material;

        return go;
    }

    static GameObject InstantiatePrefab(
        GameObject prefab,
        Transform parent,
        string instanceName,
        Vector3 position)
    {
        GameObject go =
            PrefabUtility.InstantiatePrefab(
                prefab) as GameObject;

        if (go == null)
            throw new Exception(
                prefab.name + " Prefab 인스턴스 생성 실패");

        go.name =
            instanceName;

        go.transform.SetParent(
            parent,
            true);

        go.transform.position =
            position;

        go.transform.localScale =
            Vector3.one;

        return go;
    }

    static void ConfigureHitReference(
        GameObject instance)
    {
        ParticleSystem ps =
            instance.GetComponent<ParticleSystem>() ??
            instance.GetComponentInChildren<ParticleSystem>(true);

        if (ps == null)
            return;

        var main =
            ps.main;

        main.loop =
            false;

        main.playOnAwake =
            false;

        main.stopAction =
            ParticleSystemStopAction.None;

        ps.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    static void ConfigureHealInstance(
        GameObject instance)
    {
        ParticleSystem ps =
            instance.GetComponent<ParticleSystem>() ??
            instance.GetComponentInChildren<ParticleSystem>(true);

        if (ps == null)
            return;

        var main =
            ps.main;

        main.loop =
            true;

        main.playOnAwake =
            true;

        ps.Play(true);
    }

    static void WirePlayerInputSerialized(
        PlayerInput playerInput,
        InputActionAsset inputAsset)
    {
        SerializedObject so =
            new SerializedObject(
                playerInput);

        SerializedProperty actions =
            so.FindProperty(
                "m_Actions");

        SerializedProperty defaultMap =
            so.FindProperty(
                "m_DefaultActionMap");

        SerializedProperty behavior =
            so.FindProperty(
                "m_NotificationBehavior");

        if (actions == null ||
            defaultMap == null ||
            behavior == null)
        {
            throw new Exception(
                "현재 Input System 버전의 PlayerInput 직렬화 필드를 찾지 못했습니다.");
        }

        actions.objectReferenceValue =
            inputAsset;

        defaultMap.stringValue =
            "Gameplay";

        behavior.enumValueIndex =
            (int)PlayerNotifications.SendMessages;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void WireClickSpawner(
        Component component,
        Camera camera,
        ParticleSystem hitPrefab,
        int groundLayer)
    {
        SerializedObject so =
            new SerializedObject(
                component);

        SerializedProperty cameraP =
            so.FindProperty(
                "targetCamera");

        SerializedProperty prefabP =
            so.FindProperty(
                "effectPrefab");

        SerializedProperty maskP =
            so.FindProperty(
                "groundMask");

        if (cameraP == null ||
            prefabP == null ||
            maskP == null)
        {
            throw new Exception(
                "DAY11 ClickEffectSpawner 필드 구조가 수업 코드와 다릅니다.");
        }

        cameraP.objectReferenceValue =
            camera;

        prefabP.objectReferenceValue =
            hitPrefab;

        maskP.intValue =
            1 << groundLayer;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void WireIntensityController(
        Component component,
        VisualEffect visualEffect)
    {
        SerializedObject so =
            new SerializedObject(
                component);

        SetObjectRef(
            so,
            "visualEffect",
            visualEffect);

        SetString(
            so,
            "spawnRateName",
            "SpawnRate");

        SetFloat(
            so,
            "defaultRate",
            80f);

        SetFloat(
            so,
            "lowRate",
            20f);

        SetFloat(
            so,
            "highRate",
            200f);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetObjectRef(
        SerializedObject so,
        string name,
        UnityEngine.Object value)
    {
        SerializedProperty p =
            so.FindProperty(name);

        if (p != null)
            p.objectReferenceValue =
                value;
    }

    static void SetString(
        SerializedObject so,
        string name,
        string value)
    {
        SerializedProperty p =
            so.FindProperty(name);

        if (p != null)
            p.stringValue =
                value;
    }

    static void SetFloat(
        SerializedObject so,
        string name,
        float value)
    {
        SerializedProperty p =
            so.FindProperty(name);

        if (p != null)
            p.floatValue =
                value;
    }

    static ParticleSystem GetParticleSystem(
        GameObject prefab)
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
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    path);

            if (prefab != null &&
                prefab.name.Equals(
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return prefab;
            }
        }

        return null;
    }

    static InputActionAsset FindInputAssetWithRequiredActions()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:InputActionAsset");

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            InputActionAsset asset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    path);

            if (asset == null)
                continue;

            InputActionMap map =
                asset.FindActionMap(
                    "Gameplay",
                    false);

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

    static string FindGraphicsLabScene()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "GraphicsLab t:Scene");

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            if (Path.GetFileNameWithoutExtension(path)
                .Equals(
                    "GraphicsLab",
                    StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        return "";
    }

    static string FindFileByName(
        string name,
        string extension,
        string root)
    {
        if (!Directory.Exists(root))
            return "";

        string file =
            Directory.GetFiles(
                    root,
                    "*" + extension,
                    SearchOption.AllDirectories)
                .FirstOrDefault(
                    p => Path.GetFileNameWithoutExtension(p)
                        .Equals(
                            name,
                            StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrEmpty(file)
            ? ""
            : file.Replace("\\", "/");
    }

    static Shader LoadShaderFromGraph(
        string path)
    {
        Shader shader =
            AssetDatabase.LoadAssetAtPath<Shader>(
                path);

        if (shader != null)
            return shader;

        return AssetDatabase
            .LoadAllAssetsAtPath(path)
            .OfType<Shader>()
            .FirstOrDefault();
    }

    static Material FindMaterialUsingShader(
        Shader shader,
        string[] roots)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                roots);

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            Material mat =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            if (mat != null &&
                mat.shader == shader)
            {
                return mat;
            }
        }

        return null;
    }

    static Type FindTypeByName(
        string typeName)
    {
        foreach (Assembly asm in
                 AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type type =
                    asm.GetTypes()
                        .FirstOrDefault(
                            t => t != null &&
                                 t.Name == typeName);

                if (type != null)
                    return type;
            }
            catch (ReflectionTypeLoadException e)
            {
                Type type =
                    e.Types
                        .Where(t => t != null)
                        .FirstOrDefault(
                            t => t.Name == typeName);

                if (type != null)
                    return type;
            }
        }

        return null;
    }

    static int EnsureLayer(
        string name)
    {
        int existing =
            LayerMask.NameToLayer(
                name);

        if (existing >= 0)
            return existing;

        UnityEngine.Object tagManagerAsset =
            AssetDatabase.LoadAllAssetsAtPath(
                    "ProjectSettings/TagManager.asset")
                .FirstOrDefault();

        if (tagManagerAsset == null)
            throw new Exception(
                "TagManager.asset을 찾지 못했습니다.");

        SerializedObject tagManager =
            new SerializedObject(
                tagManagerAsset);

        SerializedProperty layers =
            tagManager.FindProperty(
                "layers");

        for (int i = 8; i < 32; i++)
        {
            SerializedProperty layer =
                layers.GetArrayElementAtIndex(
                    i);

            if (string.IsNullOrEmpty(
                    layer.stringValue))
            {
                layer.stringValue =
                    name;

                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();

                return i;
            }
        }

        throw new Exception(
            "Ground Layer용 빈 Layer Slot이 없습니다.");
    }

    static GameObject NewParent(
        string name,
        Transform parent)
    {
        GameObject go =
            new GameObject(name);

        go.transform.SetParent(
            parent);

        return go;
    }

    static void AddNote(
        Transform parent,
        string name)
    {
        GameObject go =
            new GameObject(name);

        go.transform.SetParent(parent);
    }

    static void EnsureFolder(
        string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent =
            Path.GetDirectoryName(path)
                .Replace("\\", "/");

        string name =
            Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(
            parent,
            name);
    }

    static void WriteDocumentation(
        string graphicsLab,
        string sourceShieldGraph,
        string inputAssetName)
    {
        string verification =
@"# DAY14 Graphics Portfolio - 마법 훈련장 검증 기록

## 장면 주제
**마법 훈련장 - 마력 제어 실험실**

DAY05~13에서 만든 결과물을 단순히 일렬로 전시하지 않고,
보호막 훈련 / 회복 구역 / 타격 시험 / 마력 분수라는 역할로 재배치합니다.

## 원본 보존
- Base Scene: `" + graphicsLab + @"`
- DAY05 Source Graph: `" + sourceShieldGraph + @"`
- 원본 Graph는 수정하지 않고 `SG_Portfolio_MagicShield` 복제본 사용.
- 기존 GraphicsLab 오브젝트는 `LegacyReference_Disabled` 아래 비활성 보존.

## 최종 요구사항 대응

| DAY14 요구 | 구현 |
|---|---|
| Shader Graph 1개 이상 | `SG_Portfolio_MagicShield` |
| 표면 표현 2개 이상 | DAY05 Shield의 Fresnel + Emission |
| Particle System 2개 이상 | `FX_HitSpark`, `FX_HealGlow` |
| VFX Graph 1개 이상 | `VFX_GpuSpark` |
| 코드 연동 | Impact Pad 좌클릭 -> HitSpark 1회 |
| 공간 배치 분석 | `DAY14_Spatial_Placement.md` |
| 플레이 규칙 | 한 번의 타격 클릭 -> HitSpark 한 번 |
| 검증 기록 | 이 문서 + Validate Menu |

## Hierarchy
- `Environment`
- `ShaderTargets`
  - 중앙 보호막 훈련 대상
- `ParticleEffects`
  - 왼쪽 회복 구역
  - 오른쪽 타격 시험 구역
- `VfxEffects`
  - 뒤쪽 마력 분수
- `EffectInput`
- `Verification`

## Play Mode
1. 중앙 Shield의 Fresnel/Emission 표현을 Camera 거리에서 확인.
2. 왼쪽 HealGlow가 회복 구역 역할로 보이는지 확인.
3. 오른쪽 Impact Pad를 좌클릭하여 HitSpark가 클릭 위치에 한 번 생성되는지 확인.
4. 뒤쪽 VFX_GpuSpark가 마력 분수처럼 위로 솟는지 확인.
5. 숫자 `1` -> SpawnRate 20.
6. 숫자 `2` -> SpawnRate 200.
7. Console 빨간 Error가 없는지 확인.

## Input
- Input Actions: `" + inputAssetName + @"`
- PlayerInput Default Map: Gameplay
- Behavior: Send Messages
- Point / Click / LowIntensity / HighIntensity 사용

## 성능 설명
- 프레임 저하: SpawnRate를 먼저 줄임.
- 동시에 너무 많은 입자: Lifetime 감소.
- 화면 밖 VFX 계산: Bounds 확인.
- 지나치게 밝음: Color / Alpha 조절.
- 저사양: VFX 약화 또는 비활성화.
";

        File.WriteAllText(
            DOCS + "/DAY14_Verification_Record.md",
            verification);

        string spatial =
@"# DAY14 공간 배치 분석 - 마법 훈련장

## 1. 중앙 보호막 훈련 대상
| 항목 | 기록 |
|---|---|
| 역할 | 보호/피격 대상의 시인성 확인 |
| 위치 | 장면 중앙 |
| Shader | `SG_Portfolio_MagicShield` |
| 핵심 표현 | Fresnel + Emission |
| 카메라 거리 | 정면 Camera에서 가장 먼저 읽히는 중심 대상 |

## 2. FX_HealGlow
| 항목 | 기록 |
|---|---|
| 발생 위치 | 왼쪽 회복 패드 |
| 방향 | Prefab 원본의 상승 방향 |
| 크기 | Prefab Scale `(1,1,1)` |
| 지속 시간 | 회복 구역 시연 동안 Loop |
| 게임 의미 | 지속 회복/에너지 지점 |

## 3. FX_HitSpark
| 항목 | 기록 |
|---|---|
| 발생 위치 | 오른쪽 Impact Pad의 Raycast Hit Point |
| 방향 | DAY10 Prefab의 Shape 설정 사용 |
| 크기 | Prefab Scale `(1,1,1)` |
| 지속 시간 | 짧은 One-shot |
| 게임 규칙 | 클릭이 한 번 확정될 때 HitSpark 한 번 생성 |

## 4. VFX_GpuSpark
| 항목 | 기록 |
|---|---|
| 발생 위치 | 중앙 뒤쪽 마력 분수 장치 |
| 방향 | DAY12의 위로 솟고 중력으로 떨어지는 Fountain 형태 |
| 크기 | DAY12 Size Random 값 사용 |
| 지속 시간 | 지속 VFX / 개별 Particle Lifetime 사용 |
| 성능 손잡이 | SpawnRate 20 / 80 / 200 |
| 게임 의미 | 훈련장의 마력 공급 장치 |

## 장면 색 방향
- Environment: 어두운 청회색
- Accent: 청록/하늘색 Emission
- Shield: DAY05 보호막 표현
- Hit/VFX 강조: 주황 계열 Particle
- 목적: 서로 다른 DAY 결과물을 하나의 장면처럼 읽히게 함
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

    static string ValidateInternal()
    {
        List<string> lines =
            new List<string>();

        lines.Add(
            GraphicsSettings.currentRenderPipeline != null
                ? "PASS Render Pipeline"
                : "FAIL Render Pipeline");

        SceneAsset scene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(
                FINAL_SCENE);

        lines.Add(
            scene != null
                ? "PASS Final GraphicsPortfolio Scene"
                : "FAIL Final Scene");

        lines.Add(
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                PORTFOLIO_GRAPH) != null
                ? "PASS Portfolio Shader Graph"
                : "FAIL Portfolio Shader Graph");

        lines.Add(
            AssetDatabase.LoadAssetAtPath<Material>(
                SHIELD_MAT) != null
                ? "PASS Portfolio Shield Material"
                : "FAIL Portfolio Shield Material");

        VisualEffectAsset vfxAsset =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(
                DAY12_VFX);

        lines.Add(
            vfxAsset != null
                ? "PASS DAY12 VFX_GpuSpark"
                : "FAIL DAY12 VFX_GpuSpark");

        if (SceneManager.GetActiveScene().path ==
            FINAL_SCENE)
        {
            string[] required =
            {
                "Environment",
                "ShaderTargets",
                "ParticleEffects",
                "VfxEffects",
                "EffectInput",
                "Verification"
            };

            foreach (string name in required)
            {
                lines.Add(
                    GameObject.Find(name) != null
                        ? "PASS Hierarchy " + name
                        : "FAIL Hierarchy " + name);
            }

            GameObject shield =
                GameObject.Find(
                    "MagicShield_Fresnel_Emission");

            lines.Add(
                shield != null &&
                shield.GetComponent<Renderer>() != null
                    ? "PASS Central Magic Shield"
                    : "FAIL Central Magic Shield");

            ParticleSystem[] particleSystems =
                GameObject.Find("ParticleEffects")
                    ?.GetComponentsInChildren<ParticleSystem>(true);

            lines.Add(
                particleSystems != null &&
                particleSystems.Length >= 2
                    ? "PASS Particle System 2+"
                    : "FAIL Particle System 2+");

            GameObject impactPad =
                GameObject.Find(
                    "ImpactPad_Ground");

            lines.Add(
                impactPad != null &&
                impactPad.layer ==
                    LayerMask.NameToLayer("Ground")
                    ? "PASS Impact Pad Ground Layer"
                    : "FAIL Impact Pad Ground Layer");

            VisualEffect vfx =
                GameObject.Find(
                    "VFX_GpuSpark_ManaFountain")
                    ?.GetComponent<VisualEffect>();

            lines.Add(
                vfx != null &&
                vfx.visualEffectAsset == vfxAsset
                    ? "PASS VFX Graph Mana Fountain"
                    : "FAIL VFX Graph Mana Fountain");

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

            bool playerInputOK =
                false;

            if (pi != null)
            {
                SerializedObject piSO =
                    new SerializedObject(pi);

                SerializedProperty actions =
                    piSO.FindProperty("m_Actions");

                SerializedProperty map =
                    piSO.FindProperty("m_DefaultActionMap");

                SerializedProperty behavior =
                    piSO.FindProperty("m_NotificationBehavior");

                playerInputOK =
                    actions != null &&
                    actions.objectReferenceValue != null &&
                    map != null &&
                    map.stringValue == "Gameplay" &&
                    behavior != null &&
                    behavior.enumValueIndex ==
                        (int)PlayerNotifications.SendMessages;
            }

            lines.Add(
                playerInputOK
                    ? "PASS PlayerInput / Gameplay / Send Messages"
                    : "FAIL PlayerInput");

            Type clickType =
                FindTypeByName(
                    "ClickEffectSpawner");

            Type intensityType =
                FindTypeByName(
                    "VfxIntensityController");

            lines.Add(
                inputGO != null &&
                clickType != null &&
                inputGO.GetComponent(clickType) != null
                    ? "PASS Ground Click -> HitSpark"
                    : "FAIL ClickEffectSpawner");

            lines.Add(
                inputGO != null &&
                intensityType != null &&
                inputGO.GetComponent(intensityType) != null
                    ? "PASS Key 1/2 -> SpawnRate"
                    : "FAIL VfxIntensityController");

            lines.Add(
                inputGO != null &&
                inputGO.GetComponent<DAY14MagicTrainingHUD>() != null
                    ? "PASS Theme HUD / Play Guide"
                    : "FAIL Theme HUD");
        }

        lines.Add(
            File.Exists(
                DOCS + "/DAY14_Verification_Record.md") &&
            File.Exists(
                DOCS + "/DAY14_Spatial_Placement.md")
                ? "PASS Verification / Spatial Documentation"
                : "FAIL Documentation");

        return string.Join("\n", lines);
    }
}
