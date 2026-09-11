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

public static class DAY14_PortfolioBuilder
{
    const string ROOT = "Assets/DAY14";
    const string SHADERS = ROOT + "/Shaders";
    const string MATERIALS = ROOT + "/Materials";
    const string SETTINGS = ROOT + "/Settings";
    const string SCENES = ROOT + "/Scenes";
    const string DOCS = ROOT + "/Documentation";

    const string PORTFOLIO_GRAPH = SHADERS + "/SG_Portfolio_MagicTraining.shadergraph";
    const string PORTFOLIO_MAT = MATERIALS + "/Mat_Portfolio_MagicTraining.mat";
    const string VOLUME_PROFILE = SETTINGS + "/VP_DAY14_Portfolio.asset";
    const string SCENE_PATH = SCENES + "/GraphicsPortfolio.unity";

    const string DAY12_VFX = "Assets/DAY12/VFX/VFX_GpuSpark.vfx";
    const string DAY13_INPUT = "Assets/DAY13/Input/GraphicsInputActions_DAY13.inputactions";

    [MenuItem("Tools/DAY14 PORTFOLIO/1 - Build Complete Portfolio")]
    public static void Build()
    {
        try
        {
            EnsureFolders();

            string sourceGraph = FindFile("Assets/DAY05", "SG_Shield", ".shadergraph");
            string sourceMatPath = FindAsset("Assets/DAY05", "Mat_Shield", "t:Material");
            GameObject hitPrefab = FindPrefab("Assets/DAY10", "FX_HitSpark");
            GameObject healPrefab = FindPrefab("Assets/DAY10", "FX_HealGlow");
            VisualEffectAsset vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(DAY12_VFX);
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(DAY13_INPUT);
            Type clickSpawnerType = FindType("ClickEffectSpawner");
            Type intensityType = FindType("VfxIntensityController");

            if (string.IsNullOrEmpty(sourceGraph)) throw new Exception("DAY05 SG_Shield.shadergraph를 찾지 못했습니다.");
            if (string.IsNullOrEmpty(sourceMatPath)) throw new Exception("DAY05 Mat_Shield를 찾지 못했습니다.");
            if (hitPrefab == null || healPrefab == null) throw new Exception("DAY10 FX_HitSpark / FX_HealGlow Prefab을 찾지 못했습니다.");
            if (vfxAsset == null) throw new Exception("DAY12 VFX_GpuSpark.vfx를 찾지 못했습니다.");
            if (inputAsset == null) throw new Exception("DAY13 GraphicsInputActions_DAY13.inputactions를 찾지 못했습니다.");
            if (clickSpawnerType == null) throw new Exception("DAY11 ClickEffectSpawner.cs를 찾지 못했습니다.");
            if (intensityType == null) throw new Exception("DAY13 VfxIntensityController.cs를 찾지 못했습니다.");

            ValidateInput(inputAsset);
            CreatePortfolioGraphAndMaterial(sourceGraph, sourceMatPath);
            Material portfolioMat = AssetDatabase.LoadAssetAtPath<Material>(PORTFOLIO_MAT);
            if (portfolioMat == null) throw new Exception("DAY14 Portfolio Material 생성 실패");

            int groundLayer = EnsureLayer("Ground");
            VolumeProfile profile = GetOrCreateVolumeProfile();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("DAY14_GraphicsPortfolio");
            GameObject environment = Parent("Environment", root.transform);
            GameObject shaderTargets = Parent("ShaderTargets", root.transform);
            GameObject particleEffects = Parent("ParticleEffects", root.transform);
            GameObject vfxEffects = Parent("VfxEffects", root.transform);
            GameObject effectInput = Parent("EffectInput", root.transform);
            GameObject course = Parent("CourseVerification", root.transform);

            Camera cam = CreateCamera(environment.transform);
            CreateLight(environment.transform);
            CreateVolume(environment.transform, profile);
            CreateGround(environment.transform, groundLayer);
            CreatePillars(environment.transform);
            CreateShaderTarget(shaderTargets.transform, portfolioMat);

            CreateParticleInstance(hitPrefab, particleEffects.transform, "FX_HitSpark_Demo", new Vector3(-1.8f, .15f, .7f), false);
            CreateParticleInstance(healPrefab, particleEffects.transform, "FX_HealGlow_Ambient", new Vector3(1.8f, .10f, .7f), true);

            GameObject vfxGO = new GameObject("VFX_GpuSpark_Player");
            vfxGO.transform.SetParent(vfxEffects.transform);
            vfxGO.transform.position = new Vector3(0f, .15f, 1.8f);
            VisualEffect vfx = vfxGO.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = vfxAsset;
            if (vfx.HasFloat("SpawnRate")) vfx.SetFloat("SpawnRate", 80f);

            PlayerInput pi = effectInput.AddComponent<PlayerInput>();
            pi.actions = inputAsset;
            pi.defaultActionMap = "Gameplay";
            pi.notificationBehavior = PlayerNotifications.SendMessages;

            Component clickSpawner = effectInput.AddComponent(clickSpawnerType);
            WireClickSpawner(clickSpawner, cam, GetParticleSystem(hitPrefab), groundLayer);

            Component intensity = effectInput.AddComponent(intensityType);
            WireIntensity(intensity, vfx);

            effectInput.AddComponent<DAY14PortfolioHUD>();
            BuildCourseNotes(course.transform);

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string report = ValidateInternal();
            Debug.Log("========== DAY14 PORTFOLIO VALIDATION ==========\n" + report);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH);

            EditorUtility.DisplayDialog(
                "DAY14 Portfolio 완료",
                "마법 훈련장 주제로 통합했습니다.\n\n" +
                "Play 확인:\n" +
                "• Ground 좌클릭 = HitSpark 1회\n" +
                "• 숫자 1 = VFX SpawnRate 20\n" +
                "• 숫자 2 = VFX SpawnRate 200\n" +
                "• Shield Shader / HealGlow / GpuSpark 확인\n\n" +
                "씬: " + SCENE_PATH,
                "확인");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("DAY14 생성 오류", e.Message, "확인");
        }
    }

    [MenuItem("Tools/DAY14 PORTFOLIO/2 - Validate Portfolio")]
    public static void ValidateMenu()
    {
        string report = ValidateInternal();
        Debug.Log("========== DAY14 PORTFOLIO VALIDATION ==========\n" + report);
        EditorUtility.DisplayDialog(report.Contains("FAIL") ? "DAY14 확인 필요" : "DAY14 검증 통과", report, "확인");
    }

    [MenuItem("Tools/DAY14 PORTFOLIO/3 - Open Verification Record")]
    public static void OpenRecord()
    {
        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DOCS + "/DAY14_Verification_Record.md");
        if (asset != null) { Selection.activeObject = asset; EditorGUIUtility.PingObject(asset); AssetDatabase.OpenAsset(asset); }
    }

    static void CreatePortfolioGraphAndMaterial(string sourceGraph, string sourceMatPath)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PORTFOLIO_GRAPH) != null) AssetDatabase.DeleteAsset(PORTFOLIO_GRAPH);
        if (!AssetDatabase.CopyAsset(sourceGraph, PORTFOLIO_GRAPH)) throw new Exception("SG_Shield 복제 실패");
        AssetDatabase.ImportAsset(PORTFOLIO_GRAPH, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        Material source = AssetDatabase.LoadAssetAtPath<Material>(sourceMatPath);
        if (source == null) throw new Exception("Mat_Shield 로드 실패");

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PORTFOLIO_MAT) != null) AssetDatabase.DeleteAsset(PORTFOLIO_MAT);
        Material result = new Material(source.shader);
        EditorUtility.CopySerialized(source, result);

        Shader copiedShader = AssetDatabase.LoadAssetAtPath<Shader>(PORTFOLIO_GRAPH);
        if (copiedShader == null) copiedShader = AssetDatabase.LoadAllAssetsAtPath(PORTFOLIO_GRAPH).OfType<Shader>().FirstOrDefault();
        if (copiedShader != null) result.shader = copiedShader;

        result.name = "Mat_Portfolio_MagicTraining";
        AssetDatabase.CreateAsset(result, PORTFOLIO_MAT);
        EditorUtility.SetDirty(result);
        AssetDatabase.SaveAssets();
    }

    static VolumeProfile GetOrCreateVolumeProfile()
    {
        VolumeProfile p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VOLUME_PROFILE);
        if (p != null) return p;
        p = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(p, VOLUME_PROFILE);
        return p;
    }

    static Camera CreateCamera(Transform parent)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(0f, 3.7f, -7f);
        Camera cam = go.AddComponent<Camera>();
        cam.fieldOfView = 52f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.035f, .045f, .065f, 1f);
        go.transform.LookAt(new Vector3(0f, .9f, .6f));
        return cam;
    }

    static void CreateLight(Transform parent)
    {
        GameObject go = new GameObject("Directional Light");
        go.transform.SetParent(parent);
        go.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light l = go.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1.1f;
    }

    static void CreateVolume(Transform parent, VolumeProfile profile)
    {
        GameObject go = new GameObject("Global Volume"); go.transform.SetParent(parent);
        Volume v = go.AddComponent<Volume>(); v.isGlobal = true; v.sharedProfile = profile;
    }

    static void CreateGround(Transform parent, int groundLayer)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "TrainingGround"; g.transform.SetParent(parent); g.transform.localScale = new Vector3(1.5f,1f,1.25f); g.layer = groundLayer;
    }

    static void CreatePillars(Transform parent)
    {
        Vector3[] pos = { new Vector3(-3.4f,.8f,2.4f), new Vector3(3.4f,.8f,2.4f), new Vector3(-3.4f,.8f,-1.2f), new Vector3(3.4f,.8f,-1.2f) };
        foreach (Vector3 p in pos)
        {
            GameObject o = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            o.name = "TrainingPillar"; o.transform.SetParent(parent); o.transform.position = p; o.transform.localScale = new Vector3(.35f,.8f,.35f);
        }
    }

    static void CreateShaderTarget(Transform parent, Material mat)
    {
        GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        dummy.name = "TrainingDummy"; dummy.transform.SetParent(parent); dummy.transform.position = new Vector3(0f,1f,.2f); dummy.transform.localScale = new Vector3(.55f,1f,.55f);
        GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shield.name = "ShieldTarget_FresnelEmission"; shield.transform.SetParent(parent); shield.transform.position = new Vector3(0f,1.05f,.2f); shield.transform.localScale = Vector3.one * 1.6f;
        shield.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void CreateParticleInstance(GameObject prefab, Transform parent, string name, Vector3 pos, bool loop)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (go == null) throw new Exception(prefab.name + " 인스턴스 생성 실패");
        go.name = name; go.transform.SetParent(parent); go.transform.position = pos; go.transform.localScale = Vector3.one;
        ParticleSystem ps = go.GetComponent<ParticleSystem>() ?? go.GetComponentInChildren<ParticleSystem>();
        if (ps == null) throw new Exception(prefab.name + "에 ParticleSystem 없음");
        var main = ps.main; main.loop = loop; if (!loop) main.stopAction = ParticleSystemStopAction.None;
    }

    static ParticleSystem GetParticleSystem(GameObject prefab)
    {
        return prefab.GetComponent<ParticleSystem>() ?? prefab.GetComponentInChildren<ParticleSystem>();
    }

    static void WireClickSpawner(Component c, Camera cam, ParticleSystem prefab, int groundLayer)
    {
        SerializedObject so = new SerializedObject(c);
        SerializedProperty a = so.FindProperty("targetCamera");
        SerializedProperty b = so.FindProperty("effectPrefab");
        SerializedProperty m = so.FindProperty("groundMask");
        if (a == null || b == null || m == null) throw new Exception("DAY11 ClickEffectSpawner 필드 구성이 예상과 다릅니다.");
        a.objectReferenceValue = cam; b.objectReferenceValue = prefab; m.intValue = 1 << groundLayer; so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void WireIntensity(Component c, VisualEffect vfx)
    {
        SerializedObject so = new SerializedObject(c);
        SetObj(so,"visualEffect",vfx); SetStr(so,"spawnRateName","SpawnRate"); SetFloat(so,"defaultRate",80f); SetFloat(so,"lowRate",20f); SetFloat(so,"highRate",200f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetObj(SerializedObject so, string n, UnityEngine.Object v) { var p=so.FindProperty(n); if(p!=null)p.objectReferenceValue=v; }
    static void SetStr(SerializedObject so, string n, string v) { var p=so.FindProperty(n); if(p!=null)p.stringValue=v; }
    static void SetFloat(SerializedObject so, string n, float v) { var p=so.FindProperty(n); if(p!=null)p.floatValue=v; }

    static void BuildCourseNotes(Transform parent)
    {
        string[] names = {
            "ShaderGraph_1Plus", "Surface_Fresnel_Emission_2Plus", "ParticleSystem_HitSpark", "ParticleSystem_HealGlow",
            "VFXGraph_GpuSpark", "CodeEvent_GroundClick_HitSpark", "CodeState_1_2_SpawnRate",
            "SpatialAnalysis_Documentation", "PlayRule_OneClick_OneHitSpark", "Validation_Inspector_Graph_PlayMode"
        };
        foreach(string n in names){ GameObject g=new GameObject(n); g.transform.SetParent(parent); }
    }

    static string ValidateInternal()
    {
        List<string> r = new List<string>();
        r.Add(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PORTFOLIO_GRAPH)!=null ? "PASS Portfolio Shader Graph" : "FAIL Portfolio Shader Graph");
        r.Add(AssetDatabase.LoadAssetAtPath<Material>(PORTFOLIO_MAT)!=null ? "PASS Portfolio Material" : "FAIL Portfolio Material");
        r.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH)!=null ? "PASS GraphicsPortfolio Scene" : "FAIL GraphicsPortfolio Scene");
        VisualEffectAsset vfxAsset=AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(DAY12_VFX);
        InputActionAsset input=AssetDatabase.LoadAssetAtPath<InputActionAsset>(DAY13_INPUT);
        r.Add(vfxAsset!=null ? "PASS DAY12 VFX_GpuSpark" : "FAIL DAY12 VFX_GpuSpark");
        r.Add(input!=null ? "PASS DAY13 Input Actions" : "FAIL DAY13 Input Actions");

        if(SceneManager.GetActiveScene().path==SCENE_PATH)
        {
            foreach(string n in new[]{"Environment","ShaderTargets","ParticleEffects","VfxEffects","EffectInput"})
                r.Add(GameObject.Find(n)!=null ? "PASS Hierarchy "+n : "FAIL Hierarchy "+n);
            r.Add(GameObject.Find("FX_HitSpark_Demo")!=null && GameObject.Find("FX_HealGlow_Ambient")!=null ? "PASS Particle System 2+" : "FAIL Particle System 2+");
            GameObject vg=GameObject.Find("VFX_GpuSpark_Player"); VisualEffect v=vg!=null?vg.GetComponent<VisualEffect>():null;
            r.Add(v!=null && v.visualEffectAsset==vfxAsset ? "PASS Visual Effect Graph" : "FAIL Visual Effect Graph");
            if(v!=null) r.Add(v.HasFloat("SpawnRate") ? "PASS SpawnRate Exposed" : "FAIL SpawnRate Exposed");
            GameObject ei=GameObject.Find("EffectInput"); PlayerInput pi=ei!=null?ei.GetComponent<PlayerInput>():null;
            r.Add(pi!=null && pi.notificationBehavior==PlayerNotifications.SendMessages ? "PASS PlayerInput Send Messages" : "FAIL PlayerInput");
            Type ct=FindType("ClickEffectSpawner"); Type it=FindType("VfxIntensityController");
            r.Add(ei!=null && ct!=null && ei.GetComponent(ct)!=null ? "PASS ClickEffectSpawner" : "FAIL ClickEffectSpawner");
            r.Add(ei!=null && it!=null && ei.GetComponent(it)!=null ? "PASS VfxIntensityController" : "FAIL VfxIntensityController");
        }
        r.Add(File.Exists(DOCS+"/DAY14_Verification_Record.md") && File.Exists(DOCS+"/DAY14_Spatial_Placement.md") ? "PASS Documentation" : "FAIL Documentation");
        return string.Join("\n",r);
    }

    static void ValidateInput(InputActionAsset input)
    {
        InputActionMap map=input.FindActionMap("Gameplay",false); if(map==null) throw new Exception("Gameplay Action Map 없음");
        foreach(string n in new[]{"Point","Click","LowIntensity","HighIntensity"}) if(map.FindAction(n,false)==null) throw new Exception("Input Action 없음: "+n);
    }

    static string FindAsset(string root,string name,string filter)
    {
        foreach(string g in AssetDatabase.FindAssets(name+" "+filter,new[]{root}))
        { string p=AssetDatabase.GUIDToAssetPath(g); if(Path.GetFileNameWithoutExtension(p).Equals(name,StringComparison.OrdinalIgnoreCase)) return p; }
        return "";
    }

    static string FindFile(string root,string name,string ext)
    {
        if(!Directory.Exists(root)) return "";
        string p=Directory.GetFiles(root,"*"+ext,SearchOption.AllDirectories).FirstOrDefault(x=>Path.GetFileNameWithoutExtension(x).Equals(name,StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(p)?"":p.Replace("\\","/");
    }

    static GameObject FindPrefab(string root,string name)
    {
        foreach(string g in AssetDatabase.FindAssets(name+" t:Prefab",new[]{root}))
        { string p=AssetDatabase.GUIDToAssetPath(g); GameObject o=AssetDatabase.LoadAssetAtPath<GameObject>(p); if(o!=null && o.name.Equals(name,StringComparison.OrdinalIgnoreCase)) return o; }
        return null;
    }

    static Type FindType(string name)
    {
        foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies())
        { try { Type t=a.GetTypes().FirstOrDefault(x=>x.Name==name); if(t!=null)return t; } catch(ReflectionTypeLoadException e){ Type t=e.Types.Where(x=>x!=null).FirstOrDefault(x=>x.Name==name); if(t!=null)return t; } }
        return null;
    }

    static GameObject Parent(string n,Transform p){ GameObject g=new GameObject(n); g.transform.SetParent(p); return g; }

    static void EnsureFolders(){ foreach(string p in new[]{ROOT,SHADERS,MATERIALS,SETTINGS,SCENES,DOCS,ROOT+"/Scripts"}) EnsureFolder(p); }
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=Path.GetDirectoryName(path).Replace("\\","/"); string name=Path.GetFileName(path);
        if(!AssetDatabase.IsValidFolder(parent))EnsureFolder(parent); AssetDatabase.CreateFolder(parent,name);
    }

    static int EnsureLayer(string layerName)
    {
        int existing=LayerMask.NameToLayer(layerName); if(existing>=0)return existing;
        UnityEngine.Object tm=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault(); if(tm==null)throw new Exception("TagManager.asset 없음");
        SerializedObject so=new SerializedObject(tm); SerializedProperty layers=so.FindProperty("layers");
        for(int i=8;i<32;i++){ SerializedProperty p=layers.GetArrayElementAtIndex(i); if(string.IsNullOrEmpty(p.stringValue)){p.stringValue=layerName;so.ApplyModifiedProperties();AssetDatabase.SaveAssets();return i;} }
        throw new Exception("Ground Layer 빈 슬롯 없음");
    }
}
