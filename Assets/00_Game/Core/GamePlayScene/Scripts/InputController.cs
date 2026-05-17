using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : StaffSingleton<InputController>
{
    private Camera cam;
    private BoosterType boosterTypeChoosing;
    private bool canInput = true;

    public override void Init()
    {
        cam = GamePlayController.Instance.cameraGameplay;
        GameFlow.Instance.OnStateEntered += HandleGameStateChanged;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GameFlow.Instance.OnStateEntered -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        canInput = !(newState == GameState.Win ||
                     newState == GameState.Lose ||
                     newState == GameState.Paused ||
                     newState == GameState.BoosterActive ||
                     newState == GameState.Tutorial);
    }

    private void Update()
    {
        if (!canInput)
        {
            return;
        }
        if (Mouse.current == null)
        {
            return;
        }
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        HandleClick();
    }

    private void HandleClick()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        Shooter shooter = hit.collider.GetComponentInParent<Shooter>();
        if (shooter == null) return;
        if (Conveyor.Instance.itemSlots.Count == 0) return;

        Conveyor.Instance.TakeFirstSlot(shooter);
    }
}