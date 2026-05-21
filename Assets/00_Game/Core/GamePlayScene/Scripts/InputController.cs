using System.Collections.Generic;
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

        // Booster: click vào Block
        Block block = hit.collider.GetComponentInParent<Block>();
        if (block != null && block.IsAlive)
        {
            BrickGrid.Instance.DestroyAllSameColor(block.colorHex);
            return;
        }

        Shooter shooter = hit.collider.GetComponentInParent<Shooter>();
        if (shooter == null) return;

        // === GROUP CLICK ===
        if (shooter.IsInGroup)
        {
            HandleGroupClick(shooter.linkGroup);
            return;
        }

        // === SINGLE CLICK (logic cũ) ===
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

    private void HandleGroupClick(LinkGroup group)
    {
        if (!group.AllAtRow0())
        {
            Debug.Log($"[Group] Not all at row 0");
            return;
        }

        if (group.AnyBlocked())
        {
            Debug.Log($"[Group] Some blocked (Ice / animState)");
            return;
        }

        if (Conveyor.Instance.itemSlots.Count < group.Count)
        {
            Debug.Log($"[Group] Not enough slots: need {group.Count}, have {Conveyor.Instance.itemSlots.Count}");
            return;
        }

        // Tìm idx của tất cả member
        List<int> indices = new List<int>();
        foreach (var s in group.members)
        {
            int idx = FindShooterIdx(s);
            if (idx >= 0) indices.Add(idx);
        }

        Conveyor.Instance.TakeFirstSlotsForGroup(group);

        foreach (int idx in indices)
            LevelController.Instance.OnShooterTaken(idx);
    }
    private int FindShooterIdx(Shooter shooter)
    {
        foreach (var kv in LevelController.Instance.shooterMap)
            if (kv.Value == shooter) return kv.Key;
        return -1;
    }
}