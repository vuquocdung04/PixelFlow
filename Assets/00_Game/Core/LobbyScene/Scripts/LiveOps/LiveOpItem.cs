using System;
using DG.Tweening;
using EventDispatcher;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LiveOpItem : MonoBehaviour
{
    [SerializeField] private LiveOpId id;
    [SerializeField] private int levelUnlock = 1;

    [Header("Refs")]
    [SerializeField] private Button btnMain;
    [SerializeField] private Transform icon;
    [SerializeField] private GameObject objNoti;
    [SerializeField] private GameObject objLock;
    [SerializeField] private TextMeshProUGUI txtLevelUnlock;
    [SerializeField] private TextMeshProUGUI txtTimer;

    private bool _init;
    private bool _counting;
    private float _nextTick;
    private Tween _wiggle;

    public LiveOpId Id => id;
    public bool IsUnlock => UseProfile.Level.Value >= levelUnlock;

    public void Init()
    {
        if (_init) return;
        _init = true;

        btnMain.OnClicked(OnClickMain);
        this.RegisterListener(EventID.DAILY_CHANGED, OnDailyChanged);
        Refresh();
    }

    private void OnEnable()
    {
        if (_init) Refresh();
    }

    private void OnDisable() => StopIdle();

    private void OnDestroy()
    {
        if (_init) this.RemoveListener(EventID.DAILY_CHANGED, OnDailyChanged);
    }

    private void OnDailyChanged(object _) => Refresh();

    public void Refresh()
    {
        bool unlock = IsUnlock;
        bool noti = unlock && LiveOps.HasNoti(id);

        if (objLock != null) objLock.SetActive(!unlock);
        if (txtLevelUnlock != null) txtLevelUnlock.text = $"Lv.{levelUnlock}";
        if (objNoti != null) objNoti.SetActive(noti);

        _counting = unlock && txtTimer != null && LiveOps.SecondsRemaining(id) > 0L;
        if (txtTimer != null) txtTimer.gameObject.SetActive(_counting);
        RefreshTime();

        if (noti && isActiveAndEnabled) PlayIdle();
        else StopIdle();
    }

    private void Update()
    {
        if (!_counting || Time.unscaledTime < _nextTick) return;
        _nextTick = Time.unscaledTime + 0.5f;

        // Hết giờ -> liveop có quà lại
        if (LiveOps.SecondsRemaining(id) <= 0L) Refresh();
        else RefreshTime();
    }

    private void RefreshTime()
    {
        if (!_counting) return;
        txtTimer.text = TimeManager.Format(TimeSpan.FromSeconds(LiveOps.SecondsRemaining(id)));
    }

    // Có quà: icon lắc định kỳ (chấm đỏ tự đập nhịp bằng UIAttentionEffect)
    private void PlayIdle()
    {
        if (_wiggle != null && _wiggle.IsActive()) return;
        _wiggle = UIStagger.Wiggle(icon != null ? icon : transform);
    }

    private void StopIdle()
    {
        _wiggle?.Kill();
        _wiggle = null;
        if (icon != null) icon.localRotation = Quaternion.identity;
    }

    private void OnClickMain()
    {
        if (!IsUnlock)
        {
            ToastManager.Instance.ShowToast($"Reach level {levelUnlock} to unlock");
            return;
        }

        LiveOps.Open(id, LobbyController.Instance.topCanvas);
    }
}
