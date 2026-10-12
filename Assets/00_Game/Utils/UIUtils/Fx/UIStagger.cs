using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public static class UIStagger
{
    // Hiện lần lượt từng phần tử: scale 0.85 -> 1 (OutBack) + fade in. Trả về thời điểm kết thúc.
    public static float PopIn(IReadOnlyList<Transform> items, float delay = 0.15f, float stagger = 0.05f, int cap = 10)
    {
        float end = 0f;
        int order = 0;

        for (int i = 0; i < items.Count; i++)
        {
            Transform t = items[i];
            if (t == null || !t.gameObject.activeInHierarchy) continue;

            var group = t.GetComponent<CanvasGroup>();
            if (group == null) group = t.gameObject.AddComponent<CanvasGroup>();

            float d = delay + Mathf.Min(order, cap) * stagger;
            t.DOKill();
            group.DOKill();
            t.localScale = Vector3.one * 0.85f;
            group.alpha = 0f;

            t.DOScale(1f, 0.3f).SetEase(Ease.OutBack, 2f).SetDelay(d).SetUpdate(true).SetLink(t.gameObject);
            group.DOFade(1f, 0.2f).SetDelay(d).SetUpdate(true).SetLink(t.gameObject);

            end = Mathf.Max(end, d + 0.3f);
            order++;
        }

        return end;
    }

    // Nảy nhẹ định kỳ để mời bấm (chỉ xoay, an toàn với ButtonScalePress)
    public static Tween Wiggle(Transform t, float interval = 2.2f)
    {
        if (t == null) return null;
        return DOTween.Sequence()
            .AppendInterval(interval * 0.4f)
            .Append(t.DOPunchRotation(new Vector3(0f, 0f, 12f), 0.5f, 8, 0.6f))
            .AppendInterval(interval * 0.6f)
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(true)
            .SetLink(t.gameObject);
    }
}
