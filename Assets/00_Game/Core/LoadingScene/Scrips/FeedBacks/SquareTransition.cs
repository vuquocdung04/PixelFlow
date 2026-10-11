using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SquareTransition : MonoBehaviour
{
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int IsInvertId = Shader.PropertyToID("_IsInvert");
    private static readonly int GridId = Shader.PropertyToID("_Grid");
    private static readonly int InvSpreadId = Shader.PropertyToID("_InvSpread");
    private static readonly int FeatherId = Shader.PropertyToID("_Feather");

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RawImage rawImage;

    [Header("Square")]
    [SerializeField, Min(1)] private int columns = 9;
    [SerializeField, Range(0.05f, 1f)] private float spread = 0.35f;
    [SerializeField, Range(0f, 0.3f)] private float rowJitter = 0.08f;
    [SerializeField, Range(0f, 0.05f)] private float feather = 0.004f;
    [SerializeField] private Ease coverEase = Ease.InOutQuad;
    [SerializeField] private Ease revealEase = Ease.InOutQuad;

    [Header("Icon")]
    [SerializeField] private RectTransform icon;
    [SerializeField] private float iconDuration = 0.25f;
    [SerializeField] private Ease iconInEase = Ease.OutBack;
    [SerializeField] private Ease iconOutEase = Ease.InBack;

    private Material instance;
    private float progress;

    private Material Mat
    {
        get
        {
            if (instance == null)
            {
                instance = new Material(rawImage.material);
                rawImage.material = instance;
                ApplyGrid();
            }

            return instance;
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);

        if (instance != null) Destroy(instance);
    }

    public void Prepare()
    {
        DOTween.Kill(this);

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        rawImage.enabled = true;
        rawImage.raycastTarget = true;
    }

    public void Hide()
    {
        DOTween.Kill(this);

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        rawImage.enabled = false;
        rawImage.raycastTarget = false;

        SetIcon(0f);
    }

    public void SetCovered()
    {
        SetSquare(0f, true);
        SetIcon(1f);
    }

    public async UniTask Cover(float duration)
    {
        SetIcon(0f);

        await TweenSquare(false, duration, coverEase);
        await TweenIcon(1f, iconInEase);
    }

    public async UniTask Reveal(float duration)
    {
        await TweenIcon(0f, iconOutEase);
        await TweenSquare(true, duration, revealEase);
    }

    private void ApplyGrid()
    {
        float cols = Mathf.Max(1, columns);
        float safeSpread = Mathf.Max(0.001f, spread);
        float jitter = Mathf.Min(rowJitter, 1f - safeSpread);

        instance.SetVector(GridId, new Vector4(cols, 1f / cols, 1f - safeSpread - jitter, jitter));
        instance.SetFloat(InvSpreadId, 1f / safeSpread);
        instance.SetFloat(FeatherId, feather);
    }

    private UniTask TweenSquare(bool invert, float duration, Ease ease)
    {
        SetSquare(0f, invert);

        if (duration <= 0f)
        {
            SetSquare(1f, invert);
            return UniTask.CompletedTask;
        }

        return DOTween.To(() => progress, SetProgress, 1f, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .SetTarget(this)
            .ToUniTask(cancellationToken: destroyCancellationToken);
    }

    private UniTask TweenIcon(float scale, Ease ease)
    {
        if (icon == null || iconDuration <= 0f)
        {
            SetIcon(scale);
            return UniTask.CompletedTask;
        }

        return icon.DOScale(scale, iconDuration)
            .SetEase(ease)
            .SetUpdate(true)
            .SetTarget(this)
            .ToUniTask(cancellationToken: destroyCancellationToken);
    }

    private void SetSquare(float value, bool invert)
    {
        Mat.SetFloat(IsInvertId, invert ? 1f : 0f);
        SetProgress(value);
    }

    private void SetProgress(float value)
    {
        progress = value;
        Mat.SetFloat(ProgressId, value);
    }

    private void SetIcon(float scale)
    {
        if (icon != null) icon.localScale = Vector3.one * scale;
    }
}
