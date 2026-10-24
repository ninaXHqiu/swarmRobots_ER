using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Writes the UI hierarchy of the SingleCubeInteraction scene to SceneDump.txt (for reference)
public static class SceneDump
{
    public static void Dump()
    {
        var scene = EditorSceneManager.OpenScene("Assets/development/SingleCubeInteraction.unity");
        var sb = new StringBuilder();
        foreach (var root in scene.GetRootGameObjects()) { Walk(root.transform, 0, sb); }
        File.WriteAllText("SceneDump.txt", sb.ToString());
        EditorApplication.Exit(0);
    }

    static void Walk(Transform t, int depth, StringBuilder sb)
    {
        var line = new StringBuilder();
        line.Append(new string(' ', depth * 2)).Append(t.name);
        if (!t.gameObject.activeSelf) { line.Append(" [inactive]"); }
        var rt = t as RectTransform;
        if (rt != null) { line.AppendFormat(" pos=({0:0},{1:0}) size=({2:0},{3:0}) anchor=({4},{5})", rt.anchoredPosition.x, rt.anchoredPosition.y, rt.rect.width, rt.rect.height, rt.anchorMin, rt.anchorMax); }
        foreach (var c in t.GetComponents<Component>())
        {
            if (c == null || c is Transform) { continue; }
            line.Append(" | ").Append(c.GetType().Name);
            var img = c as Image; if (img != null) { line.AppendFormat("(#{0} sprite={1})", ColorUtility.ToHtmlStringRGB(img.color), img.sprite ? img.sprite.name : "none"); }
            var txt = c as Text; if (txt != null) { line.AppendFormat("(\"{0}\" {1}px)", txt.text.Replace("\n", " "), txt.fontSize); }
            var tmp = c as TMPro.TMP_Text; if (tmp != null) { line.AppendFormat("(\"{0}\" {1}px)", tmp.text.Replace("\n", " "), tmp.fontSize); }
            var cam = c as Camera; if (cam != null) { line.AppendFormat("(bg #{0} clear={1})", ColorUtility.ToHtmlStringRGB(cam.backgroundColor), cam.clearFlags); }
            var scaler = c as CanvasScaler; if (scaler != null) { line.AppendFormat("(mode={0} ref={1})", scaler.uiScaleMode, scaler.referenceResolution); }
        }
        sb.AppendLine(line.ToString());
        foreach (Transform child in t) { Walk(child, depth + 1, sb); }
    }
}
