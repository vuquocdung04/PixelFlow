using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DailyRewardBox : BaseBox<DailyRewardBox>
{
    public Button btnClose;
    public Button btnCloseByPanel;

    [SerializeField] private DailyRewardData data;
    [SerializeField] private List<DailyRewardItem> items = new();

    [Header("Progress")]
    [SerializeField] private Image fillProgress;
    [Tooltip("1 node / item, theo thứ tự. Thanh fill chạy tới tâm node đang active.")]
    [SerializeField] private List<Image> nodes = new();
    [SerializeField] private Sprite nodeDone;
    [SerializeField] private Sprite nodeActive;
    [SerializeField] private Sprite nodeLocked;
    [Tooltip("Glow phía sau node đang active")]
    [SerializeField] private RectTransform nodeFx;
    [SerializeField] private float nodePulse = 1.12f;

    [Header("Cheat (chỉ Editor / Development build)")]
    [SerializeField] private Button btnCheatNextDay;

    private readonly List<Transform> _popTargets = new();
    private int _activeNode = -1;
    private Tween _nodeTween;
    private Tween _nodeFxTween;
    private Coroutine _midnightRoutine;

    protected override void Init()
    {
        btnClose.OnClicked(Close);
        btnCloseByPanel.OnClicked(Close);

        if (btnCheatNextDay != null)
        {
            btnCheatNextDay.gameObject.SetActive(Debug.isDebugBuild);
            btnCheatNextDay.OnClicked(() =>
            {
                DailyService.CheatNextDay();
                ToastManager.Instance.ShowToast("+1 Day");
                Refresh();
                ScheduleMidnight();
            });
        }

        foreach (var item in items)
        {
            item.OnClick += OnClickItem;
            _popTargets.Add(item.transform);
        }
    }

    protected override void InitState()
    {
        StopNodeFx();
        _activeNode = -1;
        if (fillProgress != null) fillProgress.fillAmount = 0f;

        Refresh();
        UIStagger.PopIn(_popTargets, delay: 0.12f, stagger: 0.06f);
        ScheduleMidnight();
    }

    private void Refresh()
    {
        if (data == null) return;

        for (int i = 0; i < items.Count; i++)
            items[i].Setup(data.GetSlot(i), i == 0, DailyService.RewardSlotClaimed(i), DailyService.RewardSlotUnlocked(i));

        RefreshProgress();
    }

    private void RefreshProgress()
    {
        int claimed = DailyService.RewardClaimedSteps(items.Count);
        bool allDone = items.Count > 0 && claimed >= items.Count;
        int nextActive = allDone ? -1 : claimed;
        bool advanced = _activeNode >= 0 && nextActive != _activeNode && (nextActive > _activeNode || nextActive < 0);

        RefreshNodes(nextActive, claimed);

        if (fillProgress == null) return;

        float target = allDone ? 1f : GetNodeFill(nextActive, (float)claimed / Mathf.Max(1, items.Count - 1));
        fillProgress.DOKill();

        if (target > fillProgress.fillAmount)
        {
            fillProgress.DOFillAmount(target, 0.5f)
                .SetEase(Ease.OutCubic)
                .SetDelay(0.15f)
                .SetUpdate(true)
                .SetLink(fillProgress.gameObject)
                .OnComplete(() => { if (advanced) PunchNode(nextActive); });
        }
        else
        {
            fillProgress.fillAmount = target;
        }
    }

    private void RefreshNodes(int active, int claimed)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;
            Sprite sprite = i < claimed ? nodeDone : i == active ? nodeActive : nodeLocked;
            if (sprite != null) nodes[i].sprite = sprite;
        }

        if (active == _activeNode && _nodeTween != null && _nodeTween.IsActive()) return;

        StopNodeFx();
        _activeNode = active;

        Image target = active >= 0 && active < nodes.Count ? nodes[active] : null;

        if (nodeFx != null)
        {
            nodeFx.gameObject.SetActive(target != null);
            if (target != null)
            {
                nodeFx.position = target.rectTransform.position;
                nodeFx.localScale = Vector3.one * 0.9f;
                _nodeFxTween = nodeFx.DOScale(1.15f, 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true).SetLink(nodeFx.gameObject);
            }
        }

        if (target == null) return;

        RectTransform rt = target.rectTransform;
        rt.localScale = Vector3.one;
        _nodeTween = rt.DOScale(nodePulse, 0.55f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true).SetLink(rt.gameObject);
    }

    private void PunchNode(int index)
    {
        if (index < 0 || index >= nodes.Count || nodes[index] == null) return;
        nodes[index].rectTransform.DOPunchRotation(new Vector3(0f, 0f, 18f), 0.45f, 8, 0.5f)
            .SetUpdate(true).SetLink(nodes[index].gameObject);
    }

    private void StopNodeFx()
    {
        _nodeTween?.Kill();
        _nodeTween = null;
        _nodeFxTween?.Kill();
        _nodeFxTween = null;

        foreach (var n in nodes)
            if (n != null) n.rectTransform.localScale = Vector3.one;
    }

    // fillAmount để thanh chạy tới đúng tâm node
    private float GetNodeFill(int index, float fallback)
    {
        if (index < 0 || index >= nodes.Count || nodes[index] == null) return Mathf.Clamp01(fallback);

        RectTransform bar = fillProgress.rectTransform;
        Rect rect = bar.rect;
        Vector3 local = bar.InverseTransformPoint(nodes[index].rectTransform.position);
        bool horizontal = fillProgress.fillMethod == Image.FillMethod.Horizontal;
        float t = horizontal
            ? (local.x - rect.xMin) / Mathf.Max(1f, rect.width)
            : (local.y - rect.yMin) / Mathf.Max(1f, rect.height);

        if (fillProgress.fillOrigin == 1) t = 1f - t;
        return Mathf.Clamp01(t);
    }

    private void OnClickItem(DailyRewardItem item)
    {
        int index = items.IndexOf(item);
        if (index < 0 || !item.CanClaim) return;

        // Slot đầu free, các slot sau phải xem quảng cáo
        if (index == 0) Claim(item, index);
        else RewardedAds.Show("daily_reward_" + index, () => Claim(item, index));
    }

    private void Claim(DailyRewardItem item, int index)
    {
        if (item.Reward == null || !DailyService.TryClaimReward(index)) return;

        var rewards = RewardService.GrantForPopup(new List<GameReward> { item.Reward });
        Refresh();
        ScheduleMidnight();

        Transform holder = transform.parent;
        if (item.Fx != null && item.Fx.isActiveAndEnabled)
            item.Fx.PlayClaim(() => RewardBox.ShowRewards(rewards, holder));
        else
            RewardBox.ShowRewards(rewards, holder);
    }

    // Qua 0h slot free mở lại -> refresh để reset tiến trình quảng cáo
    private void ScheduleMidnight()
    {
        if (_midnightRoutine != null) StopCoroutine(_midnightRoutine);
        _midnightRoutine = null;

        long seconds = DailyService.RewardFreeSecondsRemaining;
        if (seconds > 0L) _midnightRoutine = StartCoroutine(WaitMidnight(seconds));
    }

    private IEnumerator WaitMidnight(long seconds)
    {
        yield return new WaitForSecondsRealtime(seconds + 1f);
        _midnightRoutine = null;
        DailyService.NotifyChanged();
        Refresh();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (var item in items)
            if (item != null) item.OnClick -= OnClickItem;
    }
}
