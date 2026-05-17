using System.Collections.Generic;
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

    void Start()
    {
        SpawnItems();
    }
    public void SpawnItems()
    {
        if (initialItemCount > 1)
            spacing = Mathf.Abs(returnPoint.position.x - firstPoint.position.x) / (initialItemCount - 1);

        for (int i = 0; i < initialItemCount; i++)
        {
            ItemSlot slot = Instantiate(itemSlotPrefab, transform);
            ReturnSlot(slot);
        }
    }

    public void ReturnSlot(ItemSlot slot)
    {
        itemSlots.Add(slot);
        slot.Return(returnPoint, GetSlotTargetX(itemSlots.Count - 1));

        UpdateCountText();
    }

    [ContextMenu("Take First Slot")]
    public void TakeFirstSlot()
    {
        if (itemSlots.Count == 0) return;

        ItemSlot first = itemSlots[0];
        itemSlots.RemoveAt(0);

        SendSlotAlongPath(first);

        for (int i = 0; i < itemSlots.Count; i++)
            itemSlots[i].Return(returnPoint, GetSlotTargetX(i));

        UpdateCountText();
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