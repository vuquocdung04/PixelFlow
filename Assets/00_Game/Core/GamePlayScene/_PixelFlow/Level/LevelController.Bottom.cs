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
    public int gridBottomX, gridBottomY;

    [System.NonSerialized] public Dictionary<int, Shooter> shooterMap = new Dictionary<int, Shooter>();
    [System.NonSerialized] public Dictionary<int, Tunnel> tunnelMap = new Dictionary<int, Tunnel>();
    [System.NonSerialized] public Dictionary<int, Tunnel> tunnelByColumn = new Dictionary<int, Tunnel>();
    [System.NonSerialized] public BottomData bottomData;

    [Button("GENERATE BOTTOM", ButtonSizes.Large), GUIColor(0.5f, 0.8f, 1f)]
    public void GenerateBottom()
    {
        ClearBottom();

        var data = JsonConvert.DeserializeObject<LevelJsonData>(levelTest.text);
        bottomData = data.bottom;
        gridBottomX = bottomData.gridX;
        gridBottomY = bottomData.gridY;

        SpawnShooters();
        ApplyBlinds();
        ApplyIces();
        SpawnTunnels();
        SetupLinks();
        InitShooterController();
    }

    private void SpawnShooters()
    {
        foreach (var kv in bottomData.colors)
        {
            ColorUtility.TryParseHtmlString(kv.Key, out Color color);

            foreach (var cell in kv.Value)
            {
                int idx = cell.Key;
                var shooter = Instantiate(shooterPrefab, bottomParent);
                shooter.name = $"Shooter_{idx}_{kv.Key}";
                shooter.transform.localPosition = GridToLocalBottom(idx);
                shooter.CachePropHandlers();
                shooter.SetColor(color);
                shooter.colorHex = kv.Key;
                shooter.SetProjectileCount(cell.Value);

                shooter.gridIdx = idx;

                int row = idx / gridBottomX;
                shooter.SetAnimState(row == 0 ? ShooterAnimState.Idle : ShooterAnimState.Blocked);
                shooterMap[idx] = shooter;
            }
        }
    }

    private void ApplyBlinds()
    {
        foreach (int idx in bottomData.blinds)
        {
            if (shooterMap.TryGetValue(idx, out var shooter))
                shooter.AddProps(PropState.Blind);
        }
    }

    private void ApplyIces()
    {
        foreach (var ice in bottomData.ices)
        {
            if (!shooterMap.TryGetValue(ice.Key, out var shooter)) continue;
            shooter.AddProps(PropState.Ice);
            shooter.SetIceCount(ice.Value);
        }
    }

    private void SpawnTunnels()
    {
        foreach (var kv in bottomData.tunnels)
        {
            int tunnelID = kv.Key;
            var t = kv.Value;

            var tunnel = Instantiate(tunnelPrefab, bottomParent);
            tunnel.transform.localPosition = GridToLocalBottom(tunnelID);
            tunnel.Setup(tunnelID, t.spawnAtID, GridToLocalBottom(t.spawnAtID), t.colors);

            tunnelMap[tunnelID] = tunnel;
            tunnelByColumn[tunnelID % gridBottomX] = tunnel;
        }
    }

    private void SetupLinks()
    {
        if (bottomData.links == null) return;

        foreach (var group in bottomData.links)
        {
            var linkGroup = new LinkGroup();

            foreach (int idx in group)
            {
                if (!shooterMap.TryGetValue(idx, out var s)) continue;
                linkGroup.members.Add(s);
                s.linkGroup = linkGroup;
            }

            for (int i = 0; i < group.Count - 1; i++)
            {
                if (!shooterMap.TryGetValue(group[i], out var a)) continue;
                if (!shooterMap.TryGetValue(group[i + 1], out var b)) continue;
                a.SetupLink(b, owner: true);
                b.SetupLink(a, owner: false);
            }
        }
    }

    private void InitShooterController()
    {
        int totalAlive = 0;
        foreach (var kv in bottomData.colors)
            totalAlive += kv.Value.Count;
        foreach (var kv in bottomData.tunnels)
            totalAlive += kv.Value.colors.Count;

        ShooterController.Instance.aliveCount = totalAlive;
        ShooterController.Instance.conveyorCapacity = Conveyor.Instance.Capacity;
        ShooterController.Instance.TryPostLoopEvent();
    }

    [Button("CLEAR BOTTOM", ButtonSizes.Medium), GUIColor(1f, 0.7f, 0.7f)]
    public void ClearBottom()
    {
        shooterMap.Clear();
        tunnelMap.Clear();
        tunnelByColumn.Clear();
        bottomData = null;

        for (int i = bottomParent.childCount - 1; i >= 0; i--)
        {
            var go = bottomParent.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }

    public void OnShooterTaken(Shooter shooter)
    {
        if (shooter == null || shooter.gridIdx < 0) return;

        int idx = shooter.gridIdx;
        if (!shooterMap.TryGetValue(idx, out var s) || s != shooter) return;

        shooterMap.Remove(idx);
        shooter.gridIdx = -1;

        int col = idx % gridBottomX;
        int row = idx / gridBottomX;

        for (int r = row + 1; r < gridBottomY; r++)
        {
            int oldIdx = r * gridBottomX + col;
            int newIdx = (r - 1) * gridBottomX + col;

            if (!shooterMap.TryGetValue(oldIdx, out var movedShooter)) continue;

            Vector3 newPos = GridToLocalBottom(newIdx);
            var tween = movedShooter.transform.DOLocalMove(newPos, 0.3f).SetEase(Ease.OutCubic);

            if (movedShooter.HasLink)
                tween.OnUpdate(() => movedShooter.RefreshAllLinks());

            shooterMap.Remove(oldIdx);
            shooterMap[newIdx] = movedShooter;
            movedShooter.gridIdx = newIdx;

            if (newIdx / gridBottomX == 0)
            {
                movedShooter.SetAnimState(ShooterAnimState.Idle);
                movedShooter.RemoveProps(PropState.Blind);
            }
        }

        TrySpawnFromTunnel(col);
    }

    private void TrySpawnFromTunnel(int col)
    {
        if (!tunnelByColumn.TryGetValue(col, out var tunnel)) return;
        if (!tunnel.HasNext) return;
        if (shooterMap.ContainsKey(tunnel.spawnAtID)) return;

        Shooter newShooter = tunnel.SpawnNext();
        if (newShooter == null) return;

        shooterMap[tunnel.spawnAtID] = newShooter;
        newShooter.gridIdx = tunnel.spawnAtID;

        int spawnRow = tunnel.spawnAtID / gridBottomX;
        newShooter.SetAnimState(spawnRow == 0 ? ShooterAnimState.Idle : ShooterAnimState.Blocked);
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
