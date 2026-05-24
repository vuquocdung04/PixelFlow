using DG.Tweening;
using UnityEngine;

public interface IShowAnimation
{
    Tween PlayShow(RectTransform panel, CanvasGroup cg, float duration);
    Tween PlayClose(RectTransform panel, CanvasGroup cg, float duration);
}