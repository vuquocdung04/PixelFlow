using UnityEngine;
using UnityEngine.UI;

public class AvatarItem : MonoBehaviour
{
    public Image iconAvatar;
    public Transform choosing;
    public Button btn;

    public int IdAvatar { get; private set; }

    public void Setup(int id, Sprite sprite, System.Action<AvatarItem> onClick)
    {
        IdAvatar = id;
        iconAvatar.sprite = sprite;
        SetChoosing(false);

        btn.OnClicked(() => onClick?.Invoke(this));
    }

    public void SetChoosing(bool isOn)
    {
        choosing.gameObject.SetActive(isOn);
    }
}