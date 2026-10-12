using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using EventDispatcher;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeekQuestBox : BaseBox<WeekQuestBox>
{
    public Button btnClose;
    public Button btnCloseByPanel;

    [SerializeField] private Transform content;
    [SerializeField] private WeekQuestItem itemTemplate;
    [SerializeField] private TextMeshProUGUI txtTime;
    [Tooltip("Đường dọc nối các node, cao dần tới vị trí tiến độ hiện tại")]
    [SerializeField] private Image imageFill;

    [Header("Bar Progress")]
    [SerializeField] private Image barFill;
    [SerializeField] private TextMeshProUGUI txtProgress;
    [SerializeField] private TextMeshProUGUI txtLevel;

    [Header("Play")]
    [SerializeField] private Button btnPlay;
    [SerializeField] private TextMeshProUGUI txtPlayLevel;

    [Header("Feel")]
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private UIBurstEffect burst;
    [SerializeField] private float rowStagger = 0.05f;
    [SerializeField] private int rowStaggerCap = 8;
    [SerializeField] private float fillDuration = 0.6f;
    [SerializeField] private string soundClaim = "sfx-RewardGiftbox";

    [Header("Gain")]
    [Tooltip("Icon coin trên thanh: '+X' bay lên từ đây và nảy khi đếm xong")]
    [SerializeField] private RectTransform coinIcon;
    [Tooltip("Badge mốc kế tiếp trên thanh; đập nhịp khi gần tới")]
    [SerializeField] private RectTransform badge;
    [SerializeField] private float gainDuration = 0.9f;
    [SerializeField, Range(0f, 1f)] private float almostThere = 0.7f;
    [SerializeField] private Color gainColor = new(1f, 0.89f, 0.36f, 1f);
    [SerializeField] private string soundGain = "Coins";
    [SerializeField] private string soundUnlock = "sfx-RewardGiftbox";

    [Header("Timer")]
    [Tooltip("Ngày cuối tuần: thời gian còn lại nhấp nháy sang màu này")]
    [SerializeField] private Color urgentColor = new(1f, 0.62f, 0.54f, 1f);

    [Header("Cheat (chỉ Editor / Development build)")]
    [SerializeField] private Button btnCheatAddCoin;
    [SerializeField] private int cheatCoin = 500;

    private readonly List<WeekQuestItem> _items = new();
    private readonly List<Tween> _gainTweens = new();
    private WeekQuestData _data;
    private long _lastSeconds = long.MinValue;
    private int _seenCoin;
    private bool _animateFill;
    private bool _isUrgent;
    private Color _timeColor = Color.white;
    private Tween _barTween;
    private Tween _lineTween;
    private Tween _countTween;
    private Tween _badgeTween;
    private Tween _timeTween;
    private Tween _scrollTween;
    private bool _showing;

    protected override void Init()
    {
        _data = WeekQuestData.Instance;
        if (txtTime != null) _timeColor = txtTime.color;

        btnClose.OnClicked(OnClose);
        btnCloseByPanel.OnClicked(OnClose);
        if (btnPlay != null) btnPlay.OnClicked(OnClickPlay);

        if (btnCheatAddCoin != null)
        {
            btnCheatAddCoin.gameObject.SetActive(Debug.isDebugBuild);
            btnCheatAddCoin.OnClicked(() =>
            {
                WeekQuestService.AddCoin(cheatCoin);
                ToastManager.Instance.ShowToast($"+{cheatCoin} week coin");
                Reopen();
            });
        }

        Build();
        this.RegisterListener(EventID.WEEK_QUEST_CHANGED, OnWeekChanged);
    }

    protected override void InitState()
    {
        _showing = true;
        _lastSeconds = long.MinValue;
        _isUrgent = false;

        // Phần coin kiếm thêm từ lần xem trước sẽ được đếm lên trong lần mở này
        _seenCoin = Mathf.Min(WeekQuestService.SeenCoin, WeekQuestService.Coin);
        WeekQuestService.MarkSeen();

        if (txtPlayLevel != null) txtPlayLevel.text = $"Level {UseProfile.Level.Value}";

        StopFeel();
        _animateFill = true;

        MarkPending();
        Refresh();
        PlayIntro();
        PlayGain();

        StartCoroutine(RefreshProgressNextFrame());
    }

    private void Reopen()
    {
        InitState();
    }

    private void OnClose()
    {
        _showing = false;
        StopFeel();
        Close();
    }

    private void OnClickPlay()
    {
        OnClose();
        FXManager.Instance.LoadScene(SceneName.GAME_PLAY);
    }

    private void Update()
    {
        if (_showing) RefreshTime();
    }

    private void OnWeekChanged(object _)
    {
        if (_showing) Refresh();
    }

    private IEnumerator RefreshProgressNextFrame()
    {
        yield return null;
        RefreshLine();
        ScrollToFocus();
        _animateFill = false;
    }

    private void Build()
    {
        if (itemTemplate == null || content == null) return;

        itemTemplate.gameObject.SetActive(false);
        int count = _data != null ? _data.Count : 0;

        while (_items.Count < count)
        {
            var item = Instantiate(itemTemplate, content);
            item.gameObject.SetActive(true);
            item.name = "Milestone_" + _items.Count;
            item.OnClickClaim += OnClaim;
            _items.Add(item);
        }

        for (int i = 0; i < _items.Count; i++)
        {
            bool show = i < count;
            _items[i].gameObject.SetActive(show);
            if (show) _items[i].Setup(_data, i);
        }
    }

    private void MarkPending()
    {
        for (int i = 0; i < _items.Count; i++)
            _items[i].PendingUnlock = IsNewlyReached(i);
    }

    private void Refresh()
    {
        foreach (var item in _items)
            if (item.gameObject.activeSelf) item.Refresh();

        RefreshBar();
        RefreshLine();
    }

    private void PlayIntro()
    {
        int order = 0;
        foreach (var item in _items)
        {
            if (!item.gameObject.activeSelf) continue;
            item.PlayIntro(0.12f + Mathf.Min(order, rowStaggerCap) * rowStagger);
            order++;
        }
    }

    // Đưa mốc nhận được đầu tiên (hoặc mốc kế tiếp cần đạt) vào giữa màn hình
    private void ScrollToFocus()
    {
        int count = _data != null ? Mathf.Min(_data.Count, _items.Count) : 0;
        if (scroll == null || scroll.content == null || count == 0) return;

        int focus = -1;
        for (int i = 0; i < count && focus < 0; i++)
            if (WeekQuestService.CanClaim(_data, i)) focus = i;
        for (int i = 0; i < count && focus < 0; i++)
            if (!WeekQuestService.IsReached(_data, i)) focus = i;
        if (focus < 0) focus = count - 1;

        RectTransform c = scroll.content;
        RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        float overflow = c.rect.height - viewport.rect.height;
        if (overflow <= 0f) return;

        var row = (RectTransform)_items[focus].transform;
        Vector3 center = c.InverseTransformPoint(row.TransformPoint(row.rect.center));
        float fromTop = c.rect.yMax - center.y;
        float offset = Mathf.Clamp(fromTop - viewport.rect.height * 0.5f, 0f, overflow);

        scroll.StopMovement();
        scroll.verticalNormalizedPosition = 1f;
        _scrollTween?.Kill();
        _scrollTween = scroll.DOVerticalNormalizedPos(1f - offset / overflow, 0.5f).SetDelay(0.25f)
            .SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
    }

    private void RefreshBar()
    {
        int count = _data != null ? _data.Count : 0;
        if (count == 0) return;

        int next = 0;
        while (next < count - 1 && WeekQuestService.IsReached(_data, next)) next++;

        int coin = WeekQuestService.Coin;
        int need = Mathf.Max(1, _data.GetCoin(next));
        bool reached = WeekQuestService.IsReached(_data, next);
        float progress = reached ? 1f : Mathf.Clamp01((float)coin / need);

        if (txtLevel != null) txtLevel.text = (next + 1).ToString();

        _countTween?.Kill();
        if (txtProgress != null) txtProgress.text = $"{coin}/{need}";

        // Sau khi kiếm thêm, thanh chạy tiếp từ chỗ người chơi xem lần trước; không thì chạy từ 0
        int from = _animateFill && _seenCoin < coin ? _seenCoin : 0;

        if (_animateFill && txtProgress != null && from < coin)
        {
            txtProgress.text = $"{from}/{need}";
            _countTween = DOVirtual.Float(from, coin, GainDuration, v => txtProgress.text = $"{Mathf.RoundToInt(v)}/{need}")
                .SetDelay(0.2f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }

        SetBarFill(progress, Mathf.Clamp01((float)from / need));

        if (!reached && progress >= almostThere) StartBadgePulse();
        else StopBadgePulse();
    }

    private float GainDuration => WeekQuestService.Coin > _seenCoin ? gainDuration : fillDuration;

    private bool IsNewlyReached(int index) => WeekQuestService.CanClaim(_data, index) && _seenCoin < _data.GetCoin(index);

    private void SetBarFill(float progress, float from)
    {
        if (barFill == null) return;

        _barTween?.Kill();
        if (!_animateFill)
        {
            barFill.fillAmount = progress;
            return;
        }

        barFill.fillAmount = from;
        _barTween = barFill.DOFillAmount(progress, GainDuration).SetDelay(0.2f).SetEase(Ease.OutCubic)
            .SetUpdate(true).SetLink(gameObject);
    }

    // Phần thưởng của việc mở popup: '+X' bay lên, số đếm nảy khi xong, rồi các mốc mới đạt lần lượt mở khoá
    private void PlayGain()
    {
        int gained = WeekQuestService.Coin - _seenCoin;
        float land = 0.2f + GainDuration;

        if (gained > 0)
        {
            SpawnFloater($"+{gained}");
            _gainTweens.Add(DOVirtual.DelayedCall(land, () =>
            {
                if (coinIcon != null) coinIcon.DOPunchScale(Vector3.one * 0.3f, 0.4f, 7, 0.6f).SetUpdate(true).SetLink(coinIcon.gameObject);
                AudioManager.Instance.PlaySfx(soundGain);
            }).SetUpdate(true).SetLink(gameObject));
        }

        int order = 0;
        foreach (var item in _items)
        {
            if (!item.gameObject.activeSelf || !item.PendingUnlock) continue;
            var it = item;
            _gainTweens.Add(DOVirtual.DelayedCall(land + 0.15f + order * 0.3f, () => Unlock(it)).SetUpdate(true).SetLink(gameObject));
            order++;
        }
    }

    private void Unlock(WeekQuestItem item)
    {
        item.PlayUnlock();
        if (burst != null && item.ClaimButton != null) burst.Play(item.ClaimButton.transform.position);
        AudioManager.Instance.PlaySfx(soundUnlock);
    }

    private void SpawnFloater(string text)
    {
        if (txtProgress == null || coinIcon == null) return;

        var floater = Instantiate(txtProgress, coinIcon.parent);
        var rect = floater.rectTransform;
        floater.name = "GainFloater";
        floater.text = text;
        floater.color = gainColor;
        floater.raycastTarget = false;
        rect.anchorMin = rect.anchorMax = coinIcon.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 80f);
        rect.anchoredPosition = coinIcon.anchoredPosition + new Vector2(50f, 0f);
        rect.localScale = Vector3.zero;
        rect.SetAsLastSibling();

        var seq = DOTween.Sequence().SetUpdate(true).SetLink(floater.gameObject).SetDelay(0.15f)
            .Append(rect.DOScale(1f, 0.3f).SetEase(Ease.OutBack, 2.5f))
            .Join(rect.DOAnchorPosY(rect.anchoredPosition.y + 50f, 1.1f).SetEase(Ease.OutCubic))
            .Insert(0.75f, floater.DOFade(0f, 0.5f));
        seq.OnKill(() => { if (floater != null) Destroy(floater.gameObject); });
        _gainTweens.Add(seq);
    }

    private void StartBadgePulse()
    {
        if (badge == null || (_badgeTween != null && _badgeTween.IsActive())) return;
        _badgeTween = badge.DOScale(1.15f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(badge.gameObject);
    }

    private void StopBadgePulse()
    {
        _badgeTween?.Kill();
        _badgeTween = null;
        if (badge != null) badge.localScale = Vector3.one;
    }

    // Đường dọc: chạy từ node đầu tới vị trí tiến độ (nội suy giữa 2 node)
    private void RefreshLine()
    {
        if (imageFill == null) return;

        int count = Mathf.Min(_data != null ? _data.Count : 0, _items.Count);
        if (count == 0 || _items[0].Coin == null) return;

        if (content is RectTransform contentRect) LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        RectTransform fill = imageFill.rectTransform;
        Transform space = fill.parent;
        float top = space.InverseTransformPoint(fill.position).y;
        float bottom = space.InverseTransformPoint(GetFillTarget(count)).y;
        float height = Mathf.Max(0f, top - bottom);

        _lineTween?.Kill();
        if (!_animateFill)
        {
            fill.sizeDelta = new Vector2(fill.sizeDelta.x, height);
            return;
        }

        fill.sizeDelta = new Vector2(fill.sizeDelta.x, 0f);
        _lineTween = fill.DOSizeDelta(new Vector2(fill.sizeDelta.x, height), fillDuration * 1.3f)
            .SetDelay(0.2f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
    }

    private Vector3 GetFillTarget(int count)
    {
        int coin = WeekQuestService.Coin;
        if (coin < _data.GetCoin(0)) return _items[0].Coin.position;

        for (int i = 0; i < count - 1; i++)
        {
            if (WeekQuestService.IsReached(_data, i + 1)) continue;
            float t = Mathf.InverseLerp(_data.GetCoin(i), _data.GetCoin(i + 1), coin);
            return Vector3.Lerp(_items[i].Coin.position, _items[i + 1].Coin.position, t);
        }

        return _items[count - 1].Coin.position;
    }

    private void RefreshTime()
    {
        if (txtTime == null) return;

        long seconds = WeekQuestService.SecondsRemaining;
        if (seconds == _lastSeconds) return;
        _lastSeconds = seconds;

        TimeSpan time = TimeSpan.FromSeconds(seconds);
        txtTime.text = time.Days > 0
            ? $"Ends in {time.Days}d {time.Hours:00}:{time.Minutes:00}"
            : $"Ends in {time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";

        bool urgent = time.Days == 0;
        if (urgent == _isUrgent) return;

        _isUrgent = urgent;
        _timeTween?.Kill();
        txtTime.color = _timeColor;
        if (urgent)
            _timeTween = txtTime.DOColor(urgentColor, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true).SetLink(txtTime.gameObject);
    }

    private void OnClaim(WeekQuestItem item)
    {
        if (item == null || !WeekQuestService.TryClaim(_data, item.Index)) return;

        var rewards = RewardService.GrantForPopup(_data.Get(item.Index).rewards);
        Refresh();
        item.PlayClaimed();

        if (burst != null && item.ClaimButton != null) burst.Play(item.ClaimButton.transform.position);
        AudioManager.Instance.PlaySfx(soundClaim);

        // Để dấu tick đóng xong rồi mới mở popup nhận thưởng
        Transform holder = transform.parent;
        DOVirtual.DelayedCall(0.45f, () => RewardBox.ShowRewards(rewards, holder)).SetUpdate(true).SetLink(gameObject);
    }

    private void StopFeel()
    {
        foreach (var t in _gainTweens) t?.Kill();
        _gainTweens.Clear();
        _barTween?.Kill();
        _lineTween?.Kill();
        _countTween?.Kill(true);
        _scrollTween?.Kill();
        StopBadgePulse();
        _timeTween?.Kill();
        _timeTween = null;
        if (txtTime != null) txtTime.color = _timeColor;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        this.RemoveListener(EventID.WEEK_QUEST_CHANGED, OnWeekChanged);
    }
}
