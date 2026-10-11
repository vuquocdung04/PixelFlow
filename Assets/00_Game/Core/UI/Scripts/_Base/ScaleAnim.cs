using DG.Tweening;
using UnityEngine;

public class ScaleAnim : IShowAnimation
{
    // Scale từ 0.8 thay vì 0: nhìn mềm hơn, không bị "nảy" mạnh
    private static readonly Vector3 ClosedScale = new Vector3(0.8f, 0.8f, 1f);

    public Tween PlayShow(RectTransform panel, CanvasGroup cg, float duration)
    {
        panel.localScale = ClosedScale;
        cg.SetCanvasState(true, 0);
        cg.DOFade(1f, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        return panel.DOScale(Vector3.one, duration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public Tween PlayClose(RectTransform panel, CanvasGroup cg, float duration)
    {
        cg.SetCanvasState(false);
        cg.DOFade(0f, duration * 0.8f).SetEase(Ease.InQuad).SetUpdate(true);
        return panel.DOScale(ClosedScale, duration * 0.8f).SetEase(Ease.InBack).SetUpdate(true);
    }
}
