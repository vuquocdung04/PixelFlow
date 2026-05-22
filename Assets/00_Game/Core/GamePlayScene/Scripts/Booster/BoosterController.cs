using System.Collections.Generic;
using DG.Tweening;
using EventDispatcher;
using UnityEngine;

public partial class BoosterController : StaffSingleton<BoosterController>
{

    [Header("Booster2 Cannon")]
    [SerializeField] private Booster2Cannon cannonPrefab;
    [SerializeField] private Vector3 cannonSpawnPos = new Vector3(0f, 5f, 0f);
    [Header("Refs")]
    public Transform boosterHolder;
    public float targetSize = 150f;

    [Header("Move Animation")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private Vector2 hiddenPosition;

    [Header("SFX Popup")]
    [SerializeField] private BoosterSfxPopup sfxPopup;

    [Header("Camera")]
    [SerializeField] private Vector3 cameraDefaultPos = new Vector3(0f, 32.86f, -12.65f);
    [SerializeField] private float cameraMoveDuration = 0.5f;

    private Tween _cameraTween;

    private List<BoosterItem> _items;
    private BoosterItem _active;
    private Vector2 _originalPosition;
    private Tween _moveTween;

    public bool HasActive => _active != null;
    public BoosterType? ActiveType => _active?.Type;

    public override void Init()
    {
        if (panelRoot != null) _originalPosition = panelRoot.anchoredPosition;

        _items = new List<BoosterItem>(boosterHolder.GetComponentsInChildren<BoosterItem>());
        foreach (var item in _items)
        {
            item.SetSize(targetSize);
            item.ChangeState(BoosterState.Available, force: true);
        }

        this.RegisterListener(EventID.BOOSTER_USE_REQUEST, OnUseRequest);
        this.RegisterListener(EventID.BOOSTER_DEACTIVATE_REQUEST, OnDeactivateRequest);

        CheckTutorialHighlight();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        this.RemoveListener(EventID.BOOSTER_USE_REQUEST, OnUseRequest);
        this.RemoveListener(EventID.BOOSTER_DEACTIVATE_REQUEST, OnDeactivateRequest);
    }

    // ============= USE / DEACTIVATE =============
    private void OnUseRequest(object param)
    {
        var type = (BoosterType)param;
        var item = FindItem(type);
        if (item == null) return;

        CheckAndClearTutorialPhase1(type, item);

        if (_active != null)
        {
            ToastManager.Instance.ShowToast("Another Booster is in use!");
            return;
        }

        _active = item;
        item.ChangeState(BoosterState.InUse);

        switch (type)
        {
            case BoosterType.Booster0:
                var targetPos1 = new Vector3(0f, 32.3f, -14.2f);
                ShowSfxFlow(type, item, targetPos1, 50f);
                var shooterAvailables = LevelController.Instance.GetAvailableShooters();
                HighlightSystem.Instance.Highlight(shooterAvailables);
                break;
            case BoosterType.Booster1:
                SetupPhase2Tutorial(type);
                break;
            case BoosterType.Booster2:
                var targetPos2 = new Vector3(0f, 33.5f, -11f);
                ShowSfxFlow(type, item, targetPos2, -650f);
                HighlightSystem.Instance.Highlight(BrickGrid.Instance.GetAllAliveBlocks());
                break;
        }
    }

    private void OnDeactivateRequest(object param)
    {
        if (_active == null || _active.Type != (BoosterType)param) return;
        Deactivate();
    }

    public void Deactivate()
    {
        if (_active == null) return;
        HandleTutorialCancel(_active.Type);
        _active.ChangeState(BoosterState.Available);
        _active = null;
    }

    public void OnBoosterActionSuccess()
    {
        if (_active == null) return;
        CompletePhase2Tutorial(_active.Type);
        Deactivate();
    }
    // ============= MOVE ANIMATION =============
    public void MoveOut(float duration = 0.3f)
    {
        if (panelRoot == null) return;
        _moveTween?.Kill();
        _moveTween = panelRoot.DOAnchorPos(hiddenPosition, duration).SetEase(Ease.InBack);
    }

    public void MoveIn(float duration = 0.3f)
    {
        if (panelRoot == null) return;
        _moveTween?.Kill();
        _moveTween = panelRoot.DOAnchorPos(_originalPosition, duration).SetEase(Ease.OutBack);
    }

    private BoosterItem FindItem(BoosterType type) => _items.Find(i => i.Type == type);

    public BoosterItem GetItemByIndex(int index) =>
        (_items != null && index >= 0 && index < _items.Count) ? _items[index] : null;

    private void ShowSfxFlow(BoosterType type, BoosterItem item, Vector3 cameraTargetPos, float popupY)
    {
        MoveOut();
        sfxPopup.Show(item.IconSprite, GetTitle(type), GetDescription(type), popupY);
        MoveCameraTo(cameraTargetPos);
    }

    public void OnSfxPopupClosed()
    {
        HighlightSystem.Instance.Clear();
        MoveIn();
        MoveCameraTo(cameraDefaultPos);
        Deactivate();
    }

    public void MoveCameraTo(Vector3 targetPos)
    {
        var cam = GamePlayController.Instance.cameraGameplay;
        _cameraTween?.Kill();
        _cameraTween = cam.transform.DOMove(targetPos, cameraMoveDuration).SetEase(Ease.InOutQuad);
    }

    private string GetTitle(BoosterType type) => type switch
    {
        BoosterType.Booster0 => "Booster 0",
        BoosterType.Booster1 => "Booster 1",
        BoosterType.Booster2 => "Booster 2",
        _ => ""
    };

    private string GetDescription(BoosterType type) => type switch
    {
        BoosterType.Booster0 => "Description for booster 0",
        BoosterType.Booster1 => "Description for booster 1",
        BoosterType.Booster2 => "Description for booster 2",
        _ => ""
    };


    public void TryUseOnShooter(Shooter shooter)
    {
        if (_active == null || shooter == null) return;

        switch (_active.Type)
        {
            case BoosterType.Booster0:
                ExecuteBooster0OnShooter(shooter);
                break;
        }
    }

    public void TryUseOnBlock(Block block)
    {
        if (_active == null || block == null || !block.IsAlive) return;

        switch (_active.Type)
        {
            case BoosterType.Booster2:
                ExecuteBooster2OnBlock(block);
                break;
        }
    }
    private async void ExecuteBooster2OnBlock(Block block)
    {
        string hex = block.colorHex;

        HighlightSystem.Instance.Clear();
        sfxPopup.ForceClose();
        MoveIn();
        MoveCameraTo(cameraDefaultPos);

        LevelController.Instance.DestroyAllShootersOfColor(hex);

        var targets = BrickGrid.Instance.GetAliveBlocksByColor(hex);

        var cannon = Instantiate(cannonPrefab, cannonSpawnPos, Quaternion.identity);
        await cannon.FireAt(targets);

        OnBoosterActionSuccess();
    }
    private void ExecuteBooster0OnShooter(Shooter shooter)
    {
        var available = LevelController.Instance.GetAvailableShooters();
        if (!available.Contains(shooter.gameObject)) return;
        HighlightSystem.Instance.Clear();
        sfxPopup.ForceClose();
        MoveIn();
        MoveCameraTo(cameraDefaultPos);

        shooter.RemoveProps(PropState.Blind);

        Conveyor.Instance.TakeFirstSlot(shooter);
        LevelController.Instance.OnShooterTaken(shooter);

        OnBoosterActionSuccess();
    }
}