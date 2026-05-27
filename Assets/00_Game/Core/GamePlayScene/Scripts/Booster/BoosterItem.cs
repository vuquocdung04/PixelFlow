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
    [Header("Text & Fill")]
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI unlockLevelText;

    private static readonly Dictionary<BoosterState, BoosterState[]> _transitions = new()
{
    { BoosterState.Locked,    new[] { BoosterState.Available } },
    { BoosterState.Available, new[] { BoosterState.InUse, BoosterState.Empty, BoosterState.Locked } },
    { BoosterState.Empty,     new[] { BoosterState.Available } },
    { BoosterState.InUse,     new[] { BoosterState.Available } },
};
    private void Start() => btnMain.OnClicked(OnButtonClicked);
    public void SetSize(float size) => iconBooster.FitToTargetHeight(size);

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
    }
    private void OnButtonClicked()
    {
        switch (CurrentState)
        {
            case BoosterState.Available:
                this.PostEvent(EventID.BOOSTER_USE_REQUEST, type);
                break;
            case BoosterState.InUse:
            
                break;
            case BoosterState.Empty:
                this.PostEvent(EventID.BOOSTER_BUY_REQUEST, type);
                break;
        }
    }
    public int GetQuantity()
    {
        return type switch
        {
            BoosterType.Booster0 => UseProfile.Booster0.Value,
            BoosterType.Booster1 => UseProfile.Booster1.Value,
            BoosterType.Booster2 => UseProfile.Booster2.Value,
            _ => 0
        };
    }
    public void SubQuantity()
    {
        switch (type)
        {
            case BoosterType.Booster0: UseProfile.Booster0.Value = Mathf.Max(0, UseProfile.Booster0.Value - 1); break;
            case BoosterType.Booster1: UseProfile.Booster1.Value = Mathf.Max(0, UseProfile.Booster1.Value - 1); break;
            case BoosterType.Booster2: UseProfile.Booster2.Value = Mathf.Max(0, UseProfile.Booster2.Value - 1); break;
        }
        RefreshFromQuantity();
    }
    public void RefreshFromQuantity()
    {
        int qty = GetQuantity();
        if (quantityText != null) quantityText.text = qty.ToString();

        if (CurrentState == BoosterState.Available && qty <= 0)
            ChangeState(BoosterState.Empty);
        else if (CurrentState == BoosterState.Empty && qty > 0)
            ChangeState(BoosterState.Available);
    }
}