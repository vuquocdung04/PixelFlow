using UnityEngine;

public partial class BoosterController
{
    public readonly int[] TutorialLevels = new int[] { 3, 6, 9 };
    public int GetCurrentTutorialBoosterIndex()
    {
        return System.Array.IndexOf(TutorialLevels, UseProfile.Level.Value);
    }
    public void CheckTutorialHighlight()
    {
        int currentLevel = UseProfile.Level.Value;
        BoosterItem targetBooster = null;

        void TryAssignBooster(int index, PrefVar<bool> isDoneFlag)
        {
            if (targetBooster != null) return;

            if (TutorialLevels.Length > index && currentLevel == TutorialLevels[index] && !isDoneFlag.Value)
            {
                if (index < boosterItems.Count) targetBooster = boosterItems[index];
            }
        }

        TryAssignBooster(0, UseProfile.IsDoneBooster0);
        TryAssignBooster(1, UseProfile.IsDoneBooster1);
        TryAssignBooster(2, UseProfile.IsDoneBooster2);

        if (targetBooster != null)
        {
            _ = BoosterUnlockBox.Setup(GameScene.GetPopupHolder(), box =>
            {
                box.Show();
            });
        }
    }

    private void CheckAndClearTutorialPhase1(BoosterType type, BoosterItem item)
    {
        bool isTutorialActive = false;

        if (type == BoosterType.Booster0 && !UseProfile.IsDoneBooster0.Value) isTutorialActive = true;
        if (type == BoosterType.Booster1 && !UseProfile.IsDoneBooster1.Value) isTutorialActive = true;
        if (type == BoosterType.Booster2 && !UseProfile.IsDoneBooster2.Value) isTutorialActive = true;

        if (isTutorialActive)
        {
            HandAnimation.Instance.RemoveHighlightUI(item.gameObject);
            HandAnimation.Instance.KillUI();

            if (type == BoosterType.Booster0)
            {
                UseProfile.IsDoneBooster0.Value = true;
            }
        }
    }

    private void SetupPhase2Tutorial(BoosterType type)
    {
        int level = UseProfile.Level.Value;

        if (type == BoosterType.Booster1 && level == 6 && !UseProfile.IsDoneBooster1.Value)
        {
            // Transform targetObj = GamePlayController.Instance.gameScene.GetTutorialTarget();
            // if (targetObj != null)
            // {
            //     HandAnimation.Instance.PlayAnimObj(targetObj);
            // }
        }
    }

    private void CompletePhase2Tutorial(BoosterType type)
    {
        if (type == BoosterType.Booster1 && !UseProfile.IsDoneBooster1.Value)
        {
            UseProfile.IsDoneBooster1.Value = true;
            HandAnimation.Instance.KillObj();
        }
    }

    private void HandleTutorialCancel(BoosterType type)
    {
        int level = UseProfile.Level.Value;

        if (type == BoosterType.Booster1 && level == 6 && !UseProfile.IsDoneBooster1.Value)
        {
            HandAnimation.Instance.KillObj();
            UseProfile.IsDoneBooster1.Value = true;
        }

        if (type == BoosterType.Booster2 && level == 9 && !UseProfile.IsDoneBooster2.Value)
        {
            HandAnimation.Instance.KillObj();
            UseProfile.IsDoneBooster2.Value = true;
        }
    }
}