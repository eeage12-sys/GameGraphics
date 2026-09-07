using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable UAC0005

// Unity 6.x URP 내부 구현 기준: UniversalTarget.TrySetActiveSubTarget(Type)을 사용합니다.
public static class DAY08_FULL_ShaderGraphBuilder
{
    const string ROOT = "Assets/DAY08";
    const string GRAPHS = ROOT + "/Graphs";
    const string MATS = ROOT + "/Materials";
    const string PREFABS = ROOT + "/Prefabs";
    const string SCENES = ROOT + "/Scenes";
    const string SETTINGS = ROOT + "/Settings";
    const string SCENE_PATH = SCENES + "/DAY08_NonPhotoreal_COMPLETE.unity";

    static readonly List<string> report = new List<string>();

    [MenuItem("Tools/DAY08 FULL/00 - Check Unity 6.6 Templates")]
    public static void CheckTemplates()
    {
        string a=FindShaderGraph("SG_ColorPulse");
        string b=FindShaderGraph("SG_Shield");
        if(string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            EditorUtility.DisplayDialog("DAY08 FULL",
                "SG_ColorPulse.shadergraph 또는 SG_Shield.shadergraph를 찾지 못했습니다.", "확인");
            return;
        }
        EditorUtility.DisplayDialog("DAY08 FULL",
            "템플릿 확인 완료\\n\\n"+a+"\\n"+b+"\\n\\n이제 Build ALL을 실행하면 됩니다.", "확인");
    }

    [MenuItem("Tools/DAY08 FULL/Build ALL Shader Graph Coursework")]
    public static void BuildAll()
    {
        report.Clear();
        report.Add("Builder v7 / Unity 6.6 official TrySetActiveSubTarget");
        EnsureFolders();

        string donor = FindShaderGraph("SG_ColorPulse");
        if (string.IsNullOrEmpty(donor))
            donor = FindAnyShaderGraph();

        if (string.IsNullOrEmpty(donor))
        {
            EditorUtility.DisplayDialog("DAY08 FULL", 
                "실제 Shader Graph 포맷 템플릿을 찾지 못했습니다.\nDAY04의 SG_ColorPulse.shadergraph가 프로젝트에 있어야 합니다.", "확인");
            return;
        }

        try
        {
            BuildToonBand(donor, GRAPHS + "/SG_ToonBand.shadergraph");
            BuildToonRim(donor, GRAPHS + "/SG_ToonRim.shadergraph");
            BuildOutlineShell(donor, GRAPHS + "/SG_OutlineShell.shadergraph");
            BuildScreenOutline(donor, GRAPHS + "/SG_ScreenOutline.shadergraph");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("DAY08 Shader Graph 생성 오류",
                "그래프 생성 중 오류가 발생했습니다.\nConsole의 첫 오류를 보여주면 해당 Unity 버전에 맞춰 수정할 수 있습니다.\n\n" + e.Message, "확인");
            return;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        CreateMaterials();
        CreatePrefabs();
        CreateVolumeProfile();
        CreateScene();
        TryInstallRendererFeatures();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("DAY08 FULL BUILD REPORT\n" + string.Join("\n", report));
        EditorUtility.DisplayDialog("DAY08 FULL 완료",
            "DAY08 전체 실습 자산 생성을 완료했습니다.\n\n" +
            "Assets/DAY08/Graphs에서 실제 .shadergraph 4개를 더블클릭해 노드를 확인하세요.\n" +
            "Assets/DAY08/Scenes/DAY08_NonPhotoreal_COMPLETE 씬에서 결과를 비교할 수 있습니다.\n\n" +
            "Renderer Feature는 프로젝트 Renderer 구성에 따라 자동 추가가 제한될 수 있으니 Console REPORT도 확인하세요.",
            "확인");
    }

    // ---------- Graph bootstrap / serialization ----------
    static void BuildToonBand(string donor, string dst)
    {
        var g = LoadGraphClone(donor, dst);
        SwitchUniversalSubTarget(g, "UniversalUnlitSubTarget");
        ClearGraphUserContent(g);
        if(FindBlock(g,"BaseColor")==null)
            throw new Exception("Unlit 전환 후 Base Color Block을 찾지 못했습니다. Unity 6.6 Target 전환 상태를 확인해야 합니다.");

        var lit = AddColorProperty(g, "LitColor", "_LitColor", new Color(1f, .78f, .2f, 1f));
        var shadow = AddColorProperty(g, "ShadowColor", "_ShadowColor", new Color(.24f, .08f, .45f, 1f));
        var lightDir = AddVector3Property(g, "LightDirectionWS", "_LightDirectionWS", new Vector3(.3f,.8f,.4f));
        var threshold = AddFloatProperty(g, "BandThreshold", "_BandThreshold", .5f);

        var nrm = AddNode(g, "NormalVectorNode", -900, -120);
        SetMember(nrm, new[]{"space","m_Space"}, EnumValue(GetMemberType(nrm,new[]{"space","m_Space"}), "World", 2));

        var normalize = AddNode(g, "NormalizeNode", -900, 170);
        var dot = AddNode(g, "DotProductNode", -620, 0);
        var remap = AddNode(g, "RemapNode", -360, 0);
        var step = AddNode(g, "StepNode", -90, 0);
        var lerp = AddNode(g, "LerpNode", 180, 0);

        var pLit = AddPropertyNode(g, lit, -80, 240);
        var pShadow = AddPropertyNode(g, shadow, -80, 330);
        var pDir = AddPropertyNode(g, lightDir, -1150, 180);
        var pThreshold = AddPropertyNode(g, threshold, -360, 250);

        Connect(g, pDir, "Out", normalize, "In");
        Connect(g, nrm, "Out", dot, "A");
        Connect(g, normalize, "Out", dot, "B");
        Connect(g, dot, "Out", remap, "In");
        SetSlotDefault(remap, "In Min Max", new Vector2(-1,1));
        SetSlotDefault(remap, "Out Min Max", new Vector2(0,1));
        Connect(g, pThreshold, "Out", step, "Edge");
        Connect(g, remap, "Out", step, "In");
        Connect(g, pShadow, "Out", lerp, "A");
        Connect(g, pLit, "Out", lerp, "B");
        Connect(g, step, "Out", lerp, "T");
        ConnectToBlock(g, lerp, "Out", "BaseColor");

        SaveGraph(g, dst);
        report.Add("OK SG_ToonBand");
    }

    static void BuildToonRim(string donor, string dst)
    {
        // ToonBand를 실제 그래프로 만든 후 복제/확장한다는 수업 과정도 파일 구조상 보존
        string tempBand = GRAPHS + "/SG_ToonBand.shadergraph";
        if (!File.Exists(tempBand)) BuildToonBand(donor, tempBand);
        var g = LoadGraphClone(tempBand, dst);

        var rimColor = AddColorProperty(g, "RimColor", "_RimColor", new Color(0f,1f,.95f,1f));
        var rimPower = AddFloatProperty(g, "RimPower", "_RimPower", 3f);
        var rimIntensity = AddFloatProperty(g, "RimIntensity", "_RimIntensity", 2f);

        var fresnel = AddNode(g, "FresnelNode", 130, 430);
        var mul1 = AddNode(g, "MultiplyNode", 390, 400);
        var mul2 = AddNode(g, "MultiplyNode", 650, 400);
        var add = AddNode(g, "AddNode", 920, 160);

        var pColor = AddPropertyNode(g, rimColor, -120, 520);
        var pPower = AddPropertyNode(g, rimPower, -120, 430);
        var pIntensity = AddPropertyNode(g, rimIntensity, 410, 610);

        Connect(g, pPower, "Out", fresnel, "Power");
        Connect(g, pColor, "Out", mul1, "A");
        Connect(g, fresnel, "Out", mul1, "B");
        Connect(g, mul1, "Out", mul2, "A");
        Connect(g, pIntensity, "Out", mul2, "B");

        // 기존 ToonBand -> BaseColor 연결의 출력 노드를 찾아 Add A로 재연결
        var baseInput = FindBlock(g, "BaseColor");
        var prior = FindConnectedOutputNode(g, baseInput);
        DisconnectInput(g, baseInput);
        if (prior != null) Connect(g, prior, "Out", add, "A");
        Connect(g, mul2, "Out", add, "B");
        ConnectToBlock(g, add, "Out", "BaseColor");

        SaveGraph(g, dst);
        report.Add("OK SG_ToonRim");
    }

    static void BuildOutlineShell(string donor, string dst)
    {
        var g = LoadGraphClone(donor, dst);
        SwitchUniversalSubTarget(g, "UniversalUnlitSubTarget");
        ClearGraphUserContent(g);
        if(FindBlock(g,"BaseColor")==null || FindBlock(g,"Position")==null)
            throw new Exception("Outline Graph용 Master Stack Block(Base Color / Position)을 찾지 못했습니다.");

        var color = AddColorProperty(g, "OutlineColor", "_OutlineColor", Hex("#11152A"));
        var width = AddFloatProperty(g, "OutlineWidth", "_OutlineWidth", .03f);

        var pos = AddNode(g, "PositionNode", -850, -100);
        var nrm = AddNode(g, "NormalVectorNode", -850, 170);
        SetSpaceObject(pos);
        SetSpaceObject(nrm);

        var mul = AddNode(g, "MultiplyNode", -500, 170);
        var add = AddNode(g, "AddNode", -200, 0);
        var pWidth = AddPropertyNode(g, width, -780, 410);
        var pColor = AddPropertyNode(g, color, -160, 310);

        Connect(g, nrm, "Out", mul, "A");
        Connect(g, pWidth, "Out", mul, "B");
        Connect(g, pos, "Out", add, "A");
        Connect(g, mul, "Out", add, "B");
        ConnectToBlock(g, add, "Out", "VertexDescription.Position", "Position");
        ConnectToBlock(g, pColor, "Out", "BaseColor");

        SetUniversalRenderFace(g, "Back");
        SaveGraph(g, dst);
        report.Add("OK SG_OutlineShell");
    }

    static void BuildScreenOutline(string donor, string dst)
    {
        var g = LoadGraphClone(donor, dst);
        SwitchUniversalSubTarget(g, "UniversalFullscreenSubTarget");
        ClearGraphUserContent(g);
        if(FindBlock(g,"BaseColor")==null)
            report.Add("WARN Fullscreen 전환 직후 BaseColor Block 미검출 - 최종 Validate에서 Fullscreen Block 생성 여부 확인");

        var outline = AddColorProperty(g, "OutlineColor", "_OutlineColor", Hex("#14213D"));
        var width = AddFloatProperty(g, "OutlineWidthPixels", "_OutlineWidthPixels", 1f);
        var normalT = AddFloatProperty(g, "NormalThreshold", "_NormalThreshold", .25f);
        var depthT = AddFloatProperty(g, "DepthThreshold", "_DepthThreshold", .5f);

        var screenPos = AddNode(g, "ScreenPositionNode", -1800, -100);
        var splitUV = AddNode(g, "SplitNode", -1560, -100);
        var baseUV = AddNode(g, "CombineNode", -1300, -100);
        Connect(g, screenPos, "Out", splitUV, "In");
        Connect(g, splitUV, "R", baseUV, "R", "X");
        Connect(g, splitUV, "G", baseUV, "G", "Y");

        var screen = AddNode(g, "ScreenNode", -1800, 260);
        var recipW = AddNode(g, "ReciprocalNode", -1510, 210);
        var recipH = AddNode(g, "ReciprocalNode", -1510, 330);
        var combineTexel = AddNode(g, "CombineNode", -1250, 260);
        Connect(g, screen, "Width", recipW, "In");
        Connect(g, screen, "Height", recipH, "In");
        Connect(g, recipW, "Out", combineTexel, "R", "X");
        Connect(g, recipH, "Out", combineTexel, "G", "Y");

        var pWidth = AddPropertyNode(g, width, -1260, 500);
        var mulOffset = AddNode(g, "MultiplyNode", -990, 290);
        Connect(g, combineTexel, "RG", mulOffset, "A");
        Connect(g, pWidth, "Out", mulOffset, "B");

        var splitOffset = AddNode(g, "SplitNode", -740, 290);
        Connect(g, mulOffset, "Out", splitOffset, "In");

        // Four directional offset vectors + UV additions
        var negX = AddNode(g, "MultiplyNode", -720, 470);
        var negY = AddNode(g, "MultiplyNode", -720, 560);
        Connect(g, splitOffset, "R", negX, "A");
        Connect(g, splitOffset, "G", negY, "A");
        SetSlotDefault(negX, "B", -1f);
        SetSlotDefault(negY, "B", -1f);

        var rightV = AddNode(g, "CombineNode", -500, -80);
        var leftV  = AddNode(g, "CombineNode", -500, 80);
        var upV    = AddNode(g, "CombineNode", -500, 240);
        var downV  = AddNode(g, "CombineNode", -500, 400);

        Connect(g, splitOffset, "R", rightV, "R", "X");
        Connect(g, negX, "Out", leftV, "R", "X");
        Connect(g, splitOffset, "G", upV, "G", "Y");
        Connect(g, negY, "Out", downV, "G", "Y");

        var uvR = AddNode(g, "AddNode", -220, -80);
        var uvL = AddNode(g, "AddNode", -220, 80);
        var uvU = AddNode(g, "AddNode", -220, 240);
        var uvD = AddNode(g, "AddNode", -220, 400);

        Connect(g, baseUV, "RG", uvR, "A");
        Connect(g, rightV, "RG", uvR, "B");
        Connect(g, baseUV, "RG", uvL, "A");
        Connect(g, leftV, "RG", uvL, "B");
        Connect(g, baseUV, "RG", uvU, "A");
        Connect(g, upV, "RG", uvU, "B");
        Connect(g, baseUV, "RG", uvD, "A");
        Connect(g, downV, "RG", uvD, "B");

        // Normal sampling
        object nR = AddSampleBuffer(g, -100, 650, "NormalWorldSpace");
        object nL = AddSampleBuffer(g, -100, 790, "NormalWorldSpace");
        object nU = AddSampleBuffer(g, -100, 930, "NormalWorldSpace");
        object nD = AddSampleBuffer(g, -100, 1070, "NormalWorldSpace");
        Connect(g, uvR, "Out", nR, "UV");
        Connect(g, uvL, "Out", nL, "UV");
        Connect(g, uvU, "Out", nU, "UV");
        Connect(g, uvD, "Out", nD, "UV");

        var subNRL = AddNode(g, "SubtractNode", 180, 700);
        var subNUD = AddNode(g, "SubtractNode", 180, 980);
        var lenRL = AddNode(g, "LengthNode", 430, 700);
        var lenUD = AddNode(g, "LengthNode", 430, 980);
        var addN = AddNode(g, "AddNode", 670, 830);
        Connect(g, nR, "Out", subNRL, "A"); Connect(g, nL, "Out", subNRL, "B");
        Connect(g, nU, "Out", subNUD, "A"); Connect(g, nD, "Out", subNUD, "B");
        Connect(g, subNRL, "Out", lenRL, "In"); Connect(g, subNUD, "Out", lenUD, "In");
        Connect(g, lenRL, "Out", addN, "A"); Connect(g, lenUD, "Out", addN, "B");

        var pNT = AddPropertyNode(g, normalT, 650, 1070);
        var stepN = AddNode(g, "StepNode", 930, 830);
        Connect(g, pNT, "Out", stepN, "Edge"); Connect(g, addN, "Out", stepN, "In");

        // Depth sampling
        object dR = AddSceneDepth(g, 160, 1220);
        object dL = AddSceneDepth(g, 160, 1350);
        object dU = AddSceneDepth(g, 160, 1480);
        object dD = AddSceneDepth(g, 160, 1610);
        Connect(g, uvR, "Out", dR, "UV"); Connect(g, uvL, "Out", dL, "UV");
        Connect(g, uvU, "Out", dU, "UV"); Connect(g, uvD, "Out", dD, "UV");

        var subDRL = AddNode(g, "SubtractNode", 450, 1250);
        var subDUD = AddNode(g, "SubtractNode", 450, 1510);
        var absRL = AddNode(g, "AbsoluteNode", 690, 1250);
        var absUD = AddNode(g, "AbsoluteNode", 690, 1510);
        var addD = AddNode(g, "AddNode", 930, 1380);
        Connect(g, dR, "Out", subDRL, "A"); Connect(g, dL, "Out", subDRL, "B");
        Connect(g, dU, "Out", subDUD, "A"); Connect(g, dD, "Out", subDUD, "B");
        Connect(g, subDRL, "Out", absRL, "In"); Connect(g, subDUD, "Out", absUD, "In");
        Connect(g, absRL, "Out", addD, "A"); Connect(g, absUD, "Out", addD, "B");

        var pDT = AddPropertyNode(g, depthT, 910, 1630);
        var stepD = AddNode(g, "StepNode", 1190, 1380);
        Connect(g, pDT, "Out", stepD, "Edge"); Connect(g, addD, "Out", stepD, "In");

        var max = AddNode(g, "MaximumNode", 1430, 1050);
        Connect(g, stepN, "Out", max, "A"); Connect(g, stepD, "Out", max, "B");

        object blit = AddSampleBuffer(g, 1180, 560, "BlitSource");
        Connect(g, baseUV, "RG", blit, "UV");
        var pOutline = AddPropertyNode(g, outline, 1420, 650);
        var lerp = AddNode(g, "LerpNode", 1680, 720);
        Connect(g, blit, "Out", lerp, "A");
        Connect(g, pOutline, "Out", lerp, "B");
        Connect(g, max, "Out", lerp, "T");
        ConnectToBlock(g, lerp, "Out", "BaseColor");

        SaveGraph(g, dst);
        report.Add("OK SG_ScreenOutline (Fullscreen target/URP Sample Buffer 자동 구성 시도)");
    }

    // ---------- Reflection graph helpers ----------
    static object LoadGraphClone(string donor, string dst)
    {
        EnsureFolder(Path.GetDirectoryName(dst).Replace("\\","/"));
        if (File.Exists(dst)) AssetDatabase.DeleteAsset(dst);
        File.Copy(donor, dst, true);

        string text = File.ReadAllText(dst);
        Type graphType = FindType("UnityEditor.ShaderGraph.GraphData");
        Type multiType = FindType("UnityEditor.ShaderGraph.Serialization.MultiJson");
        if (graphType == null || multiType == null) throw new Exception("Shader Graph Editor API를 찾지 못했습니다.");

        object graph = Activator.CreateInstance(graphType, true);
        var des = multiType.GetMethods(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name=="Deserialize" && m.IsGenericMethodDefinition);
        if (des == null) throw new Exception("MultiJson.Deserialize를 찾지 못했습니다.");

        var gm = des.MakeGenericMethod(graphType);
        var ps = gm.GetParameters();
        object[] args = new object[ps.Length];
        for(int i=0;i<ps.Length;i++)
        {
            if(i==0) args[i]=graph;
            else if(ps[i].ParameterType==typeof(string)) args[i]=text;
            else if(ps[i].ParameterType==typeof(bool)) args[i]=false;
            else args[i]=null;
        }
        gm.Invoke(null,args);
        InvokeIfExists(graph,"OnEnable");
        InvokeIfExists(graph,"ValidateGraph");
        return graph;
    }

    static void ClearGraphUserContent(object g)
    {
        // Keep master-stack BlockNodes and target data, remove ordinary nodes and user properties.
        var nodes = GetGraphNodes(g).ToList();
        foreach(var n in nodes)
        {
            if(n==null) continue;
            if(n.GetType().Name=="BlockNode") continue;
            TryInvoke(g, new[]{"RemoveNode"}, n);
        }

        var props = EnumerateMember(g, new[]{"properties","m_Properties"}).ToList();
        foreach(var p in props)
            TryInvoke(g, new[]{"RemoveGraphInput","RemoveShaderInput","RemoveProperty"}, p);

        // remove all edges
        var edges = EnumerateMember(g, new[]{"edges","m_Edges"}).ToList();
        foreach(var e in edges)
            TryInvoke(g, new[]{"RemoveEdge"}, e);
    }

    static object AddColorProperty(object g,string display,string reference,Color value)
    {
        var p=Activator.CreateInstance(FindType("UnityEditor.ShaderGraph.Internal.ColorShaderProperty"),true);
        InitProperty(p,display,reference);
        SetMember(p,new[]{"value","m_Value"},value);
        AddGraphInput(g,p); return p;
    }
    static object AddVector3Property(object g,string display,string reference,Vector3 value)
    {
        var p=Activator.CreateInstance(FindType("UnityEditor.ShaderGraph.Internal.Vector3ShaderProperty"),true);
        InitProperty(p,display,reference);
        SetMember(p,new[]{"value","m_Value"},value);
        AddGraphInput(g,p); return p;
    }
    static object AddFloatProperty(object g,string display,string reference,float value)
    {
        Type t=FindType("UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty") ?? FindType("UnityEditor.ShaderGraph.Vector1ShaderProperty");
        var p=Activator.CreateInstance(t,true);
        InitProperty(p,display,reference);
        SetMember(p,new[]{"value","m_Value"},value);
        AddGraphInput(g,p); return p;
    }
    static void InitProperty(object p,string display,string reference)
    {
        SetMember(p,new[]{"displayName","m_DisplayName","name","m_Name"},display);
        SetMember(p,new[]{"overrideReferenceName","referenceName","m_OverrideReferenceName"},reference);
        SetMember(p,new[]{"generatePropertyBlock","m_GeneratePropertyBlock"},true);
    }
    static void AddGraphInput(object g,object p)
    {
        if(TryInvoke(g,new[]{"AddGraphInput","AddShaderInput","AddProperty"},p)) return;
        string sigs=string.Join(" | ",g.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
            .Where(m=>m.Name.IndexOf("GraphInput",StringComparison.OrdinalIgnoreCase)>=0 || m.Name.IndexOf("Property",StringComparison.OrdinalIgnoreCase)>=0)
            .Select(m=>m.Name+"("+string.Join(",",m.GetParameters().Select(x=>x.ParameterType.Name+(x.IsOptional?"?":"")))+")"));
        throw new Exception("Graph property 추가 API 호출 실패: "+p.GetType().FullName+"\n후보: "+sigs);
    }
    static object AddPropertyNode(object g,object prop,float x,float y)
    {
        Type t=FindType("UnityEditor.ShaderGraph.PropertyNode") ?? FindTypeBySimpleName("PropertyNode");
        if(t==null) throw new Exception("PropertyNode type not found");
        object n=Activator.CreateInstance(t,true);

        // Unity 6.x / Shader Graph 17.x:
        // PropertyNode.property에 실제 ShaderInput을 넣으면 출력 슬롯이 Property 타입에 맞게 다시 만들어집니다.
        // 구버전 호환용으로 propertyGuid / serialized guid 방식도 뒤에 fallback으로 둡니다.
        SetMember(n,new[]{"owner","m_Owner"},g);

        bool bound = SetMember(n,new[]{"property"},prop);

        object guid = GetMember(prop,new[]{"guid","m_Guid"});
        if(!bound && guid!=null)
            bound = SetMember(n,new[]{"propertyGuid","m_PropertyGuid"},guid);

        if(!bound)
        {
            string guidText = ExtractGuidString(guid) ?? ExtractGuidString(prop);
            if(!string.IsNullOrEmpty(guidText))
                bound = SetMember(n,new[]{"m_PropertyGuidSerialized","propertyGuidSerialized"},guidText);
        }

        if(!bound)
            throw new Exception("PropertyNode 바인딩 실패\nPropertyNode 멤버: "+DescribeMembers(n));

        // owner가 연결된 동안 슬롯 재생성을 먼저 시도합니다.
        InvokeIfExists(n,"UpdateNodeAfterDeserialization");
        InvokeIfExists(n,"OnEnable");
        InvokeIfExists(n,"ValidateNode");

        SetMember(n,new[]{"owner","m_Owner"},null);

        if(!TryInvoke(g,new[]{"AddNode"},n))
            throw new Exception("Graph.AddNode(PropertyNode) 호출 실패");

        // AddNode 후 owner가 graph로 지정되므로 한 번 더 갱신합니다.
        InvokeIfExists(n,"UpdateNodeAfterDeserialization");
        InvokeIfExists(n,"ValidateNode");
        InvokeIfExists(n,"Dirty");

        SetNodePosition(n,x,y);

        var outs=GetSlots(n,true).ToList();
        if(outs.Count==0)
            throw new Exception("PropertyNode 출력 슬롯 생성 실패: "+
                (GetMember(prop,new[]{"displayName","name","m_Name"})??prop.GetType().Name)+
                "\nPropertyNode 멤버: "+DescribeMembers(n));

        return n;
    }
    static object AddNode(object g,string simpleName,float x,float y)
    {
        Type t=FindType("UnityEditor.ShaderGraph."+simpleName);
        if(t==null) t=FindTypeBySimpleName(simpleName);
        if(t==null) throw new Exception("Node type not found: "+simpleName);
        object n=Activator.CreateInstance(t,true);
        TryInvoke(g,new[]{"AddNode"},n);
        SetNodePosition(n,x,y);
        return n;
    }
    static object AddSampleBuffer(object g,float x,float y,string source)
    {
        Type t=FindType("UnityEditor.Rendering.Universal.UniversalSampleBufferNode")
            ?? FindTypeBySimpleName("UniversalSampleBufferNode")
            ?? FindTypeBySimpleName("SampleBufferNode")
            ?? FindTypeByNameContains("SampleBuffer");
        if(t==null) throw new Exception("URP Sample Buffer node type not found.");
        object n=Activator.CreateInstance(t,true);
        TryInvoke(g,new[]{"AddNode"},n); SetNodePosition(n,x,y);
        SetEnumMemberByName(n,new[]{"sourceBuffer","m_SourceBuffer","source","bufferType","m_BufferType"},source);
        return n;
    }
    static object AddSceneDepth(object g,float x,float y)
    {
        object n=AddNode(g,"SceneDepthNode",x,y);
        SetEnumMemberByName(n,new[]{"depthSamplingMode","m_DepthSamplingMode","samplingMode","m_SamplingMode"}, "Eye");
        return n;
    }
    static void SetNodePosition(object n,float x,float y)
    {
        var ds=GetMember(n,new[]{"drawState","m_DrawState"});
        if(ds==null) return;
        Type dst=ds.GetType();
        var posM=dst.GetProperty("position") ?? (MemberInfo)dst.GetField("m_Position",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if(posM==null) return;
        Rect r=new Rect(x,y,210,160);
        if(posM is PropertyInfo pi && pi.CanWrite){ pi.SetValue(ds,r); SetMember(n,new[]{"drawState","m_DrawState"},ds); }
        if(posM is FieldInfo fi){ fi.SetValue(ds,r); SetMember(n,new[]{"drawState","m_DrawState"},ds); }
    }
    static void Connect(object g,object outNode,string outName,object inNode,string inName,string altIn=null)
    {
        object os=FindSlot(outNode,outName,true);
        object ins=FindSlot(inNode,inName,false);
        if(ins==null && altIn!=null) ins=FindSlot(inNode,altIn,false);

        // A few Unity 6.x nodes localize/change display names while retaining a single compatible port.
        // Only fall back when there is exactly one output/input, so we don't silently wire a wrong multi-port node.
        var outs=GetSlots(outNode,true).ToList();
        var insList=GetSlots(inNode,false).ToList();
        if(os==null && outs.Count==1) os=outs[0];
        if(ins==null && insList.Count==1) ins=insList[0];

        if(os==null || ins==null)
        {
            string oslots=string.Join(", ",outs.Select(SlotLabel));
            string islots=string.Join(", ",insList.Select(SlotLabel));
            throw new Exception("Slot 연결 실패: "+outNode.GetType().Name+"."+outName+" -> "+inNode.GetType().Name+"."+inName+
                "\n출력 슬롯=["+oslots+"] 입력 슬롯=["+islots+"]");
        }
        object oref=GetSlotReference(os);
        object iref=GetSlotReference(ins);
        if(oref==null || iref==null) throw new Exception("SlotReference 생성 실패: "+outNode.GetType().Name+" -> "+inNode.GetType().Name);
        var m=g.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
            .FirstOrDefault(mm=>mm.Name=="Connect" && mm.GetParameters().Length==2);
        if(m==null) throw new Exception("Graph.Connect API not found");
        m.Invoke(g,new[]{oref,iref});
    }
    static string SlotLabel(object s)
    {
        if(s==null)return "null";
        string n=(GetMember(s,new[]{"displayName","m_DisplayName","shaderOutputName","m_ShaderOutputName"})??s.GetType().Name).ToString();
        object id=GetMember(s,new[]{"id","m_Id"});
        return id==null ? n : n+"("+id+")";
    }
    static void ConnectToBlock(object g,object outNode,string outSlot,params string[] blockNames)
    {
        object b=null;
        foreach(var s in blockNames){ b=FindBlock(g,s); if(b!=null) break; }
        if(b==null) throw new Exception("Master Stack block not found: "+string.Join("/",blockNames));
        var input=FindFirstSlot(b,false);
        var os=FindSlot(outNode,outSlot,true) ?? FindFirstSlot(outNode,true);
        var m=g.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
            .FirstOrDefault(mm=>mm.Name=="Connect" && mm.GetParameters().Length==2);
        m.Invoke(g,new[]{GetSlotReference(os),GetSlotReference(input)});
    }
    static object FindBlock(object g,string term)
    {
        string want=NormSlotName(term);
        foreach(var item in EnumerateMember(g,new[]{"nodes","m_Nodes"}))
        {
            object n=UnwrapValue(item);
            if(n==null || n.GetType().Name!="BlockNode") continue;

            string name=(GetMember(n,new[]{"name","m_Name"})??"").ToString();
            string desc=(GetMember(n,new[]{"descriptor","serializedDescriptor","m_SerializedDescriptor"})??"").ToString();

            string nn=NormSlotName(name);
            string dd=NormSlotName(desc);
            if(nn.Contains(want) || dd.Contains(want))
                return n;
        }

        // Unity 6.x에서 nodes wrapper가 바뀐 경우를 대비한 대체 탐색
        foreach(var item in EnumerateMember(g,new[]{"vertexContext","fragmentContext","m_VertexContext","m_FragmentContext"}))
        {
            object ctx=UnwrapValue(item);
            if(ctx==null) continue;
            foreach(var ni in EnumerateMember(ctx,new[]{"blocks","m_Blocks","children","m_Children"}))
            {
                object n=UnwrapValue(ni);
                if(n==null) continue;
                string name=(GetMember(n,new[]{"name","m_Name"})??"").ToString();
                string desc=(GetMember(n,new[]{"descriptor","serializedDescriptor","m_SerializedDescriptor"})??"").ToString();
                if(NormSlotName(name).Contains(want) || NormSlotName(desc).Contains(want))
                    return n;
            }
        }
        return null;
    }
    static object FindConnectedOutputNode(object g,object inputNode)
    {
        if(inputNode==null) return null;
        foreach(var e in EnumerateMember(g,new[]{"edges","m_Edges"}))
        {
            object input=GetMember(e,new[]{"inputSlot","m_InputSlot"});
            object output=GetMember(e,new[]{"outputSlot","m_OutputSlot"});
            object inNodeRef=GetMember(input,new[]{"node","m_Node"});
            object actual=ResolveNodeRef(g,inNodeRef);
            if(actual==inputNode) return ResolveNodeRef(g,GetMember(output,new[]{"node","m_Node"}));
        }
        return null;
    }
    static object ResolveNodeRef(object g,object nodeRef)
    {
        if(nodeRef==null) return null;
        object id=GetMember(nodeRef,new[]{"id","m_Id","objectId","m_ObjectId"});
        foreach(var n in EnumerateMember(g,new[]{"nodes","m_Nodes"}))
        {
            if(n==nodeRef) return n;
            object nid=GetMember(n,new[]{"objectId","m_ObjectId","id","m_Id"});
            if(id!=null && nid!=null && id.ToString()==nid.ToString()) return n;
        }
        return null;
    }
    static void DisconnectInput(object g,object node)
    {
        if(node==null)return;
        var slot=FindFirstSlot(node,false); if(slot==null)return;
        var sr=GetSlotReference(slot);
        TryInvoke(g,new[]{"RemoveEdges"},sr);
        foreach(var e in EnumerateMember(g,new[]{"edges","m_Edges"}).ToList())
        {
            object input=GetMember(e,new[]{"inputSlot","m_InputSlot"});
            if(input!=null && input.ToString()==sr.ToString()) TryInvoke(g,new[]{"RemoveEdge"},e);
        }
    }
    static object FindSlot(object node,string display,bool output)
    {
        var slots=GetSlots(node,output).ToList();
        string want=NormSlotName(display);

        // 1) Unity가 실제로 만든 슬롯의 표시명/ShaderOutputName/내부 이름을 먼저 비교
        foreach(var slot in slots)
        {
            foreach(var key in new[]{"displayName","m_DisplayName","shaderOutputName","m_ShaderOutputName","name","m_Name"})
            {
                object raw=GetMember(slot,new[]{key});
                if(raw!=null && NormSlotName(raw.ToString())==want)
                    return slot;
            }
        }

        // 2) Unity 6.6에서 일부 슬롯 이름 reflection이 불안정한 경우,
        //    표준 Shader Graph 노드의 포트 순서를 이용해 안전하게 선택.
        int idx=SemanticSlotIndex(node.GetType().Name, display, output, slots.Count);
        if(idx>=0 && idx<slots.Count) return slots[idx];

        // 3) 출력/입력이 하나뿐인 노드는 그 포트를 사용.
        if(slots.Count==1) return slots[0];

        return null;
    }
    static string NormSlotName(string v)
    {
        if(string.IsNullOrEmpty(v)) return "";
        return new string(v.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
    static int SemanticSlotIndex(string typeName,string requested,bool output,int count)
    {
        string r=NormSlotName(requested);

        if(output)
        {
            // Split: R,G,B,A
            if(typeName=="SplitNode")
            {
                if(r=="r"||r=="x")return 0;
                if(r=="g"||r=="y")return 1;
                if(r=="b"||r=="z")return 2;
                if(r=="a"||r=="w")return 3;
            }
            // Combine outputs: RGBA, RGB, RG
            if(typeName=="CombineNode")
            {
                if(r=="rgba")return 0;
                if(r=="rgb")return 1;
                if(r=="rg"||r=="xy")return 2;
            }
            // Screen: Width, Height
            if(typeName=="ScreenNode")
            {
                if(r=="width")return 0;
                if(r=="height")return 1;
            }
            // 대부분의 수학/Property/Input 노드는 출력 하나
            if(count==1)return 0;
            if(r=="out" && count>0)return count-1;
            return -1;
        }

        // 공통 2입력 수학 노드
        if(typeName=="DotProductNode" || typeName=="MultiplyNode" || typeName=="AddNode" ||
           typeName=="SubtractNode" || typeName=="MaximumNode")
        {
            if(r=="a")return 0;
            if(r=="b")return 1;
        }
        if(typeName=="LerpNode")
        {
            if(r=="a")return 0;
            if(r=="b")return 1;
            if(r=="t")return 2;
        }
        if(typeName=="StepNode")
        {
            if(r=="edge")return 0;
            if(r=="in")return 1;
        }
        if(typeName=="RemapNode")
        {
            if(r=="in")return 0;
            if(r=="inminmax")return 1;
            if(r=="outminmax")return 2;
        }
        if(typeName=="FresnelNode")
        {
            if(r.Contains("normal"))return 0;
            if(r.Contains("view"))return 1;
            if(r=="power")return 2;
        }
        if(typeName=="CombineNode")
        {
            if(r=="r"||r=="x")return 0;
            if(r=="g"||r=="y")return 1;
            if(r=="b"||r=="z")return 2;
            if(r=="a"||r=="w")return 3;
        }

        // 단일 입력 노드들
        if(typeName=="NormalizeNode" || typeName=="LengthNode" || typeName=="AbsoluteNode" ||
           typeName=="ReciprocalNode" || typeName=="SplitNode")
            return 0;

        // URP Sample Buffer / Scene Depth UV
        if(r=="uv" && count>0)return 0;

        if(count==1)return 0;
        return -1;
    }
    static object FindFirstSlot(object node,bool output){ return GetSlots(node,output).FirstOrDefault(); }
    static IEnumerable<object> GetSlots(object node,bool output)
    {
        var all=new List<object>();
        string wanted = output ? "GetOutputSlots" : "GetInputSlots";
        Type materialSlot = FindTypeBySimpleName("MaterialSlot");

        foreach(var raw in node.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Where(x=>x.Name==wanted))
        {
            try
            {
                MethodInfo m = raw;
                if(m.IsGenericMethodDefinition)
                {
                    if(materialSlot==null) continue;
                    var ga=m.GetGenericArguments();
                    if(ga.Length!=1) continue;
                    m=m.MakeGenericMethod(materialSlot);
                }

                var ps=m.GetParameters();
                if(ps.Length==0)
                {
                    object r=m.Invoke(node,null);
                    if(r is IEnumerable er)
                    {
                        foreach(var s in er) all.Add(s);
                        if(all.Count>0) return all;
                    }
                }
                else if(ps.Length==1 && ps[0].ParameterType.IsGenericType)
                {
                    Type elem=ps[0].ParameterType.GetGenericArguments()[0];
                    if(elem.IsGenericParameter && materialSlot!=null) elem=materialSlot;
                    Type lt=typeof(List<>).MakeGenericType(elem);
                    object list=Activator.CreateInstance(lt);
                    m.Invoke(node,new[]{list});
                    foreach(var s in (IEnumerable)list) all.Add(s);
                    if(all.Count>0) return all;
                }
            }
            catch{}
        }

        // Fallback: read serialised/internal slot collection directly.
        foreach(var s in EnumerateMember(node,new[]{"slots","m_Slots"}))
        {
            bool isOut=Convert.ToBoolean(GetMember(s,new[]{"isOutputSlot"})??false);
            if(isOut==output) all.Add(s);
        }
        return all;
    }
    static object GetSlotReference(object slot)
    {
        object sr=GetMember(slot,new[]{"slotReference"});
        if(sr!=null)return sr;
        object owner=GetMember(slot,new[]{"owner"});
        object id=GetMember(slot,new[]{"id","m_Id"});
        if(owner!=null && id!=null)
        {
            var m=owner.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                .FirstOrDefault(x=>x.Name=="GetSlotReference" && x.GetParameters().Length==1);
            if(m!=null)return m.Invoke(owner,new[]{Convert.ChangeType(id,m.GetParameters()[0].ParameterType)});
        }
        return null;
    }
    static void SetSlotDefault(object node,string slotName,object value) { var s=FindSlot(node,slotName,false); if(s!=null) SetMember(s,new[]{"value","m_Value","defaultValue","m_DefaultValue"},value); }
    static void SetSlotDefault(object node,string[] names,float value,bool multiplyExisting=false)
    {
        object s=null; foreach(var n in names){s=FindSlot(node,n,false);if(s!=null)break;}
        if(s!=null) SetMember(s,new[]{"value","m_Value","defaultValue","m_DefaultValue"},value);
    }
    static void SetSpaceObject(object n){ SetEnumMemberByName(n,new[]{"space","m_Space"},"Object"); }

    static object UnwrapValue(object o)
    {
        if(o==null) return null;
        object v=GetMember(o,new[]{"value","m_Value"});
        return v ?? o;
    }

    static void SetUniversalRenderFace(object g,string face)
    {
        foreach(var item in EnumerateMember(g,new[]{"activeTargets","m_ActiveTargets"}))
        {
            object t=UnwrapValue(item);
            if(t!=null) SetEnumMemberByName(t,new[]{"renderFace","m_RenderFace"},face);
        }
    }

    static void SwitchUniversalSubTarget(object g,string subTargetSimpleName)
    {
        Type targetT=FindType("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget");
        Type subT=FindType("UnityEditor.Rendering.Universal.ShaderGraph."+subTargetSimpleName);
        if(targetT==null || subT==null)
            throw new Exception("URP Shader Graph Target type을 찾지 못했습니다: "+subTargetSimpleName);

        object target=null;
        foreach(var item in EnumerateMember(g,new[]{"activeTargets","m_ActiveTargets"}))
        {
            object t=UnwrapValue(item);
            if(t!=null && targetT.IsAssignableFrom(t.GetType())) { target=t; break; }
        }

        if(target==null)
            throw new Exception("현재 Shader Graph에서 UniversalTarget을 찾지 못했습니다.");

        // Unity 자체 CreateUnlitShaderGraph/CreateFullscreenShaderGraph가 사용하는 방식:
        // UniversalTarget.TrySetActiveSubTarget(Type)
        MethodInfo setter = targetT.GetMethod("TrySetActiveSubTarget",
            BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,
            null, new[]{typeof(Type)}, null);

        if(setter==null)
            throw new Exception("UniversalTarget.TrySetActiveSubTarget(Type) API를 찾지 못했습니다.");

        object result=setter.Invoke(target,new object[]{subT});
        if(result is bool ok && !ok)
            throw new Exception("TrySetActiveSubTarget 실패: "+subTargetSimpleName);

        SetEnumMemberByName(target,new[]{"surfaceType","m_SurfaceType"},"Opaque");
        SetEnumMemberByName(target,new[]{"renderFace","m_RenderFace"},"Front");

        // 여기서 ValidateGraph를 호출하지 않는다.
        // donor의 Master Stack Block을 먼저 유지하고, 최종 SaveGraph에서 한 번만 검증한다.
    }

    static void TrySwitchToFullscreenTarget(object g)
    {
        Type targetT=FindType("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget");
        Type subT=FindType("UnityEditor.Rendering.Universal.ShaderGraph.UniversalFullscreenSubTarget");
        if(targetT==null || subT==null) { report.Add("WARN Fullscreen target type not found; graph remains valid Shader Graph but target auto-switch skipped."); return; }

        object target=Activator.CreateInstance(targetT,true);
        object sub=Activator.CreateInstance(subT,true);
        SetMember(target,new[]{"activeSubTarget","m_ActiveSubTarget"},sub);

        if(TryInvoke(g,new[]{"SetTarget","AddTarget"},target))
        {
            report.Add("OK Fullscreen Target installed");
            return;
        }

        var member=FindMember(g,new[]{"activeTargets","m_ActiveTargets"});
        if(member!=null)
        {
            object list=GetMember(g,new[]{"activeTargets","m_ActiveTargets"});
            if(list is IList il){il.Clear();il.Add(target); report.Add("OK Fullscreen Target installed(list)");}
        }
    }

    static void SaveGraph(object g,string path)
    {
        InvokeIfExists(g,"ValidateGraph");
        Type multi=FindType("UnityEditor.ShaderGraph.Serialization.MultiJson");
        string text=null;
        foreach(var m in multi.GetMethods(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Where(x=>x.Name=="Serialize"))
        {
            try
            {
                MethodInfo mm=m;
                if(mm.IsGenericMethodDefinition) mm=mm.MakeGenericMethod(g.GetType());
                var p=mm.GetParameters();
                object[] a=new object[p.Length];
                for(int i=0;i<p.Length;i++)
                {
                    if(p[i].ParameterType.IsAssignableFrom(g.GetType())) a[i]=g;
                    else if(p[i].ParameterType==typeof(string)) a[i]=null;
                    else if(p[i].ParameterType==typeof(bool)) a[i]=false;
                    else a[i]=null;
                }
                object r=mm.Invoke(null,a);
                if(r is string s && s.Contains("GraphData")){text=s;break;}
            } catch {}
        }
        if(string.IsNullOrEmpty(text)) throw new Exception("MultiJson.Serialize로 Shader Graph를 저장하지 못했습니다.");
        File.WriteAllText(path,text,Encoding.UTF8);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
    }

    // ---------- Materials ----------
    static void CreateMaterials()
    {
        MakeMat("Mat_ToonBand", GRAPHS+"/SG_ToonBand.shadergraph");
        MakeMat("Mat_ToonRim", GRAPHS+"/SG_ToonRim.shadergraph");
        MakeMat("Mat_OutlineShell", GRAPHS+"/SG_OutlineShell.shadergraph");
        MakeMat("Mat_ScreenOutline", GRAPHS+"/SG_ScreenOutline.shadergraph");

        var tf2=CloneMat("Mat_ToonRim","Mat_StyleCharacter_TF2");
        SetColor(tf2,"_LitColor",Hex("#FFD58A")); SetColor(tf2,"_ShadowColor",Hex("#456FAD"));
        SetFloat(tf2,"_BandThreshold",.45f); SetColor(tf2,"_RimColor",Hex("#FFF3C4")); SetFloat(tf2,"_RimIntensity",.35f);

        var gg=CloneMat("Mat_ToonRim","Mat_StyleCharacter_GuiltyGear");
        SetColor(gg,"_RimColor",Hex("#F5C43D")); SetFloat(gg,"_RimPower",3f); SetFloat(gg,"_RimIntensity",.8f);
        var ggs=CloneMat("Mat_OutlineShell","Mat_StyleShell_GuiltyGear");
        SetColor(ggs,"_OutlineColor",Hex("#11152A")); SetFloat(ggs,"_OutlineWidth",.035f);

        var hifi=CloneMat("Mat_ToonRim","Mat_StyleCharacter_HiFi");
        SetColor(hifi,"_LitColor",Hex("#FFB74D")); SetColor(hifi,"_ShadowColor",Hex("#553D9C"));
        SetColor(hifi,"_RimColor",Hex("#00E5FF")); SetFloat(hifi,"_RimIntensity",1.5f);
        var hifis=CloneMat("Mat_ScreenOutline","Mat_StyleScreenOutline_HiFi");
        SetColor(hifis,"_OutlineColor",Hex("#14213D")); SetFloat(hifis,"_OutlineWidthPixels",1f);
        SetFloat(hifis,"_NormalThreshold",.25f); SetFloat(hifis,"_DepthThreshold",.5f);
    }
    static Material MakeMat(string name,string graphPath)
    {
        string p=MATS+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        Shader s=AssetDatabase.LoadAssetAtPath<Shader>(graphPath);
        if(s==null) throw new Exception("Shader Graph import 실패: "+graphPath);
        if(m==null){m=new Material(s);AssetDatabase.CreateAsset(m,p);}else m.shader=s;
        EditorUtility.SetDirty(m); return m;
    }
    static Material CloneMat(string src,string dst)
    {
        string sp=MATS+"/"+src+".mat", dp=MATS+"/"+dst+".mat";
        if(File.Exists(dp)) AssetDatabase.DeleteAsset(dp);
        AssetDatabase.CopyAsset(sp,dp);
        return AssetDatabase.LoadAssetAtPath<Material>(dp);
    }

    // ---------- Prefabs / Scene ----------
    static void CreatePrefabs()
    {
        SaveSinglePrefab("PF_ToonBand", "Mat_ToonBand");
        SaveSinglePrefab("PF_ToonRim", "Mat_ToonRim");
        SaveOutlinePrefab("PF_OutlineDemo","Mat_ToonRim","Mat_OutlineShell");
        SaveSinglePrefab("PF_Style_TF2","Mat_StyleCharacter_TF2");
        SaveOutlinePrefab("PF_Style_GuiltyGear","Mat_StyleCharacter_GuiltyGear","Mat_StyleShell_GuiltyGear");

        var root=new GameObject("PF_Style_HiFi");
        AddPrimitive(root.transform,PrimitiveType.Capsule,"ToonCharacter",Vector3.zero,"Mat_StyleCharacter_HiFi");
        AddPrimitive(root.transform,PrimitiveType.Cube,"ComparisonCube",new Vector3(2,0,0),null);
        AddPrimitive(root.transform,PrimitiveType.Plane,"ComparisonPlane",new Vector3(0,-1,0),null);
        PrefabUtility.SaveAsPrefabAsset(root,PREFABS+"/PF_Style_HiFi.prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }
    static void SaveSinglePrefab(string prefab,string mat)
    {
        var root=new GameObject(prefab);
        AddPrimitive(root.transform,PrimitiveType.Capsule,"ToonCharacter",Vector3.zero,mat);
        PrefabUtility.SaveAsPrefabAsset(root,PREFABS+"/"+prefab+".prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }
    static void SaveOutlinePrefab(string prefab,string surface,string shell)
    {
        var root=new GameObject(prefab);
        AddPrimitive(root.transform,PrimitiveType.Capsule,"ToonCharacter",Vector3.zero,surface);
        var s=AddPrimitive(root.transform,PrimitiveType.Capsule,"ToonOutlineShell",Vector3.zero,shell);
        var c=s.GetComponent<Collider>(); if(c) UnityEngine.Object.DestroyImmediate(c);
        PrefabUtility.SaveAsPrefabAsset(root,PREFABS+"/"+prefab+".prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }
    static GameObject AddPrimitive(Transform parent,PrimitiveType type,string name,Vector3 pos,string matName)
    {
        var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent);o.transform.localPosition=pos;
        if(matName!=null)o.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(MATS+"/"+matName+".mat");
        return o;
    }
    static void CreateScene()
    {
        Scene s=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("DAY08_COMPLETE");

        SpawnPrefab("01_ToonBand","PF_ToonBand",new Vector3(-6,0,0),root.transform);
        SpawnPrefab("02_ToonRim","PF_ToonRim",new Vector3(-3,0,0),root.transform);
        SpawnPrefab("03_OutlineShell","PF_OutlineDemo",new Vector3(0,0,0),root.transform);
        SpawnPrefab("04_TF2","PF_Style_TF2",new Vector3(3,0,0),root.transform);
        SpawnPrefab("05_GuiltyGear","PF_Style_GuiltyGear",new Vector3(6,0,0),root.transform);
        SpawnPrefab("06_HiFi_ScreenOutline","PF_Style_HiFi",new Vector3(0,0,4),root.transform);

        var cam=new GameObject("Main Camera");cam.tag="MainCamera";cam.transform.SetParent(root.transform);
        var c=cam.AddComponent<Camera>();c.fieldOfView=58;cam.transform.position=new Vector3(0,4,-13);cam.transform.LookAt(new Vector3(0,.3f,1.5f));

        var l=new GameObject("Directional Light");l.transform.SetParent(root.transform);var li=l.AddComponent<Light>();
        li.type=LightType.Directional;li.intensity=1.1f;l.transform.rotation=Quaternion.Euler(50,-30,0);

        var vol=new GameObject("Global Volume DAY08");vol.transform.SetParent(root.transform);
        var v=vol.AddComponent<Volume>();v.isGlobal=true;v.profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(SETTINGS+"/VP_DAY08_ColorGrading.asset");

        EditorSceneManager.SaveScene(s,SCENE_PATH);
    }
    static void SpawnPrefab(string name,string prefab,Vector3 pos,Transform parent)
    {
        var p=AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS+"/"+prefab+".prefab");
        var o=PrefabUtility.InstantiatePrefab(p) as GameObject;o.name=name;o.transform.SetParent(parent);o.transform.position=pos;
    }

    // ---------- Volume / Renderer Features ----------
    static void CreateVolumeProfile()
    {
        string path=SETTINGS+"/VP_DAY08_ColorGrading.asset";
        var p=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(p==null){p=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(p,path);}
        Type ca=FindType("UnityEngine.Rendering.Universal.ColorAdjustments");
        if(ca!=null)
        {
            try
            {
                var add=p.GetType().GetMethod("Add",new[]{typeof(Type),typeof(bool)});
                object comp=add?.Invoke(p,new object[]{ca,true});
                if(comp!=null)
                {
                    object contrast=GetMember(comp,new[]{"contrast"});
                    if(contrast!=null){SetMember(contrast,new[]{"overrideState"},true);SetMember(contrast,new[]{"value"},10f);}
                }
            }catch{}
        }
        EditorUtility.SetDirty(p);
    }
    static void TryInstallRendererFeatures()
    {
        string[] guids=AssetDatabase.FindAssets("t:ScriptableRendererData");
        if(guids.Length==0){report.Add("WARN Renderer Data를 찾지 못해 Renderer Feature 자동 설치 생략");return;}
        string path=AssetDatabase.GUIDToAssetPath(guids[0]);
        var data=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if(data==null)return;

        bool r1=TryAddRendererFeature(data,"RenderObjects","Outline Shell Pass",AssetDatabase.LoadAssetAtPath<Material>(MATS+"/Mat_OutlineShell.mat"));
        bool r2=TryAddRendererFeature(data,"FullScreenPassRendererFeature","Screen Outline",AssetDatabase.LoadAssetAtPath<Material>(MATS+"/Mat_ScreenOutline.mat"));
        report.Add((r1?"OK":"WARN")+" Render Objects feature");
        report.Add((r2?"OK":"WARN")+" Full Screen Pass feature");
    }
    static bool TryAddRendererFeature(UnityEngine.Object data,string typeContains,string featureName,Material mat)
    {
        Type t=AppDomain.CurrentDomain.GetAssemblies().SelectMany(SafeTypes)
            .FirstOrDefault(x=>typeof(ScriptableObject).IsAssignableFrom(x)&&x.Name.IndexOf(typeContains,StringComparison.OrdinalIgnoreCase)>=0);
        if(t==null)return false;
        try
        {
            var feature=ScriptableObject.CreateInstance(t);feature.name=featureName;
            if(typeContains.IndexOf("FullScreen",StringComparison.OrdinalIgnoreCase)>=0)
                SetMember(feature,new[]{"passMaterial","m_PassMaterial","material"},mat);
            else
                SetMember(feature,new[]{"overrideMaterial","m_OverrideMaterial"},mat);

            AssetDatabase.AddObjectToAsset(feature,data);
            var so=new SerializedObject(data);
            var arr=so.FindProperty("m_RendererFeatures");
            if(arr==null)return false;
            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize-1).objectReferenceValue=feature;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);EditorUtility.SetDirty(feature);
            return true;
        }catch{return false;}
    }

    static IEnumerable<object> GetGraphNodes(object g)
    {
        if(g==null) yield break;
        var m=g.GetType().GetMethod("GetNodes",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        if(m!=null)
        {
            object r=null;
            try{r=m.Invoke(g,null);}catch{}
            if(r is IEnumerable e)
            {
                foreach(var n in e) if(n!=null) yield return n;
                yield break;
            }
        }

        // Older/fallback format may expose JsonRef wrappers; unwrap .value when present.
        foreach(var item in EnumerateMember(g,new[]{"nodes","m_Nodes"}))
        {
            if(item==null) continue;
            object v=GetMember(item,new[]{"value","m_Value"});
            yield return v ?? item;
        }
    }

    // ---------- generic helpers ----------
    static string FindShaderGraph(string name)
    {
        foreach(var g in AssetDatabase.FindAssets(name))
        {
            string p=AssetDatabase.GUIDToAssetPath(g);
            if(p.EndsWith(".shadergraph",StringComparison.OrdinalIgnoreCase)&&Path.GetFileNameWithoutExtension(p).Equals(name,StringComparison.OrdinalIgnoreCase))return p;
        }
        return null;
    }
    static string FindAnyShaderGraph()
    {
        foreach(var p in AssetDatabase.GetAllAssetPaths())if(p.EndsWith(".shadergraph",StringComparison.OrdinalIgnoreCase))return p;
        return null;
    }
    static Type FindType(string full)
    {
        foreach(var a in AppDomain.CurrentDomain.GetAssemblies()){var t=a.GetType(full,false);if(t!=null)return t;}return null;
    }
    static Type FindTypeBySimpleName(string n){return AppDomain.CurrentDomain.GetAssemblies().SelectMany(SafeTypes).FirstOrDefault(t=>t.Name==n);}
    static Type FindTypeByNameContains(string n){return AppDomain.CurrentDomain.GetAssemblies().SelectMany(SafeTypes).FirstOrDefault(t=>t.Name.IndexOf(n,StringComparison.OrdinalIgnoreCase)>=0);}
    static IEnumerable<Type> SafeTypes(Assembly a){try{return a.GetTypes();}catch{return new Type[0];}}
    static MemberInfo FindMember(object o,string[] names)
    {
        if(o==null)return null;
        // private 멤버는 부모 타입에서 GetField/GetProperty로 바로 안 잡히므로 상속 계층을 직접 탐색합니다.
        for(Type t=o.GetType(); t!=null; t=t.BaseType)
        {
            foreach(var n in names)
            {
                var p=t.GetProperty(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
                if(p!=null)return p;
                var f=t.GetField(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
                if(f!=null)return f;
            }
        }
        return null;
    }
    static string ExtractGuidString(object o)
    {
        if(o==null)return null;
        if(o is Guid sysGuid)return sysGuid.ToString();

        object nested=GetMember(o,new[]{"m_GuidSerialized","guidSerialized","serializedGuid"});
        if(nested is string ns && !string.IsNullOrEmpty(ns))return ns;

        string text=o.ToString();
        if(string.IsNullOrEmpty(text))return null;
        var m=System.Text.RegularExpressions.Regex.Match(text,
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        return m.Success ? m.Value : null;
    }

    static string DescribeMembers(object o)
    {
        if(o==null)return "null";
        var names=new List<string>();
        for(Type t=o.GetType(); t!=null; t=t.BaseType)
        {
            names.AddRange(t.GetProperties(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly)
                .Select(p=>t.Name+"."+p.Name+":"+p.PropertyType.Name));
            names.AddRange(t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly)
                .Select(f=>t.Name+"."+f.Name+":"+f.FieldType.Name));
        }
        return string.Join(", ",names.Distinct().Take(120));
    }

    static object GetMember(object o,string[] n){var m=FindMember(o,n);if(m is PropertyInfo p)return p.GetValue(o);if(m is FieldInfo f)return f.GetValue(o);return null;}
    static Type GetMemberType(object o,string[] n){var m=FindMember(o,n);if(m is PropertyInfo p)return p.PropertyType;if(m is FieldInfo f)return f.FieldType;return null;}
    static bool SetMember(object o,string[] n,object v)
    {
        var m=FindMember(o,n);if(m==null)return false;
        try{
            if(m is PropertyInfo p&&p.CanWrite){p.SetValue(o,ConvertValue(v,p.PropertyType));return true;}
            if(m is FieldInfo f){f.SetValue(o,ConvertValue(v,f.FieldType));return true;}
        }catch{}return false;
    }
    static object ConvertValue(object v,Type t)
    {
        if(v==null)return null;if(t.IsInstanceOfType(v))return v;
        if(t.IsEnum){if(v is string s)return Enum.Parse(t,s,true);return Enum.ToObject(t,v);}
        try{return Convert.ChangeType(v,t);}catch{return v;}
    }
    static object EnumValue(Type t,string name,int fallback){if(t==null||!t.IsEnum)return fallback;try{return Enum.Parse(t,name,true);}catch{return Enum.ToObject(t,fallback);}}
    static void SetEnumMemberByName(object o,string[] n,string val){var t=GetMemberType(o,n);if(t!=null&&t.IsEnum)SetMember(o,n,EnumValue(t,val,0));}
    static IEnumerable<object> EnumerateMember(object o,string[] n)
    {
        object v=GetMember(o,n);if(v is IEnumerable e)foreach(var x in e)yield return x;
    }
    static bool TryInvoke(object o,string[] names,params object[] args)
    {
        if(o==null)return false;
        foreach(var n in names)
        {
            foreach(var raw in o.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Where(x=>x.Name==n))
            {
                try
                {
                    MethodInfo m=raw;
                    if(m.IsGenericMethodDefinition) continue;
                    var ps=m.GetParameters();
                    if(ps.Length < args.Length) continue;

                    bool remainingOptional=true;
                    for(int i=args.Length;i<ps.Length;i++)
                        if(!ps[i].IsOptional && !ps[i].HasDefaultValue){remainingOptional=false;break;}
                    if(!remainingOptional) continue;

                    object[] callArgs=new object[ps.Length];
                    bool compatible=true;
                    for(int i=0;i<args.Length;i++)
                    {
                        object a=args[i];
                        Type pt=ps[i].ParameterType;
                        if(a==null) callArgs[i]=null;
                        else if(pt.IsInstanceOfType(a)) callArgs[i]=a;
                        else
                        {
                            try{callArgs[i]=ConvertValue(a,pt);}
                            catch{compatible=false;break;}
                            if(callArgs[i]!=null && !pt.IsInstanceOfType(callArgs[i]) && !(pt.IsValueType && callArgs[i].GetType()==pt)){compatible=false;break;}
                        }
                    }
                    if(!compatible) continue;

                    for(int i=args.Length;i<ps.Length;i++)
                        callArgs[i]=ps[i].DefaultValue==DBNull.Value ? Type.Missing : ps[i].DefaultValue;

                    m.Invoke(o,callArgs);
                    return true;
                }
                catch{}
            }
        }
        return false;
    }
    static void InvokeIfExists(object o,string n){TryInvoke(o,new[]{n});}
    static Color Hex(string h){Color c=Color.white;ColorUtility.TryParseHtmlString(h,out c);return c;}
    static void SetColor(Material m,string p,Color c){if(m&&m.HasProperty(p))m.SetColor(p,c);}
    static void SetFloat(Material m,string p,float v){if(m&&m.HasProperty(p))m.SetFloat(p,v);}
    static void EnsureFolders(){foreach(var p in new[]{ROOT,GRAPHS,MATS,PREFABS,SCENES,SETTINGS})EnsureFolder(p);}
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=Path.GetDirectoryName(path).Replace("\\","/");string name=Path.GetFileName(path);
        if(!AssetDatabase.IsValidFolder(parent))EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,name);
    }
}
