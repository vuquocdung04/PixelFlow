using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Popup nhận thưởng: tia sáng + glow nở ra, ribbon tiêu đề rơi xuống, từng item bật ra (nảy, lắc, nổ sao, đếm số),
// xong hiện "Tap to continue". Đóng popup thì coin bay về ví rồi ví mới đếm số.
public class RewardBox : BaseBox<RewardBox>
{
    [Header("Refs")]
    [SerializeField] private Button btnTap;
    [SerializeField] private Image dim;
    [SerializeField] private RectTransform content;
    [SerializeField] private RewardDisplay itemTemplate;
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private GameObject txtTapToContinue;

    [Header("Layout")]
    [SerializeField, Min(1)] private int maxColumns = 4;
    [SerializeField] private Vector2 cellLarge = new(300f, 300f);
    [SerializeField] private Vector2 cellSmall = new(240f, 240f);

    [Header("Stage")]
    [SerializeField] private RectTransform[] rays;
    [SerializeField] private float[] raySpeeds = { 14f, -9f };
    [SerializeField] private Graphic glow;
    [SerializeField] private RectTransform title;
    [SerializeField] private UIBurstEffect itemBurst;
    [SerializeField] private UIBurstEffect confetti;
    [SerializeField] private RectTransform[] confettiAnchors;

    [Header("Reveal")]
    [SerializeField, Min(0f)] private float titleDrop = 160f;
    [SerializeField, Min(0f)] private float itemsDelay = 0.35f;
    [SerializeField, Min(0f)] private float revealDelay = 0.14f;
    [SerializeField, Min(0.01f)] private float revealDuration = 0.4f;
    [SerializeField, Min(0f)] private float countDuration = 0.5f;
    [SerializeField] private float wobbleAngle = 12f;

    [Header("Sound")]
    [SerializeField] private string soundOpen = "sfx-RewardGiftbox";
    [SerializeField] private string soundItem = "Coins";
    [SerializeField] private string soundFlyArrive = "Coins";

    [Header("Fly To Wallet")]
    [SerializeField, Min(1)] private int flyCount = 8;

    private readonly List<RewardDisplay> _items = new();
    private readonly List<GameReward> _rewards = new();
    private Coroutine _revealRoutine;
    private Sequence _stageTween;
    private Tween _glowPulse;
    private Tween _tapPulse;
    private Vector2 _titleBase;
    private bool _playing;
    private bool _claimable;
    private float _dimAlpha = 0.85f;

    public static void ShowRewards(List<GameReward> rewards, Transform holder)
    {
        if (holder == null)
        {
            RewardService.PostCoinChanged();
            return;
        }

        _ = Setup(holder, box =>
        {
            if (box == null)
            {
                RewardService.PostCoinChanged();
                return;
            }
            box.SetRewards(rewards);
            box.Show(BoxAnimationFactory.NoAnim);
            box.Play();
        });
    }

    protected override void Init()
    {
        if (itemTemplate != null)
        {
            itemTemplate.gameObject.SetActive(false);
        }
        if (title != null) _titleBase = title.anchoredPosition;
        if (dim != null) _dimAlpha = dim.color.a;
        btnTap.onClick.AddListener(OnTap);
    }

    protected override void InitState()
    {
    }

    private void Update()
    {
        if (!_playing || rays == null) return;

        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < rays.Length; i++)
            if (rays[i] != null) rays[i].Rotate(0f, 0f, (i < raySpeeds.Length ? raySpeeds[i] : 10f) * dt);
    }

    private void SetRewards(List<GameReward> list)
    {
        StopAll();
        _rewards.Clear();
        _rewards.AddRange(list);

        while (_items.Count < _rewards.Count)
        {
            var item = Instantiate(itemTemplate, content, false);
            item.name = "Reward_" + _items.Count;
            _items.Add(item);
        }

        // Tối đa maxColumns phần thưởng 1 hàng, ô nhỏ dần theo số lượng; nhiều hơn thì xuống hàng
        int count = _rewards.Count;
        float cell = count <= 2 ? cellLarge.x : count == 3 ? Mathf.Lerp(cellSmall.x, cellLarge.x, 0.5f) : cellSmall.x;

        if (grid != null)
        {
            grid.cellSize = new Vector2(cell, cell);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Clamp(count, 1, maxColumns);
        }

        for (int i = 0; i < _items.Count; i++)
        {
            bool used = i < count;
            _items[i].gameObject.SetActive(used);
            if (!used) continue;
            _items[i].Setup(_rewards[i]);
            _items[i].SetCellSize(cell);
        }
    }

    private void Play()
    {
        _playing = true;
        SetClaimable(false);
        PlayStage();
        _revealRoutine = StartCoroutine(Reveal());
    }

    private void PlayStage()
    {
        _stageTween?.Kill();
        _glowPulse?.Kill();
        _stageTween = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        AudioManager.Instance.PlaySfx(soundOpen);

        if (dim != null)
        {
            var c = dim.color;
            c.a = 0f;
            dim.color = c;
            _stageTween.Insert(0f, dim.DOFade(_dimAlpha, 0.25f));
        }

        if (rays != null)
        {
            for (int i = 0; i < rays.Length; i++)
            {
                if (rays[i] == null) continue;
                rays[i].localScale = Vector3.zero;
                _stageTween.Insert(0.05f * i, rays[i].DOScale(1f, 0.6f).SetEase(Ease.OutBack));
            }
        }

        if (glow != null)
        {
            Transform g = glow.transform;
            g.localScale = Vector3.one * 0.6f;
            _stageTween.Insert(0f, g.DOScale(1f, 0.5f).SetEase(Ease.OutCubic));
            _stageTween.InsertCallback(0.5f, () =>
                _glowPulse = g.DOScale(1.08f, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject));
        }

        if (title != null)
        {
            title.anchoredPosition = _titleBase + new Vector2(0f, titleDrop);
            title.localScale = new Vector3(1.25f, 1.25f, 1f);
            _stageTween.Insert(0.05f, title.DOAnchorPos(_titleBase, 0.4f).SetEase(Ease.OutBack));
            _stageTween.Insert(0.05f, title.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
            _stageTween.Insert(0.45f, title.DOPunchScale(new Vector3(0.08f, -0.08f, 0f), 0.3f, 6, 0.6f));
        }

        // Pháo sao 2 bên khi ribbon chạm đất
        if (confetti != null && confettiAnchors != null)
        {
            for (int i = 0; i < confettiAnchors.Length; i++)
            {
                var anchor = confettiAnchors[i];
                if (anchor == null) continue;
                _stageTween.InsertCallback(0.35f + 0.18f * i, () => confetti.Play(anchor.position));
            }
        }
    }

    private IEnumerator Reveal()
    {
        for (int i = 0; i < _rewards.Count; i++)
        {
            _items[i].transform.localScale = Vector3.zero;
            _items[i].transform.localRotation = Quaternion.identity;
        }

        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        if (itemsDelay > 0f) yield return new WaitForSecondsRealtime(itemsDelay);

        for (int i = 0; i < _rewards.Count; i++)
        {
            RevealItem(i);
            if (i < _rewards.Count - 1 && revealDelay > 0f) yield return new WaitForSecondsRealtime(revealDelay);
        }

        yield return new WaitForSecondsRealtime(revealDuration);

        _revealRoutine = null;
        SetClaimable(true);
    }

    // 1 phần thưởng bật ra: nảy quá đà, lắc, nổ sao, số đếm tăng dần
    private void RevealItem(int index)
    {
        var item = _items[index];
        Transform t = item.transform;

        DOTween.Sequence()
            .Append(t.DOScale(1.2f, revealDuration * 0.55f).SetEase(Ease.OutBack))
            .Append(t.DOScale(1f, revealDuration * 0.45f).SetEase(Ease.OutQuad))
            .Insert(0f, t.DOPunchRotation(new Vector3(0f, 0f, wobbleAngle), revealDuration * 1.5f, 6, 0.5f))
            .SetUpdate(true)
            .SetLink(t.gameObject);

        AudioManager.Instance.PlaySfx(soundItem);
        if (itemBurst != null) itemBurst.Play(t.position);

        GameReward reward = _rewards[index];
        if (reward.amount <= 1 || countDuration <= 0f || reward.type == RewardType.UnlimitedHeart) return;

        item.SetAmount(0);
        DOVirtual.Float(0f, reward.amount, countDuration, v => item.SetAmount(Mathf.RoundToInt(v)))
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetLink(t.gameObject)
            .OnComplete(() =>
            {
                if (item.txtAmount != null)
                    item.txtAmount.transform.DOPunchScale(Vector3.one * 0.25f, 0.25f, 6, 0.6f).SetUpdate(true);
            });
    }

    private void SetClaimable(bool value)
    {
        _claimable = value;
        btnTap.interactable = value;

        if (txtTapToContinue == null) return;
        txtTapToContinue.SetActive(value);

        _tapPulse?.Kill();
        _tapPulse = null;
        txtTapToContinue.transform.localScale = Vector3.one;

        if (value)
            _tapPulse = txtTapToContinue.transform.DOScale(1.08f, 0.6f)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(txtTapToContinue);
    }

    private void OnTap()
    {
        if (!_claimable) return;
        _claimable = false;
        AudioManager.Instance.PlaySfx("Click");

        LaunchCoinFly();
        StopAll();
        Close(BoxAnimationFactory.Scale);
    }

    private void LaunchCoinFly()
    {
        int index = _rewards.FindIndex(r => r.type == RewardType.Coin);
        if (index < 0) return;

        var target = FindFirstObjectByType<CoinView>();
        var item = _items[index];
        Transform layer = transform.parent;

        if (target == null || !target.isActiveAndEnabled || layer == null)
        {
            RewardService.PostCoinChanged();
            return;
        }

        Transform targetTf = target.transform;
        Vector3 baseScale = targetTf.localScale;
        Vector3 from = item.icon != null ? item.icon.transform.position : item.transform.position;

        UICoinFly.Launch(layer, item.icon != null ? item.icon.sprite : null, from, targetTf,
            Mathf.Clamp(_rewards[index].amount, 1, flyCount),
            onFirstArrive: RewardService.PostCoinChanged,
            onEachArrive: () =>
            {
                AudioManager.Instance.PlaySfx(soundFlyArrive);
                targetTf.DOKill(true);
                targetTf.localScale = baseScale;
                targetTf.DOPunchScale(baseScale * 0.2f, 0.15f, 1, 0f).SetUpdate(true).SetLink(targetTf.gameObject);
            },
            onComplete: null);
    }

    private void StopAll()
    {
        _playing = false;
        if (_revealRoutine != null)
        {
            StopCoroutine(_revealRoutine);
            _revealRoutine = null;
        }

        _stageTween?.Kill();
        _stageTween = null;
        _glowPulse?.Kill();
        _glowPulse = null;
        _tapPulse?.Kill();
        _tapPulse = null;

        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].transform.DOKill();
            _items[i].transform.localScale = Vector3.one;
            _items[i].transform.localRotation = Quaternion.identity;
        }
    }
}
