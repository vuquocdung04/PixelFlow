using UnityEngine;

public partial class BoosterController
{
    public readonly int[] TutorialLevels = { 3, 6, 9 };

    public int GetCurrentTutorialBoosterIndex()
        => System.Array.IndexOf(TutorialLevels, UseProfile.Level.Value);

    public void CheckTutorialHighlight()
    {
        int currentLevel = UseProfile.Level.Value;
        BoosterItem target = null;

        void TryAssign(int index, PrefVar<bool> doneFlag)
        {
            if (target != null) return;
            if (TutorialLevels.Length > index
                && currentLevel == TutorialLevels[index]
                && !doneFlag.Value
                && index < _items.Count)
            {
                target = _items[index];
            }
        }

        TryAssign(0, UseProfile.IsDoneBooster0);
        TryAssign(1, UseProfile.IsDoneBooster1);
        TryAssign(2, UseProfile.IsDoneBooster2);

        if (target != null)
        {
            _ = BoosterUnlockBox.Setup(GameScene.GetPopupHolder(), box => box.Show());
        }
    }

    private PrefVar<bool> GetDoneFlag(BoosterType type) => type switch
    {
        BoosterType.Booster0 => UseProfile.IsDoneBooster0,
        BoosterType.Booster1 => UseProfile.IsDoneBooster1,
        BoosterType.Booster2 => UseProfile.IsDoneBooster2,
        _ => null
    };

    private void CheckAndClearTutorialPhase1(BoosterType type, BoosterItem item)
    {
        var flag = GetDoneFlag(type);
        if (flag == null || flag.Value) return;

        HandAnimation.Instance.RemoveHighlightUI(item.gameObject);
        HandAnimation.Instance.KillUI();

        if (type == BoosterType.Booster0) flag.Value = true;
    }

    private void SetupPhase2Tutorial(BoosterType type)
    {
        if (type == BoosterType.Booster1
            && UseProfile.Level.Value == 6
            && !UseProfile.IsDoneBooster1.Value)
        {
            // Transform targetObj = GamePlayController.Instance.gameScene.GetTutorialTarget();
            // if (targetObj != null) HandAnimation.Instance.PlayAnimObj(targetObj);
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