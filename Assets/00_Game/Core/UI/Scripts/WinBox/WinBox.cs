using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinBox : BaseBox<WinBox>
{
    public Button btnReward;
    public Button btnDoubleReward;

    [Header("Progress")]
    public Image imageIconFill;
    public Image imageProgressFill;
    public Image imageIconBg;
    public TextMeshProUGUI txtPercent;

    [Header("Sprites (theo thứ tự unlock)")]
    public Sprite[] propSprites;

    protected override void Init()
    {
        btnReward.OnClicked(delegate
        {
            FXManager.Instance.LoadSceneWithIrisWipe(SceneName.GAME_PLAY);
        });
    }

    protected override void InitState()
    {
        RefreshProgress();
    }

    private void RefreshProgress()
    {
        int[] unlockLevels = new[] { 10, 20, 30, 40 };
        int currentLevel = UseProfile.Level.Value;

        int targetIdx = -1;
        for (int i = 0; i < unlockLevels.Length; i++)
        {
            if (currentLevel <= unlockLevels[i])
            {
                targetIdx = i;
                break;
            }
        }

        if (targetIdx < 0)
        {
            int lastIdx = unlockLevels.Length - 1;
            ApplySprite(lastIdx);
            AnimateFill(1f);
            return;
        }

        ApplySprite(targetIdx);

        int prevMilestone = targetIdx == 0 ? 0 : unlockLevels[targetIdx - 1];
        int target = unlockLevels[targetIdx];
        float percent = (float)(currentLevel - prevMilestone) / (target - prevMilestone);

        AnimateFill(percent);
    }

    private void ApplySprite(int idx)
    {
        if (idx < 0 || idx >= propSprites.Length) return;
        imageIconBg.sprite = propSprites[idx];
        imageIconFill.sprite = propSprites[idx];
    }

    private void AnimateFill(float targetPercent)
    {
        imageIconFill.fillAmount = 0f;
        imageProgressFill.fillAmount = 0f;

        DOTween.To(() => imageIconFill.fillAmount, x =>
        {
            imageIconFill.fillAmount = x;
            imageProgressFill.fillAmount = x;
            txtPercent.text = $"{(int)(x * 100)}%";
        }, targetPercent, 0.6f).SetEase(Ease.OutCubic);
    }
}