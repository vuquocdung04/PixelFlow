using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardDisplay : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI txtAmount;

    [Tooltip("Khung chứa icon + số lượng, scale theo kích thước ô của grid")]
    [SerializeField] private RectTransform inner;
    [SerializeField] private float baseSize = 360f;

    public GameReward Reward { get; private set; }

    public void Setup(GameReward reward)
    {
        Reward = reward;
        var db = RewardDataBase.Instance;
        var sprite = db != null ? db.GetIcon(reward.type) : null;
        if (icon != null && sprite != null) icon.sprite = sprite;
        SetAmount(reward.amount);
    }

    public void SetAmount(int amount)
    {
        if (txtAmount != null && Reward != null) txtAmount.text = RewardDataBase.FormatAmount(Reward.type, amount);
    }

    public void SetCellSize(float size)
    {
        if (inner != null) inner.localScale = Vector3.one * (size / baseSize);
    }
}
