using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Coin UI bay từ popup về ví: toả ra 1 nhịp rồi lần lượt bay vào target.
public static class UICoinFly
{
    public static void Launch(Transform layer, Sprite sprite, Vector3 from, Transform target, int count,
        Action onFirstArrive, Action onEachArrive, Action onComplete, float size = 90f, float spread = 140f)
    {
        if (layer == null || target == null || count <= 0)
        {
            onFirstArrive?.Invoke();
            onComplete?.Invoke();
            return;
        }

        var root = new GameObject("CoinFly", typeof(RectTransform));
        var rootRt = (RectTransform)root.transform;
        rootRt.SetParent(layer, false);
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;
        rootRt.SetAsLastSibling();

        bool first = true;
        int arrived = 0;
        const float burstDuration = 0.25f, flyDuration = 0.5f, stagger = 0.05f;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Coin", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(rootRt, false);
            rt.sizeDelta = new Vector2(size, size);
            rt.position = from;
            rt.localScale = Vector3.zero;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            Vector2 offset = UnityEngine.Random.insideUnitCircle * spread;
            Vector3 spreadPos = rt.localPosition + (Vector3)offset;

            DOTween.Sequence()
                .Append(rt.DOScale(1f, burstDuration).SetEase(Ease.OutBack))
                .Join(rt.DOLocalMove(spreadPos, burstDuration).SetEase(Ease.OutCubic))
                .AppendInterval(i * stagger)
                .Append(rt.DOMove(target.position, flyDuration).SetEase(Ease.InCubic))
                .Join(rt.DOScale(0.7f, flyDuration).SetEase(Ease.InQuad))
                .SetUpdate(true)
                .SetLink(root)
                .OnComplete(() =>
                {
                    if (first)
                    {
                        first = false;
                        onFirstArrive?.Invoke();
                    }
                    onEachArrive?.Invoke();
                    UnityEngine.Object.Destroy(go);

                    if (++arrived < count) return;
                    UnityEngine.Object.Destroy(root);
                    onComplete?.Invoke();
                });
        }
    }
}
