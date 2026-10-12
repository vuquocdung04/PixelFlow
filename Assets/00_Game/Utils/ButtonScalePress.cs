using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonScalePress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float pressScale = 0.8f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.15f;
    [SerializeField] private Ease releaseEase = Ease.OutBack;

    private Vector3 _originalScale;
    private Tween _tween;

    // Khi nút co lại thì nới vùng raycast tương ứng, để thả tay ở mép nút vẫn tính là click
    private readonly List<Graphic> _raycastGraphics = new();
    private readonly List<Vector4> _defaultPaddings = new();

    private void Awake()
    {
        _originalScale = transform.localScale;
        CacheRaycastGraphics();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ExpandRaycastArea();

        _tween?.Kill();
        _tween = transform.DOScale(_originalScale * pressScale, pressDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RestoreRaycastArea();

        _tween?.Kill();
        _tween = transform.DOScale(_originalScale, releaseDuration).SetEase(releaseEase).SetUpdate(true);
    }

    private void OnDisable()
    {
        RestoreRaycastArea();

        _tween?.Kill();
        transform.localScale = _originalScale;
    }

    private void CacheRaycastGraphics()
    {
        _raycastGraphics.Clear();
        _defaultPaddings.Clear();

        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (!graphic.raycastTarget) continue;
            _raycastGraphics.Add(graphic);
            _defaultPaddings.Add(graphic.raycastPadding);
        }
    }

    private void ExpandRaycastArea()
    {
        float factor = 1f / Mathf.Max(pressScale, 0.01f) - 1f;
        if (Mathf.Approximately(factor, 0f)) return;

        for (int i = 0; i < _raycastGraphics.Count; i++)
        {
            Graphic graphic = _raycastGraphics[i];
            if (graphic == null) continue;

            Rect rect = graphic.rectTransform.rect;
            Vector2 offset = GetLocalOffset(graphic.rectTransform);

            graphic.raycastPadding = new Vector4(
                (rect.xMin + offset.x) * factor,
                (rect.yMin + offset.y) * factor,
                -(rect.xMax + offset.x) * factor,
                -(rect.yMax + offset.y) * factor);
        }
    }

    private void RestoreRaycastArea()
    {
        for (int i = 0; i < _raycastGraphics.Count; i++)
            if (_raycastGraphics[i] != null) _raycastGraphics[i].raycastPadding = _defaultPaddings[i];
    }

    private Vector2 GetLocalOffset(Transform target)
    {
        Vector2 offset = Vector2.zero;
        while (target != null && target != transform)
        {
            offset += (Vector2)target.localPosition;
            target = target.parent;
        }
        return offset;
    }
}
