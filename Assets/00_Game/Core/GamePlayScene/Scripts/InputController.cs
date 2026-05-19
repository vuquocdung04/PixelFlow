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

        // Booster: click vào Block thì destroy all same color
        Block block = hit.collider.GetComponentInParent<Block>();
        if (block != null && block.IsAlive)
        {
            // Tại đây check booster mode đang active không, sau đó:
            BrickGrid.Instance.DestroyAllSameColor(block.colorHex);
            return;
        }

        // Click vào Shooter
        Shooter shooter = hit.collider.GetComponentInParent<Shooter>();
        if (shooter == null) return;

        if (shooter.IsBlocked)
        {
            Debug.LogError($"Shooter '{shooter.name}' is BLOCKED");
            return;
        }

        if (Conveyor.Instance.itemSlots.Count == 0) return;

        int shooterIdx = FindShooterIdx(shooter);

        Conveyor.Instance.TakeFirstSlot(shooter);

        if (shooterIdx >= 0)
            LevelController.Instance.OnShooterTaken(shooterIdx);
    }

    private int FindShooterIdx(Shooter shooter)
    {
        foreach (var kv in LevelController.Instance.shooterMap)
            if (kv.Value == shooter) return kv.Key;
        return -1;
    }
}