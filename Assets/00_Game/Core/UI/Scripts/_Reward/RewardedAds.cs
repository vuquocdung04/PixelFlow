using System;

// Cầu nối quảng cáo thưởng. Project chưa có Ads SDK nên tạm cho nhận luôn;
// khi tích hợp SDK chỉ cần sửa hàm này (onSuccess chỉ gọi khi xem hết video).
public static class RewardedAds
{
    public static void Show(string placement, Action onSuccess, Action onFail = null)
    {
        onSuccess?.Invoke();
    }
}
