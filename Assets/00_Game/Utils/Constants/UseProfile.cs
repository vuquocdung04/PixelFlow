using System;

public static class UseProfile
{
    public static int DefaultBoosterAmount = 3;
    public static int DefaultStartingCoins = 500;

    public static readonly PrefVar<int> Level = new(StringHelper.LEVEL, 1);
    public static readonly PrefVar<int> Booster0 = new(StringHelper.BOOSTER_0, DefaultBoosterAmount);
    public static readonly PrefVar<int> Booster1 = new(StringHelper.BOOSTER_1, DefaultBoosterAmount);
    public static readonly PrefVar<int> Booster2 = new(StringHelper.BOOSTER_2, DefaultBoosterAmount);
    public static readonly PrefVar<int> Coin = new(StringHelper.COIN, DefaultStartingCoins);

    public static readonly PrefVar<int> AvatarId = new(StringHelper.AVATAR_ID, 0);

    // --- SETTINGS ---
    public static readonly PrefVar<bool> OnMusic = new(StringHelper.ONOFF_MUSIC, true);
    public static readonly PrefVar<bool> OnSound = new(StringHelper.ONOFF_SOUND, true);
    public static readonly PrefVar<bool> OnVib = new(StringHelper.ONOFF_VIB, true);

    // --- TIM & QUẢNG CÁO ---
    public static readonly PrefVar<int> Heart = new(StringHelper.HEART, 5);
    public static readonly PrefVar<bool> IsUnlimitedHeart = new(StringHelper.IS_UNLIMITER_HEART, false);
    public static readonly PrefVar<bool> IsRemoveAds = new(StringHelper.REMOVE_ADS, false);

    // --- TUTORIAL & TIẾN TRÌNH ---
    public static readonly PrefVar<bool> IsDoneBooster0 = new(StringHelper.IS_DONE_TUT_BOOSTER_0, false);
    public static readonly PrefVar<bool> IsDoneBooster1 = new(StringHelper.IS_DONE_TUT_BOOSTER_1, false);
    public static readonly PrefVar<bool> IsDoneBooster2 = new(StringHelper.IS_DONE_TUT_BOOSTER_2, false);
    // ========================================================
    // --- XỬ LÝ THỜI GIAN (TÍCH HỢP TIMEMANAGER) ---
    // ========================================================

    public static DateTime TimeUnlimitedHeart
    {
        get => DateTime.FromBinary(GamePrefs.Get(StringHelper.TIME_UNLIMITER_HEART, TimeManager.GetCurrentTime().AddDays(-1).ToBinary()));
        set => GamePrefs.Set(StringHelper.TIME_UNLIMITER_HEART, value.ToBinary());
    }

    public static DateTime TimeLastOverHeart
    {
        get => DateTime.FromBinary(GamePrefs.Get(StringHelper.TIME_LAST_OVER_HEART, TimeManager.GetCurrentTime().ToBinary()));
        set => GamePrefs.Set(StringHelper.TIME_LAST_OVER_HEART, value.ToBinary());
    }

    public static DateTime LastTimeLogin
    {
        get => DateTime.FromBinary(GamePrefs.Get(StringHelper.LAST_TIME_LOGIN, TimeManager.GetCurrentTime().ToBinary()));
        set => GamePrefs.Set(StringHelper.LAST_TIME_LOGIN, value.ToBinary());
    }
}