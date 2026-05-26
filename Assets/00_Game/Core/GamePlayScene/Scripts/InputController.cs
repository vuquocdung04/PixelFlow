using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : StaffSingleton<InputController>
{
    private Camera cam;
    private InputMode _currentMode;
    private InputMode _normalMode;
    private InputMode _booster0Mode;
    private InputMode _booster2Mode;
    private InputMode _disabledMode;

    public override void Init()
    {
        cam = GamePlayController.Instance.cameraGameplay;

        _normalMode = new NormalInputMode();
        _booster0Mode = new Booster0InputMode();
        _booster2Mode = new Booster2InputMode();
        _disabledMode = new DisabledInputMode();

        SetMode(_normalMode);

        GameFlow.Instance.OnStateEntered += OnGameStateChanged;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GameFlow.Instance.OnStateEntered -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState newState)
    {
        bool shouldDisable = newState == GameState.Win ||
                            newState == GameState.Lose ||
                            newState == GameState.Paused ||
                            newState == GameState.Tutorial;

        if (shouldDisable)
            SetMode(_disabledMode);
        else
            SetMode(_normalMode);
    }

    public void SetMode(InputMode newMode)
    {
        _currentMode?.OnExit();
        _currentMode = newMode;
        _currentMode?.OnEnter(this);
    }

    public void SetBooster0Mode() => SetMode(_booster0Mode);
    public void SetBooster2Mode() => SetMode(_booster2Mode);
    public void RestoreNormalMode() => SetMode(_normalMode);

    private void Update()
    {
        if (Pointer.current == null) return;
        if (!Pointer.current.press.wasPressedThisFrame) return;

        HandleClick();
    }

    private void HandleClick()
    {
        Vector2 screenPos = Pointer.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        _currentMode.HandleClick(hit);
    }
}