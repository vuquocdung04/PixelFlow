using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeekQuestItem : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI txtAmount;
        [Tooltip("Tia sáng xoay sau slot, chỉ bật ở mốc cuối (grand prize)")]
        public GameObject ray;
    }

    [SerializeField] private GameObject objClaimed;
    [SerializeField] private GameObject objLock;
    [SerializeField] private GameObject objCoinOn;
    [SerializeField] private GameObject objCoinOff;
    [Tooltip("Node trên đường tiến trình, đường fill chạy qua tâm node này")]
    [SerializeField] private RectTransform coin;
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private Button btnClaim;
    [SerializeField] private List<Slot> slots = new();

    [Header("Feel")]
    [SerializeField] private RectTransform claimedStamp;
    [SerializeField] private float wiggleInterval = 1.6f;

    [Header("Highlight")]
    [Tooltip("Halo mềm sau card; nhấp nháy khi nhận được (và ở mốc cuối)")]
    [SerializeField] private Graphic halo;
    [Tooltip("Vệt sáng + lấp lánh vẽ đè lên card")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private float bobHeight = 6f;
    [SerializeField] private Color claimTint = new(1f, 0.96f, 0.8f, 1f);

    [Header("Goal")]
    [Tooltip("Số coin cần cho mốc, hiện trên nút khoá")]
    [SerializeField] private TextMeshProUGUI txtNeed;
    [Tooltip("RectMask2D neo trái trên nút khoá; độ rộng = tiến độ tới mốc này")]
    [SerializeField] private RectTransform progressClip;

    [Header("Grand Prize")]
    [SerializeField] private Image card;
    [SerializeField] private Color grandTint = new(1f, 0.92f, 0.68f, 1f);

    private enum Fx { None, Target, Claimable, Grand }

    private WeekQuestData _data;
    private CanvasGroup _group;
    private Tween _introTween;
    private Tween _wiggleTween;
    private Tween _goalTween;
    private Sequence _idleSeq;
    private Sequence _stampSeq;
    private Sequence _unlockSeq;
    private Fx _fx = Fx.None;
    private bool _fxApplied;
    private float _goalProgress;
    private bool _cached;
    private Color _cardColor = Color.white;
    private Color _haloColor = Color.white;
    private readonly List<Vector2> _slotPositions = new();

    public int Index { get; private set; } = -1;
    public RectTransform Coin => coin;
    public Button ClaimButton => btnClaim;

    // Đạt mốc kể từ lần xem trước: giữ khoá tới khi PlayUnlock
    public bool PendingUnlock { get; set; }

    public event Action<WeekQuestItem> OnClickClaim;

    private bool IsGrand => _data != null && Index == _data.Count - 1;

    private void Awake()
    {
        Cache();
        if (btnClaim != null) btnClaim.OnClicked(() => OnClickClaim?.Invoke(this));
    }

    private void OnEnable()
    {
        // Popup có thể refresh trước khi OnEnable chạy -> bật lại idle
        if (_fx != Fx.None)
        {
            Fx value = _fx;
            _fx = Fx.None;
            SetFx(value);
        }
    }

    private void OnDisable()
    {
        _introTween?.Kill(true);
        _goalTween?.Kill();
        _unlockSeq?.Kill(true);
        StopWiggle();
        _stampSeq?.Kill(true);
        SetFx(Fx.None);
    }

    private void Cache()
    {
        if (_cached) return;
        _cached = true;

        if (card != null) _cardColor = card.color;
        if (halo != null) _haloColor = halo.color;

        _slotPositions.Clear();
        foreach (var slot in slots)
            _slotPositions.Add(slot?.root != null ? ((RectTransform)slot.root.transform).anchoredPosition : Vector2.zero);
    }

    public void Setup(WeekQuestData data, int index)
    {
        Cache();
        _data = data;
        Index = index;

        if (txtLevel != null) txtLevel.text = (index + 1).ToString();
        if (txtNeed != null) txtNeed.text = data != null ? data.GetCoin(index).ToString() : string.Empty;

        SetupSlots(data?.Get(index));
        Refresh();
    }

    public void Refresh()
    {
        bool claimed = WeekQuestService.IsClaimed(Index);
        bool reached = WeekQuestService.IsReached(_data, Index) && !PendingUnlock;
        bool canClaim = reached && !claimed;
        bool target = !reached && (Index == 0 || WeekQuestService.IsReached(_data, Index - 1));

        if (objClaimed != null) objClaimed.SetActive(claimed);
        if (objLock != null) objLock.SetActive(!reached);
        if (objCoinOn != null) objCoinOn.SetActive(reached);
        if (objCoinOff != null) objCoinOff.SetActive(!reached);

        if (btnClaim != null)
        {
            btnClaim.gameObject.SetActive(canClaim);
            btnClaim.interactable = canClaim;
        }

        if (canClaim) StartWiggle();
        else StopWiggle();

        _goalProgress = target && _data != null ? Mathf.Clamp01((float)WeekQuestService.Coin / Mathf.Max(1, _data.GetCoin(Index))) : 0f;

        if (progressClip != null)
        {
            progressClip.gameObject.SetActive(target);
            _goalTween?.Kill();
            SetGoalWidth(_goalProgress);
        }

        SetFx(canClaim ? Fx.Claimable
            : claimed ? Fx.None
            : IsGrand ? Fx.Grand
            : target ? Fx.Target
            : Fx.None);
    }

    // Hàng bật vào từ hơi nhỏ + trong suốt (dùng cho hiệu ứng mở popup lần lượt)
    public void PlayIntro(float delay)
    {
        if (_group == null) _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

        _introTween?.Kill(true);
        _group.alpha = 0f;
        transform.localScale = Vector3.one * 0.85f;

        _introTween = DOTween.Sequence().SetUpdate(true).SetLink(gameObject).SetDelay(delay)
            .Join(_group.DOFade(1f, 0.2f))
            .Join(transform.DOScale(1f, 0.32f).SetEase(Ease.OutBack, 2f));

        if (progressClip != null && progressClip.gameObject.activeSelf)
        {
            _goalTween?.Kill();
            SetGoalWidth(0f);
            _goalTween = DOVirtual.Float(0f, _goalProgress, 0.7f, SetGoalWidth)
                .SetDelay(delay + 0.25f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }
    }

    // Mốc vừa đạt từ lần xem trước: ổ khoá biến mất, nút Claim bật ra
    public void PlayUnlock()
    {
        PendingUnlock = false;
        Refresh();

        _unlockSeq?.Kill(true);
        _unlockSeq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        if (btnClaim != null)
        {
            Transform button = btnClaim.transform;
            button.localScale = Vector3.zero;
            _unlockSeq.Insert(0f, button.DOScale(1f, 0.45f).SetEase(Ease.OutBack, 2.5f));
        }

        if (coin != null) _unlockSeq.Insert(0f, coin.DOPunchScale(Vector3.one * 0.35f, 0.45f, 7, 0.6f));
        _unlockSeq.Insert(0.05f, transform.DOPunchScale(Vector3.one * 0.06f, 0.35f, 6, 0.6f));
        _unlockSeq.OnKill(() => { if (btnClaim != null) btnClaim.transform.localScale = Vector3.one; });
    }

    // Dấu "đã nhận" đóng xuống hàng
    public void PlayClaimed()
    {
        if (objClaimed == null) return;

        objClaimed.SetActive(true);
        _stampSeq?.Kill(true);
        _stampSeq = DOTween.Sequence().SetUpdate(true).SetLink(objClaimed);

        if (claimedStamp != null)
        {
            claimedStamp.localScale = Vector3.one * 2.2f;
            claimedStamp.localRotation = Quaternion.Euler(0f, 0f, -25f);
            _stampSeq.Join(claimedStamp.DOScale(1f, 0.28f).SetEase(Ease.InQuad));
            _stampSeq.Join(claimedStamp.DORotate(Vector3.zero, 0.28f).SetEase(Ease.OutQuad));
            _stampSeq.Append(claimedStamp.DOPunchScale(Vector3.one * 0.18f, 0.3f, 6, 0.5f));
        }

        _stampSeq.Insert(0.26f, transform.DOPunchPosition(new Vector3(0f, -10f, 0f), 0.3f, 6, 0.5f));
    }

    private void SetGoalWidth(float progress)
    {
        if (progressClip == null || !(progressClip.parent is RectTransform area)) return;
        progressClip.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, area.rect.width * progress);
    }

    // Idle theo trạng thái: nhận được thì sáng + nhấp nhô, mốc kế tiếp nhấp nhô, mốc cuối toả sáng
    private void SetFx(Fx value)
    {
        if (_fxApplied && _fx == value && (value == Fx.None || (_idleSeq != null && _idleSeq.IsActive()))) return;

        _fxApplied = true;
        _fx = value;
        _idleSeq?.Kill();
        _idleSeq = null;
        ResetIdlePose();

        bool glowing = value == Fx.Claimable || value == Fx.Grand;

        if (card != null) card.color = value == Fx.Claimable ? claimTint : IsGrand ? grandTint : _cardColor;
        if (halo != null) halo.gameObject.SetActive(glowing);
        if (overlay != null) overlay.SetActive(glowing);

        foreach (var slot in slots)
            if (slot?.ray != null) slot.ray.SetActive(value == Fx.Grand && slot.root != null && slot.root.activeSelf);

        if (value == Fx.None || !isActiveAndEnabled) return;

        const float half = 0.6f;
        _idleSeq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject).SetLoops(-1);

        if (glowing && halo != null)
        {
            float alpha = _haloColor.a * (value == Fx.Claimable ? 1f : 0.6f);
            _idleSeq.Insert(0f, halo.DOFade(alpha * 0.35f, half).SetEase(Ease.InOutSine));
            _idleSeq.Insert(half, halo.DOFade(alpha, half).SetEase(Ease.InOutSine));
        }

        if (value == Fx.Grand) return;

        for (int i = 0; i < slots.Count && i < _slotPositions.Count; i++)
        {
            GameObject root = slots[i]?.root;
            if (root == null || !root.activeSelf) continue;

            var rect = (RectTransform)root.transform;
            float delay = i * 0.12f;
            float y = _slotPositions[i].y;
            _idleSeq.Insert(delay, rect.DOAnchorPosY(y + bobHeight, half).SetEase(Ease.InOutSine));
            _idleSeq.Insert(delay + half, rect.DOAnchorPosY(y, half).SetEase(Ease.InOutSine));
        }

        _idleSeq.AppendInterval(0.2f);
    }

    private void ResetIdlePose()
    {
        if (halo != null)
        {
            Color c = _haloColor;
            if (_fx == Fx.Grand) c.a *= 0.6f;
            halo.color = c;
        }

        for (int i = 0; i < slots.Count && i < _slotPositions.Count; i++)
            if (slots[i]?.root != null) ((RectTransform)slots[i].root.transform).anchoredPosition = _slotPositions[i];
    }

    private void StartWiggle()
    {
        if (btnClaim == null || (_wiggleTween != null && _wiggleTween.IsActive())) return;

        Transform target = btnClaim.transform;
        _wiggleTween = DOTween.Sequence().SetUpdate(true).SetLink(btnClaim.gameObject)
            .AppendInterval(wiggleInterval * 0.5f)
            .Append(target.DOPunchRotation(new Vector3(0f, 0f, 10f), 0.5f, 8, 0.6f))
            .AppendInterval(wiggleInterval * 0.5f)
            .SetLoops(-1, LoopType.Restart);
    }

    private void StopWiggle()
    {
        if (_wiggleTween == null) return;
        _wiggleTween.Kill();
        _wiggleTween = null;
        if (btnClaim != null) btnClaim.transform.localRotation = Quaternion.identity;
    }

    private void SetupSlots(WeekQuestData.Milestone milestone)
    {
        var rewards = milestone?.rewards;
        int count = rewards?.Count ?? 0;
        var db = RewardDataBase.Instance;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;

            bool show = i < count;
            if (slot.root != null) slot.root.SetActive(show);
            if (!show) continue;

            var reward = rewards[i];
            var sprite = db != null ? db.GetIcon(reward.type) : null;
            if (slot.icon != null && sprite != null) slot.icon.sprite = sprite;
            if (slot.txtAmount != null) slot.txtAmount.text = RewardDataBase.FormatAmount(reward);
        }
    }
}
