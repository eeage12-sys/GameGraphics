using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static class DAY08_OutlineObjectSpaceFix
{
    [MenuItem("Tools/DAY08 FIX/Fix Outline Object Space")]
    public static void Fix()
    {
        string path = FindOutlineGraph();
        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog(
                "DAY08 Outline Fix",
                "SG_OutlineShell.shadergraph를 찾지 못했습니다.",
                "확인");
            return;
        }

        try
        {
            string text = File.ReadAllText(path);
            int positionCount = 0;
            int normalCount = 0;

            text = PatchNodeSpace(
                text,
                "UnityEditor.ShaderGraph.PositionNode",
                0,
                ref positionCount);

            text = PatchNodeSpace(
                text,
                "UnityEditor.ShaderGraph.NormalVectorNode",
                0,
                ref normalCount);

            if (positionCount == 0 || normalCount == 0)
            {
                EditorUtility.DisplayDialog(
                    "DAY08 Outline Fix",
                    "Position 또는 Normal Vector 노드를 찾지 못했습니다.\n" +
                    "PositionNode: " + positionCount + "\n" +
                    "NormalVectorNode: " + normalCount,
                    "확인");
                return;
            }

            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceUpdate |
                ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "DAY08 Outline Fix 완료",
                "SG_OutlineShell의 좌표 공간을 수정했습니다.\n\n" +
                "Position = Object\n" +
                "Normal Vector = Object\n\n" +
                "이제 ToonCharacter / ToonOutlineShell의 Local Position은 둘 다 (0,0,0)으로 두고,\n" +
                "부모 03_OutlineShell의 Y만 움직여 보세요.",
                "확인");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog(
                "DAY08 Outline Fix 오류",
                e.Message,
                "확인");
        }
    }

    static string FindOutlineGraph()
    {
        foreach (string guid in AssetDatabase.FindAssets("SG_OutlineShell"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith(".shadergraph", StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileNameWithoutExtension(p)
                    .Equals("SG_OutlineShell", StringComparison.OrdinalIgnoreCase))
                return p;
        }
        return null;
    }

    static string PatchNodeSpace(
        string text,
        string nodeType,
        int objectSpaceValue,
        ref int patchedCount)
    {
        // Shader Graph 파일은 여러 JSON object가 연속된 MultiJson 형식.
        // 해당 Node object 안의 m_Space 값만 0(Object)으로 변경한다.
        string marker = "\"m_Type\": \"" + nodeType + "\"";
        int searchFrom = 0;

        while (true)
        {
            int typeIndex = text.IndexOf(marker, searchFrom, StringComparison.Ordinal);
            if (typeIndex < 0)
                break;

            int objectStart = text.LastIndexOf("\n{", typeIndex, StringComparison.Ordinal);
            if (objectStart < 0)
                objectStart = 0;
            else
                objectStart += 1;

            int nextObject = text.IndexOf("\n\n{", typeIndex, StringComparison.Ordinal);
            int objectEnd = nextObject >= 0 ? nextObject : text.Length;

            string chunk = text.Substring(objectStart, objectEnd - objectStart);

            Match m = Regex.Match(
                chunk,
                "\"m_Space\"\\s*:\\s*\\d+",
                RegexOptions.CultureInvariant);

            if (m.Success)
            {
                Regex spaceRegex = new Regex(
                    "\"m_Space\"\\s*:\\s*\\d+",
                    RegexOptions.CultureInvariant);

                string replaced = spaceRegex.Replace(
                    chunk,
                    "\"m_Space\": " + objectSpaceValue,
                    1);

                text =
                    text.Substring(0, objectStart) +
                    replaced +
                    text.Substring(objectEnd);

                patchedCount++;
                searchFrom = objectStart + replaced.Length;
            }
            else
            {
                searchFrom = objectEnd;
            }
        }

        return text;
    }
}
