using UnityEngine;
using UnityEngine.UI;

public class LoadingBox : MonoBehaviour
{
    public Image fill;
    public CanvasGroup canvasGroup;

    public float Progress => fill.fillAmount;

    public void Init()
    {
        fill.fillAmount = 0f;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    // Thanh chỉ tiến lên, không lùi
    public void SetProgress(float value)
    {
        fill.fillAmount = Mathf.Max(fill.fillAmount, Mathf.Clamp01(value));
    }
}
