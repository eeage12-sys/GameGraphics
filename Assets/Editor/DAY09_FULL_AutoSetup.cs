using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;

public static class DAY09_FULL_AutoSetup
{
    const string ROOT = "Assets/DAY09";
    const string SCENES = ROOT + "/Scenes";
    const string MATERIALS = ROOT + "/Materials";
    const string TEXTURES = ROOT + "/Textures";
    const string SCRIPTS = ROOT + "/Scripts";
    const string SCENE_PATH = SCENES + "/DAY09_ParticleBasics_COMPLETE.unity";

    [MenuItem("Tools/DAY09 FULL/Build Complete DAY09")]
    public static void BuildCompleteDAY09()
    {
        EnsureFolders();

        try
        {
            Texture2D tex = CreateSoftParticleTexture();
            Material sparkMat = CreateParticleMaterial("Mat_HitSpark", tex, new Color(1f, 0.72f, 0.15f, 1f));
            Material dustMat = CreateParticleMaterial("Mat_Dust", tex, new Color(0.55f, 0.48f, 0.40f, 0.65f));
            Material explosionMat = CreateParticleMaterial("Mat_Explosion", tex, new Color(1f, 0.32f, 0.06f, 1f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("DAY09_COMPLETE");

            Camera cam = CreateCamera(root.transform);
            CreateLight(root.transform);
            CreateGround(root.transform);

            BuildHitEffectSection(root.transform, cam, sparkMat);
            BuildSpaceAnalysisSection(root.transform);
            BuildSimulationSpaceSection(root.transform, sparkMat);
            BuildExtraEffectsSection(root.transform, dustMat, explosionMat);

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "DAY09 완료",
                "DAY09 전체 실습을 다시 구성했습니다.\n\n" +
                "Scene: Assets/DAY09/Scenes/DAY09_ParticleBasics_COMPLETE.unity\n\n" +
                "포함 내용:\n" +
                "• FX_HitSpark_Test\n" +
                "• Main / Emission Burst / Shape\n" +
                "• Color over Lifetime / Size over Lifetime\n" +
                "• World/Local Simulation Space 비교\n" +
                "• 실제 Raycast Hit Point 1회 재생\n" +
                "• 3D 공간 분석용 배치\n" +
                "• 폭발 / 먼지 보조 예제\n" +
                "• URP Particle Material / Renderer",
                "확인");

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("DAY09 생성 오류", e.Message, "확인");
        }
    }

    static void EnsureFolders()
    {
        EnsureFolder(ROOT);
        EnsureFolder(SCENES);
        EnsureFolder(MATERIALS);
        EnsureFolder(TEXTURES);
        EnsureFolder(SCRIPTS);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string name = Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }

    static Texture2D CreateSoftParticleTexture()
    {
        string path = TEXTURES + "/T_SoftParticle.png";

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / radius;
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material CreateParticleMaterial(string name, Texture2D tex, Color tint)
    {
        string path = MATERIALS + "/" + name + ".mat";

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            throw new Exception("Particle용 Shader를 찾지 못했습니다.");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Camera CreateCamera(Transform parent)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(0f, 4.5f, -12.5f);

        Camera cam = go.AddComponent<Camera>();
        cam.fieldOfView = 55f;
        go.transform.LookAt(new Vector3(0f, 0.75f, 1.4f));
        return cam;
    }

    static void CreateLight(Transform parent)
    {
        GameObject go = new GameObject("Directional Light");
        go.transform.SetParent(parent);
        go.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        Light l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.1f;
    }

    static void CreateGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent);
        ground.transform.position = new Vector3(0f, -1f, 1.5f);
        ground.transform.localScale = new Vector3(1.4f, 1f, 1.0f);
    }

    static void BuildHitEffectSection(Transform parent, Camera cam, Material mat)
    {
        GameObject section = new GameObject("01_HitEffect_Core");
        section.transform.SetParent(parent);

        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        target.name = "HitTarget_Capsule";
        target.transform.SetParent(section.transform);
        target.transform.position = new Vector3(0f, 0f, 0f);

        ParticleSystem hit = CreateHitSpark("FX_HitSpark_Test", section.transform, mat);
        hit.transform.position = new Vector3(0f, 0.5f, -1f);
        hit.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        DAY09_HitPointDemo demo = cam.gameObject.AddComponent<DAY09_HitPointDemo>();
        demo.demoCamera = cam;
        demo.target = target.transform;
        demo.hitEffect = hit;
        demo.autoDemo = true;
        demo.interval = 2f;
    }

    static ParticleSystem CreateHitSpark(string name, Transform parent, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 0.35f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        main.startColor = new Color(1f, 0.85f, 0.35f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 48;
        main.stopAction = ParticleSystemStopAction.None;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0f, (short)18, (short)28)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.05f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(new Color(1f, 1f, 0.65f), 0f),
                new GradientColorKey(new Color(1f, 0.42f, 0.05f), 1f)
            },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.75f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = new ParticleSystem.MinMaxGradient(g);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0f)
        );
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        // X/Y/Z를 모두 같은 Constant 모드로 맞춰 Unity 오류를 피합니다.
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.35f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);
        velocity.space = ParticleSystemSimulationSpace.World;

        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.lengthScale = 1.8f;
        r.velocityScale = 0.35f;
        r.material = mat;
        r.sortingFudge = 0.2f;

        return ps;
    }

    static void BuildSpaceAnalysisSection(Transform parent)
    {
        GameObject section = new GameObject("02_3DSpace_Analysis");
        section.transform.SetParent(parent);
        section.transform.position = new Vector3(4f, 0f, 1.2f);

        GameObject weaponTip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        weaponTip.name = "A_WeaponTip_Position";
        weaponTip.transform.SetParent(section.transform);
        weaponTip.transform.localPosition = new Vector3(-1.4f, 0.2f, 0f);
        weaponTip.transform.localScale = Vector3.one * 0.25f;

        GameObject targetCenter = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        targetCenter.name = "B_TargetCenter_Position";
        targetCenter.transform.SetParent(section.transform);
        targetCenter.transform.localPosition = new Vector3(0f, 0f, 0f);

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "C_WallSurface_Direction";
        wall.transform.SetParent(section.transform);
        wall.transform.localPosition = new Vector3(1.8f, 0f, 0f);
        wall.transform.localScale = new Vector3(0.25f, 2f, 2f);

        GameObject far = GameObject.CreatePrimitive(PrimitiveType.Cube);
        far.name = "D_CameraDistance_Check";
        far.transform.SetParent(section.transform);
        far.transform.localPosition = new Vector3(0f, 0f, 4f);
        far.transform.localScale = Vector3.one * 0.5f;
    }

    static void BuildSimulationSpaceSection(Transform parent, Material mat)
    {
        GameObject section = new GameObject("03_SimulationSpace_Comparison");
        section.transform.SetParent(parent);
        section.transform.position = new Vector3(-4.5f, 0f, 2.2f);

        GameObject mover = new GameObject("MovingEmitterRoot");
        mover.transform.SetParent(section.transform);
        mover.AddComponent<DAY09_LocalWorldMover>();

        CreateTrailEmitter("Local_SimulationSpace", mover.transform, new Vector3(0f, 0f, 0f), mat, ParticleSystemSimulationSpace.Local);
        CreateTrailEmitter("World_SimulationSpace", mover.transform, new Vector3(0f, 1.25f, 0f), mat, ParticleSystemSimulationSpace.World);
    }

    static void CreateTrailEmitter(string name, Transform parent, Vector3 localPos, Material mat, ParticleSystemSimulationSpace space)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 0.9f;
        main.startSpeed = 0.25f;
        main.startSize = 0.13f;
        main.startColor = new Color(0.35f, 0.85f, 1f, 1f);
        main.simulationSpace = space;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 16f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.06f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.4f, 0.95f, 1f), 0f),
                new GradientColorKey(new Color(0.2f, 0.45f, 1f), 1f)
            },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = new ParticleSystem.MinMaxGradient(g);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);
        velocity.space = space;

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = mat;
    }

    static void BuildExtraEffectsSection(Transform parent, Material dustMat, Material explosionMat)
    {
        GameObject section = new GameObject("04_Extra_Explosion_Dust");
        section.transform.SetParent(parent);
        section.transform.position = new Vector3(4.2f, 0f, -2.2f);

        CreateDust("FX_Dust_Test", section.transform, new Vector3(-1.1f, 0f, 0f), dustMat);
        CreateExplosion("FX_Explosion_Test", section.transform, new Vector3(1.1f, 0f, 0f), explosionMat);
    }

    static void CreateDust(string name, Transform parent, Vector3 localPos, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.8f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startColor = new Color(0.55f, 0.48f, 0.40f, 0.55f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)10, (short)16) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.35f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.7f, 0.62f, 0.52f), 0f),
                new GradientColorKey(new Color(0.4f, 0.35f, 0.32f), 1f)
            },
            new[] {
                new GradientAlphaKey(0.65f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = new ParticleSystem.MinMaxGradient(g);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.4f, 1f, 1.2f));

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.material = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
    }

    static void CreateExplosion(string name, Transform parent, Vector3 localPos, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.45f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
        main.startColor = new Color(1f, 0.4f, 0.05f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)20, (short)32) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(new Color(1f, 1f, 0.6f), 0f),
                new GradientColorKey(new Color(1f, 0.15f, 0.02f), 1f)
            },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = new ParticleSystem.MinMaxGradient(g);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.25f),
                new Keyframe(0.15f, 1.2f),
                new Keyframe(1f, 0f)
            )
        );

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.material = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
    }
}
