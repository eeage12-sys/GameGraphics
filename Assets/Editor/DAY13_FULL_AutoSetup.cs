using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class DAY13_FULL_AutoSetup
{
    const string ROOT = "Assets/DAY13";
    const string INPUT_PATH = ROOT + "/Input/GraphicsInputActions_DAY13.inputactions";
    const string SCENES = ROOT + "/Scenes";
    const string SCENE_PATH = SCENES + "/DAY13_VFXControlPerformance_COMPLETE.unity";
    const string DAY12_VFX = "Assets/DAY12/VFX/VFX_GpuSpark.vfx";

    [MenuItem("Tools/DAY13 FULL/1 - Build Control + Performance Scene")]
    public static void BuildScene()
    {
        try
        {
            EnsureFolder(ROOT);
            EnsureFolder(ROOT + "/Input");
            EnsureFolder(ROOT + "/Scripts");
            EnsureFolder(ROOT + "/Notes");
            EnsureFolder(SCENES);

            AssetDatabase.ImportAsset(
                INPUT_PATH,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            InputActionAsset actions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_PATH);

            if (actions == null)
                throw new Exception("DAY13 Input Actions Asset을 불러오지 못했습니다.");

            ValidateInputActions(actions, true);

            VisualEffectAsset vfxAsset =
                AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(DAY12_VFX);

            if (vfxAsset == null)
                throw new Exception(
                    "DAY12의 VFX_GpuSpark.vfx를 찾지 못했습니다.\n" +
                    "Assets/DAY12/VFX/VFX_GpuSpark.vfx가 있는지 확인하세요.");

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            GameObject root = new GameObject("DAY13_COMPLETE");
            CreateCamera(root.transform);
            CreateGround(root.transform);
            CreateLight(root.transform);

            GameObject vfxGO = new GameObject("VFX_GpuSpark_Player");
            vfxGO.transform.SetParent(root.transform);
            vfxGO.transform.position = new Vector3(0f, 0.25f, 0f);

            VisualEffect visualEffect = vfxGO.AddComponent<VisualEffect>();
            visualEffect.visualEffectAsset = vfxAsset;
            visualEffect.playRate = 1f;

            GameObject controllerGO = new GameObject("VfxController");
            controllerGO.transform.SetParent(root.transform);

            PlayerInput playerInput = controllerGO.AddComponent<PlayerInput>();
            playerInput.actions = actions;
            playerInput.defaultActionMap = "Gameplay";
            playerInput.notificationBehavior = PlayerNotifications.SendMessages;

            VfxIntensityController controller =
                controllerGO.AddComponent<VfxIntensityController>();

            SerializedObject controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("visualEffect").objectReferenceValue = visualEffect;
            controllerSO.FindProperty("spawnRateName").stringValue = "SpawnRate";
            controllerSO.FindProperty("defaultRate").floatValue = 80f;
            controllerSO.FindProperty("lowRate").floatValue = 20f;
            controllerSO.FindProperty("highRate").floatValue = 200f;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            BuildProcessHierarchy(root.transform);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool spawnRateReady = HasExposedFloat(vfxAsset, "SpawnRate");

            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH);

            if (!spawnRateReady)
            {
                EditorUtility.DisplayDialog(
                    "DAY13 - SpawnRate 연결 1단계 남음",
                    "씬/코드/Input System/Low 20/High 200 연결까지는 완료했습니다.\n\n" +
                    "DAY12 VFX Graph에서 딱 한 번만 직접 해야 합니다:\n\n" +
                    "1. Blackboard + > Float\n" +
                    "2. 이름 SpawnRate / 기본값 80\n" +
                    "3. Exposed ON\n" +
                    "4. SpawnRate를 Graph Area로 드래그\n" +
                    "5. Constant Spawn Rate의 Rate 입력에 연결\n" +
                    "6. Ctrl+S\n\n" +
                    "그 다음 Tools > DAY13 FULL > 3 - Validate DAY13 을 누르세요.\n\n" +
                    "※ DAY12 그래프를 다시 망가뜨리지 않기 위해 VFX 내부 비공개 API로\n" +
                    "자동 노드 삽입은 하지 않습니다.",
                    "확인");

                AssetDatabase.OpenAsset(vfxAsset);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "DAY13 구축 완료",
                    "SpawnRate Exposed Property까지 확인됐습니다.\n\n" +
                    "Play 후\n1 = Low (20)\n2 = High (200)\n를 눌러 입자 밀도 차이를 확인하세요.",
                    "확인");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("DAY13 생성 오류", e.Message, "확인");
        }
    }

    [MenuItem("Tools/DAY13 FULL/2 - Open DAY12 VFX Graph")]
    public static void OpenVFXGraph()
    {
        VisualEffectAsset asset =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(DAY12_VFX);

        if (asset == null)
        {
            EditorUtility.DisplayDialog(
                "VFX 없음",
                "Assets/DAY12/VFX/VFX_GpuSpark.vfx를 찾지 못했습니다.",
                "확인");
            return;
        }

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        AssetDatabase.OpenAsset(asset);
    }

    [MenuItem("Tools/DAY13 FULL/3 - Validate DAY13")]
    public static void ValidateDAY13()
    {
        try
        {
            List<string> lines = new List<string>();

            InputActionAsset actions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_PATH);

            if (actions == null)
            {
                lines.Add("FAIL GraphicsInputActions_DAY13 없음");
            }
            else
            {
                AddInputValidation(lines, actions);
            }

            VisualEffectAsset vfxAsset =
                AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(DAY12_VFX);

            if (vfxAsset == null)
            {
                lines.Add("FAIL DAY12 VFX_GpuSpark 없음");
            }
            else
            {
                bool spawnRate =
                    HasExposedFloat(vfxAsset, "SpawnRate");

                lines.Add(
                    spawnRate
                        ? "PASS SpawnRate Float Exposed Property"
                        : "FAIL SpawnRate가 Exposed Float로 등록되지 않음");
            }

            SceneAsset sceneAsset =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH);

            lines.Add(
                sceneAsset != null
                    ? "PASS DAY13 COMPLETE Scene"
                    : "FAIL DAY13 COMPLETE Scene 없음");

            if (SceneManager.GetActiveScene().path == SCENE_PATH)
            {
                GameObject controllerGO = GameObject.Find("VfxController");

                if (controllerGO == null)
                {
                    lines.Add("FAIL VfxController 없음");
                }
                else
                {
                    PlayerInput pi =
                        controllerGO.GetComponent<PlayerInput>();

                    VfxIntensityController c =
                        controllerGO.GetComponent<VfxIntensityController>();

                    bool piOK =
                        pi != null &&
                        pi.actions == actions &&
                        pi.defaultActionMap == "Gameplay" &&
                        pi.notificationBehavior == PlayerNotifications.SendMessages;

                    lines.Add(
                        piOK
                            ? "PASS PlayerInput / Gameplay / Send Messages"
                            : "FAIL PlayerInput 연결");

                    lines.Add(
                        c != null
                            ? "PASS VfxIntensityController"
                            : "FAIL VfxIntensityController 없음");
                }

                GameObject vfxGO = GameObject.Find("VFX_GpuSpark_Player");
                VisualEffect vfx =
                    vfxGO != null ? vfxGO.GetComponent<VisualEffect>() : null;

                lines.Add(
                    vfx != null && vfx.visualEffectAsset == vfxAsset
                        ? "PASS VisualEffect + VFX_GpuSpark 연결"
                        : "FAIL VisualEffect 연결");
            }

            string report = string.Join("\n", lines);
            bool pass = !report.Contains("FAIL");

            Debug.Log("========== DAY13 VALIDATION ==========\n" + report);

            EditorUtility.DisplayDialog(
                pass ? "DAY13 검증 통과" : "DAY13 확인 필요",
                report,
                "확인");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("DAY13 검증 오류", e.Message, "확인");
        }
    }

    static void ValidateInputActions(InputActionAsset actions, bool throwOnFail)
    {
        var map = actions.FindActionMap("Gameplay", false);
        var low = map != null ? map.FindAction("LowIntensity", false) : null;
        var high = map != null ? map.FindAction("HighIntensity", false) : null;

        bool ok =
            map != null &&
            low != null &&
            high != null &&
            low.type == InputActionType.Button &&
            high.type == InputActionType.Button &&
            low.bindings.Any(b => b.path == "<Keyboard>/1") &&
            high.bindings.Any(b => b.path == "<Keyboard>/2");

        if (!ok && throwOnFail)
            throw new Exception(
                "DAY13 Input Actions 설정이 올바르지 않습니다.\n" +
                "Gameplay / LowIntensity(<Keyboard>/1) / " +
                "HighIntensity(<Keyboard>/2)를 확인하세요.");
    }

    static void AddInputValidation(
        List<string> lines,
        InputActionAsset actions)
    {
        var map = actions.FindActionMap("Gameplay", false);
        lines.Add(map != null ? "PASS Gameplay Action Map" : "FAIL Gameplay Action Map");

        if (map == null)
            return;

        var low = map.FindAction("LowIntensity", false);
        var high = map.FindAction("HighIntensity", false);

        bool lowOK =
            low != null &&
            low.type == InputActionType.Button &&
            low.bindings.Any(b => b.path == "<Keyboard>/1");

        bool highOK =
            high != null &&
            high.type == InputActionType.Button &&
            high.bindings.Any(b => b.path == "<Keyboard>/2");

        lines.Add(
            lowOK
                ? "PASS LowIntensity / Button / Keyboard 1"
                : "FAIL LowIntensity");
        lines.Add(
            highOK
                ? "PASS HighIntensity / Button / Keyboard 2"
                : "FAIL HighIntensity");
    }

    static bool HasExposedFloat(
        VisualEffectAsset asset,
        string propertyName)
    {
        if (asset == null)
            return false;

        List<VFXExposedProperty> props =
            new List<VFXExposedProperty>();

        asset.GetExposedProperties(props);

        return props.Any(p =>
            p.name == propertyName &&
            p.type == typeof(float));
    }

    static void BuildProcessHierarchy(UnityEngine.Transform parent)
    {
        GameObject flow = new GameObject("00_ControlFlow");
        flow.transform.SetParent(parent);
        Note(flow.transform, "01_PlayerInput_Action");
        Note(flow.transform, "02_OnLowIntensity_or_OnHighIntensity");
        Note(flow.transform, "03_VisualEffect_SetFloat_SpawnRate");
        Note(flow.transform, "04_VFX_Blackboard_SpawnRate");
        Note(flow.transform, "05_Constant_Spawn_Rate");
        Note(flow.transform, "06_Particle_Count_Changes");

        GameObject properties = new GameObject("01_ExposedProperty_Check");
        properties.transform.SetParent(parent);
        Note(properties.transform, "SpawnRate_Float_Exposed");
        Note(properties.transform, "Reference_Name_CaseSensitive");
        Note(properties.transform, "Default_80");
        Note(properties.transform, "Connected_to_ConstantSpawnRate");

        GameObject input = new GameObject("02_Input_Check");
        input.transform.SetParent(parent);
        Note(input.transform, "LowIntensity_Button_Keyboard1");
        Note(input.transform, "HighIntensity_Button_Keyboard2");
        Note(input.transform, "PlayerInput_SendMessages");
        Note(input.transform, "OnLowIntensity");
        Note(input.transform, "OnHighIntensity");

        GameObject performance = new GameObject("03_Performance_Check");
        performance.transform.SetParent(parent);
        Note(performance.transform, "Low_SpawnRate_20");
        Note(performance.transform, "Default_SpawnRate_80");
        Note(performance.transform, "High_SpawnRate_200");
        Note(performance.transform, "Lifetime_Compare");
        Note(performance.transform, "Bounds_Check");
        Note(performance.transform, "Output_Color_Alpha_Check");
        Note(performance.transform, "Quality_Toggle_Check");
        Note(performance.transform, "AliveParticleCount_Overlay");
    }

    static Camera CreateCamera(UnityEngine.Transform parent)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(0f, 1.7f, -4.3f);

        Camera cam = go.AddComponent<Camera>();
        cam.fieldOfView = 50f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.015f, 0.018f, 0.028f, 1f);
        go.transform.LookAt(new Vector3(0f, 0.75f, 0f));

        return cam;
    }

    static void CreateGround(UnityEngine.Transform parent)
    {
        GameObject ground =
            GameObject.CreatePrimitive(PrimitiveType.Plane);

        ground.name = "Ground_Reference";
        ground.transform.SetParent(parent);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale =
            new Vector3(0.55f, 1f, 0.55f);
    }

    static void CreateLight(UnityEngine.Transform parent)
    {
        GameObject go = new GameObject("Directional Light");
        go.transform.SetParent(parent);
        go.transform.rotation =
            Quaternion.Euler(50f, -30f, 0f);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.65f;
    }

    static void Note(UnityEngine.Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
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
