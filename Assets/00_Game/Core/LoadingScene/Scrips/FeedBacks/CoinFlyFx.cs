using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class CoinFlyFx : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public List<Sprite> sprites;
    public float frameDuration = 0.1f;
    public float scaleUpDuration = 0.2f;
    public float flyDelay = 0.1f;
    public float moveDuration = 0.5f;

    Vector3 originalScale;

    void OnEnable()
    {
        originalScale = transform.localScale;
        AnimateSprites().Forget();
    }

    async UniTaskVoid AnimateSprites()
    {
        if (sprites == null || sprites.Count == 0) return;

        var token = this.GetCancellationTokenOnDestroy();
        int index = 0;
        while (!token.IsCancellationRequested)
        {
            spriteRenderer.sprite = sprites[index];
            index = (index + 1) % sprites.Count;
            await UniTask.Delay(TimeSpan.FromSeconds(frameDuration), cancellationToken: token);
        }
    }

    public async UniTask MoveTo(Transform target, Action onArrived = null)
    {
        var token = this.GetCancellationTokenOnDestroy();

        transform.localScale = Vector3.zero;
        await transform.DOScale(originalScale, scaleUpDuration).SetEase(Ease.OutBack).ToUniTask(cancellationToken: token);

        await UniTask.Delay(TimeSpan.FromSeconds(flyDelay), cancellationToken: token);

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        await transform.DOMove(target.position, moveDuration).SetEase(Ease.InQuad).ToUniTask(cancellationToken: token);

        onArrived?.Invoke();
        Destroy(gameObject);
    }
}