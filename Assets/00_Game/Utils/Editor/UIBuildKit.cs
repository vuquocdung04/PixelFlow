#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;

// Helper dựng UI prefab bằng code (dùng tạm khi build prefab qua MCP)
public static class UIBuildKit
{
    public const string PrefabDir = "Assets/00_Game/Core/UI/Prefabs";
    const string T2D = "Assets/00_Game/Arts/Texture2D/";
    const string Popup = T2D + "sactx-0-2048x2048-ASTC 6x6-Popup_SpriteAtlas-aedb7c8f.png";
    const string Button0 = T2D + "sactx-0-2048x2048-ASTC 6x6-Button_SpriteAtlas-37f7e1ed.png";
    const string Button1 = T2D + "sactx-1-1024x2048-ASTC 6x6-Button_SpriteAtlas-37f7e1ed.png";
    const string UI01 = "Assets/00_Game/Arts/Texture/UI_01.png";

    static Dictionary<string, Sprite> _sprites;

    public static Sprite S(string key)
    {
        if (_sprites == null)
        {
            _sprites = new Dictionary<string, Sprite>
            {
                ["dim"] = One("Assets/00_Game/Arts/Texture/square_0.png"),
                ["panel"] = Sub(Popup, "_2"),
                ["panel_blue"] = Sub(Popup, "_0"),
                ["panel_purple"] = Sub(Popup, "_7"),
                ["card_blue"] = Sub(Popup, "_1"),
                ["card_red"] = Sub(Popup, "_3"),
                ["card_yellow"] = Sub(Popup, "_4"),
                ["card_purple"] = Sub(Popup, "_5"),
                ["card_navy"] = Sub(Popup, "_6"),
                ["btn_yellow"] = Sub(Button0, "_0"),
                ["btn_green"] = Sub(Button0, "_1"),
                ["btn_red"] = Sub(Button0, "_2"),
                ["btn_blue"] = Sub(Button0, "_3"),
                ["close"] = Sub(Button1, "_0"),
                ["ribbon"] = One(T2D + "Title_Orange.png"),
                ["check"] = Sub(UI01, "_5"),
                ["lock"] = Sub(UI01, "_26"),
                ["video"] = Sub(UI01, "_25"),
                ["noti"] = One(T2D + "Notification_Red.png"),
                ["bar_bg"] = One(T2D + "Progress Bar Silver Horizontal.png"),
                ["bar_fill"] = One(T2D + "Progress Bar Fill Horizontal.png"),
                ["badge_green"] = One(T2D + "Label_Badge01_Green.png"),
                ["frame_white"] = One(T2D + "Icon_Frame_White.png"),
                ["glow"] = One(T2D + "Daily/Fx_SoftGlow.png"),
                ["rays"] = One(T2D + "Daily/Fx_Rays.png"),
                ["band"] = One(T2D + "Daily/Fx_ShineBand.png"),
                ["sparkle"] = One(T2D + "Daily/Fx_Sparkle.png"),
                ["star_y"] = One(T2D + "Daily/Fx_Star_Yellow.png"),
                ["star_r"] = One(T2D + "Daily/Fx_Star_Red.png"),
                ["star_b"] = One(T2D + "Daily/Fx_Star_Blue.png"),
                ["star_p"] = One(T2D + "Daily/Fx_Star_Purple.png"),
                ["gift_big"] = One(T2D + "Daily/Gift_Box_Big.png"),
                ["calendar"] = One(T2D + "Daily/Icon_Calendar.png"),
                ["gift"] = One(T2D + "Daily/Icon_Gift.png"),
                ["coin"] = RewardDataBase.Instance.GetIcon(RewardType.Coin),
                ["mask"] = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
            };
        }
        return _sprites.TryGetValue(key, out var s) ? s : null;
    }

    static Sprite One(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    static Sprite Sub(string path, string suffix) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name.EndsWith(suffix));

    public static TMP_FontAsset FontTitle => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/00_Game/Arts/Font/GangwonEducationTteontteonBlack SDF.asset");
    public static TMP_FontAsset FontNumber => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/00_Game/Arts/Font/Black_Inter.asset");

    // ===================== CREATE =====================

    public static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size, Vector2? anchor = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
        rt.anchorMin = rt.anchorMax = a;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(Transform parent, string name, float inset = 0f)
    {
        var rt = Rect(parent, name, Vector2.zero, Vector2.zero);
        Fill(rt, inset);
        return rt;
    }

    public static void Fill(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    public static Image Img(Transform parent, string name, string sprite, Vector2 pos, Vector2 size, bool sliced = false, Color? color = null, float ppu = 1f)
    {
        var rt = Rect(parent, name, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = S(sprite);
        img.raycastTarget = false;
        img.color = color ?? Color.white;
        if (sliced)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppu;
        }
        else img.preserveAspect = true;
        return img;
    }

    public static Image StretchImg(Transform parent, string name, string sprite, bool sliced = true, Color? color = null, float ppu = 1f, float inset = 0f)
    {
        var img = Img(parent, name, sprite, Vector2.zero, Vector2.zero, sliced, color, ppu);
        Fill(img.rectTransform, inset);
        img.preserveAspect = false;
        return img;
    }

    public static TextMeshProUGUI Txt(Transform parent, string name, string text, float fontSize, Vector2 pos, Vector2 size, bool title = true, Color? color = null)
    {
        var rt = Rect(parent, name, pos, size);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = title ? FontTitle : FontNumber;
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.color = color ?? Color.white;
        return t;
    }

    public static Button Btn(Transform parent, string name, string sprite, Vector2 pos, Vector2 size, bool sliced = true, bool scalePress = true)
    {
        var img = Img(parent, name, sprite, pos, size, sliced);
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
        if (scalePress) img.gameObject.AddComponent<ButtonScalePress>();
        return b;
    }

    // Root của 1 box: Canvas + CanvasGroup + Raycaster + script; dim nền bấm để đóng; SafeArea.
    public static GameObject BoxRoot<T>(string name, out Button dimButton, out RectTransform safe, float dimAlpha = 0.9f) where T : Component
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Canvas), typeof(GraphicRaycaster));
        root.layer = LayerMask.NameToLayer("UI");
        Fill((RectTransform)root.transform);
        root.AddComponent<T>();

        var dim = StretchImg(root.transform, "bg", "dim", false, new Color(0, 0, 0, dimAlpha));
        dim.raycastTarget = true;
        dimButton = dim.gameObject.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;
        safe = Stretch(root.transform, "SafeArea");
        return root;
    }

    // ===================== SERIALIZE =====================

    public static void Set(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogError($"[UIBuildKit] {target.GetType().Name} không có field '{field}'"); return; }

        switch (value)
        {
            case Object o: p.objectReferenceValue = o; break;
            case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
            case int i: p.intValue = i; break;
            case float f: p.floatValue = f; break;
            case bool b: p.boolValue = b; break;
            case string s: p.stringValue = s; break;
            case Vector2 v: p.vector2Value = v; break;
            case System.Collections.IList list:
                p.arraySize = list.Count;
                for (int k = 0; k < list.Count; k++)
                {
                    var e = p.GetArrayElementAtIndex(k);
                    if (list[k] is Object lo) e.objectReferenceValue = lo;
                    else if (list[k] is float lf) e.floatValue = lf;
                }
                break;
            default: Debug.LogError($"[UIBuildKit] Không hỗ trợ kiểu {value?.GetType()} cho {field}"); break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetEnum(Object target, string field, int enumValueIndex)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).enumValueIndex = enumValueIndex;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Gán phần tử 'field' (struct/class serializable) trong list
    public static void SetInList(Object target, string listField, int index, string relField, Object value)
    {
        var so = new SerializedObject(target);
        var list = so.FindProperty(listField);
        if (list.arraySize <= index) list.arraySize = index + 1;
        list.GetArrayElementAtIndex(index).FindPropertyRelative(relField).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ===================== SAVE =====================

    public static GameObject SavePrefab(GameObject root, string path, bool addressable = true)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        if (addressable)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false);
            entry.address = Path.GetFileNameWithoutExtension(path);
            settings.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        }
        AssetDatabase.SaveAssets();
        return prefab;
    }

    // ===================== PREVIEW =====================

    // Render prefab (hoặc instance đã dựng) ra PNG ở độ phân giải tham chiếu 1440x2560, scale /2
    public static string Render(GameObject prefabOrInstance, string outPng, System.Action<GameObject> prepare = null, bool asIs = false)
    {
        var camGo = new GameObject("PreviewCam", typeof(Camera));
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.25f, 0.55f, 0.85f, 1f);
        cam.orthographic = true;
        cam.cullingMask = ~0;
        camGo.transform.position = new Vector3(5000, 5000, -10);

        var canvasGo = new GameObject("PreviewCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 5;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 2560);
        scaler.matchWidthOrHeight = 1f;

        var rtex = new RenderTexture(720, 1280, 24);
        cam.targetTexture = rtex;

        GameObject inst = asIs ? prefabOrInstance : (GameObject)Object.Instantiate(prefabOrInstance);
        inst.transform.SetParent(canvasGo.transform, false);
        var cg = inst.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;
        var c = inst.GetComponent<Canvas>();
        if (c != null) c.enabled = true;
        prepare?.Invoke(inst);

        Canvas.ForceUpdateCanvases();
        foreach (var lg in inst.GetComponentsInChildren<LayoutGroup>(true))
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)lg.transform);
        Canvas.ForceUpdateCanvases();

        cam.Render();
        RenderTexture.active = rtex;
        var tex = new Texture2D(720, 1280, TextureFormat.RGB24, false);
        tex.ReadPixels(new UnityEngine.Rect(0, 0, 720, 1280), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(outPng, tex.EncodeToPNG());

        cam.targetTexture = null;
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rtex);
        Object.DestroyImmediate(canvasGo);
        Object.DestroyImmediate(camGo);
        return outPng;
    }
}
#endif
