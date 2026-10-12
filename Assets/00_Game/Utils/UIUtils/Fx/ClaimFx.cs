using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Hiệu ứng cho 1 ô thưởng: idle highlight (nhấp nháy, glow, vệt sáng quét) khi nhận được,
// và punch + đóng dấu + nổ sao khi bấm nhận.
public class ClaimFx : MonoBehaviour
{
    [Header("Highlight (claimable)")]
    [SerializeField] private GameObject highlight;
    [Tooltip("Nhóm vẽ đè lên nội dung ô (vệt sáng, sparkle)")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private Graphic glow;
    [SerializeField] private Transform aura;
    [SerializeField] private RectTransform sweep;
    [SerializeField] private float sweepInterval = 1.6f;
    [SerializeField] private Transform bob;
    [SerializeField] private float bobScale = 1.06f;

    [Header("Claim")]
    [SerializeField] private Transform punchTarget;
    [SerializeField] private Transform stamp;
    [SerializeField] private UIBurstEffect burst;
    [SerializeField] private Transform burstAnchor;
    [SerializeField] private float rewardDelay = 0.45f;

    private bool _isHighlighted;
    private bool _initialized;
    private Sequence _idle;
    private Sequence _sweep;
    private Sequence _claim;
    private Vector3 _bobScale = Vector3.one;
    private Vector3 _punchScale = Vector3.one;
    private Vector3 _stampScale = Vector3.one;
    private Vector3 _auraScale = Vector3.one;
    private float _glowAlpha = 1f;

    public UIBurstEffect Burst { get => burst; set => burst = value; }

    private void Awake() => Init();

    private void Init()
    {
        if (_initialized) return;
        _initialized = true;

        if (bob != null) _bobScale = bob.localScale;
        if (punchTarget != null) _punchScale = punchTarget.localScale;
        if (stamp != null) _stampScale = stamp.localScale;
        if (glow != null) _glowAlpha = glow.color.a;
        if (aura != null) _auraScale = aura.localScale;
    }

    private void OnEnable()
    {
        if (_isHighlighted) PlayIdle();
    }

    private void OnDisable()
    {
        StopIdle();

        // Kill vẫn gọi callback trong OnKill -> không bao giờ mất thưởng
        _claim?.Kill();
        _claim = null;

        ResetPose();
    }

    public void SetHighlight(bool value)
    {
        Init();
        _isHighlighted = value;

        if (highlight != null) highlight.SetActive(value);
        if (overlay != null) overlay.SetActive(value);

        if (value && isActiveAndEnabled) PlayIdle();
        else StopIdle();
    }

    public void PlayClaim(Action onComplete)
    {
        Init();
        SetHighlight(false);

        _claim?.Kill();
        ResetPose();

        Action callback = onComplete;
        _claim = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        if (punchTarget != null)
            _claim.Insert(0f, punchTarget.DOPunchScale(_punchScale * 0.18f, 0.35f, 8, 0.6f));

        if (stamp != null && stamp.gameObject.activeInHierarchy)
        {
            stamp.localScale = _stampScale * 2.4f;
            stamp.localRotation = Quaternion.Euler(0f, 0f, -25f);
            _claim.Insert(0.08f, stamp.DOScale(_stampScale, 0.3f).SetEase(Ease.OutBack, 2.2f));
            _claim.Insert(0.08f, stamp.DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.OutBack));
        }

        _claim.InsertCallback(0.05f, PlayBurst);
        _claim.AppendInterval(Mathf.Max(0f, rewardDelay - _claim.Duration()));
        _claim.OnKill(() =>
        {
            _claim = null;
            Action pending = callback;
            callback = null;
            pending?.Invoke();
        });
    }

    public void PlayBurst()
    {
        if (burst == null) return;
        Transform anchor = burstAnchor != null ? burstAnchor : transform;
        burst.Play(anchor.position);
    }

    private void PlayIdle()
    {
        StopIdle();

        _idle = DOTween.Sequence().SetUpdate(true).SetLink(gameObject).SetLoops(-1);
        const float half = 0.55f;

        if (bob != null)
        {
            _idle.Insert(0f, bob.DOScale(_bobScale * bobScale, half).SetEase(Ease.InOutSine));
            _idle.Insert(half, bob.DOScale(_bobScale, half).SetEase(Ease.InOutSine));
        }

        if (glow != null)
        {
            _idle.Insert(0f, glow.DOFade(_glowAlpha * 0.45f, half).SetEase(Ease.InOutSine));
            _idle.Insert(half, glow.DOFade(_glowAlpha, half).SetEase(Ease.InOutSine));
        }

        if (aura != null)
        {
            _idle.Insert(0f, aura.DOScale(_auraScale * 1.15f, half).SetEase(Ease.InOutSine));
            _idle.Insert(half, aura.DOScale(_auraScale * 0.92f, half).SetEase(Ease.InOutSine));
        }

        PlaySweep();
    }

    private void PlaySweep()
    {
        if (sweep == null) return;

        RectTransform area = sweep.parent as RectTransform;
        float half = area != null ? area.rect.width * 0.5f + sweep.rect.width : 300f;
        sweep.anchoredPosition = new Vector2(-half, 0f);

        _sweep = DOTween.Sequence().SetUpdate(true).SetLink(gameObject).SetLoops(-1);
        _sweep.AppendInterval(0.25f);
        _sweep.Append(sweep.DOAnchorPosX(half, 0.55f).SetEase(Ease.InOutSine));
        _sweep.AppendCallback(() => sweep.anchoredPosition = new Vector2(-half, 0f));
        _sweep.AppendInterval(sweepInterval);
    }

    private void StopIdle()
    {
        _idle?.Kill();
        _idle = null;
        _sweep?.Kill();
        _sweep = null;

        if (aura != null) aura.localScale = _auraScale;
        if (bob != null) bob.localScale = _bobScale;
        if (glow != null)
        {
            Color c = glow.color;
            c.a = _glowAlpha;
            glow.color = c;
        }
    }

    private void ResetPose()
    {
        if (punchTarget != null) punchTarget.localScale = _punchScale;
        if (stamp != null)
        {
            stamp.localScale = _stampScale;
            stamp.localRotation = Quaternion.identity;
        }
    }
}
