using System;

public enum RewardType
{
    Coin = 0,
    Booster0 = 1,
    Booster1 = 2,
    Booster2 = 3,
    Heart = 4,
    // amount = số phút tim vô hạn
    UnlimitedHeart = 5,
}

[Serializable]
public class GameReward
{
    public RewardType type;
    public int amount;

    public GameReward() { }

    public GameReward(RewardType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }

    public GameReward Clone() => new GameReward(type, amount);
}
