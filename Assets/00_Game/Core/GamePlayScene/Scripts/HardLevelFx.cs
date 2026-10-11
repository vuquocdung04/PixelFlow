using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;

// Báo level Hard (giống DrinkPacking): nền tối full màn với viền đỏ nhấp nháy (UILevelHardOutline) + icon đầu lâu.
[RequireComponent(typeof(CanvasGroup))]
public class HardLevelFx : MonoBehaviour
{
    private const string ADDRESS = nameof(HardLevelFx);

    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform icon;

    [Header("Tween")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float duration = 1.5f;

    // Ẩn ngay lúc Instantiate: InstantiateAsync có thể render 1 frame trước khi await xong, gây giật 1 -> 0 -> 1
    private void Awake()
    {
        group.alpha = 0f;
        icon.localScale = Vector3.zero;
    }

    // Đặt ngay dưới popupHolder: đè lên HUD nhưng không che popup
    public static async UniTask Play(Transform popupHolder)
    {
        var handle = Addressables.InstantiateAsync(ADDRESS, popupHolder.parent, false);
        await handle.Task;
        if (handle.Result == null) return;

        var fx = handle.Result.GetComponent<HardLevelFx>();
        fx.transform.SetSiblingIndex(popupHolder.GetSiblingIndex());
        var rect = (RectTransform)fx.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        await fx.PlayFade();
        Addressables.ReleaseInstance(fx.gameObject);
    }

    private async UniTask PlayFade()
    {
        var seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(group.DOFade(1f, fadeDuration));
        seq.Join(icon.DOScale(1f, fadeDuration + 0.15f).SetEase(Ease.OutBack, 2.5f));
        seq.AppendInterval(duration);
        seq.Append(group.DOFade(0f, fadeDuration));

        await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(destroyCancellationToken);
    }
}
