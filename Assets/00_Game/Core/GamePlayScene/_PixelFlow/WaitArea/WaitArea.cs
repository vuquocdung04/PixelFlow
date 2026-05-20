using DG.Tweening;
using UnityEngine;

public class WaitArea : MonoBehaviour
{
    [field: SerializeField] public Shooter Occupant { get; private set; }
    public SpriteRenderer strokeWarning;
    public float blinkDuration = 0.25f;
    public int blinkCount = 2;
    private Tween blinkTween;
    public bool IsEmpty => Occupant == null;
    public void AddOccupant(Shooter shooter)
    {
        Occupant = shooter;
    }
    public void ResetToDefault()
    {
        Occupant = null;
    }

    public void PlayWarningBlink()
    {
        blinkTween?.Kill();
        strokeWarning.gameObject.SetActive(true);

        Color c = strokeWarning.color;
        c.a = 0f;
        strokeWarning.color = c;

        blinkTween = strokeWarning
            .DOFade(1f, blinkDuration)
            .SetLoops(blinkCount * 2, LoopType.Yoyo)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                strokeWarning.gameObject.SetActive(false);
            });
    }
}