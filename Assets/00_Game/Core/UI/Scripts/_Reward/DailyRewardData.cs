using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DailyRewardData", menuName = "PixelFlow/Daily/DailyRewardData")]
public class DailyRewardData : ScriptableObject
{
    [Tooltip("Slot 0 là quà free, các slot sau phải xem quảng cáo")]
    public List<GameReward> slots = new();

    public int Count => slots.Count;
    public GameReward GetSlot(int index) => index >= 0 && index < slots.Count ? slots[index] : null;
}
