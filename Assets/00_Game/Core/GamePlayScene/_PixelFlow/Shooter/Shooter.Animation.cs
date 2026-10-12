using System;
using DG.Tweening;
using UnityEngine;

public enum ShooterAnimState { Idle, Combat, Blocked }

public partial class Shooter
{
    [Space(10), Header("Animation")]
    public AudioClip blockSFX;
    public float jumpDuration = 0.5f;
    public ShooterAnimState currentAnimState = ShooterAnimState.Idle;
    Tween stateTween;
    Vector3 baseScale;
    bool baseScaleCached;
    private Tween _wobbleTween;
    private readonly IdleState _idleState = new IdleState();
    private readonly CombatState _combatState = new CombatState();
    private readonly BlockedState _blockedState = new BlockedState();

    [Space(10), Header("Launch (tap -> conveyor)")]
    [Tooltip("Pivot chỉ chứa phần hình, để squash/stretch không ảnh hưởng vị trí & scale gốc")]
    public Transform scalePivot;
    [Tooltip("Thời gian bay khi gần / xa, nội suy theo khoảng cách")]
    public Vector2 launchDuration = new(0.32f, 0.5f);
    public float launchFarDistance = 10f;
    [Tooltip("Độ cao vòng cung = khoảng cách * ratio, kẹp trong khoảng")]
    public float launchHeightRatio = 0.35f;
    public Vector2 launchHeightRange = new(1.2f, 3.2f);
    [Tooltip("Xoay quanh trục đứng khi bay (độ). 0 = không xoay")]
    public float launchSpin = 360f;
    public float anticipation = 0.07f;

    [Header("Land")]
    public ParticleSystem landFx;
    public AudioClip landSFX;
    [Tooltip("Màu bụi đáp hoà với màu shooter")]
    [Range(0f, 1f)] public float landFxTint = 0.45f;

    private Sequence _launchSeq;
    private Tween _squashTween;
    private Vector3 _pivotScale = Vector3.one;
    private bool _pivotCached;

    private Transform Pivot => scalePivot != null ? scalePivot : transform;

    private void CachePivot()
    {
        if (_pivotCached) return;
        _pivotCached = true;
        _pivotScale = Pivot.localScale;
    }

    // Nhảy ngắn giữa các ô chờ (dồn hàng, quay về wait area)
    public void JumpTo(Vector3 target, TweenCallback onComplete = null)
    {
        float dist = Vector3.Distance(transform.position, target);
        float power = Mathf.Clamp(dist * launchHeightRatio, 0.8f, 2f);
        var tween = transform.DOJump(target, power, 1, jumpDuration).SetEase(Ease.OutQuad);

        if (HasLink)
            tween.OnUpdate(() => RefreshAllLinks());

        tween.OnComplete(() =>
        {
            PlayLandSquash(0.7f);
            onComplete?.Invoke();
        });
    }

    // Tap -> bật lên, bay theo vòng cung bám theo slot đang di chuyển, đáp xuống có squash + bụi.
    // target: điểm đáp (thường là ItemParent của slot), vị trí được đọc lại mỗi frame.
    public float LaunchTo(Transform target, Action onLanded)
    {
        CachePivot();
        _launchSeq?.Kill();
        _squashTween?.Kill();
        Pivot.localScale = _pivotScale;

        Vector3 start = transform.position;
        float dist = Vector3.Distance(start, target.position);
        float k = Mathf.Sqrt(Mathf.Clamp01(dist / Mathf.Max(0.01f, launchFarDistance)));
        float duration = Mathf.Lerp(launchDuration.x, launchDuration.y, k);
        float height = Mathf.Clamp(dist * launchHeightRatio, launchHeightRange.x, launchHeightRange.y);
        Quaternion startRot = transform.rotation;

        _launchSeq = DOTween.Sequence().SetLink(gameObject);

        // 1. Lấy đà: bẹp xuống (cũng là phản hồi tap)
        _launchSeq.Append(Pivot.DOScale(Vector3.Scale(_pivotScale, new Vector3(1.25f, 0.7f, 1.25f)), anticipation).SetEase(Ease.OutQuad));

        // 2. Bay: ngang ease-out (nhanh lúc rời, chậm dần khi tới), dọc parabol, thân vươn dài khi lên
        _launchSeq.Append(DOVirtual.Float(0f, 1f, duration, t =>
        {
            if (target == null) return;

            float h = 1f - (1f - t) * (1f - t);
            Vector3 pos = Vector3.LerpUnclamped(start, target.position, h);
            pos.y += height * 4f * t * (1f - t);
            transform.position = pos;

            float stretch = Mathf.Sin(t * Mathf.PI);
            float rise = Mathf.Clamp01(1f - t * 1.6f); // vươn mạnh lúc mới rời đất
            Pivot.localScale = Vector3.Scale(_pivotScale, new Vector3(1f - 0.12f * rise, 1f + 0.22f * rise + 0.04f * stretch, 1f - 0.12f * rise));

            if (launchSpin != 0f)
                transform.rotation = startRot * Quaternion.Euler(0f, launchSpin * Mathf.SmoothStep(0f, 1f, t), 0f);

            if (HasLink) RefreshAllLinks();
        }).SetEase(Ease.Linear));

        _launchSeq.OnComplete(() =>
        {
            transform.rotation = startRot;
            PlayLand();
            onLanded?.Invoke();
        });

        return anticipation + duration;
    }

    // Đáp: bẹp mạnh rồi bật về, bụi tung + âm thanh
    public void PlayLand()
    {
        PlayLandSquash(1f);

        Color tint = Color.Lerp(Color.white, BodyColor, landFxTint);
        SpawnFx(landFx, transform.position, (name, c) => new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a));

        if (landSFX != null) AudioManager.Instance.PlaySfx(landSFX);
    }

    // Spawn particle từ pool; colorize(tên system, màu gốc prefab) -> màu dùng lần này
    private static void SpawnFx(ParticleSystem prefab, Vector3 pos, Func<string, Color, Color> colorize, float lifetime = 1.5f)
    {
        if (prefab == null) return;

        var fx = SimplePool2.Spawn(prefab.gameObject, pos, Quaternion.identity);
        if (fx == null) return;

        // Lấy màu gốc từ prefab (instance trong pool đã bị tô màu lần trước)
        var src = prefab.GetComponentsInChildren<ParticleSystem>(true);
        var dst = fx.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < dst.Length; i++)
        {
            var main = dst[i].main;
            Color c = i < src.Length ? src[i].main.startColor.color : Color.white;
            main.startColor = colorize != null ? colorize(dst[i].name, c) : c;
            dst[i].Clear(true);
            dst[i].Play(true);
        }

        DOVirtual.DelayedCall(lifetime, () => { if (fx != null && fx.activeSelf) SimplePool2.Despawn(fx); });
    }

    [Header("Out of ammo")]
    public ParticleSystem despawnFx;
    public AudioClip despawnSFX;

    // Hết đạn: sao bung theo màu shooter, bụi giữ màu gốc
    public void PlayDespawnFx()
    {
        Color star = Color.Lerp(BodyColor, Color.white, 0.25f);
        Vector3 pos = scalePivot != null ? scalePivot.position + Vector3.up * 0.6f : transform.position;
        SpawnFx(despawnFx, pos, (name, c) => name.Contains("Dust") || name.Contains("Smoke") ? c : new Color(star.r, star.g, star.b, c.a));
        if (despawnSFX != null) AudioManager.Instance.PlaySfx(despawnSFX);
    }

    private void PlayLandSquash(float strength)
    {
        CachePivot();
        _squashTween?.Kill();
        Pivot.localScale = Vector3.Scale(_pivotScale, new Vector3(1f + 0.3f * strength, 1f - 0.32f * strength, 1f + 0.3f * strength));
        _squashTween = Pivot.DOScale(_pivotScale, 0.32f).SetEase(Ease.OutElastic, 1.1f, 0.35f).SetLink(gameObject);
    }

    public void SetAnimState(ShooterAnimState state)
    {
        if (!baseScaleCached)
        {
            baseScale = transform.localScale;
            baseScaleCached = true;
        }

        stateTween?.Kill(true);
        transform.localScale = baseScale;

        ShooterState next = state switch
        {
            ShooterAnimState.Idle => _idleState,
            ShooterAnimState.Combat => _combatState,
            ShooterAnimState.Blocked => _blockedState,
            _ => null,
        };

        ChangeState(next);
    }

    public void PlayRejectWobble()
    {
        if (_wobbleTween != null && _wobbleTween.IsActive() && _wobbleTween.IsPlaying()) return;
        _wobbleTween = transform.DOPunchRotation(new Vector3(0f, 0f, 10f), 0.35f, vibrato: 10, elasticity: 1f);
        AudioManager.Instance.PlaySfx(blockSFX);
    }

    private void KillLaunch()
    {
        _launchSeq?.Kill();
        _squashTween?.Kill();
        if (_pivotCached) Pivot.localScale = _pivotScale;
    }
}
