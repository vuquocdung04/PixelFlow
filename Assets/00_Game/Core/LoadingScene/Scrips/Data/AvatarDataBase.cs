using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AvatarDataBase", menuName = "DATA/AvatarDataBase")]
public class AvatarDataBase : ScriptableObject
{
    public List<Sprite> avatars;

    [Tooltip("Level mở khoá theo cùng index với avatars. Thiếu phần tử hoặc <= 1 = mở sẵn.")]
    public List<int> levelUnlocks = new();

    public Sprite GetSpriteById(int id)
    {
        if (id < 0 || id >= avatars.Count) return null;
        return avatars[id];
    }

    public int GetLevelUnlock(int id) => id >= 0 && id < levelUnlocks.Count ? levelUnlocks[id] : 0;

    public bool IsUnlock(int id)
    {
        int level = GetLevelUnlock(id);
        return level <= 1 || UseProfile.Level.Value >= level;
    }

    public int Count => avatars.Count;
}
