using System.Collections.Generic;
using DG.Tweening;
using EventDispatcher;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvatarBox : BaseBox<AvatarBox>
{
    [Header("Buttons")]
    public Button btnClose;
    public Button btnSave;

    [Header("Main")]
    public Image mainAvatar;

    [Header("Avatar Items")]
    public AvatarItem avatarItemPrefab;
    public Transform avatarItemParent;

    [Header("Feel")]
    [SerializeField] private ScrollRect scroll;
    [Tooltip("Khung avatar lớn: pop khi mở, nảy khi lưu")]
    [SerializeField] private RectTransform previewFrame;
    [Tooltip("Wrapper ngoài nút Save; ButtonScalePress giữ scale của chính nút")]
    [SerializeField] private RectTransform saveHolder;
    [SerializeField] private UIBurstEffect burst;
    [SerializeField] private float itemStagger = 0.03f;
    [SerializeField] private int itemStaggerCap = 12;
    [SerializeField] private float savePulse = 1.05f;
    [SerializeField] private float closeDelayAfterSave = 0.65f;

    [Header("Bubble (avatar chưa mở)")]
    [SerializeField] private CanvasGroup bubble;
    [SerializeField] private TextMeshProUGUI txtBubble;
    [SerializeField] private float bubbleHold = 1.2f;

    [Header("Sound")]
    [SerializeField] private string soundPick = "Click";
    [SerializeField] private string soundDenied = "Toast";
    [SerializeField] private string soundSave = "sfx-RewardGiftbox";

    private readonly List<AvatarItem> _items = new();
    private int _previewId;
    private int _savedId;
    private bool _opening;
    private bool _canSaveShown;
    private Vector3 _saveScale = Vector3.one;
    private Sequence _intro;
    private Sequence _bubbleSeq;
    private Tween _previewTween;
    private Tween _saveTween;
    private Tween _closeTween;

    private AvatarDataBase Data => DataRepo.Instance.avatarData;

    protected override void Init()
    {
        btnClose.OnClicked(OnCloseClicked);
        btnSave.OnClicked(OnSaveClicked);
        if (saveHolder != null) _saveScale = saveHolder.localScale;

        SpawnItems();
    }

    protected override void InitState()
    {
        _opening = true;
        StopFeel();
        HideBubble();

        _savedId = UseProfile.AvatarId.Value;
        _previewId = _savedId;

        foreach (var item in _items) item.RefreshLock();
        ApplyPreview(_previewId, false);

        _canSaveShown = CanSave();
        ScrollToSelected();
        PlayIntro();
        _opening = false;
    }

    private void SpawnItems()
    {
        for (int i = 0; i < Data.Count; i++)
        {
            var item = Instantiate(avatarItemPrefab, avatarItemParent);
            item.Setup(i, Data.GetSpriteById(i), Data.GetLevelUnlock(i), OnAvatarItemClicked);
            _items.Add(item);
        }
    }

    private void OnAvatarItemClicked(AvatarItem item)
    {
        bool changed = item.IdAvatar != _previewId;
        _previewId = item.IdAvatar;
        ApplyPreview(_previewId, changed);
        if (changed) PlayPreviewSwap(item);

        if (item.IsUnlock)
        {
            item.PlayPick();
            AudioManager.Instance.PlaySfx(soundPick);
            HideBubble();
        }
        else
        {
            item.PlayDenied();
            AudioManager.Instance.PlaySfx(soundDenied);
            ShowBubble(item);
        }
    }

    private void ApplyPreview(int id, bool animate)
    {
        mainAvatar.sprite = Data.GetSpriteById(id);

        foreach (var item in _items)
            item.SetChoosing(item.IdAvatar == id, animate);

        RefreshSave();
    }

    // Chỉ lưu được khi đổi sang avatar khác đã mở khoá
    private bool CanSave() => _previewId != _savedId && Data.IsUnlock(_previewId);

    private void RefreshSave()
    {
        bool canSave = CanSave();
        btnSave.interactable = canSave;

        if (canSave != _canSaveShown && !_opening && _intro == null)
            AnimateSaveState(canSave);

        _canSaveShown = canSave;
    }

    private void OnSaveClicked()
    {
        if (!CanSave()) return;

        UseProfile.AvatarId.Value = _previewId;
        _savedId = _previewId;
        this.PostEvent(EventID.CHANGE_AVATAR);

        RefreshSave();
        PlaySaved();

        _closeTween?.Kill();
        _closeTween = DOVirtual.DelayedCall(closeDelayAfterSave, Close).SetUpdate(true).SetLink(gameObject);
    }

    private void OnCloseClicked()
    {
        _previewId = _savedId;
        Close();
    }

    // ================= FEEL =================

    private void PlayIntro()
    {
        _intro?.Kill();
        _intro = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        if (previewFrame != null)
        {
            previewFrame.localScale = Vector3.zero;
            _intro.Insert(0.12f, previewFrame.DOScale(1f, 0.4f).SetEase(Ease.OutBack, 2f));
            _intro.Insert(0.12f, previewFrame.DOPunchRotation(new Vector3(0f, 0f, -8f), 0.55f, 6, 0.5f));
        }

        for (int i = 0; i < _items.Count; i++)
            _items[i].PlayIntro(0.25f + Mathf.Min(i, itemStaggerCap) * itemStagger);

        if (saveHolder != null)
        {
            saveHolder.localScale = Vector3.zero;
            _intro.Insert(0.4f, saveHolder.DOScale(_saveScale, 0.35f).SetEase(Ease.OutBack));
        }

        _intro.OnComplete(() =>
        {
            _intro = null;
            if (_canSaveShown) StartSavePulse();
        });
    }

    // Avatar mới đáp xuống bị bẹp rồi bật lên
    private void PlayPreviewSwap(AvatarItem item)
    {
        RectTransform rt = mainAvatar.rectTransform;
        _previewTween?.Kill();
        rt.localScale = new Vector3(1.18f, 0.78f, 1f);
        _previewTween = DOTween.Sequence().SetUpdate(true).SetLink(mainAvatar.gameObject)
            .Append(rt.DOScale(new Vector3(0.92f, 1.1f, 1f), 0.1f).SetEase(Ease.OutQuad))
            .Append(rt.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutElastic, 1.1f, 0.4f));

        if (burst != null && item.IsUnlock) burst.Play(rt.position);
    }

    private void PlaySaved()
    {
        AudioManager.Instance.PlaySfx(soundSave);

        if (burst != null)
        {
            burst.Play(mainAvatar.rectTransform.position);
            burst.Play(mainAvatar.rectTransform.position);
        }

        if (previewFrame == null) return;

        _previewTween?.Kill();
        previewFrame.localScale = Vector3.one;
        previewFrame.localRotation = Quaternion.identity;
        mainAvatar.rectTransform.localScale = Vector3.one;

        _previewTween = DOTween.Sequence().SetUpdate(true).SetLink(previewFrame.gameObject)
            .Insert(0f, previewFrame.DOPunchScale(Vector3.one * 0.15f, 0.45f, 6, 0.5f))
            .Insert(0f, previewFrame.DOPunchRotation(new Vector3(0f, 0f, 8f), 0.55f, 7, 0.5f));
    }

    private void AnimateSaveState(bool canSave)
    {
        if (saveHolder == null) return;

        _saveTween?.Kill();
        saveHolder.localScale = _saveScale;
        if (!canSave) return;

        _saveTween = saveHolder.DOPunchScale(Vector3.one * 0.12f, 0.35f, 6, 0.5f)
            .SetUpdate(true).SetLink(saveHolder.gameObject).OnComplete(StartSavePulse);
    }

    private void StartSavePulse()
    {
        if (saveHolder == null) return;

        _saveTween?.Kill();
        saveHolder.localScale = _saveScale;
        _saveTween = saveHolder.DOScale(_saveScale * savePulse, 0.6f)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(saveHolder.gameObject);
    }

    private void StopFeel()
    {
        _intro?.Kill();
        _intro = null;
        _previewTween?.Kill();
        _previewTween = null;
        _saveTween?.Kill();
        _saveTween = null;
        _closeTween?.Kill();
        _closeTween = null;

        if (previewFrame != null)
        {
            previewFrame.localScale = Vector3.one;
            previewFrame.localRotation = Quaternion.identity;
        }
        mainAvatar.rectTransform.localScale = Vector3.one;
        if (saveHolder != null) saveHolder.localScale = _saveScale;
    }

    private void ShowBubble(AvatarItem item)
    {
        if (bubble == null) return;

        if (txtBubble != null) txtBubble.text = $"Reach level {item.LevelUnlock} to unlock";

        var rt = (RectTransform)bubble.transform;
        var parent = (RectTransform)rt.parent;
        var cell = (RectTransform)item.transform;

        // Đặt bubble ngay trên ô vừa bấm, giữ trong khung popup
        Vector3 top = cell.TransformPoint(new Vector3(cell.rect.center.x, cell.rect.yMax, 0f));
        Vector2 local = parent.InverseTransformPoint(top);
        float halfWidth = rt.rect.width * 0.5f;
        float limit = Mathf.Max(0f, parent.rect.width * 0.5f - halfWidth - 20f);
        float centerX = parent.rect.center.x;
        local.x = Mathf.Clamp(local.x, centerX - limit, centerX + limit);
        local.y += rt.rect.height * rt.pivot.y - 6f;
        rt.localPosition = new Vector3(local.x, local.y, 0f);

        _bubbleSeq?.Kill();
        bubble.alpha = 1f;
        rt.localScale = Vector3.one * 0.5f;
        _bubbleSeq = DOTween.Sequence().SetUpdate(true).SetLink(bubble.gameObject)
            .Append(rt.DOScale(1f, 0.28f).SetEase(Ease.OutBack, 2f))
            .AppendInterval(bubbleHold)
            .Append(bubble.DOFade(0f, 0.4f).SetEase(Ease.InQuad))
            .Join(rt.DOScale(0.85f, 0.4f).SetEase(Ease.InQuad));
    }

    private void HideBubble()
    {
        if (bubble == null) return;

        _bubbleSeq?.Kill();
        _bubbleSeq = null;
        bubble.alpha = 0f;
        bubble.blocksRaycasts = false;
        bubble.transform.localScale = Vector3.one;
    }

    private void ScrollToSelected()
    {
        if (scroll == null || scroll.content == null || _previewId < 0 || _previewId >= _items.Count) return;

        RectTransform content = scroll.content;
        RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        float overflow = content.rect.height - viewport.rect.height;
        if (overflow <= 0f) return;

        var cell = (RectTransform)_items[_previewId].transform;
        Vector3 cellCenter = content.InverseTransformPoint(cell.TransformPoint(cell.rect.center));
        float centerFromTop = content.rect.yMax - cellCenter.y;
        float offset = Mathf.Clamp(centerFromTop - viewport.rect.height * 0.5f, 0f, overflow);
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = 1f - offset / overflow;
    }
}
