using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoosterSfxPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private Button btnClose;
    [SerializeField] private RectTransform root;

    [Header("Animation")]
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private Ease scaleInEase = Ease.OutBack;
    [SerializeField] private Ease scaleOutEase = Ease.InBack;

    private void Awake()
    {
        btnClose.OnClicked(OnCloseClicked);
        gameObject.SetActive(false);
    }

    public void Show(Sprite icon, string titleText, string descText, float targetY)
    {
        image.sprite = icon;
        title.text = titleText;
        description.text = descText;
        root.anchoredPosition = new Vector3(0, targetY, 0);
        gameObject.SetActive(true);

    }
    private void OnCloseClicked()
    {
        gameObject.SetActive(false);
        BoosterController.Instance.OnSfxPopupClosed();
    }
    public void ForceClose()
    {
        gameObject.SetActive(false);
    }
}