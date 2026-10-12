using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class RewardSlot
{
    public GameObject root;
    public Image icon;
    public TextMeshProUGUI txtAmount;

    public void Setup(GameReward reward)
    {
        // Ẩn hẳn slot để layout group căn giữa các phần thưởng còn lại
        if (root != null) root.SetActive(reward != null);
        if (reward == null) return;

        var db = RewardDataBase.Instance;
        var sprite = db != null ? db.GetIcon(reward.type) : null;
        if (icon != null && sprite != null) icon.sprite = sprite;
        if (txtAmount != null) txtAmount.text = RewardDataBase.FormatAmount(reward);
    }
}

public class DailyLoginItem : MonoBehaviour
{
    [SerializeField] private GameObject objUnlock;
    [SerializeField] private GameObject objLock;
    [SerializeField] private GameObject objClaimed;
    [SerializeField] private GameObject objNoti;
    [SerializeField] private List<RewardSlot> slots = new();
    [SerializeField] private Button btnMain;
    [SerializeField] private TextMeshProUGUI txtDay;
    [SerializeField] private ClaimFx fx;

    private DailyState _state;

    public int Day { get; private set; }
    public DailyState State => _state;
    public bool CanClaim => _state == DailyState.Unlock;
    public ClaimFx Fx => fx;

    public event Action<DailyLoginItem> OnClick;

    private void Awake()
    {
        // Luôn bấm được: box tự xử lý theo trạng thái (nhận thưởng / toast khi còn khoá)
        if (btnMain != null) btnMain.OnClicked(() => OnClick?.Invoke(this));
    }

    public void Setup(int day, DayReward dayReward, DailyState state)
    {
        Day = day;
        if (txtDay != null) txtDay.text = $"Day {day + 1}";

        int count = dayReward != null ? dayReward.rewards.Count : 0;
        for (int i = 0; i < slots.Count; i++)
            slots[i]?.Setup(i < count ? dayReward.rewards[i] : null);

        SetState(state);
    }

    public void SetState(DailyState state)
    {
        _state = state;

        if (objLock != null) objLock.SetActive(state == DailyState.Lock);
        if (objUnlock != null) objUnlock.SetActive(state != DailyState.Lock);
        if (objClaimed != null) objClaimed.SetActive(state == DailyState.Claimed);
        if (objNoti != null) objNoti.SetActive(CanClaim);
        if (fx != null) fx.SetHighlight(CanClaim);
    }
}
