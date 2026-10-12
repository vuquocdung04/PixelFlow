using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DailyLoginData", menuName = "PixelFlow/Daily/DailyLoginData")]
public class DailyLoginData : ScriptableObject
{
    public List<DayReward> days = new();

    public int Count => days.Count;
    public DayReward GetDay(int index) => index >= 0 && index < days.Count ? days[index] : null;
}
