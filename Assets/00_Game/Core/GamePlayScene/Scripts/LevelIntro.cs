using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// Intro đầu màn: lưới pixel mọc theo đợt sóng từ trên xuống, sau đó shooter/tunnel pop lên theo hàng.
// Chạy bằng 1 vòng lặp duy nhất thay vì mỗi block 1 tween (level có thể tới cả nghìn block).
[System.Serializable]
public class LevelIntro
{
    [Header("Timing")]
    [Tooltip("Chờ sau khi scene sẵn sàng để màn che mở bớt rồi mới chạy")]
    public float startDelay = 0.45f;

    [Header("Blocks")]
    public float blockPopDuration = 0.25f;
    [Tooltip("Thời gian đợt sóng quét hết lưới từ trên xuống")]
    public float blockWaveDuration = 0.6f;
    [Range(0f, 1f), Tooltip("Độ lệch theo cột (tạo sóng chéo)")]
    public float blockDiagonal = 0.35f;

    [Header("Shooters")]
    [Tooltip("Thời điểm shooter bắt đầu (tính từ lúc intro bắt đầu)")]
    public float shooterStart = 0.4f;
    public float shooterPopDuration = 0.3f;
    public float shooterRowStep = 0.08f;
    public float shooterColStep = 0.03f;

    [Header("HUD")]
    public float levelTextPunch = 0.25f;

    private struct Entry
    {
        public Transform target;
        public Vector3 scale;
        public float delay;
        public float duration;
        public Shooter linked;
    }

    private readonly List<Entry> _entries = new();

    // Gọi ngay sau khi sinh level (trước khi mở màn che) để mọi thứ ẩn sẵn
    public void Prepare(LevelController level)
    {
        _entries.Clear();

        int maxRow = 0;
        int maxCol = 0;
        foreach (var block in level.blockMap.Values)
        {
            maxRow = Mathf.Max(maxRow, block.gridRow);
            maxCol = Mathf.Max(maxCol, block.gridCol);
        }

        foreach (var block in level.blockMap.Values)
        {
            float rowT = maxRow > 0 ? (float)block.gridRow / maxRow : 0f;
            float colT = maxCol > 0 ? (float)block.gridCol / maxCol : 0f;
            float t = Mathf.Lerp(rowT, (rowT + colT) * 0.5f, blockDiagonal);
            Add(block.transform, t * blockWaveDuration, blockPopDuration, null);
        }

        int gridX = Mathf.Max(1, level.gridBottomX);
        foreach (var shooter in level.shooterMap.Values)
            Add(shooter.transform, ShooterDelay(shooter.gridIdx, gridX), shooterPopDuration,
                shooter.HasLink ? shooter : null);

        foreach (var kv in level.tunnelMap)
            Add(kv.Value.transform, ShooterDelay(kv.Key, gridX), shooterPopDuration, null);

        foreach (var e in _entries)
            e.target.localScale = Vector3.zero;
    }

    public async UniTask Play(TMPro.TMP_Text levelText = null)
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(startDelay));

        if (levelText != null)
            levelText.transform.DOPunchScale(Vector3.one * levelTextPunch, 0.4f, 6, 0.6f)
                .SetLink(levelText.gameObject);

        float end = 0f;
        foreach (var e in _entries) end = Mathf.Max(end, e.delay + e.duration);

        float time = 0f;
        while (time < end)
        {
            time += Time.deltaTime;
            Apply(time);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        Apply(float.MaxValue);
        _entries.Clear();
    }

    private float ShooterDelay(int gridIdx, int gridX)
    {
        int row = gridIdx / gridX;
        int col = gridIdx % gridX;
        return shooterStart + row * shooterRowStep + col * shooterColStep;
    }

    private void Add(Transform target, float delay, float duration, Shooter linked)
    {
        if (target == null) return;
        _entries.Add(new Entry
        {
            target = target,
            scale = target.localScale,
            delay = delay,
            duration = Mathf.Max(0.01f, duration),
            linked = linked,
        });
    }

    private void Apply(float time)
    {
        foreach (var e in _entries)
        {
            if (e.target == null) continue;

            float t = Mathf.Clamp01((time - e.delay) / e.duration);
            e.target.localScale = e.scale * DOVirtual.EasedValue(0f, 1f, t, Ease.OutBack);

            // Dây nối là con của shooter nên phải tính lại theo scale mới
            if (e.linked != null) e.linked.RefreshAllLinks();
        }
    }
}
