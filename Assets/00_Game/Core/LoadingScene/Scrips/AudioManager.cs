using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioSource asBg;

    [Header("SFX Pooling")]
    public GameObject sfxPrefab;
    public float sfxSpamCooldown = 0.08f;

    private AudioDataBase audioDataBase;
    private Dictionary<string, AudioConfig> audioLookup;
    private Dictionary<string, float> lastPlayTimes = new();
    private float currentSfxVolume = 1f;

    public void Init()
    {
        Instance = this;
        audioDataBase = DataRepo.Instance.audioData;
        BuildAudioLookup();
        SetMusicVolume();
        SetSoundVolume(1f);
    }

    private void BuildAudioLookup()
    {
        audioLookup = new Dictionary<string, AudioConfig>();
        foreach (var config in audioDataBase.audioConfigs)
        {
            if (string.IsNullOrEmpty(config.key)) continue;

            string lowerKey = config.key.ToLower();
            if (!audioLookup.TryAdd(lowerKey, config))
                Debug.LogWarning($"Tìm thấy AudioKey bị trùng: {config.key}");
        }
    }

    public void PlaySfx(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        string lowerKey = key.ToLower();

        if (!audioLookup.TryGetValue(lowerKey, out var config))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"Không tìm thấy AudioKey SFX: {key}");
#endif
            return;
        }

        float volume = lowerKey == "coin" ? 0.2f : currentSfxVolume;
        PlayClipInternal(lowerKey, config.GetRandomClip(), config.GetRandomPitch(), volume);
    }

    public void PlaySfx(AudioClip clip, float pitch = 1f)
    {
        if (clip == null) return;
        PlayClipInternal(clip.name.ToLower(), clip, pitch, currentSfxVolume);
    }

    private void PlayClipInternal(string throttleKey, AudioClip clip, float pitch, float volume)
    {
        if (!UseProfile.OnSound || clip == null) return;

        if (lastPlayTimes.TryGetValue(throttleKey, out float lastTime) &&
            Time.time - lastTime < sfxSpamCooldown) return;
        lastPlayTimes[throttleKey] = Time.time;

        var sfxObj = SimplePool2.Spawn(sfxPrefab, Vector3.zero, Quaternion.identity);
        if (sfxObj == null) return;

        var source = sfxObj.GetComponent<AudioSource>();
        source.clip = clip;
        source.pitch = pitch;
        source.volume = volume;
        source.Play();

        DespawnAfterPlayAsync(sfxObj, clip.length).Forget();
    }

    private async UniTaskVoid DespawnAfterPlayAsync(GameObject obj, float delay)
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(delay));
        if (obj != null && obj.activeInHierarchy) SimplePool2.Despawn(obj);
    }

    public void PlayMusic(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (!audioLookup.TryGetValue(key.ToLower(), out var config))
        {
            Debug.LogWarning($"Không tìm thấy AudioKey nhạc: {key}");
            return;
        }

        var clip = config.GetRandomClip();
        if (clip == null) return;

        asBg.clip = clip;
        asBg.loop = true;
        asBg.pitch = 1f;
        asBg.Play();
        SetMusicVolume();
    }

    public void RefreshMusicVolume() => SetMusicVolume();

    private void SetMusicVolume()
    {
        asBg.volume = UseProfile.OnMusic ? 0.2f : 0f;
    }

    private void SetSoundVolume(float volume)
    {
        currentSfxVolume = UseProfile.OnSound ? volume : 0f;
    }
}