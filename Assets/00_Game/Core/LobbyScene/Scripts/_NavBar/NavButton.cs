using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


public class NavButton : MonoBehaviour
{
    public ENavType navType;
    [Space(5)] [SerializeField] private Button btnMain;

    [SerializeField] RectTransform icon;
    
    [SerializeField] private Sprite sprUnSelected;
    [SerializeField] private Image imgMain;
    [SerializeField] private RectTransform rectMain;

    public void Init()
    {
        UIUtils.FitToTargetHeight(icon.GetComponent<Image>(), 200f);
    }

    public RectTransform Icon => icon;

    public void HandleSelected(bool isSelected, Sprite sprSelected, Vector2 targetSize, Vector2 defaultSize, bool animate = false)
    {
        rectMain.sizeDelta = isSelected ? targetSize : defaultSize;
        imgMain.SetSprite(isSelected ? sprSelected : sprUnSelected);

        icon.DOKill();
        icon.localRotation = Quaternion.identity;

        if (isSelected)
        {
            if (animate)
            {
                // Bẹp lúc bật lên, rồi nảy quá đà vào vị trí + lắc nhẹ
                icon.localScale = new Vector3(icon.localScale.x * 1.15f, icon.localScale.y * 0.85f, 1f);
                icon.DOScale(Vector3.one * 1.3f, 0.35f).SetEase(Ease.OutBack);
                icon.DOAnchorPosY(100f, 0.25f).SetEase(Ease.OutBack);
                icon.DOPunchRotation(new Vector3(0f, 0f, 14f), 0.5f, 6, 0.5f);
            }
            else
            {
                icon.DOScale(Vector3.one * 1.3f, 0.15f);
                icon.DOAnchorPosY(100f, 0.15f);
            }
        }
        else
        {
            icon.DOScale(Vector3.one * 0.7f, 0.15f);
            icon.DOAnchorPosY(0f, 0.15f);
        }
    }

    // Bấm lại tab đang mở
    public void Nudge()
    {
        icon.DOComplete();
        icon.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.6f);
        icon.DOPunchRotation(new Vector3(0f, 0f, 8f), 0.35f, 6, 0.5f);
    }

    public void SetupClick(System.Action callback)
    {
        btnMain.OnClicked(() => callback?.Invoke());
    }


    public void InitSetup()
    {
        rectMain = GetComponent<RectTransform>();
        btnMain = GetComponent<Button>();
        icon = transform.GetComponentInChildren<RectTransform>();
        imgMain = GetComponent<Image>();
    }
}

public enum ENavType
{
    Shop = 0,
    Lobby = 1,
    Rank = 2,
}