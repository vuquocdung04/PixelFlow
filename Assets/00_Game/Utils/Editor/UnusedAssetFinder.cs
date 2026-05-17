// File: Assets/Editor/UnusedAssetFinder.cs
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class UnusedAssetFinder : OdinEditorWindow
{
    [MenuItem("Tools/Unused Asset Finder")]
    private static void OpenWindow()
    {
        var window = GetWindow<UnusedAssetFinder>();
        window.titleContent = new GUIContent("Unused Asset Finder");
        window.Show();
    }

    [Title("Cấu hình quét", bold: true)]
    [FolderPath(RequireExistingPath = true)]
    [LabelText("Folder cần kiểm tra")]
    [InfoBox("Kéo folder vào đây. Tool sẽ quét toàn bộ project để tìm các asset trong folder không được tham chiếu ở đâu cả.")]
    public string targetFolder;

    [EnumToggleButtons, LabelText("Loại asset")]
    public AssetTypeFilter assetType = AssetTypeFilter.Mesh;

    [LabelText("Bao gồm subfolders")]
    public bool includeSubfolders = true;

    [Title("Kết quả", bold: true)]
    [ShowInInspector, ReadOnly, LabelText("Tổng dung lượng có thể xóa")]
    [ShowIf("@unusedAssets != null && unusedAssets.Count > 0")]
    public string totalSize = "0 B";

    [ShowInInspector]
    [ListDrawerSettings(NumberOfItemsPerPage = 15, DraggableItems = false,
        HideAddButton = true, HideRemoveButton = false, ShowFoldout = true)]
    [LabelText("Assets không được sử dụng")]
    public List<UnusedAssetInfo> unusedAssets = new List<UnusedAssetInfo>();

    public enum AssetTypeFilter
    {
        Mesh, Texture, Material, AudioClip,
        Prefab, Animation, ScriptableObject, All
    }

    [System.Serializable]
    public class UnusedAssetInfo
    {
        [HorizontalGroup("row", Width = 55)]
        [PreviewField(50, ObjectFieldAlignment.Center), HideLabel]
        public Object asset;

        [HorizontalGroup("row"), VerticalGroup("row/info")]
        [LabelText("Path"), ReadOnly]
        public string path;

        [VerticalGroup("row/info")]
        [LabelText("Size"), ReadOnly]
        public string size;

        [HorizontalGroup("row", Width = 70)]
        [Button("Ping")]
        public void Ping()
        {
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
        }
    }

    [Button("QUÉT", ButtonSizes.Gigantic), GUIColor(0.4f, 0.9f, 0.5f)]
    public void Scan()
    {
        if (string.IsNullOrEmpty(targetFolder) || !AssetDatabase.IsValidFolder(targetFolder))
        {
            EditorUtility.DisplayDialog("Lỗi", "Folder không hợp lệ!", "OK");
            return;
        }

        unusedAssets.Clear();

        // 1. Lấy danh sách asset ứng viên trong folder
        string[] guids = AssetDatabase.FindAssets(GetFilter(assetType), new[] { targetFolder });
        var candidates = new HashSet<string>();
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (!includeSubfolders)
            {
                string dir = System.IO.Path.GetDirectoryName(p).Replace('\\', '/');
                if (dir != targetFolder) continue;
            }
            // Bỏ folder ra khỏi danh sách
            if (AssetDatabase.IsValidFolder(p)) continue;
            candidates.Add(p);
        }

        if (candidates.Count == 0)
        {
            EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy asset nào.", "OK");
            return;
        }

        // 2. Quét toàn project, build set các path được tham chiếu
        var referenced = new HashSet<string>();
        var allPaths = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/") || p.StartsWith("Packages/"))
            .Where(p => !candidates.Contains(p)) // tránh self-reference
            .ToArray();

        try
        {
            int total = allPaths.Length;
            for (int i = 0; i < total; i++)
            {
                if (i % 50 == 0 && EditorUtility.DisplayCancelableProgressBar(
                    "Đang quét tham chiếu...", $"{i}/{total}", (float)i / total))
                {
                    return;
                }

                var deps = AssetDatabase.GetDependencies(allPaths[i], false);
                foreach (var d in deps)
                    if (candidates.Contains(d)) referenced.Add(d);
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        // 3. Tổng hợp kết quả
        long totalBytes = 0;
        foreach (var path in candidates.OrderBy(x => x))
        {
            if (referenced.Contains(path)) continue;

            var info = new System.IO.FileInfo(path);
            long bytes = info.Exists ? info.Length : 0;
            totalBytes += bytes;

            unusedAssets.Add(new UnusedAssetInfo
            {
                asset = AssetDatabase.LoadMainAssetAtPath(path),
                path = path,
                size = FormatBytes(bytes)
            });
        }

        totalSize = FormatBytes(totalBytes);

        Debug.Log($"<color=cyan>[Unused Asset Finder]</color> Tìm thấy " +
                  $"<b>{unusedAssets.Count}/{candidates.Count}</b> assets không sử dụng. " +
                  $"Tiết kiệm được: <b>{totalSize}</b>");

        foreach (var item in unusedAssets)
            Debug.Log($"<color=yellow>[UNUSED]</color> {item.path} ({item.size})", item.asset);
    }

    [Button("Xóa tất cả", ButtonSizes.Large), GUIColor(1f, 0.4f, 0.4f)]
    [ShowIf("@unusedAssets != null && unusedAssets.Count > 0")]
    public void DeleteAll()
    {
        if (!EditorUtility.DisplayDialog("Xác nhận xóa",
            $"Xóa {unusedAssets.Count} assets ({totalSize})?\nKhông thể hoàn tác!",
            "Xóa", "Hủy")) return;

        var paths = unusedAssets.Where(u => u.asset != null).Select(u => u.path).ToArray();
        AssetDatabase.DeleteAssets(paths, new List<string>());
        AssetDatabase.Refresh();

        Debug.Log($"<color=red>[Unused Asset Finder]</color> Đã xóa {paths.Length} assets.");
        unusedAssets.Clear();
        totalSize = "0 B";
    }

    [Button("Clear"), GUIColor(0.7f, 0.7f, 0.7f)]
    [ShowIf("@unusedAssets != null && unusedAssets.Count > 0")]
    public void Clear()
    {
        unusedAssets.Clear();
        totalSize = "0 B";
    }

    private string GetFilter(AssetTypeFilter t) => t switch
    {
        AssetTypeFilter.Mesh => "t:Mesh",
        AssetTypeFilter.Texture => "t:Texture",
        AssetTypeFilter.Material => "t:Material",
        AssetTypeFilter.AudioClip => "t:AudioClip",
        AssetTypeFilter.Prefab => "t:Prefab",
        AssetTypeFilter.Animation => "t:AnimationClip",
        AssetTypeFilter.ScriptableObject => "t:ScriptableObject",
        _ => ""
    };

    private string FormatBytes(long bytes)
    {
        string[] s = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int i = 0;
        while (size >= 1024 && i < s.Length - 1) { size /= 1024; i++; }
        return $"{size:0.##} {s[i]}";
    }
}