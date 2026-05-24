using Cysharp.Threading.Tasks;
using DG.Tweening;
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
    private static AsyncOperationHandle<GameObject> handle;
    private static bool isInstantiating;

    public static async UniTaskVoid Setup(Transform parent, System.Action<T> callback)
    {
        string addressableKey = typeof(T).Name;
        var instance = await GetInstanceAsync(addressableKey, parent);
        callback?.Invoke(instance);
    }

    private static async UniTask<T> GetInstanceAsync(string addressableKey, Transform parent)
    {
        if (Instance != null) return Instance;

        if (isInstantiating)
        {
            await UniTask.WaitUntil(() => Instance != null || !isInstantiating);
            return Instance;
        }

        isInstantiating = true;
        handle = Addressables.InstantiateAsync(addressableKey, parent);
        GameObject obj = await handle.Task;

        if (obj == null)
        {
            Debug.LogError($"[BaseBox] Không tìm thấy key: {addressableKey}");
            isInstantiating = false;
            return null;
        }

        Instance = obj.GetComponent<T>();
        if (Instance == null)
        {
            Addressables.ReleaseInstance(obj);
            isInstantiating = false;
            return null;
        }

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

        if (handle.IsValid()) Addressables.ReleaseInstance(gameObject);
    }

    // ========== FIELDS ==========
    [Header("UI Animation Settings")]
    [SerializeField] protected RectTransform mainPanel;
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected float durationAppeared = 0.3f;
    [SerializeField] protected BoxAnimationType animationType = BoxAnimationType.Scale;

    private Tween currentTween;
    private IShowAnimation _activeAnim;

    public System.Action OnClosed;

    // ========== SHOW / CLOSE ==========
    public void Show() => Show(BoxAnimationFactory.Get(animationType));

    public void Show(IShowAnimation anim)
    {
        _activeAnim = anim;
        InitState();
        KillCurrentTween();
        transform.SetAsLastSibling();
        currentTween = anim.PlayShow(mainPanel, canvasGroup, durationAppeared);
    }

    public void Close() => Close(_activeAnim ?? BoxAnimationFactory.Get(animationType));

    public void Close(IShowAnimation anim)
    {
        KillCurrentTween();
        currentTween = anim.PlayClose(mainPanel, canvasGroup, durationAppeared);

        if (currentTween != null)
            currentTween.OnComplete(InvokeOnClosed);
        else
            InvokeOnClosed();
    }

    private void InvokeOnClosed()
    {
        var cb = OnClosed;
        OnClosed = null;
        cb?.Invoke();
    }

    // ========== HELPERS ==========
    private void ForceHide() => canvasGroup.SetCanvasState(false, 0f);

    private void KillCurrentTween()
    {
        currentTween?.Kill();
        currentTween = null;
    }
}