using DG.Tweening;
using UnityEngine;

// Vệt sáng quét ngang qua nút theo chu kỳ. band nằm trong 1 Mask cùng hình nút.
public class UIShineSweep : MonoBehaviour
{
    [SerializeField] private RectTransform band;
    [SerializeField] private float startDelay = 0.8f;
    [SerializeField] private float duration = 0.55f;
    [SerializeField] private float interval = 2.2f;

    private Sequence _loop;

    private void OnEnable()
    {
        if (band == null) return;

        var area = (RectTransform)band.parent;
        float half = area.rect.width * 0.5f + band.rect.width;
        band.anchoredPosition = new Vector2(-half, band.anchoredPosition.y);

        _loop = DOTween.Sequence()
            .AppendInterval(startDelay)
            .Append(band.DOAnchorPosX(half, duration).SetEase(Ease.InOutSine))
            .AppendCallback(() => band.anchoredPosition = new Vector2(-half, band.anchoredPosition.y))
            .AppendInterval(interval)
            .SetLoops(-1)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        _loop?.Kill();
        _loop = null;
    }
}
