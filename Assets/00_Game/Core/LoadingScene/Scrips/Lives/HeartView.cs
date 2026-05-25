using System;
using EventDispatcher;
using TMPro;
using UnityEngine;

public class HeartView : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private TextMeshProUGUI heartCountText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Optional Icons")]
    [SerializeField] private GameObject normalHeartIcon;
    [SerializeField] private GameObject unlimitedHeartIcon;

    private int lastUpdateKey = -1;

    private void OnEnable()
    {
        this.RegisterListener(EventID.CHANGE_HEART, OnHeartChanged);
        UpdateHeartStateVisuals();
    }

    private void OnDisable()
    {
        this.RemoveListener(EventID.CHANGE_HEART, OnHeartChanged);
    }

    private void OnHeartChanged(object param) => UpdateHeartStateVisuals();

    private void UpdateHeartStateVisuals()
    {
        if (HeartManager.Instance == null) return;

        bool isUnlimited = UseProfile.IsUnlimitedHeart;

        if (normalHeartIcon != null) normalHeartIcon.SetActive(!isUnlimited);
        if (unlimitedHeartIcon != null) unlimitedHeartIcon.SetActive(isUnlimited);
        if (heartCountText != null) heartCountText.gameObject.SetActive(!isUnlimited);

        if (!isUnlimited)
        {
            int hearts = UseProfile.Heart;
            int max = HeartManager.Instance.MaxHearts;
            heartCountText.text = $"{hearts}";

            if (hearts >= max)
            {
                timerText.text = HeartManager.FULL_LABEL;
                lastUpdateKey = -2;
            }
        }
    }

    private void Update()
    {
        TimeSpan time;
        if (UseProfile.IsUnlimitedHeart)
        {
            time = HeartManager.Instance.GetUnlimitedTimeRemaining();
        }
        else if (UseProfile.Heart < HeartManager.Instance.MaxHearts)
        {
            time = TimeSpan.FromSeconds(HeartManager.Instance.GetTimeToNextHeart());
        }
        else
        {
            return;
        }

        UpdateTimerText(time);
    }

    private void UpdateTimerText(TimeSpan time)
    {
        int updateKey = time.TotalHours >= 1 ? (int)time.TotalMinutes : (int)time.TotalSeconds;
        if (updateKey == lastUpdateKey) return;
        lastUpdateKey = updateKey;

        timerText.text = TimeManager.Format(time);
    }
}