using DG.Tweening;
using UnityEngine;

public class NoAnim : IShowAnimation
{
    public Tween PlayShow(RectTransform panel, CanvasGroup cg, float duration)
    {
        panel.localScale = Vector3.one;
        cg.SetCanvasState(true, 1f);
        return null;
    }

    public Tween PlayClose(RectTransform panel, CanvasGroup cg, float duration)
    {
        cg.SetCanvasState(false, 0f);
        return null;
    }
}