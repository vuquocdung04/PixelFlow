using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Phồng xẹp nhẹ liên tục, kèm glow (tuỳ chọn) sáng/to theo nhịp. Đặt trên wrapper của nút (ButtonScalePress giữ scale nút).
public class UIBreathLoop : MonoBehaviour
{
    [SerializeField] private float scale = 1.05f;
    [SerializeField] private float duration = 0.7f;

    [Header("Glow (optional)")]
    [SerializeField] private Graphic glow;
    [SerializeField] private float glowAlphaMin = 0.35f;
    [SerializeField] private float glowAlphaMax = 0.8f;
    [SerializeField] private float glowScale = 1.12f;

    private Vector3 _originalScale;
    private Vector3 _glowOriginalScale;
    private Sequence _loop;

    private void Awake()
    {
        _originalScale = transform.localScale;
        if (glow != null) _glowOriginalScale = glow.transform.localScale;
    }

    private void OnEnable()
    {
        ResetPose();

        _loop = DOTween.Sequence().Append(transform.DOScale(_originalScale * scale, duration).SetEase(Ease.InOutSine));
        if (glow != null)
        {
            _loop.Join(glow.DOFade(glowAlphaMax, duration).SetEase(Ease.InOutSine));
            _loop.Join(glow.transform.DOScale(_glowOriginalScale * glowScale, duration).SetEase(Ease.InOutSine));
        }
        _loop.SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
    }

    private void OnDisable()
    {
        _loop?.Kill();
        _loop = null;
        ResetPose();
    }

    private void ResetPose()
    {
        transform.localScale = _originalScale;
        if (glow == null) return;

        glow.transform.localScale = _glowOriginalScale;
        Color c = glow.color;
        c.a = glowAlphaMin;
        glow.color = c;
    }
}
