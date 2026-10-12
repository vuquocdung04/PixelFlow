using System;
using System.Collections.Generic;
using UnityEngine;

// Mốc thưởng Weekly Quest. Đặt trong Resources để LiveOp đọc được mà không cần gán tay.
[CreateAssetMenu(fileName = "WeekQuestData", menuName = "PixelFlow/WeekQuestData")]
public class WeekQuestData : ScriptableObject
{
    [Serializable]
    public class Milestone
    {
        [Tooltip("Số coin kiếm được trong tuần để mở mốc này")]
        public int coin;
        public List<GameReward> rewards = new();
    }

    private const string ResourcePath = "WeekQuestData";

    public List<Milestone> milestones = new();

    private static WeekQuestData _instance;
    public static WeekQuestData Instance => _instance != null ? _instance : _instance = Resources.Load<WeekQuestData>(ResourcePath);

    public int Count => milestones.Count;
    public Milestone Get(int index) => index >= 0 && index < milestones.Count ? milestones[index] : null;
    public int GetCoin(int index) => Get(index)?.coin ?? 0;
}
