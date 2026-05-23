using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Booster2Cannon : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private ParticleSystem shootFx;
    [SerializeField] private Projectile projectilePrefab;
    private Sequence _recoilSeq;
    public async UniTask FireAt(List<Block> targets)
    {
        transform.localScale = Vector3.zero;
        await transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).AsyncWaitForCompletion();
        await transform.DORotate(new Vector3(0f, 180f, 0f), 0.2f, RotateMode.LocalAxisAdd)
                       .SetEase(Ease.OutBack)
                       .AsyncWaitForCompletion();

        targets.Sort((a, b) => b.gridRow.CompareTo(a.gridRow));

        int i = 0;
        while (i < targets.Count)
        {
            int currentRow = targets[i].gridRow;

            if (shootFx != null) shootFx.Play();
            PlayRecoil();

            while (i < targets.Count && targets[i].gridRow == currentRow)
            {
                var target = targets[i++];
                if (target == null || !target.IsAlive || target.IsClaimed) continue;

                target.Claim();
                Projectile p = SimplePool2.Spawn(projectilePrefab, shootPoint.position, Quaternion.identity);
                p.Fire(target);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.05f));
        }

        await UniTask.Delay(TimeSpan.FromSeconds(0.2f), ignoreTimeScale: false);
        await transform.DOMove(new Vector3(0f, 30f, 10f), 0.5f).SetEase(Ease.InBack).AsyncWaitForCompletion();

        Destroy(gameObject);
    }

    private void PlayRecoil()
    {
        if (_recoilSeq != null && _recoilSeq.IsActive() && _recoilSeq.IsPlaying()) return;

        _recoilSeq = DOTween.Sequence();
        _recoilSeq.Append(transform.DOScale(1.3f, 0.02f).SetEase(Ease.OutQuad));
        _recoilSeq.Append(transform.DOScale(1.5f, 0.05f).SetEase(Ease.OutBack));
    }
}