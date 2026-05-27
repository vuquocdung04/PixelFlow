using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class DevTool : OdinEditorWindow
{
    [MenuItem("Tools/Mở DevTool")]
    public static void OpenWindow()
    {
        GetWindow<DevTool>("My Dev Tool").Show();
    }

    [SerializeField]
    [ListDrawerSettings(HideRemoveButton = true, ShowIndexLabels = false, CustomAddFunction = "OnAddTester")]
    [Searchable]
    private List<PopupTesterItem> popupTesters = new List<PopupTesterItem>();

    private void OnAddTester()
    {
        popupTesters.Add(new PopupTesterItem(popupTesters));
    }

    [Button("🚀 Bật Fast Play Mode", ButtonHeight = 35)]
    [GUIColor(0.2f, 1f, 0.2f)]
    private void EnableFastPlayMode()
    {
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
    }

    [Button("🐢 Tắt Fast Play Mode", ButtonHeight = 35)]
    [GUIColor(1f, 1f, 0.2f)]
    private void DisableFastPlayMode()
    {
        EditorSettings.enterPlayModeOptionsEnabled = false;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
    }

    [Button("🧹 Clear All & Restart Scene", ButtonHeight = 35)]
    [GUIColor(1f, 0.4f, 0.4f)]
    private void ClearAllAndRestart()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Clear All Data",
            "Xóa toàn bộ save data và reload scene?",
            "OK",
            "Hủy"
        );

        if (!confirm) return;

        GamePrefs.ClearAll();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        if (Application.isPlaying)
        {
            var current = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(current);
        }

        Debug.Log("[DevTool] Cleared all data");
    }
}

[Serializable]
public class PopupTesterItem
{
    [NonSerialized]
    private List<PopupTesterItem> parentList;

    public PopupTesterItem(List<PopupTesterItem> list)
    {
        parentList = list;
    }

    [HorizontalGroup("Row", 0.5f)]
    [AssetSelector(Paths = "Assets")]
    [AssetsOnly, HideLabel]
    public GameObject PopupPrefab;

    [HorizontalGroup("Row", 0.15f)]
    [Button("Show"), GUIColor(0.4f, 1f, 0.4f)]
    [EnableIf("CanClick")]
    public void Show()
    {
        if (PopupPrefab == null) return;

        Transform parent = null;
        SceneUtils.ExecuteInScene(SceneName.GAME_PLAY, () => parent = GameScene.Instance.popupHolder);
        if (parent == null) return;

        Type type = GetBaseBoxType();
        if (type == null) return;

        MethodInfo setupMethod = type.GetMethod("Setup", BindingFlags.Public | BindingFlags.Static);
        setupMethod?.Invoke(null, new object[] { parent, null });
    }

    [HorizontalGroup("Row", 0.15f)]
    [Button("Close"), GUIColor(1f, 0.4f, 0.4f)]
    [EnableIf("CanClick")]
    public void Close()
    {
        if (PopupPrefab == null) return;

        Type type = GetBaseBoxType();
        if (type == null) return;

        PropertyInfo instanceProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        object instance = instanceProp?.GetValue(null);

        if (instance != null)
        {
            MethodInfo closeMethod = type.GetMethod("Close", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            closeMethod?.Invoke(instance, null);
        }
    }

    [HorizontalGroup("Row", 0.1f)]
    [Button(SdfIconType.TrashFill, ""), GUIColor(1f, 0.2f, 0.2f)]
    public void Remove()
    {
        parentList?.Remove(this);
    }

    private bool CanClick => Application.isPlaying && PopupPrefab != null;

    private Type GetBaseBoxType()
    {
        if (PopupPrefab == null) return null;
        var scripts = PopupPrefab.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script == null) continue;
            Type t = script.GetType();
            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(BaseBox<>)) return script.GetType();
                t = t.BaseType;
            }
        }
        return null;
    }
}