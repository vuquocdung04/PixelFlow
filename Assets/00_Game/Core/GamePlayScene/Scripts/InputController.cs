using System.Collections.Generic;
using DG.Tweening;
using EventDispatcher;
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
                     newState == GameState.Tutorial);
    }

    private void Update()
    {
        if (!canInput) return;
        if (Pointer.current == null) return;
        if (!Pointer.current.press.wasPressedThisFrame) return;

        HandleClick();
    }

    private void HandleClick()
    {
        Vector2 screenPos = Pointer.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        if (BoosterController.Instance.HasActive)
        {
            HandleBoosterClick(hit);
            return;
        }

        Block block = hit.collider.GetComponentInParent<Block>();
        if (block != null && block.IsAlive)
        {
            BrickGrid.Instance.DestroyAllSameColor(block.colorHex);
            return;
        }

        Shooter shooter = hit.collider.GetComponentInParent<Shooter>();
        if (shooter == null) return;

        if (Conveyor.Instance.IsGroupStaggering)
        {
            Debug.Log("[Click] Blocked — group staggering");
            return;
        }

        if (shooter.IsInGroup)
        {
            HandleGroupClick(shooter, shooter.linkGroup);
            return;
        }

        if (shooter.IsBlocked)
        {
            Debug.LogError($"Shooter '{shooter.name}' is BLOCKED");
            return;
        }

        if (Conveyor.Instance.itemSlots.Count == 0)
        {
            this.PostEvent(EventID.CONVEYOR_NOT_ENOUGH_SLOT);
            return;
        }

        Conveyor.Instance.TakeFirstSlot(shooter);
        LevelController.Instance.OnShooterTaken(shooter);
    }
    private void HandleGroupClick(Shooter clicked, LinkGroup group)
    {
        int gridX = LevelController.Instance.gridBottomX;

        if (!group.CanClick(clicked, gridX))
        {
            Debug.Log($"[Group] Cannot click — clicked={clicked.name}");
            return;
        }

        if (Conveyor.Instance.itemSlots.Count < group.Count)
        {
            Debug.Log($"[Group] Not enough slots");
            this.PostEvent(EventID.CONVEYOR_NOT_ENOUGH_SLOT);
            return;
        }

        List<Shooter> takenShooters = new List<Shooter>(group.members);

        Conveyor.Instance.TakeFirstSlotsForGroup(group);

        for (int i = 0; i < takenShooters.Count; i++)
        {
            Shooter captured = takenShooters[i];
            float delay = i * 0.2f;

            if (delay > 0f)
                DOVirtual.DelayedCall(delay, () => LevelController.Instance.OnShooterTaken(captured));
            else
                LevelController.Instance.OnShooterTaken(captured);
        }
    }

    private void HandleBoosterClick(RaycastHit hit)
    {
        var activeType = BoosterController.Instance.ActiveType;
        if (activeType == null) return;

        switch (activeType.Value)
        {
            case BoosterType.Booster0:
                Shooter shooter = hit.collider.GetComponentInParent<Shooter>();
                if (shooter == null) return;
                BoosterController.Instance.TryUseOnShooter(shooter);
                break;

            case BoosterType.Booster2:
                Block block = hit.collider.GetComponentInParent<Block>();
                if (block == null) return;
                BoosterController.Instance.TryUseOnBlock(block);
                break;
        }
    }
}