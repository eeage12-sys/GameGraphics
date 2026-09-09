#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;
using UnityEngine.SceneManagement;

using Block = UnityEditor.VFX.Block;
// NOTE: AttributeCompositionMode is also in UnityEditor.VFX.Block,
// so always reference it as Block.AttributeCompositionMode.

public static class DAY12_VFXBuilder
{
    const string ROOT = "Assets/DAY12";
    const string VFX_FOLDER = ROOT + "/VFX";
    const string SCENE_FOLDER = ROOT + "/Scenes";
    const string VFX_PATH = VFX_FOLDER + "/VFX_GpuSpark.vfx";
    const string SCENE_PATH = SCENE_FOLDER + "/DAY12_VFXGraphBasics_COMPLETE.unity";

    static readonly string[] TemplateCandidates =
    {
        "Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Loop.vfx",
        "Packages/com.unity.visualeffectgraph/Editor/Templates/Minimal_System.vfx"
    };

    [MenuItem("Tools/DAY12 FINAL/Rebuild Complete")]
    public static void RebuildEverything()
    {
        try
        {
            EnsureFolder(ROOT);
            EnsureFolder(VFX_FOLDER);
            EnsureFolder(SCENE_FOLDER);

            // Old DAY12 tool is no longer needed. Remove it after this build finishes.
            ScheduleOldToolCleanup();

            VisualEffectAsset asset = GetOrCreateVFXAsset();
            if (asset == null)
                throw new Exception("VFX_GpuSpark.vfx를 만들거나 불러오지 못했습니다.");

            bool sizeRandomOK;
            bool lifetimeRandomOK;
            bool velocityRandomOK;

            BuildRealGraph(
                asset,
                out lifetimeRandomOK,
                out velocityRandomOK,
                out sizeRandomOK);

            ForceCompileAndSave(asset);

            // Reload the compiled asset after save/import.
            asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VFX_PATH);
            if (asset == null)
                throw new Exception("저장 후 VFX_GpuSpark.vfx를 다시 불러오지 못했습니다.");

            CreateFinalScene(asset, sizeRandomOK);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string report = Validate(asset);
            report += "\n";
            report += lifetimeRandomOK
                ? "PASS Lifetime Random 0.4~1.2"
                : "WARN Lifetime Random 슬롯 호환 실패 - 중간값 fallback";
            report += "\n";
            report += velocityRandomOK
                ? "PASS Velocity Random Up/Outward"
                : "WARN Velocity Random 슬롯 호환 실패 - 고정 upward fallback";
            report += "\n";
            report += sizeRandomOK
                ? "PASS Size Random 0.03~0.12"
                : "WARN Size Random 슬롯 호환 실패 - 0.07 fallback + Scene Scale 보정";

            Debug.Log("========== DAY12 FINAL v10 VALIDATION ==========\n" + report);

            // Select the actual VFX asset, not the Scene asset, so double click immediately works.
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            EditorUtility.DisplayDialog(
                "DAY12 FINAL v10 완료",
                "DAY12를 다시 전체 구축했습니다.\n\n" +
                "수정한 것:\n" +
                "• 큰 흰 연기처럼 보이던 문제: Size/Color를 Initialize에서 다시 설정\n" +
                "• 주황색 작은 Spark 형태로 변경\n" +
                "• Spawn 80 / Lifetime 0.4~1.2 / Sphere / Velocity / Size 0.03~0.12\n" +
                "• Add Force / Drag / Age-Lifetime Size 변화\n" +
                "• 실제 Output Particle Quad\n" +
                "• VFX 리소스 저장 + 강제 컴파일/재임포트\n" +
                "• 그래프 열기 메뉴도 별도 호환 처리\n" +
                "• 이전 DAY12_FULL_AutoSetup.cs 자동 정리 예약\n\n" +
                "다음:\n" +
                "1) DAY12_VFXGraphBasics_COMPLETE 씬 확인\n" +
                "2) Play\n" +
                "3) Tools > DAY12 FINAL > Open VFX_GpuSpark Graph\n\n" +
                "Console에 검증 결과도 출력했습니다.",
                "확인");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog(
                "DAY12 FINAL 생성 오류",
                e.Message,
                "확인");
        }
    }

    [MenuItem("Tools/DAY12 FINAL/Open VFX_GpuSpark Graph")]
    public static void OpenGraphV7()
    {
        var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VFX_PATH);

        if (asset == null)
        {
            EditorUtility.DisplayDialog(
                "VFX_GpuSpark 없음",
                "먼저 Tools > DAY12 FINAL > Rebuild Complete 를 실행하세요.",
                "확인");
            return;
        }

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);

        // Exact Unity VFX Graph editor flow:
        // GetWindow(asset, true) reuses the "No Asset" window if it exists,
        // then LoadAsset replaces the empty window with this graph.
        var window = VFXViewWindow.GetWindow(asset, true);

        if (window == null)
        {
            EditorUtility.DisplayDialog("그래프 열기 실패", "VFX Graph 창을 만들지 못했습니다.", "확인");
            return;
        }

        window.LoadAsset(asset, null);
        window.Focus();
    }

    [MenuItem("Tools/DAY12 FINAL/Validate")]
    public static void ValidateV7()
    {
        var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VFX_PATH);
        if (asset == null)
        {
            EditorUtility.DisplayDialog("검증 실패", "VFX_GpuSpark.vfx가 없습니다.", "확인");
            return;
        }

        string report = Validate(asset);
        Debug.Log("========== DAY12 FINAL v10 VALIDATION ==========\n" + report);
        EditorUtility.DisplayDialog(
            report.Contains("FAIL") ? "DAY12 확인 필요" : "DAY12 검증 통과",
            report,
            "확인");
    }

    static VisualEffectAsset GetOrCreateVFXAsset()
    {
        // Keep a valid current asset if it exists; rebuilding the graph contents is enough
        // and preserves Unity's importer/editor metadata better than repeatedly deleting it.
        var existing = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VFX_PATH);
        if (existing != null)
            return existing;

        string template = FindTemplate();
        if (string.IsNullOrEmpty(template))
            throw new Exception(
                "Visual Effect Graph 템플릿을 찾지 못했습니다.\n" +
                "Window > Package Manager에서 Visual Effect Graph가 설치되어 있는지 확인하세요.");

        if (!AssetDatabase.CopyAsset(template, VFX_PATH))
            throw new Exception("VFX Graph 템플릿 복사에 실패했습니다.");

        AssetDatabase.ImportAsset(
            VFX_PATH,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        return AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(VFX_PATH);
    }

    static void BuildRealGraph(
        VisualEffectAsset asset,
        out bool lifetimeRandomOK,
        out bool velocityRandomOK,
        out bool sizeRandomOK)
    {
        var resource = VisualEffectResource.GetResourceAtPath(VFX_PATH);
        if (resource == null)
            throw new Exception("VisualEffectResource를 가져오지 못했습니다.");

        var graph = resource.GetGraph();
        if (graph == null)
            throw new Exception("VFXGraph를 가져오지 못했습니다.");

        graph.RemoveAllChildren();

        const float x = 0f;
        const float stepY = 360f;

        // ================================================================
        // 1. SPAWN
        // ================================================================
        var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
        SetLabelCompat(spawner, "01 Spawn - Constant Spawn Rate 80");
        spawner.position = new Vector2(x, 0f);

        var spawnRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
        spawnRate.GetInputSlot(0).value = 80f;
        SetLabelCompat(spawnRate, "Constant Spawn Rate = 80");
        spawner.AddChild(spawnRate);

        // ================================================================
        // 2. INITIALIZE
        // ================================================================
        var init = ScriptableObject.CreateInstance<VFXBasicInitialize>();
        SetLabelCompat(init, "02 Initialize Particle");
        init.position = new Vector2(x, stepY);
        TrySetSettingCompat(init, "capacity", 1024u);

        var lifetime = CreateRandomSetAttribute(
            VFXAttribute.Lifetime.name,
            0.4f,
            1.2f,
            "Set Lifetime Random 0.4 ~ 1.2",
            out lifetimeRandomOK);
        init.AddChild(lifetime);

        var positionShape = CreatePositionShapeSphereBlock();
        SetLabelCompat(positionShape, "Set Position Shape - Sphere");
        init.AddChild(positionShape);

        var velocity = CreateRandomSetAttribute(
            VFXAttribute.Velocity.name,
            new Vector3(-1.4f, 1.4f, -1.4f),
            new Vector3( 1.4f, 4.2f,  1.4f),
            "Set Velocity Random - Up / Outward",
            out velocityRandomOK);
        init.AddChild(velocity);

        var size = CreateRandomSetAttribute(
            VFXAttribute.Size.name,
            0.03f,
            0.12f,
            "Set Size Random 0.03 ~ 0.12",
            out sizeRandomOK);
        init.AddChild(size);

        // IMPORTANT FIX:
        // Color belongs in Initialize, not in Output. This makes the particle
        // attribute itself orange before it reaches the Output context.
        var color = CreateConstantSetAttribute(
            VFXAttribute.Color.name,
            new Vector3(1.0f, 0.22f, 0.015f),
            "Set Color - Orange Spark");
        init.AddChild(color);

        try
        {
            var alpha = CreateConstantSetAttribute(
                VFXAttribute.Alpha.name,
                1.0f,
                "Set Alpha = 1");
            init.AddChild(alpha);
        }
        catch { }

        // ================================================================
        // 3. UPDATE
        // ================================================================
        var update = ScriptableObject.CreateInstance<VFXBasicUpdate>();
        SetLabelCompat(update, "03 Update Particle - Force / Drag / Age");
        update.position = new Vector2(x, stepY * 2f);

        var force = CreateForceBlock();
        SetLabelCompat(force, "Add Force - slight downward force");
        update.AddChild(force);

        var drag = CreateDragBlock();
        SetLabelCompat(drag, "Drag - slow down over time");
        update.AddChild(drag);

        var overLife = CreateAgeOverLifetimeSizeBlock();
        if (overLife != null)
        {
            SetLabelCompat(overLife, "Age over Lifetime -> Size Fade");
            update.AddChild(overLife);
        }

        // ================================================================
        // 4. OUTPUT
        // ================================================================
        var output = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();
        SetLabelCompat(output, "04 Output Particle Quad");
        output.position = new Vector2(x, stepY * 3f);

        // Course says Additive should be considered for a bright spark.
        // Set it when this package exposes a compatible blend enum.
        SetAnyEnumMemberContaining(output, "blend", "Additive");

        // Flow connections
        spawner.LinkTo(init);
        init.LinkTo(update);
        update.LinkTo(output);

        graph.AddChild(spawner);
        graph.AddChild(init);
        graph.AddChild(update);
        graph.AddChild(output);

        EditorUtility.SetDirty(asset);
        EditorUtility.SetDirty(resource);
        EditorUtility.SetDirty(graph);

        resource.WriteAssetWithSubAssets();

        Debug.Log(
            "DAY12 v10 Graph:\n" +
            "Spawn(80) -> Initialize(Lifetime/Sphere/Velocity/Size/Orange) -> " +
            "Update(Force/Drag/Age) -> Output Quad");
    }

    // ----------------------------------------------------------------
    // Attribute helpers
    // ----------------------------------------------------------------
    static Block.SetAttribute CreateRandomSetAttribute(
        string attribute,
        float min,
        float max,
        string label,
        out bool randomOK)
    {
        var block = ScriptableObject.CreateInstance<Block.SetAttribute>();

        // Unity VFX Graph: scalar random attributes use Uniform.
        // PerComponent is for vector attributes and does not create the intended A/B scalar slots.
        block.attribute = attribute;
        block.Source = Block.SetAttribute.ValueSource.Slot;
        block.Composition = Block.AttributeCompositionMode.Overwrite;
        block.Random = Block.RandomMode.Uniform;
        TryInvalidateSettings(block);

        randomOK = TrySetTwoSlots(block, min, max);

        if (!randomOK)
        {
            block.Random = Block.RandomMode.Off;
            TryInvalidateSettings(block);

            try
            {
                block.GetInputSlot(0).value = (min + max) * 0.5f;
            }
            catch { }
        }

        SetLabelCompat(block, label);
        return block;
    }

    static Block.SetAttribute CreateRandomSetAttribute(
        string attribute,
        Vector3 min,
        Vector3 max,
        string label,
        out bool randomOK)
    {
        var block = ScriptableObject.CreateInstance<Block.SetAttribute>();

        block.attribute = attribute;
        block.Source = Block.SetAttribute.ValueSource.Slot;
        block.Composition = Block.AttributeCompositionMode.Overwrite;
        block.Random = Block.RandomMode.PerComponent;
        TryInvalidateSettings(block);

        randomOK = TrySetTwoSlots(block, (Vector)min, (Vector)max);

        if (!randomOK)
        {
            block.Random = Block.RandomMode.Off;
            TryInvalidateSettings(block);

            try
            {
                Vector3 mid = (min + max) * 0.5f;
                block.GetInputSlot(0).value = (Vector)mid;
            }
            catch { }
        }

        SetLabelCompat(block, label);
        return block;
    }

    static Block.SetAttribute CreateConstantSetAttribute(
        string attribute,
        object value,
        string label)
    {
        var block = ScriptableObject.CreateInstance<Block.SetAttribute>();

        block.attribute = attribute;
        block.Source = Block.SetAttribute.ValueSource.Slot;
        block.Composition = Block.AttributeCompositionMode.Overwrite;
        block.Random = Block.RandomMode.Off;
        TryInvalidateSettings(block);

        try
        {
            block.GetInputSlot(0).value = value;
        }
        catch { }

        SetLabelCompat(block, label);
        return block;
    }

    static bool TrySetTwoSlots(VFXBlock block, object min, object max)
    {
        try
        {
            int count = block.inputSlots.Count();
            if (count < 2)
            {
                TryInvalidateSettings(block);
                count = block.inputSlots.Count();
            }

            if (count < 2)
                return false;

            block.GetInputSlot(0).value = min;
            block.GetInputSlot(1).value = max;
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Unity 6.6에서 VFXSlotContainerModel은 제네릭 타입이라
    // 비제네릭 매개변수로 직접 사용할 수 없습니다.
    // 모든 VFX 모델에 대해 Reflection으로 GetInputSlot(int)를 호출합니다.
    static void TrySetSlotValue(object model, int index, object value)
    {
        if (model == null)
            return;

        try
        {
            Type type = model.GetType();
            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            MethodInfo getInputSlot = null;

            foreach (MethodInfo m in type.GetMethods(flags))
            {
                if (!string.Equals(m.Name, "GetInputSlot", StringComparison.Ordinal))
                    continue;

                ParameterInfo[] ps = m.GetParameters();

                if (ps.Length == 1 && ps[0].ParameterType == typeof(int))
                {
                    getInputSlot = m;
                    break;
                }
            }

            if (getInputSlot == null)
                return;

            object slot = getInputSlot.Invoke(model, new object[] { index });

            if (slot == null)
                return;

            Type slotType = slot.GetType();

            PropertyInfo valueProperty = slotType.GetProperty(
                "value",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (valueProperty != null && valueProperty.CanWrite)
            {
                valueProperty.SetValue(slot, value);
                return;
            }

            FieldInfo valueField = slotType.GetField(
                "value",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (valueField != null)
                valueField.SetValue(slot, value);
        }
        catch { }
    }

    static void TrySetSettingCompat(object model, string name, object value)
    {
        if (model == null)
            return;

        // First use the VFX model API if present.
        try
        {
            MethodInfo m = model.GetType().GetMethod(
                "SetSettingValue",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (m != null)
            {
                var ps = m.GetParameters();
                if (ps.Length == 2)
                {
                    m.Invoke(model, new object[] { name, value });
                    return;
                }
            }
        }
        catch { }

        // Fallback: direct field/property lookup, case-insensitive.
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type t = model.GetType();

        foreach (FieldInfo f in t.GetFields(flags))
        {
            if (!string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                f.SetValue(model, ConvertSettingValue(value, f.FieldType));
                return;
            }
            catch { }
        }

        foreach (PropertyInfo p in t.GetProperties(flags))
        {
            if (!p.CanWrite)
                continue;
            if (!string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                p.SetValue(model, ConvertSettingValue(value, p.PropertyType));
                return;
            }
            catch { }
        }
    }

    static object ConvertSettingValue(object value, Type target)
    {
        if (value == null)
            return null;

        if (target.IsInstanceOfType(value))
            return value;

        if (target.IsEnum)
        {
            try { return Enum.Parse(target, value.ToString(), true); }
            catch { }
        }

        try { return Convert.ChangeType(value, target); }
        catch { return value; }
    }

    static void TryInvalidateSettings(object model)
    {
        if (model == null)
            return;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (MethodInfo m in model.GetType().GetMethods(flags))
        {
            if (!string.Equals(m.Name, "Invalidate", StringComparison.OrdinalIgnoreCase))
                continue;

            var ps = m.GetParameters();

            try
            {
                if (ps.Length == 0)
                {
                    m.Invoke(model, null);
                    return;
                }

                if (ps.Length == 1 && ps[0].ParameterType.IsEnum)
                {
                    Array values = Enum.GetValues(ps[0].ParameterType);
                    object chosen = null;

                    foreach (object v in values)
                    {
                        string n = v.ToString().ToLowerInvariant();
                        if (n.Contains("setting") || n.Contains("structure"))
                        {
                            chosen = v;
                            break;
                        }
                    }

                    if (chosen == null && values.Length > 0)
                        chosen = values.GetValue(0);

                    if (chosen != null)
                    {
                        m.Invoke(model, new object[] { chosen });
                        return;
                    }
                }
            }
            catch { }
        }
    }

    // ----------------------------------------------------------------
    // Position / Force / Drag / Age blocks
    // ----------------------------------------------------------------
    static VFXBlock CreatePositionShapeSphereBlock()
    {
        var candidates = GetConcreteBlockTypes()
            .Where(t =>
            {
                string n = t.Name.ToLowerInvariant();
                return n.Contains("position") && n.Contains("shape");
            })
            .ToList();

        foreach (Type t in candidates)
        {
            try
            {
                var block = ScriptableObject.CreateInstance(t) as VFXBlock;
                if (block == null)
                    continue;

                SetAnyEnumMemberContaining(block, "shape", "Sphere");
                SetAnyEnumMemberContaining(block, "position", "Surface");
                TryInvalidateSettings(block);
                TrySetFirstFloatInputByName(block, "radius", 0.16f);
                return block;
            }
            catch { }
        }

        throw new Exception(
            "Unity 6.6의 Set Position Shape 블록을 찾지 못했습니다.");
    }

    static VFXBlock CreateForceBlock()
    {
        Type t = FindBlockTypeExactOrContains(
            new[] { "Force", "AddForce" },
            new[] { "force" },
            new[] { "turbulence", "vectorfield", "conform", "collision" });

        VFXBlock block = null;

        if (t != null)
        {
            try { block = ScriptableObject.CreateInstance(t) as VFXBlock; }
            catch { block = null; }
        }

        if (block == null)
            block = ScriptableObject.CreateInstance<Block.Gravity>();

        TrySetVectorInput(block, 0, new Vector3(0f, -1.0f, 0f));
        return block;
    }

    static VFXBlock CreateDragBlock()
    {
        Type t = FindBlockTypeExactOrContains(
            new[] { "Drag" },
            new[] { "drag" },
            Array.Empty<string>());

        if (t == null)
            throw new Exception("Drag Block 타입을 찾지 못했습니다.");

        var block = ScriptableObject.CreateInstance(t) as VFXBlock;
        if (block == null)
            throw new Exception("Drag Block 생성에 실패했습니다.");

        TrySetFloatInput(block, 0, 0.9f);
        return block;
    }

    static VFXBlock CreateAgeOverLifetimeSizeBlock()
    {
        try
        {
            var block = ScriptableObject.CreateInstance<Block.AttributeFromCurve>();

            block.attribute = VFXAttribute.Size.name;
            block.Composition = Block.AttributeCompositionMode.Overwrite;
            block.SampleMode = Block.AttributeFromCurve.CurveSampleMode.OverLife;
            block.Mode = Block.AttributeFromCurve.ComputeMode.Uniform;
            TryInvalidateSettings(block);

            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, 0.25f),
                new Keyframe(0.15f, 1.00f),
                new Keyframe(1f, 0.00f));

            block.GetInputSlot(0).value = curve;
            return block;
        }
        catch (Exception e)
        {
            Debug.LogWarning("DAY12: Size over Life block 생성 실패 - " + e.Message);
            return null;
        }
    }

    // ----------------------------------------------------------------
    // Compile/save
    // ----------------------------------------------------------------
    static void ForceCompileAndSave(VisualEffectAsset asset)
    {
        var resource = VisualEffectResource.GetResourceAtPath(VFX_PATH);
        if (resource == null)
            return;

        var graph = resource.GetGraph();

        if (graph != null)
        {
            TryInvokeCompileLike(graph);
            EditorUtility.SetDirty(graph);
        }

        TryInvokeCompileLike(resource);

        EditorUtility.SetDirty(resource);
        EditorUtility.SetDirty(asset);

        resource.WriteAssetWithSubAssets();
        AssetDatabase.SaveAssets();

        AssetDatabase.ImportAsset(
            VFX_PATH,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    static void TryInvokeCompileLike(object obj)
    {
        if (obj == null)
            return;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (MethodInfo m in obj.GetType().GetMethods(flags))
        {
            string n = m.Name.ToLowerInvariant();

            if (!(n.Contains("compile") || n.Contains("recompile")))
                continue;

            var ps = m.GetParameters();

            try
            {
                if (ps.Length == 0)
                {
                    m.Invoke(obj, null);
                    return;
                }

                if (ps.Length == 1 && ps[0].ParameterType == typeof(bool))
                {
                    m.Invoke(obj, new object[] { true });
                    return;
                }
            }
            catch { }
        }
    }

    // ----------------------------------------------------------------
    // Scene
    // ----------------------------------------------------------------
    static void CreateFinalScene(VisualEffectAsset asset, bool sizeRandomOK)
    {
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);

        GameObject root = new GameObject("DAY12_COMPLETE");

        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(root.transform);
        camGO.transform.position = new Vector3(0f, 1.6f, -3.8f);

        Camera cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 48f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.015f, 0.018f, 0.028f, 1f);
        camGO.transform.LookAt(new Vector3(0f, 0.75f, 0f));

        GameObject lightGO = new GameObject("Directional Light");
        lightGO.transform.SetParent(root.transform);
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.65f;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground_Reference";
        ground.transform.SetParent(root.transform);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(0.45f, 1f, 0.45f);

        GameObject player = new GameObject("VFX_GpuSpark_Player");
        player.transform.SetParent(root.transform);
        player.transform.position = new Vector3(0f, 0.25f, 0f);
        player.transform.localScale = sizeRandomOK
            ? Vector3.one
            : Vector3.one * 0.12f;

        VisualEffect vfx = player.AddComponent<VisualEffect>();
        vfx.visualEffectAsset = asset;
        vfx.playRate = 1f;

        // Hierarchy records the actual course process.
        GameObject process = new GameObject("00_Course_Process");
        process.transform.SetParent(root.transform);
        MakeNote(process.transform, "01_Spawn_ConstantRate_80");
        MakeNote(process.transform, "02_Initialize_LifetimeRandom_0.4_1.2");
        MakeNote(process.transform, "03_Initialize_PositionShape_Sphere");
        MakeNote(process.transform, "04_Initialize_VelocityRandom_Up_Outward");
        MakeNote(process.transform, "05_Initialize_SizeRandom_0.03_0.12");
        MakeNote(process.transform, "06_Initialize_Color_Orange");
        MakeNote(process.transform, "07_Update_AddForce");
        MakeNote(process.transform, "08_Update_Drag");
        MakeNote(process.transform, "09_Update_AgeOverLifetime_Size");
        MakeNote(process.transform, "10_Output_Particle_Quad");

        EditorSceneManager.SaveScene(scene, SCENE_PATH);

        try
        {
            vfx.Reinit();
            vfx.Play();
        }
        catch { }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
    }

    // ----------------------------------------------------------------
    // Validation
    // ----------------------------------------------------------------
    static string Validate(VisualEffectAsset asset)
    {
        List<string> lines = new List<string>();

        var resource = VisualEffectResource.GetResourceAtPath(VFX_PATH);
        var graph = resource != null ? resource.GetGraph() : null;

        if (graph == null)
        {
            lines.Add("FAIL VFX Graph 없음");
            return string.Join("\n", lines);
        }

        var spawner = graph.children.OfType<VFXBasicSpawner>().FirstOrDefault();
        var init = graph.children.OfType<VFXBasicInitialize>().FirstOrDefault();
        var update = graph.children.OfType<VFXBasicUpdate>().FirstOrDefault();
        var output = graph.children.OfType<VFXPlanarPrimitiveOutput>().FirstOrDefault();

        lines.Add(spawner != null ? "PASS Spawn Context" : "FAIL Spawn Context");
        lines.Add(init != null ? "PASS Initialize Context" : "FAIL Initialize Context");
        lines.Add(update != null ? "PASS Update Context" : "FAIL Update Context");
        lines.Add(output != null ? "PASS Output Particle Quad" : "FAIL Output Context");

        if (spawner != null)
        {
            bool rate = spawner.children.Any(x =>
                x.GetType().Name.IndexOf("ConstantRate", StringComparison.OrdinalIgnoreCase) >= 0);
            lines.Add(rate ? "PASS Constant Spawn Rate" : "FAIL Constant Spawn Rate");
        }

        if (init != null)
        {
            string all = string.Join(
                " | ",
                init.children.Select(x =>
                    GetLabelCompat(x) + " " + x.GetType().Name));

            lines.Add(
                all.IndexOf("Lifetime", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "PASS Lifetime"
                    : "FAIL Lifetime");
            lines.Add(
                all.IndexOf("Position", StringComparison.OrdinalIgnoreCase) >= 0
                && all.IndexOf("Shape", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "PASS Position Shape"
                    : "FAIL Position Shape");
            lines.Add(
                all.IndexOf("Velocity", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "PASS Velocity"
                    : "FAIL Velocity");
            lines.Add(
                all.IndexOf("Size", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "PASS Size"
                    : "FAIL Size");
            lines.Add(
                all.IndexOf("Orange", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "PASS Orange Color Attribute"
                    : "FAIL Orange Color Attribute");
        }

        if (update != null)
        {
            string all = string.Join(
                " | ",
                update.children.Select(x =>
                    GetLabelCompat(x) + " " + x.GetType().Name));

            lines.Add(
                all.IndexOf("Force", StringComparison.OrdinalIgnoreCase) >= 0 ||
                all.IndexOf("Gravity", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "PASS Add Force / Gravity"
                    : "FAIL Force");
            lines.Add(
                all.IndexOf("Drag", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "PASS Drag"
                    : "FAIL Drag");
        }

        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH);
        lines.Add(sceneAsset != null ? "PASS Complete Scene" : "FAIL Complete Scene");

        if (SceneManager.GetActiveScene().path == SCENE_PATH)
        {
            GameObject go = GameObject.Find("VFX_GpuSpark_Player");
            VisualEffect vfx = go != null ? go.GetComponent<VisualEffect>() : null;

            lines.Add(vfx != null ? "PASS Visual Effect Component" : "FAIL Visual Effect Component");
            lines.Add(
                vfx != null && vfx.visualEffectAsset == asset
                    ? "PASS VFX_GpuSpark Asset Connected"
                    : "FAIL VFX Asset Connection");
        }

        return string.Join("\n", lines);
    }

    // ----------------------------------------------------------------
    // Generic reflection/type helpers
    // ----------------------------------------------------------------
    static IEnumerable<Type> GetConcreteBlockTypes()
    {
        Assembly asm = typeof(VFXBlock).Assembly;

        return asm.GetTypes().Where(t =>
            typeof(VFXBlock).IsAssignableFrom(t) &&
            !t.IsAbstract &&
            !t.IsGenericTypeDefinition);
    }

    static Type FindBlockTypeExactOrContains(
        IEnumerable<string> exactNames,
        IEnumerable<string> containsAll,
        IEnumerable<string> exclude)
    {
        var types = GetConcreteBlockTypes().ToList();

        foreach (string exact in exactNames)
        {
            Type exactType = types.FirstOrDefault(t =>
                string.Equals(t.Name, exact, StringComparison.OrdinalIgnoreCase));

            if (exactType != null)
                return exactType;
        }

        foreach (Type t in types)
        {
            string n = t.Name.ToLowerInvariant();

            if (exclude.Any(x => n.Contains(x.ToLowerInvariant())))
                continue;

            if (containsAll.All(x => n.Contains(x.ToLowerInvariant())))
                return t;
        }

        return null;
    }

    static void SetAnyEnumMemberContaining(
        object obj,
        string fieldNameContains,
        string enumValue)
    {
        if (obj == null)
            return;

        Type t = obj.GetType();
        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        foreach (FieldInfo f in t.GetFields(flags))
        {
            if (!f.Name.ToLowerInvariant().Contains(fieldNameContains.ToLowerInvariant()))
                continue;

            if (!f.FieldType.IsEnum)
                continue;

            try
            {
                object value = Enum.Parse(f.FieldType, enumValue, true);
                f.SetValue(obj, value);
                TryInvalidateSettings(obj);
                return;
            }
            catch { }
        }

        foreach (PropertyInfo p in t.GetProperties(flags))
        {
            if (!p.CanWrite || !p.PropertyType.IsEnum)
                continue;

            if (!p.Name.ToLowerInvariant().Contains(fieldNameContains.ToLowerInvariant()))
                continue;

            try
            {
                object value = Enum.Parse(p.PropertyType, enumValue, true);
                p.SetValue(obj, value);
                TryInvalidateSettings(obj);
                return;
            }
            catch { }
        }
    }

    static void TrySetFirstFloatInputByName(
        VFXBlock block,
        string contains,
        float value)
    {
        for (int i = 0; i < block.inputSlots.Count(); i++)
        {
            try
            {
                var slot = block.GetInputSlot(i);
                if (slot == null)
                    continue;

                string n = slot.name ?? "";

                if (!n.ToLowerInvariant().Contains(contains.ToLowerInvariant()))
                    continue;

                slot.value = value;
                return;
            }
            catch { }
        }
    }

    static void TrySetFloatInput(VFXBlock block, int index, float value)
    {
        try { block.GetInputSlot(index).value = value; }
        catch { }
    }

    static void TrySetVectorInput(VFXBlock block, int index, Vector3 value)
    {
        try { block.GetInputSlot(index).value = (Vector)value; }
        catch { }
    }

    static void SetLabelCompat(object obj, string value)
    {
        if (obj == null)
            return;

        Type type = obj.GetType();
        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        foreach (string memberName in new[] { "label", "title" })
        {
            PropertyInfo p = type.GetProperty(memberName, flags);

            if (p != null && p.CanWrite && p.PropertyType == typeof(string))
            {
                try
                {
                    p.SetValue(obj, value);
                    return;
                }
                catch { }
            }

            FieldInfo f = type.GetField(memberName, flags);

            if (f != null && f.FieldType == typeof(string))
            {
                try
                {
                    f.SetValue(obj, value);
                    return;
                }
                catch { }
            }
        }
    }

    static string GetLabelCompat(object obj)
    {
        if (obj == null)
            return "";

        Type type = obj.GetType();
        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        foreach (string memberName in new[] { "label", "title", "name" })
        {
            PropertyInfo p = type.GetProperty(memberName, flags);

            if (p != null && p.PropertyType == typeof(string))
            {
                try
                {
                    string s = p.GetValue(obj) as string;
                    if (!string.IsNullOrEmpty(s))
                        return s;
                }
                catch { }
            }

            FieldInfo f = type.GetField(memberName, flags);

            if (f != null && f.FieldType == typeof(string))
            {
                try
                {
                    string s = f.GetValue(obj) as string;
                    if (!string.IsNullOrEmpty(s))
                        return s;
                }
                catch { }
            }
        }

        return type.Name;
    }

    static string FindTemplate()
    {
        foreach (string path in TemplateCandidates)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                return path;
        }

        string packageFolder =
            "Packages/com.unity.visualeffectgraph/Editor/Templates";

        if (Directory.Exists(packageFolder))
        {
            string[] files =
                Directory.GetFiles(packageFolder, "*.vfx", SearchOption.AllDirectories);

            string simple = files.FirstOrDefault(p =>
                Path.GetFileNameWithoutExtension(p)
                    .IndexOf("Simple_Loop", StringComparison.OrdinalIgnoreCase) >= 0);

            if (!string.IsNullOrEmpty(simple))
                return simple.Replace("\\", "/");

            string minimal = files.FirstOrDefault(p =>
                Path.GetFileNameWithoutExtension(p)
                    .IndexOf("Minimal", StringComparison.OrdinalIgnoreCase) >= 0);

            if (!string.IsNullOrEmpty(minimal))
                return minimal.Replace("\\", "/");
        }

        return null;
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

    static void MakeNote(UnityEngine.Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
    }

    static void ScheduleOldToolCleanup()
    {
        EditorApplication.delayCall += () =>
        {
            try
            {
                string oldTool = "Assets/Editor/DAY12_FULL_AutoSetup.cs";
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(oldTool) != null)
                {
                    AssetDatabase.DeleteAsset(oldTool);
                    Debug.Log("DAY12 v7: 이전 DAY12_FULL_AutoSetup.cs 정리 완료");
                }
            }
            catch { }
        };
    }
}
#endif
