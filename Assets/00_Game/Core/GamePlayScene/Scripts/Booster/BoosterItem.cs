using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EventDispatcher;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoosterItem : MonoBehaviour
{
    [SerializeField] private BoosterType type;
    public BoosterType Type => type;
    public Sprite IconSprite => iconBooster.sprite;
    public BoosterState CurrentState { get; private set; } = BoosterState.Locked;

    public Button btnMain;
    public Image iconBooster;

    [Header("Containers")]
    [SerializeField] private GameObject unlockedContainer;
    [SerializeField] private GameObject lockedContainer;

    [Header("State UI")]
    [SerializeField] private GameObject quantityInfoGroup;
    [SerializeField] private GameObject addIconOverlay;
    [SerializeField] private GameObject inUseHighlight;
    [SerializeField] private GameObject cooldownGroup;

    [Header("Text & Fill")]
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI unlockLevelText;
    [SerializeField] private Image cooldownFillImage;

    // Rule transition: từ state X có thể nhảy sang state nào
    private static readonly Dictionary<BoosterState, BoosterState[]> _transitions = new()
    {
        { BoosterState.Locked,    new[] { BoosterState.Available } },
        { BoosterState.Available, new[] { BoosterState.InUse, BoosterState.Empty, BoosterState.Cooldown, BoosterState.Locked } },
        { BoosterState.Empty,     new[] { BoosterState.Available } },
        { BoosterState.InUse,     new[] { BoosterState.Available, BoosterState.Cooldown } },
        { BoosterState.Cooldown,  new[] { BoosterState.Available } },
    };

    private Tween _cooldownTween;

    private void Start() => btnMain.OnClicked(OnButtonClicked);
    private void OnDestroy() => _cooldownTween?.Kill();

    public void SetSize(float size) => iconBooster.FitToTargetHeight(size);

    /// <summary>
    /// Đổi state. force=true để bypass rule (init, load save game).
    /// </summary>
    public bool ChangeState(BoosterState next, bool force = false)
    {
        if (!force && CurrentState != next
            && !_transitions[CurrentState].Contains(next))
        {
            Debug.LogWarning($"[{type}] Invalid transition: {CurrentState} → {next}");
            return false;
        }

        CurrentState = next;
        ApplyStateUI(next);
        return true;
    }

    private void ApplyStateUI(BoosterState s)
    {
        lockedContainer.SetActive(s == BoosterState.Locked);
        unlockedContainer.SetActive(s != BoosterState.Locked);
        quantityInfoGroup.SetActive(s == BoosterState.Available);
        addIconOverlay.SetActive(s == BoosterState.Empty);
        inUseHighlight.SetActive(s == BoosterState.InUse);
        cooldownGroup.SetActive(s == BoosterState.Cooldown);
    }

    public void StartCooldown(float duration)
    {
        if (!ChangeState(BoosterState.Cooldown)) return;
        _cooldownTween?.Kill();

        _cooldownTween = DOVirtual.Float(duration, 0f, duration, t =>
        {
            if (CurrentState != BoosterState.Cooldown) return;
            cooldownFillImage.fillAmount = t / duration;
            cooldownText.text = Mathf.CeilToInt(t).ToString();
        })
        .SetEase(Ease.Linear)
        .OnComplete(() => ChangeState(BoosterState.Available));
    }

    private void OnButtonClicked()
    {
        switch (CurrentState)
        {
            case BoosterState.Available:
                this.PostEvent(EventID.BOOSTER_USE_REQUEST, type);
                break;
            case BoosterState.InUse:
                this.PostEvent(EventID.BOOSTER_DEACTIVATE_REQUEST, type);
                break;
            case BoosterState.Empty:
                Debug.Log("Mở popup mua thêm!");
                break;
        }
    }
}