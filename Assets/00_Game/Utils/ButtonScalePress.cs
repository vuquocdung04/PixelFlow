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

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _tween?.Kill();
        _tween = transform.DOScale(_originalScale * pressScale, pressDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _tween?.Kill();
        _tween = transform.DOScale(_originalScale, releaseDuration).SetEase(releaseEase);
    }

    private void OnDisable()
    {
        _tween?.Kill();
        transform.localScale = _originalScale;
    }
}