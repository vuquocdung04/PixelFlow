using DG.Tweening;
using UnityEngine;

// Đập nhịp liên tục + định kỳ lắc mạnh 1 cái để mời bấm (chấm noti, nút có quà...)
public class UIAttentionEffect : MonoBehaviour
{
    [SerializeField] private float pulseScale = 1.06f;
    [SerializeField] private float pulseDuration = 0.6f;
    [SerializeField] private float wobbleDelay = 1f;
    [SerializeField] private float wobbleInterval = 3f;
    [SerializeField] private float wobbleDuration = 0.5f;
    [SerializeField] private float wobbleAngle = 12f;
    [SerializeField] private float wobblePunch = 0.12f;

    private Vector3 _originalScale;
    private Quaternion _originalRotation;
    private Tween _pulse;
    private Sequence _wobble;

    private void Awake()
    {
        _originalScale = transform.localScale;
        _originalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        _pulse = transform.DOScale(_originalScale * pulseScale, pulseDuration)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);

        _wobble = DOTween.Sequence()
            .AppendCallback(BeginWobble)
            .Append(transform.DOPunchRotation(new Vector3(0f, 0f, wobbleAngle), wobbleDuration, 8, 0.8f))
            .Join(transform.DOPunchScale(_originalScale * wobblePunch, wobbleDuration, 8, 0.8f))
            .AppendCallback(EndWobble)
            .AppendInterval(wobbleInterval)
            .SetLoops(-1)
            .SetDelay(wobbleDelay)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        _pulse?.Kill();
        _wobble?.Kill();
        _pulse = null;
        _wobble = null;
        ResetPose();
    }

    private void BeginWobble()
    {
        _pulse?.Pause();
        ResetPose();
    }

    private void EndWobble()
    {
        ResetPose();
        _pulse?.Restart();
    }

    private void ResetPose()
    {
        transform.localScale = _originalScale;
        transform.localRotation = _originalRotation;
    }
}
