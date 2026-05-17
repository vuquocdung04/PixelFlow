// Assets/Editor/FolderColors/FolderColorizer.cs
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FolderColorizer
{
    private static readonly Dictionary<string, Color> _colorMap = new();
    private static bool _loaded;
    private static Texture2D _folderTex;

    static FolderColorizer()
    {
        EditorApplication.projectWindowItemOnGUI += OnGUI;
        FolderColorData.OnDataChanged += RebuildCache;
        EditorApplication.delayCall += RebuildCache;
    }

    private static Texture2D FolderTex =>
        _folderTex != null ? _folderTex :
        (_folderTex = (Texture2D)EditorGUIUtility.IconContent("Folder Icon").image);

    private static void RebuildCache()
    {
        _colorMap.Clear();
        var guids = AssetDatabase.FindAssets("t:FolderColorData");
        foreach (var g in guids)
        {
            var data = AssetDatabase.LoadAssetAtPath<FolderColorData>(
                AssetDatabase.GUIDToAssetPath(g));
            if (data == null) continue;
            foreach (var e in data.entries)
                if (!string.IsNullOrEmpty(e.folderGuid))
                    _colorMap[e.folderGuid] = e.color;
        }
        _loaded = true;
        EditorApplication.RepaintProjectWindow();
    }

    private static void OnGUI(string guid, Rect rect)
    {
        if (!_loaded) RebuildCache();
        if (!_colorMap.TryGetValue(guid, out var color)) return;
        if (FolderTex == null) return;

        // Tree/list view: icon 16x16 ở đầu row
        // Icon view (2-column lớn): icon vuông ở phần trên của rect
        bool isTreeRow = rect.height < 20;
        Rect iconRect = isTreeRow
            ? new Rect(rect.x, rect.y, rect.height, rect.height)
            : new Rect(rect.x, rect.y, rect.width, rect.width);

        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(iconRect, FolderTex, ScaleMode.ScaleToFit);
        GUI.color = prev;
    }
}