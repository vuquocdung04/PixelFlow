using DG.Tweening;
using UnityEngine;

public class SlideAnim : IShowAnimation
{
    public enum Direction { FromLeft, FromRight, ToLeft, ToRight }

    private readonly Direction _direction;

    public SlideAnim(Direction direction)
    {
        _direction = direction;
    }

    public Tween PlayShow(RectTransform panel, CanvasGroup cg, float duration)
    {
        cg.SetCanvasState(true, 1f);
        float w = panel.rect.width > 0 ? panel.rect.width : Screen.width;
        float startX = _direction == Direction.FromLeft ? -w : w;
        panel.anchoredPosition = new Vector2(startX, 0);
        return panel.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic);
    }

    public Tween PlayClose(RectTransform panel, CanvasGroup cg, float duration)
    {
        float w = panel.rect.width > 0 ? panel.rect.width : Screen.width;
        float endX = _direction == Direction.ToLeft ? -w : w;
        return panel.DOAnchorPos(new Vector2(endX, 0), duration).SetEase(Ease.OutCubic);
    }
}