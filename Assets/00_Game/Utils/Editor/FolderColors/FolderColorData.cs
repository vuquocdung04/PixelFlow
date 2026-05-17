// Assets/Editor/FolderColors/FolderColorData.cs
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "FolderColorData", menuName = "Tools/Folder Color Data")]
public class FolderColorData : ScriptableObject
{
    public static event System.Action OnDataChanged;

    [System.Serializable]
    public class Entry
    {
        [HorizontalGroup("r", Width = 70), HideLabel]
        public Color color = new Color(0.3f, 0.6f, 1f, 0.35f);

        [HorizontalGroup("r"), HideLabel]
        [AssetSelector(Filter = "t:DefaultAsset")]
        public DefaultAsset folder;

        [HideInInspector] public string folderGuid;
    }

    [InfoBox("Kéo folder vào, chọn màu (nhớ chỉnh alpha thấp ~0.3 để không che icon). Lưu là tự refresh.")]
    [ListDrawerSettings(ShowFoldout = false, DraggableItems = true, ShowPaging = false)]
    public List<Entry> entries = new List<Entry>();

    private void OnValidate()
    {
        foreach (var e in entries)
        {
            if (e.folder == null) { e.folderGuid = ""; continue; }
            string path = AssetDatabase.GetAssetPath(e.folder);
            if (AssetDatabase.IsValidFolder(path))
                e.folderGuid = AssetDatabase.AssetPathToGUID(path);
            else { e.folder = null; e.folderGuid = ""; }
        }
        OnDataChanged?.Invoke();
        EditorApplication.RepaintProjectWindow();
    }

    [Button("Force Refresh"), GUIColor(0.5f, 0.9f, 0.5f)]
    private void ForceRefresh()
    {
        OnValidate();
    }
}