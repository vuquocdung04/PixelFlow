using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvatarItem : MonoBehaviour
{
    public Image iconAvatar;
    public Transform choosing;
    public Button btn;

    [Header("Lock")]
    [SerializeField] private GameObject locked;
    [Tooltip("Ổ khoá lắc khi bấm vào avatar chưa mở")]
    [SerializeField] private RectTransform lockIcon;
    [SerializeField] private TextMeshProUGUI txtLevel;

    [Header("Feel")]
    [SerializeField] private float pickPunch = 0.18f;
    [SerializeField] private float deniedShake = 14f;

    private Tween _introTween;
    private Tween _iconTween;
    private Tween _lockTween;
    private Tween _selectTween;
    private Tween _shakeTween;

    public int IdAvatar { get; private set; }
    public int LevelUnlock { get; private set; }
    public bool IsUnlock => LevelUnlock <= 1 || UseProfile.Level.Value >= LevelUnlock;

    public void Setup(int id, Sprite sprite, int levelUnlock, System.Action<AvatarItem> onClick)
    {
        IdAvatar = id;
        LevelUnlock = levelUnlock;
        iconAvatar.sprite = sprite;
        if (txtLevel != null) txtLevel.text = $"Lv.{levelUnlock}";
        SetChoosing(false);
        RefreshLock();

        btn.OnClicked(() => onClick?.Invoke(this));
    }

    public void SetChoosing(bool isOn, bool animate = false)
    {
        bool changed = isOn != choosing.gameObject.activeSelf;
        choosing.gameObject.SetActive(isOn);

        if (!animate || !changed || !isOn) return;

        _selectTween?.Kill(true);
        choosing.localScale = Vector3.one * 0.7f;
        _selectTween = choosing.DOScale(1f, 0.32f).SetEase(Ease.OutBack, 2.2f).SetUpdate(true).SetLink(choosing.gameObject);
    }

    public void RefreshLock()
    {
        if (locked != null) locked.SetActive(!IsUnlock);
    }

    // Ô bật ra từ 0, dùng cho hiệu ứng mở popup lần lượt
    public void PlayIntro(float delay)
    {
        _introTween?.Kill();
        transform.localScale = Vector3.zero;
        _introTween = transform.DOScale(1f, 0.32f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
    }

    // Icon avatar bên trong nảy to hơn (nút đã có ButtonScalePress co lại rồi bounce về)
    public void PlayPick()
    {
        _iconTween?.Kill(true);
        _iconTween = iconAvatar.rectTransform.DOPunchScale(Vector3.one * pickPunch, 0.35f, 6, 0.6f).SetUpdate(true).SetLink(gameObject);
    }

    // Chưa mở: rung ngang + ổ khoá lắc
    public void PlayDenied()
    {
        var rt = (RectTransform)transform;
        _shakeTween?.Kill(true);
        _shakeTween = rt.DOShakeAnchorPos(0.4f, new Vector2(deniedShake, 0f), 18, 0f, false, true).SetUpdate(true).SetLink(gameObject);

        Transform padlock = lockIcon != null ? lockIcon : locked != null ? locked.transform : null;
        if (padlock == null) return;

        _lockTween?.Kill(true);
        _lockTween = DOTween.Sequence().SetUpdate(true).SetLink(padlock.gameObject)
            .Insert(0f, padlock.DOPunchRotation(new Vector3(0f, 0f, 22f), 0.5f, 8, 0.5f))
            .Insert(0f, padlock.DOPunchScale(Vector3.one * 0.2f, 0.3f, 4, 0.5f));
    }

    private void OnDisable()
    {
        _introTween?.Kill();
        _iconTween?.Kill(true);
        _lockTween?.Kill(true);
        _selectTween?.Kill(true);
        _shakeTween?.Kill(true);
        transform.localScale = Vector3.one;
    }
}
