using RDG;
using UnityEngine.UI;

public static partial class UIUtils
{
    public static void OnClicked(this Button btn, System.Action callback)
    {
        btn.onClick.AddListener(delegate
        {
            AudioManager.Instance.PlaySfx("Click");
            Vibration.Vibrate(50);
            callback.Invoke();
        });
    }

    public static void SetActive(this Button btn, bool isActive)
    {
        btn.gameObject.SetActive(isActive);
    }
}