using Cysharp.Threading.Tasks;
using DG.Tweening;
using EventDispatcher;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(GraphicRaycaster))]
public abstract class BaseBox<T> : MonoBehaviour where T : BaseBox<T>
{
    // ========== SINGLETON & ADDRESSABLES ==========
    public static T Instance { get; private set; }

    // Prefab load 1 lần rồi giữ suốt game (UI dùng lại nhiều lần, không unload/load lại mỗi scene)
    private static AsyncOperationHandle<GameObject> prefabHandle;
    private static bool isInstantiating;
    private bool _postedOpen;
    private Canvas _canvas;

    public static async UniTaskVoid Setup(Transform parent, System.Action<T> callback)
    {
        bool isNew = Instance == null;
        var instance = await GetInstanceAsync(parent);

        // Frame Instantiate rất nặng -> chờ qua frame đó rồi mới chạy anim, tránh tween bị nhảy
        if (isNew && instance != null) await UniTask.NextFrame();
        callback?.Invoke(instance);
    }

    // Load sẵn prefab vào bộ nhớ (chưa Instantiate, chưa gọi Init) để lần mở đầu chỉ tốn Instantiate.
    public static async UniTask Preload()
    {
        if (Instance != null) return;
        await LoadPrefabAsync();
    }

    private static async UniTask<GameObject> LoadPrefabAsync()
    {
        if (!prefabHandle.IsValid())
            prefabHandle = Addressables.LoadAssetAsync<GameObject>(typeof(T).Name);

        await prefabHandle.Task;
        return prefabHandle.Status == AsyncOperationStatus.Succeeded ? prefabHandle.Result : null;
    }

    private static async UniTask<T> GetInstanceAsync(Transform parent)
    {
        if (Instance != null) return Instance;

        if (isInstantiating)
        {
            await UniTask.WaitUntil(() => Instance != null || !isInstantiating);
            return Instance;
        }

        isInstantiating = true;
        GameObject prefab = await LoadPrefabAsync();

        if (prefab == null || parent == null)
        {
            if (prefab == null) Debug.LogError($"[BaseBox] Không tìm thấy key: {typeof(T).Name}");
            isInstantiating = false;
            return null;
        }

        // Instantiate đồng bộ ngay khi có prefab (nhanh hơn Addressables.InstantiateAsync)
        var box = Instantiate(prefab, parent, false).GetComponent<T>();
        if (box == null)
        {
            Debug.LogError($"[BaseBox] Prefab '{typeof(T).Name}' thiếu component {typeof(T).Name}");
            isInstantiating = false;
            return null;
        }

        Instance = box;
        Instance.ForceHide();
        Instance.Init();
        isInstantiating = false;
        return Instance;
    }

    protected abstract void Init();
    protected abstract void InitState();

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            isInstantiating = false;
        }
    }

    // ========== FIELDS ==========
    [Header("UI Animation Settings")]
    [SerializeField] protected RectTransform mainPanel;
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected float durationAppeared = 0.2f;
    [SerializeField] protected BoxAnimationType animationType = BoxAnimationType.Scale;

    private Tween currentTween;
    private IShowAnimation _activeAnim;

    public System.Action OnClosed;

    // ========== SHOW / CLOSE ==========
    public void Show() => Show(BoxAnimationFactory.Get(animationType));

    public void Show(IShowAnimation anim)
    {
        _activeAnim = anim;
        KillCurrentTween();
        SetCanvasVisible(true);
        InitState();
        transform.SetAsLastSibling();

        SceneUtils.ExecuteInScene(SceneName.GAME_PLAY, () =>
       {
           if (!_postedOpen)
           {
               _postedOpen = true;
               this.PostEvent(EventID.POPUP_OPENED);
           }
       });
        currentTween = anim.PlayShow(mainPanel, canvasGroup, durationAppeared);
    }

    public void Close() => Close(_activeAnim ?? BoxAnimationFactory.Get(animationType));

    public void Close(IShowAnimation anim)
    {
        KillCurrentTween();
        if (_postedOpen) { _postedOpen = false; this.PostEvent(EventID.POPUP_CLOSED); }

        currentTween = anim.PlayClose(mainPanel, canvasGroup, durationAppeared);
        if (currentTween != null)
            currentTween.OnComplete(() =>
            {
                ForceHide();
                InvokeOnClosed();
            });
        else
        {
            ForceHide();
            InvokeOnClosed();
        }
    }
    private void InvokeOnClosed()
    {
        var cb = OnClosed;
        OnClosed = null;
        cb?.Invoke();
    }

    // ========== HELPERS ==========
    private void ForceHide()
    {
        canvasGroup.SetCanvasState(false, 0f);
        SetCanvasVisible(false);
    }

    // Box ẩn thì tắt Canvas của nó: không render, không rebuild mỗi frame (alpha = 0 vẫn tốn như đang hiện).
    // Không SetActive(false) để giữ nguyên OnEnable/OnDisable và các listener bên trong box.
    private void SetCanvasVisible(bool visible)
    {
        if (_canvas == null) _canvas = GetComponent<Canvas>();
        if (_canvas != null) _canvas.enabled = visible;
    }

    private void KillCurrentTween()
    {
        currentTween?.Kill();
        currentTween = null;
        // Anim show/close có thêm tween fade riêng trên canvasGroup -> kill luôn để không chồng nhau khi bấm nhanh
        if (mainPanel != null) mainPanel.DOKill();
        if (canvasGroup != null) canvasGroup.DOKill();
    }
}