using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class NavController : MonoBehaviour
{
    public static NavController Instance { get; private set; }
    [SerializeField] private Sprite sprSelected;
    public List<NavButton> navButtons;
    private Vector2 sizeSelected;
    private Vector2 sizeUnselected;
    private NavButton currentNavSelected;

    public void Init()
    {
        Instance = this;

        foreach (var nav in navButtons)
        {
            nav.Init();
            nav.SetupClick(delegate
            {
                if (nav != currentNavSelected)
                {
                    UpdateNavButtonState(nav);
                }
            });
        }
        InitAfterLayoutAsync().Forget();
    }
    private async UniTaskVoid InitAfterLayoutAsync()
    {
        await UniTask.WaitForEndOfFrame(this);
        InitNavButtonStateWith(ENavType.Lobby);
    }
    private void InitSize()
    {
        int countNavBar = navButtons.Count;
        float totalWidth = GetComponent<RectTransform>().rect.width;
        float widthSelected = totalWidth * 0.45f;

        float height = 250;
        sizeSelected = new Vector2(widthSelected, height);
        if (countNavBar > 1)
        {
            float remainingPercent = 1.0f - 0.45f;
            float widthUnselected = totalWidth * remainingPercent / (countNavBar - 1);
            sizeUnselected = new Vector2(widthUnselected, height);
        }
    }
    public void NavigateTo(ENavType type)
    {
        var target = navButtons.Find(n => n.navType == type);
        if (target == null || target == currentNavSelected) return;
        UpdateNavButtonState(target);
    }
    private void InitNavButtonStateWith(ENavType type)
    {
        InitSize();
        foreach (var t in navButtons)
        {
            if (t.navType == type)
            {
                currentNavSelected = t;
                t.HandleSelected(true, sprSelected, sizeSelected, sizeUnselected);
            }
            else
            {
                t.HandleSelected(false, sprSelected, sizeSelected, sizeUnselected);
            }

        }
    }
    private void UpdateNavButtonState(NavButton navButton)
    {
        foreach (var t in navButtons)
        {
            t.HandleSelected(false, sprSelected, sizeSelected, sizeUnselected);
        }
        HandleScreenSliding(navButton);
        currentNavSelected = navButton;
        navButton.HandleSelected(true, sprSelected, sizeSelected, sizeUnselected);
    }
    private void HandleScreenSliding(NavButton clicked)
    {
        bool clickedIsRight = clicked.transform.localPosition.x > currentNavSelected.transform.localPosition.x;

        var outAnim = clickedIsRight ? BoxAnimationFactory.SlideToLeft : BoxAnimationFactory.SlideToRight;
        var inAnim = clickedIsRight ? BoxAnimationFactory.SlideFromRight : BoxAnimationFactory.SlideFromLeft;

        ClosePrevBox(currentNavSelected.navType, outAnim);
        OpenCurrentBox(clicked.navType, inAnim);
    }
    private void OpenCurrentBox(ENavType type, IShowAnimation anim)
    {
        switch (type)
        {
            case ENavType.Shop:
                ShopBox.Instance.Show(anim);
                break;
            case ENavType.Lobby:
                LobbyBox.Instance.Show(anim);
                break;
            case ENavType.Rank:
                RankBox.Instance.Show(anim);
                break;
        }
    }
    private void ClosePrevBox(ENavType type, IShowAnimation anim)
    {
        switch (type)
        {
            case ENavType.Shop:
                if (ShopBox.Instance != null) ShopBox.Instance.Close(anim);
                break;
            case ENavType.Lobby:
                if (LobbyBox.Instance != null) LobbyBox.Instance.Close(anim);
                break;
            case ENavType.Rank:
                if (RankBox.Instance != null) RankBox.Instance.Close(anim);
                break;
        }
    }

    [ContextMenu("Setup Nav button")]
    private void Setup()
    {
        navButtons.Clear();
        navButtons = GetComponentsInChildren<NavButton>().ToList();

        foreach (var t in navButtons)
        {
            t.InitSetup();
        }
    }
}
