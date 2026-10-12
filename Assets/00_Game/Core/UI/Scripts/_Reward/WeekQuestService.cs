using System;
using System.Collections.Generic;
using EventDispatcher;
using Newtonsoft.Json;

[Serializable]
public class WeekQuestSave
{
    public int week = -1;
    public int coin;
    public int seenCoin;
    public List<int> claimed = new();
}

// Weekly Quest: coin kiếm được trong tuần (UTC, reset sáng thứ 2) mở dần các mốc thưởng.
// Gọi AddCoin ở nơi người chơi kiếm coin (vd thắng level).
public static class WeekQuestService
{
    private const string SaveKey = "WEEK_QUEST";
    private static readonly DateTime EpochMonday = new(2024, 1, 1);

    public static int WeekIndex => (int)((DateTime.UtcNow.Date - EpochMonday).TotalDays / 7);
    public static DateTime NextReset => EpochMonday.AddDays((WeekIndex + 1) * 7d);
    public static long SecondsRemaining => Math.Max(0L, (long)(NextReset - DateTime.UtcNow).TotalSeconds);

    public static int Coin => Load().coin;

    // Coin lúc người chơi mở popup lần trước, để lần mở sau chạy anim phần vừa kiếm thêm
    public static int SeenCoin => Load().seenCoin;

    public static void MarkSeen()
    {
        var data = Load();
        if (data.seenCoin == data.coin) return;
        data.seenCoin = data.coin;
        Save(data);
    }

    public static void AddCoin(int value)
    {
        if (value <= 0) return;
        var data = Load();
        data.coin += value;
        Save(data);
        NotifyChanged();
    }

    public static void SetCoin(int value)
    {
        var data = Load();
        data.coin = Math.Max(0, value);
        Save(data);
        NotifyChanged();
    }

    public static bool IsClaimed(int index) => Load().claimed.Contains(index);

    public static bool IsReached(WeekQuestData data, int index) =>
        data != null && index >= 0 && index < data.Count && Coin >= data.GetCoin(index);

    public static bool CanClaim(WeekQuestData data, int index) => IsReached(data, index) && !IsClaimed(index);

    public static bool HasClaimable(WeekQuestData data)
    {
        int count = data != null ? data.Count : 0;
        for (int i = 0; i < count; i++)
            if (CanClaim(data, i)) return true;
        return false;
    }

    public static bool TryClaim(WeekQuestData data, int index)
    {
        if (!CanClaim(data, index)) return false;
        var save = Load();
        save.claimed.Add(index);
        Save(save);
        NotifyChanged();
        return true;
    }

    public static void NotifyChanged()
    {
        if (EventDispatcher.EventDispatcher.HasInstance())
            EventDispatcher.EventDispatcher.Instance.PostEvent(EventID.WEEK_QUEST_CHANGED);
    }

    private static string _cachedJson;
    private static WeekQuestSave _cached;

    private static WeekQuestSave Load()
    {
        WeekQuestSave data = null;
        string json = GamePrefs.Get(SaveKey, "");

        // Chỉ parse lại khi chuỗi lưu thay đổi
        if (json == _cachedJson && _cached != null && _cached.week == WeekIndex) return _cached;

        if (!string.IsNullOrEmpty(json))
        {
            try { data = JsonConvert.DeserializeObject<WeekQuestSave>(json); }
            catch { data = null; }
        }

        data ??= new WeekQuestSave();
        data.claimed ??= new List<int>();

        // Sang tuần mới -> reset tiến trình
        int week = WeekIndex;
        if (data.week != week)
        {
            data.week = week;
            data.coin = 0;
            data.seenCoin = 0;
            data.claimed.Clear();
            Save(data);
            return data;
        }

        _cachedJson = json;
        _cached = data;
        return data;
    }

    // Lưu dạng chuỗi JSON: GamePrefs bỏ qua Set khi cùng reference, lưu object sẽ không bị đánh dấu dirty
    private static void Save(WeekQuestSave data)
    {
        _cachedJson = JsonConvert.SerializeObject(data);
        _cached = data;
        GamePrefs.Set(SaveKey, _cachedJson);
    }
}
