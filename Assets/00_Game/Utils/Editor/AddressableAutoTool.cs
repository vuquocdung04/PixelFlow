using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class AddressableAutoTool : OdinEditorWindow
{
    [MenuItem("Tools/Auto Addressable & PathPrefabs")]
    private static void OpenWindow()
    {
        var window = GetWindow<AddressableAutoTool>("Auto Addressable");
        window.prefabFolderPath = string.Empty; // mở lên để trống
        window.Show();
    }

    [Title("Cấu hình Thư mục Prefab")]
    [FolderPath(RequireExistingPath = true)]
    [InfoBox("Chọn thư mục chứa các UI Prefab (Tool sẽ tự nhớ đường dẫn này cho lần sau)")]
    [OnValueChanged("SavePrefs")] // Tự động lưu mỗi khi bạn thay đổi đường dẫn
    public string prefabFolderPath;

    private void LoadPrefs()
    {
        prefabFolderPath = EditorPrefs.GetString("AutoTool_PrefabPath", "");
    }

    private void SavePrefs()
    {
        EditorPrefs.SetString("AutoTool_PrefabPath", prefabFolderPath);
    }

    [Button("⚡ Tự Động Tích Addressable & Sinh Script", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    private void ProcessPrefabs()
    {
        if (string.IsNullOrEmpty(prefabFolderPath) || !AssetDatabase.IsValidFolder(prefabFolderPath))
        {
            Debug.LogError("Vui lòng chọn thư mục chứa Prefab hợp lệ!");
            return;
        }

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Không tìm thấy Addressable Settings!");
            return;
        }

        AddressableAssetGroup group = settings.DefaultGroup;
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
        List<string> generatedConstants = new List<string>();

        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string prefabName = Path.GetFileNameWithoutExtension(assetPath);

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = prefabName;

            string constName = Regex.Replace(prefabName, "([a-z])([A-Z])", "$1_$2").ToUpper();
            generatedConstants.Add($"    public const string {constName} = \"{prefabName}\";");
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();

        // TỰ ĐỘNG TÌM HOẶC TẠO ĐƯỜNG DẪN SCRIPT (Không cần kéo thả nữa)
        string scriptPath = AutoDetectScriptPath();
        GenerateScript(scriptPath, generatedConstants);

        Debug.Log($"<color=green>[Thành Công]</color> Đã update {prefabGuids.Length} prefabs. File lưu tại: {scriptPath}");
    }

    private string AutoDetectScriptPath()
    {
        // 1. Quét toàn bộ project tìm file tên là "PathPrefabs"
        string[] foundGuids = AssetDatabase.FindAssets("PathPrefabs t:MonoScript");

        if (foundGuids.Length > 0)
        {
            // Trả về đường dẫn của file nếu đã tồn tại (dù bạn vứt nó ở ngóc ngách nào)
            return AssetDatabase.GUIDToAssetPath(foundGuids[0]);
        }

        // 2. Nếu là lần chạy đầu tiên (chưa có file), tự động tạo thư mục mặc định
        string defaultFolder = "Assets/Scripts/Data";

        if (!AssetDatabase.IsValidFolder("Assets/Scripts"))
            AssetDatabase.CreateFolder("Assets", "Scripts");

        if (!AssetDatabase.IsValidFolder(defaultFolder))
            AssetDatabase.CreateFolder("Assets/Scripts", "Data");

        return $"{defaultFolder}/PathPrefabs.cs";
    }

    private void GenerateScript(string path, List<string> constants)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("// AUTO-GENERATED CODE. DO NOT EDIT MANUALLY.");
            writer.WriteLine("public class PathPrefabs");
            writer.WriteLine("{");

            foreach (string constLine in constants)
            {
                writer.WriteLine(constLine);
            }

            writer.WriteLine("}");
        }

        AssetDatabase.Refresh();
    }
    [Button("❌ Untick Addressable (cả sub-folder)", ButtonSizes.Large), GUIColor(1f, 0.5f, 0.4f)]
    private void RemoveAddressables()
    {
        if (string.IsNullOrEmpty(prefabFolderPath) || !AssetDatabase.IsValidFolder(prefabFolderPath))
        {
            Debug.LogError("Vui lòng chọn thư mục chứa Prefab hợp lệ!");
            return;
        }

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Không tìm thấy Addressable Settings!");
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
        int removed = 0;

        foreach (string guid in prefabGuids)
        {
            // RemoveAssetEntry trả về true nếu prefab đó đang là addressable
            if (settings.RemoveAssetEntry(guid, postEvent: false))
                removed++;
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=orange>[AutoTool]</color> Đã untick {removed}/{prefabGuids.Length} prefab trong {prefabFolderPath} (gồm cả sub-folder).");
    }
}