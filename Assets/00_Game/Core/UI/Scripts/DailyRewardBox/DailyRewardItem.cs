using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyRewardItem : MonoBehaviour
{
    private const string TimeFormat = @"hh\:mm\:ss";

    [SerializeField] private GameObject objUnlock;
    [SerializeField] private GameObject objClaimed;
    [SerializeField] private GameObject objLock;
    [SerializeField] private GameObject objNoti;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI txtAmount;
    [Tooltip("Chỉ slot free: đếm ngược tới lần nhận tiếp theo")]
    [SerializeField] private TextMeshProUGUI txtTimer;
    [SerializeField] private Button btnMain;
    [SerializeField] private ClaimFx fx;

    private bool _claimed;
    private bool _unlocked;
    private bool _counting;
    private float _nextTick;

    public GameReward Reward { get; private set; }
    public ClaimFx Fx => fx;
    public bool IsFreeSlot { get; private set; }
    public bool CanClaim => Reward != null && _unlocked && !_claimed;

    public event Action<DailyRewardItem> OnClick;

    private void Awake()
    {
        if (btnMain != null) btnMain.OnClicked(() => { if (CanClaim) OnClick?.Invoke(this); });
    }

    public void Setup(GameReward reward, bool isFreeSlot, bool claimed, bool unlocked)
    {
        Reward = reward;
        IsFreeSlot = isFreeSlot;
        _claimed = claimed;
        _unlocked = unlocked;

        if (reward != null)
        {
            var db = RewardDataBase.Instance;
            var sprite = db != null ? db.GetIcon(reward.type) : null;
            if (icon != null && sprite != null) icon.sprite = sprite;
            if (txtAmount != null) txtAmount.text = RewardDataBase.FormatAmount(reward);
        }

        Refresh();
    }

    private void Refresh()
    {
        _counting = IsFreeSlot && _claimed && DailyService.RewardFreeSecondsRemaining > 0L;

        if (objClaimed != null) objClaimed.SetActive(_claimed);
        if (objLock != null) objLock.SetActive(!_unlocked && !_claimed);
        if (objUnlock != null) objUnlock.SetActive(CanClaim);
        if (objNoti != null) objNoti.SetActive(CanClaim);
        if (btnMain != null) btnMain.interactable = CanClaim;
        if (fx != null) fx.SetHighlight(CanClaim);
        if (txtTimer != null) txtTimer.gameObject.SetActive(_counting);

        RefreshTime();
    }

    private void Update()
    {
        if (!_counting || Time.unscaledTime < _nextTick) return;
        _nextTick = Time.unscaledTime + 0.25f;
        RefreshTime();
    }

    private void RefreshTime()
    {
        if (txtTimer == null || !_counting) return;
        txtTimer.text = TimeSpan.FromSeconds(DailyService.RewardFreeSecondsRemaining).ToString(TimeFormat);
    }
}
