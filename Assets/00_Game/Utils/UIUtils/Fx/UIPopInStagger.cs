using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Bật lần lượt các target (wave) mỗi khi object được enable.
// Chờ 1 frame để con (vd ButtonScalePress) kịp cache scale trong Awake.
public class UIPopInStagger : MonoBehaviour
{
    [SerializeField] private List<Transform> targets = new();
    [SerializeField] private float startDelay = 0.1f;
    [SerializeField] private float interval = 0.045f;
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float fromScale = 0f;
    [SerializeField] private Ease ease = Ease.OutBack;

    [Tooltip("Chờ tới lúc màn chuyển scene bắt đầu mở rồi mới bật, để không diễn sau màn che")]
    [SerializeField] private bool waitSceneReveal = true;

    private readonly List<Vector3> _originalScales = new();
    private Sequence _sequence;
    private Coroutine _routine;

    private void Awake()
    {
        _originalScales.Clear();
        for (int i = 0; i < targets.Count; i++)
            _originalScales.Add(targets[i] != null ? targets[i].localScale : Vector3.one);
    }

    private void OnEnable()
    {
        _routine = StartCoroutine(PlayNextFrame());
    }

    private void OnDisable()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (waitSceneReveal && FXManager.Instance != null) FXManager.Instance.CancelWhenRevealing(Play);

        _sequence?.Kill();
        _sequence = null;
        RestoreScales();
    }

    private IEnumerator PlayNextFrame()
    {
        yield return null;
        _routine = null;

        // Ẩn sau 1 frame (con đã cache scale) để không loé full size trong lúc chờ màn mở
        for (int i = 0; i < targets.Count; i++)
            if (targets[i] != null) targets[i].localScale = _originalScales[i] * fromScale;

        if (waitSceneReveal && FXManager.Instance != null) FXManager.Instance.WhenRevealing(Play);
        else Play();
    }

    public void Play()
    {
        if (this == null) return;

        _sequence?.Kill();
        _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        float delay = startDelay;
        for (int i = 0; i < targets.Count; i++)
        {
            Transform target = targets[i];
            if (target == null) continue;

            Vector3 scale = _originalScales[i];
            if (!target.gameObject.activeInHierarchy)
            {
                target.localScale = scale;
                continue;
            }

            target.localScale = scale * fromScale;
            _sequence.Insert(delay, target.DOScale(scale, duration).SetEase(ease));
            delay += interval;
        }
    }

    private void RestoreScales()
    {
        for (int i = 0; i < targets.Count; i++)
            if (targets[i] != null) targets[i].localScale = _originalScales[i];
    }
}
