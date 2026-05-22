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
    private Tween _scaleTween;

    private void Awake()
    {
        btnClose.OnClicked(OnCloseClicked);
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void Show(Sprite icon, string titleText, string descText, float targetY)
    {
        image.sprite = icon;
        title.text = titleText;
        description.text = descText;

        root.anchoredPosition = new Vector3(0,targetY,0);


        gameObject.SetActive(true);
        _scaleTween?.Kill();
        _scaleTween = transform.DOScale(Vector3.one, scaleDuration).SetEase(scaleInEase);
    }

    private void OnCloseClicked()
    {
        _scaleTween?.Kill();
        _scaleTween = transform.DOScale(Vector3.zero, scaleDuration)
            .SetEase(scaleOutEase)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                BoosterController.Instance.OnSfxPopupClosed();
            });
    }
}