using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyLoginBox : BaseBox<DailyLoginBox>
{
    public Button btnClose;
    public Button btnCloseByPanel;

    [SerializeField] private DailyLoginData data;
    [SerializeField] private List<DailyLoginItem> items = new();

    [Header("Cheat (chỉ Editor / Development build)")]
    [SerializeField] private Button btnCheatNextDay;

    private readonly List<Transform> _popTargets = new();

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
        Refresh();
        UIStagger.PopIn(_popTargets, delay: 0.12f, stagger: 0.05f);
    }

    private void Refresh()
    {
        if (data == null) return;

        for (int i = 0; i < items.Count; i++)
            items[i].Setup(i, data.GetDay(i), DailyService.LoginState(i, data.Count));
    }

    private void OnClickItem(DailyLoginItem item)
    {
        int index = items.IndexOf(item);

        if (item.State == DailyState.Lock)
        {
            // Số ngày phải chờ: hôm nay đã nhận thì ngày kế tiếp cũng phải đợi sang mai
            int wait = index - DailyService.LoginDay(data.Count) + (DailyService.LoginCanClaim ? 0 : 1);
            ToastManager.Instance.ShowToast(wait <= 1 ? "Come back tomorrow!" : $"Available in {wait} days");
            return;
        }

        if (!item.CanClaim) return;

        var dayReward = data.GetDay(index);
        if (dayReward == null || dayReward.rewards.Count == 0) return;
        if (!DailyService.TryClaimLogin(index, data.Count)) return;

        var rewards = RewardService.GrantForPopup(dayReward.rewards);
        Refresh();

        Transform holder = transform.parent;
        if (item.Fx != null && item.Fx.isActiveAndEnabled)
            item.Fx.PlayClaim(() => RewardBox.ShowRewards(rewards, holder));
        else
            RewardBox.ShowRewards(rewards, holder);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (var item in items)
            if (item != null) item.OnClick -= OnClickItem;
    }
}
