using System.Collections.Generic;
using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

public partial class LevelController
{
    [Space(10), Header("Bottom")]
    public Shooter shooterPrefab;
    public Tunnel tunnelPrefab;
    public Transform bottomParent;

    public float spacingBottomX;
    public float spacingBottomY;
    public float startZ;
    private int gridBottomX, gridBottomY;

    [System.NonSerialized] public Dictionary<int, Shooter> shooterMap = new Dictionary<int, Shooter>();
    [System.NonSerialized] public Dictionary<int, Tunnel> tunnelMap = new Dictionary<int, Tunnel>();
    [System.NonSerialized] public BottomData bottomData;

    [Button("GENERATE BOTTOM", ButtonSizes.Large), GUIColor(0.5f, 0.8f, 1f)]
    public void GenerateBottom()
    {
        ClearBottom();

        var data = JsonConvert.DeserializeObject<LevelJsonData>(levelTest.text);
        bottomData = data.bottom;
        gridBottomX = bottomData.gridX;
        gridBottomY = bottomData.gridY;

        // ===== 1) COLORS → Shooter =====
        foreach (var kv in bottomData.colors)
        {
            ColorUtility.TryParseHtmlString(kv.Key, out Color color);

            foreach (var cellEntry in kv.Value)
            {
                int idx = cellEntry.Key;
                int projectileCount = cellEntry.Value;

                var shooter = Instantiate(shooterPrefab, bottomParent);
                shooter.name = $"Shooter_{idx}_{kv.Key}";
                shooter.transform.localPosition = GridToLocalBottom(idx);
                shooter.SetColor(color);
                shooter.colorHex = kv.Key;
                shooter.SetProjectileCount(projectileCount);

                int row = idx / gridBottomX;
                shooter.SetAnimState(row == 0 ? ShooterAnimState.Idle : ShooterAnimState.Blocked);
                shooterMap[idx] = shooter;
            }
        }

        // ===== 2) BLINDS =====
        foreach (int idx in bottomData.blinds)
        {
            if (!shooterMap.TryGetValue(idx, out var shooter)) continue;
            shooter.AddProps(PropState.Blind);
        }

        // ===== 3) ICES =====
        foreach (var ice in bottomData.ices)
        {
            if (!shooterMap.TryGetValue(ice.Key, out var shooter)) continue;
            shooter.AddProps(PropState.Ice);
            shooter.SetIceCount(ice.Value);
        }

        // ===== 4) TUNNELS =====
        // ===== 4) TUNNELS =====
        foreach (var kv in bottomData.tunnels)
        {
            int tunnelID = kv.Key;
            var t = kv.Value;

            var tunnel = Instantiate(tunnelPrefab, bottomParent);
            tunnel.transform.localPosition = GridToLocalBottom(tunnelID);
            tunnel.Setup(tunnelID, t.spawnAtID, GridToLocalBottom(t.spawnAtID), t.colors);

            tunnelMap[tunnelID] = tunnel;
        }
        // ===== 5) LINKS =====
        if (bottomData.links != null)
        {
            foreach (var group in bottomData.links)
            {
                var linkGroup = new LinkGroup();

                // Tập hợp members + assign reference
                foreach (int idx in group)
                {
                    if (!shooterMap.TryGetValue(idx, out var s)) continue;
                    linkGroup.members.Add(s);
                    s.linkGroup = linkGroup;
                }

                // Setup rope links
                for (int i = 0; i < group.Count - 1; i++)
                {
                    if (!shooterMap.TryGetValue(group[i], out var a)) continue;
                    if (!shooterMap.TryGetValue(group[i + 1], out var b)) continue;

                    a.SetupLink(b, owner: true);
                    b.SetupLink(a, owner: false);
                }
            }
        }

        int totalAlive = 0;
        foreach (var kv in bottomData.colors)
            totalAlive += kv.Value.Count;
        foreach (var kv in bottomData.tunnels)
            foreach (var c in kv.Value.colors)
                totalAlive += c.count;

        ShooterController.Instance.aliveCount = totalAlive;
        ShooterController.Instance.conveyorCapacity = Conveyor.Instance.Capacity;
        ShooterController.Instance.TryPostLoopEvent();
    }

    [Button("CLEAR BOTTOM", ButtonSizes.Medium), GUIColor(1f, 0.7f, 0.7f)]
    public void ClearBottom()
    {
        shooterMap.Clear();
        tunnelMap.Clear();
        bottomData = null;

        for (int i = bottomParent.childCount - 1; i >= 0; i--)
        {
            var go = bottomParent.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
    public void OnShooterTaken(int idx)
    {
        shooterMap.Remove(idx);

        int col = idx % gridBottomX;
        int row = idx / gridBottomX;

        for (int r = row + 1; r < gridBottomY; r++)
        {
            int oldIdx = r * gridBottomX + col;
            int newIdx = (r - 1) * gridBottomX + col;

            if (!shooterMap.TryGetValue(oldIdx, out var shooter)) continue;

            Vector3 newPos = GridToLocalBottom(newIdx);
            var tween = shooter.transform.DOLocalMove(newPos, 0.3f).SetEase(Ease.OutCubic);


            if (shooter.HasLink)
                tween.OnUpdate(() => shooter.RefreshAllLinks());

            shooterMap.Remove(oldIdx);
            shooterMap[newIdx] = shooter;

            if (newIdx / gridBottomX == 0)
            {
                shooter.SetAnimState(ShooterAnimState.Idle);
                shooter.RemoveProps(PropState.Blind);
            }
        }
    }
    private Vector3 GridToLocalBottom(int idx)
    {
        int row = idx / gridBottomX;
        int col = idx % gridBottomX;

        float offsetX = -(gridBottomX - 1) * spacingBottomX * 0.5f;
        float lx = offsetX + col * spacingBottomX;
        float lz = startZ - row * spacingBottomY;

        return new Vector3(lx, 0f, lz);
    }
}