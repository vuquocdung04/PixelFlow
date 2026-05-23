using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BoosterUnlockBox : BaseBox<BoosterUnlockBox>
{
    public Image imgBooster;
    public Button btnClaim;

    public Transform txtHolder;
    private BoosterItem _targetBoosterItem;

    protected override void Init()
    {
        btnClaim.OnClicked(OnClickedClaim);

        int boosterIndex = BoosterController.Instance.GetCurrentTutorialBoosterIndex();

        if (boosterIndex == -1) boosterIndex = 0;
        _targetBoosterItem = BoosterController.Instance.GetItemByIndex(boosterIndex);

        if (_targetBoosterItem != null)
        {
            imgBooster.sprite = _targetBoosterItem.iconBooster.sprite;
            //imgBooster.FitToTargetHeight(targetSize);
            imgBooster.transform.localScale = Vector3.one * 3f;
        }
    }

    protected override void InitState()
    {

    }

    private void OnClickedClaim()
    {
        btnClaim.SetActive(false);
        txtHolder.gameObject.SetActive(false);
        if (_targetBoosterItem == null)
        {
            Debug.LogError("Target Booster Item đang bị Null, không thể bay!");
            Close();
            return;
        }

        RectTransform targetRect = _targetBoosterItem.GetComponent<RectTransform>();
        var currentTimeAnim = 0f;
        var duration = 0.75f;
        RectTransform boosterRect = imgBooster.GetComponent<RectTransform>();

        Sequence seq = DOTween.Sequence();

        seq.Insert(
            currentTimeAnim,
            imgBooster.transform.DOJump(
                targetRect.position,
                3,
                1,
                duration
            )
        );

        seq.Insert(
            currentTimeAnim,
            imgBooster.transform.DOScale(Vector3.one, duration)
        );

        currentTimeAnim += duration;

        seq.InsertCallback(
            currentTimeAnim,
            () =>
            {
                Close();
                HandAnimation.Instance.HighlightUI(_targetBoosterItem.gameObject);
                HandAnimation.Instance.PlayAnimUI(_targetBoosterItem.transform);
            }
        );
    }
}