using System.Collections.Generic;
using EventDispatcher;
using UnityEngine;
using UnityEngine.UI;

public class AvatarBox : BaseBox<AvatarBox>
{
    [Header("Buttons")]
    public Button btnClose;
    public Button btnSave;

    [Header("Main")]
    public Image mainAvatar;

    [Header("Avatar Items")]
    public AvatarItem avatarItemPrefab;
    public Transform avatarItemParent;

    private readonly List<AvatarItem> _items = new();
    private int _previewId;
    private int _savedId;

    private AvatarDataBase Data => DataRepo.Instance.avatarData;

    protected override void Init()
    {
        btnClose.OnClicked(OnCloseClicked);
        btnSave.OnClicked(OnSaveClicked);

        SpawnItems();
    }

    protected override void InitState()
    {
        _savedId = UseProfile.AvatarId.Value;
        _previewId = _savedId;
        ApplyPreview(_previewId);
    }

    private void SpawnItems()
    {
        for (int i = 0; i < Data.Count; i++)
        {
            var item = Instantiate(avatarItemPrefab, avatarItemParent);
            item.Setup(i, Data.GetSpriteById(i), OnAvatarItemClicked);
            _items.Add(item);
        }
    }

    private void OnAvatarItemClicked(AvatarItem item)
    {
        _previewId = item.IdAvatar;
        ApplyPreview(_previewId);
    }

    private void ApplyPreview(int id)
    {
        mainAvatar.sprite = Data.GetSpriteById(id);

        foreach (var item in _items)
            item.SetChoosing(item.IdAvatar == id);
    }

    private void OnSaveClicked()
    {
        UseProfile.AvatarId.Value = _previewId;
        Debug.LogError($"{UseProfile.AvatarId.Value}");
        _savedId = _previewId;
        this.PostEvent(EventID.CHANGE_AVATAR);
        Close();
    }

    private void OnCloseClicked()
    {
        _previewId = _savedId;
        Close();
    }
}