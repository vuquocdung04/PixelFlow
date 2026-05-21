using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public partial class Conveyor
{
    [Space(10), Header("Item Group")]
    public ItemSlot itemSlotPrefab;
    public Transform firstPoint;
    public Transform returnPoint;
    public float spacing;
    public int initialItemCount = 5;
    public TextMeshPro countText;
    public List<ItemSlot> itemSlots = new List<ItemSlot>();
    public int Capacity => initialItemCount;

    private void SpawnItems()
    {
        if (initialItemCount > 1)
            spacing = Mathf.Abs(returnPoint.position.x - firstPoint.position.x) / (initialItemCount - 1);

        for (int i = 0; i < initialItemCount; i++)
        {
            ItemSlot slot = Instantiate(itemSlotPrefab, transform);
            slot.Init();
            ReturnSlot(slot);
        }
    }

    public void ReturnSlot(ItemSlot slot)
    {
        slot.transform.rotation = Quaternion.identity;
        slot.transform.position = returnPoint.position;
        itemSlots.Add(slot);

        slot.MoveToX(GetSlotTargetX(itemSlots.Count - 1));
        slot.ResetVisualRotation();

        UpdateCountText();
    }

    public void TakeFirstSlot(Shooter shooter)
    {
        WaitArea currentArea = shooter.GetComponentInParent<WaitArea>();
        if (currentArea != null)
        {
            currentArea.ResetToDefault();
            WaitAreaController.Instance.ShiftForward();
        }

        ItemSlot slot = itemSlots[0];
        itemSlots.RemoveAt(0);

        LaunchShooterOnSlot(slot, shooter);

        for (int i = 0; i < itemSlots.Count; i++)
            itemSlots[i].MoveToX(GetSlotTargetX(i));

        UpdateCountText();
    }

    public void TakeFirstSlotsForGroup(LinkGroup group)
    {
        const float STAGGER = 0.2f;

        for (int i = 0; i < group.Count; i++)
        {
            Shooter shooter = group.members[i];
            ItemSlot slot = itemSlots[0];
            itemSlots.RemoveAt(0);

            WaitArea currentArea = shooter.GetComponentInParent<WaitArea>();
            if (currentArea != null)
                currentArea.ResetToDefault();

            LaunchShooterOnSlot(slot, shooter, delay: i * STAGGER);
        }

        for (int i = 0; i < itemSlots.Count; i++)
            itemSlots[i].MoveToX(GetSlotTargetX(i));

        WaitAreaController.Instance.ShiftForward();
        UpdateCountText();
    }

    private void LaunchShooterOnSlot(ItemSlot slot, Shooter shooter, float delay = 0f)
    {
        Vector3 pathStart = itemPath[0].position;
        slot.PrepareToReceive(pathStart, shooter.jumpDuration * 0.8f);

        void JumpAndDock() => shooter.JumpTo(pathStart, () =>
        {
            slot.DockShooter(shooter);
            shooter.SetAnimState(ShooterAnimState.Combat);
            SendSlotAlongPath(slot, shooter);
        });

        if (delay > 0f) DOVirtual.DelayedCall(delay, JumpAndDock);
        else JumpAndDock();
    }

    private void UpdateCountText()
    {
        if (countText != null)
            countText.text = $"{itemSlots.Count}/{initialItemCount}";
    }

    private float GetSlotTargetX(int index)
    {
        float dir = Mathf.Sign(returnPoint.position.x - firstPoint.position.x);
        if (dir == 0f) dir = 1f;
        return firstPoint.position.x + index * spacing * dir;
    }
}
