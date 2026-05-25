using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class AudioConfig
{
    [Header("🔊 Identification")] 
    public string key;

    [Header("Audio Data")]
    public List<AudioClip> variants;

    [Header("Pitch Randomization")]
    [Range(0.5f, 2f)] public float minPitch = 0.7f;
    [Range(0.5f, 2f)] public float maxPitch = 1.1f;
    public AudioClip GetRandomClip()
    {
        if (variants == null || variants.Count == 0)
            return null;

        int rand = Random.Range(0, variants.Count);
        return variants[rand];
    }

    public float GetRandomPitch()
    {
        return Random.Range(minPitch, maxPitch);
    }
}

[CreateAssetMenu(fileName = "AudioDataBase", menuName = "DATA/AudioDataBase")]
public class AudioDataBase : ScriptableObject
{
#if UNITY_EDITOR
    [Header("Editor Only: Chọn thư mục chứa Audio")]
    [FolderPath(RequireExistingPath = true)]
    public string audioFolder;

    [Button("Auto Fill From Folder", ButtonSizes.Large)]
    private void AutoFillFromFolder()
    {
        if (string.IsNullOrEmpty(audioFolder))
        {
            Debug.LogWarning("Vui lòng chọn thư mục chứa Audio trước!");
            return;
        }

        if (!AssetDatabase.IsValidFolder(audioFolder))
        {
            Debug.LogWarning($"Đường dẫn không hợp lệ hoặc không phải thư mục: {audioFolder}");
            return;
        }

        List<AudioConfig> parsedData = new List<AudioConfig>();

        // 1. Quét file lẻ ở thư mục gốc
        string[] topLevelFiles = Directory.GetFiles(audioFolder, "*.*", SearchOption.TopDirectoryOnly);
        foreach (string filePath in topLevelFiles)
        {
            if (filePath.EndsWith(".meta")) continue;

            string assetPath = filePath.Replace('\\', '/');
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            if (clip != null)
            {
                parsedData.Add(new AudioConfig
                {
                    key = clip.name,
                    variants = new List<AudioClip> { clip },
                    minPitch = 0.7f,
                    maxPitch = 1.1f
                });
            }
        }

        // 2. Quét các thư mục con
        string[] subFolders = AssetDatabase.GetSubFolders(audioFolder);
        foreach (string subFolder in subFolders)
        {
            string folderName = Path.GetFileName(subFolder);
            AudioConfig folderData = new AudioConfig 
            { 
                key = folderName,
                variants = new List<AudioClip>(),
                minPitch = 0.7f,
                maxPitch = 1.1f
            };

            string[] clipGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { subFolder });
            foreach (string guid in clipGuids)
            {
                string clipPath = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null)
                {
                    folderData.variants.Add(clip);
                }
            }

            if (folderData.variants.Count > 0)
            {
                parsedData.Add(folderData);
            }
        }

        // 3. Ghi dữ liệu
        audioConfigs = parsedData;
        
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"Auto filled {parsedData.Count} audio keys from {audioFolder}!");
    }
#endif

    [Header("🔊 All Audio Configurations")]
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "key")] // Bật lại List gọn gàng của Odin
    public List<AudioConfig> audioConfigs = new();
}