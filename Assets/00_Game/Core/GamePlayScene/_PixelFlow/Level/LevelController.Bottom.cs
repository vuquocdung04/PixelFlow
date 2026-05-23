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
    [System.NonSerialized] public Dictionary<string, List<Shooter>> shootersByColor = new();
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
                RegisterShooter(shooter);

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
        shootersByColor.Clear();
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

    public List<GameObject> GetAvailableShooters()
    {
        var result = new List<GameObject>();
        foreach (var kv in shooterMap)
        {
            var s = kv.Value;
            if (s == null) continue;
            if (s.HasProp(PropState.Ice)) continue;
            if (s.IsInGroup) continue;
            result.Add(s.gameObject);
        }
        return result;
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
        RegisterShooter(newShooter);

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

    public void DestroyAllShootersOfColor(string hex)
    {
        ClearTunnelColors(hex);

        if (!shootersByColor.TryGetValue(hex, out var list) || list.Count == 0) return;

        var targets = new List<Shooter>(list);
        var affectedCols = new HashSet<int>();

        foreach (var s in targets)
            if (s != null && s.gridIdx >= 0)
                affectedCols.Add(s.gridIdx % gridBottomX);

        RebuildAffectedLinkGroups(targets);

        foreach (var s in targets)
        {
            if (s == null) continue;

            if (s.gridIdx >= 0)
                DestroyShooterInGridRaw(s);
            else if (s.GetComponentInParent<WaitArea>() != null)
                DestroyShooterInWaitArea(s);
            else
                DestroyShooterOnConveyor(s);
        }

        foreach (var col in affectedCols) FillColumnFromTunnel(col);
    }
    private void FillColumnFromTunnel(int col)
    {
        if (!tunnelByColumn.TryGetValue(col, out var tunnel))
        {
            CompactColumn(col);
            return;
        }

        int safety = gridBottomY + 1;
        while (safety-- > 0)
        {
            CompactColumn(col);

            if (!tunnel.HasNext) break;
            if (shooterMap.ContainsKey(tunnel.spawnAtID)) break;

            if (!IsColumnHasEmptyAbove(col, tunnel.spawnAtID)) break;

            Shooter newShooter = tunnel.SpawnNext();
            if (newShooter == null) break;

            shooterMap[tunnel.spawnAtID] = newShooter;
            newShooter.gridIdx = tunnel.spawnAtID;
            RegisterShooter(newShooter);

            int spawnRow = tunnel.spawnAtID / gridBottomX;
            newShooter.SetAnimState(spawnRow == 0 ? ShooterAnimState.Idle : ShooterAnimState.Blocked);
        }

        CompactColumn(col);
    }

    private bool IsColumnHasEmptyAbove(int col, int spawnAtID)
    {
        int spawnRow = spawnAtID / gridBottomX;
        for (int r = 0; r <= spawnRow; r++)
        {
            int idx = r * gridBottomX + col;
            if (!shooterMap.ContainsKey(idx)) return true;
        }
        return false;
    }
    private void ClearTunnelColors(string hex)
    {
        foreach (var kv in tunnelMap)
        {
            var tunnel = kv.Value;
            int removed = tunnel.RemoveColors(hex);
            ShooterController.Instance.aliveCount -= removed;
        }
    }

    private void DestroyShooterInGridRaw(Shooter shooter)
    {
        if (shooter == null || shooter.gridIdx < 0) return;
        shooterMap.Remove(shooter.gridIdx);
        shooter.gridIdx = -1;
        UnregisterShooter(shooter);
        ShooterController.Instance.UnregisterCombat(shooter);
        ShooterController.Instance.OnShooterDespawn();
        Destroy(shooter.gameObject);
    }

    private void DestroyShooterInWaitArea(Shooter shooter)
    {
        if (shooter == null) return;
        var wa = shooter.GetComponentInParent<WaitArea>();
        if (wa != null) wa.ResetToDefault();
        UnregisterShooter(shooter);
        ShooterController.Instance.UnregisterCombat(shooter);
        ShooterController.Instance.OnShooterDespawn();
        Destroy(shooter.gameObject);
    }

    private void DestroyShooterOnConveyor(Shooter shooter)
    {
        if (shooter == null) return;
        var slot = shooter.GetComponentInParent<ItemSlot>();
        if (slot != null) slot.AbortAndReturn();
        UnregisterShooter(shooter);
        ShooterController.Instance.UnregisterCombat(shooter);
        ShooterController.Instance.OnShooterDespawn();
        Destroy(shooter.gameObject);
    }

    private void CompactColumn(int col)
    {
        int writeRow = 0;
        for (int readRow = 0; readRow < gridBottomY; readRow++)
        {
            int readIdx = readRow * gridBottomX + col;
            if (!shooterMap.TryGetValue(readIdx, out var shooter)) continue;

            if (readRow == writeRow)
            {
                writeRow++;
                continue;
            }

            int writeIdx = writeRow * gridBottomX + col;
            Vector3 newPos = GridToLocalBottom(writeIdx);

            shooter.transform.DOKill();
            var tween = shooter.transform.DOLocalMove(newPos, 0.3f).SetEase(Ease.OutCubic);
            if (shooter.HasLink) tween.OnUpdate(() => shooter.RefreshAllLinks());

            shooterMap.Remove(readIdx);
            shooterMap[writeIdx] = shooter;
            shooter.gridIdx = writeIdx;

            if (writeRow == 0)
            {
                shooter.SetAnimState(ShooterAnimState.Idle);
                shooter.RemoveProps(PropState.Blind);
            }

            writeRow++;
        }
    }

    private void RebuildAffectedLinkGroups(List<Shooter> destroyed)
    {
        var destroyedSet = new HashSet<Shooter>(destroyed);
        var affectedGroups = new HashSet<LinkGroup>();
        foreach (var s in destroyed)
            if (s.linkGroup != null) affectedGroups.Add(s.linkGroup);

        foreach (var group in affectedGroups)
        {
            var survivors = new List<Shooter>();
            foreach (var m in group.members)
                if (m != null && !destroyedSet.Contains(m))
                    survivors.Add(m);

            foreach (var s in survivors)
                s.RemoveProps(PropState.Link);

            if (survivors.Count <= 1)
            {
                group.members.Clear();
                continue;
            }

            var newGroup = new LinkGroup();
            foreach (var s in survivors)
            {
                newGroup.members.Add(s);
                s.linkGroup = newGroup;
            }

            for (int i = 0; i < survivors.Count - 1; i++)
            {
                survivors[i].SetupLink(survivors[i + 1], owner: true);
                survivors[i + 1].SetupLink(survivors[i], owner: false);
            }
        }
    }
    public void RegisterShooter(Shooter s)
    {
        if (s == null) return;
        if (!shootersByColor.TryGetValue(s.colorHex, out var list))
            shootersByColor[s.colorHex] = list = new List<Shooter>();
        list.Add(s);
    }

    public void UnregisterShooter(Shooter s)
    {
        if (s == null) return;
        if (shootersByColor.TryGetValue(s.colorHex, out var list))
            list.Remove(s);
    }

    public void DoBooster1Swap()
    {
        var outerColors = BrickGrid.Instance.GetOuterRingColors();
        if (outerColors.Count == 0) return;

        // Tìm shooter row 0 cần swap (màu không match outer + không Ice/Link)
        var needSwap = new List<Shooter>();
        for (int col = 0; col < gridBottomX; col++)
        {
            if (!shooterMap.TryGetValue(col, out var s) || s == null) continue;
            if (outerColors.Contains(s.colorHex)) continue;
            if (s.HasProp(PropState.Ice) || s.HasProp(PropState.Link)) continue;
            needSwap.Add(s);
        }

        if (needSwap.Count == 0) return;
        var candidates = new List<Shooter>();
        foreach (var kv in shooterMap)
        {
            var s = kv.Value;
            if (s == null) continue;
            if (kv.Key / gridBottomX == 0) continue;
            if (!outerColors.Contains(s.colorHex)) continue;
            if (s.HasProp(PropState.Ice) || s.HasProp(PropState.Link)) continue;
            candidates.Add(s);
        }

        foreach (var row0Shooter in needSwap)
        {
            if (candidates.Count == 0) break;

            int pickIdx = UnityEngine.Random.Range(0, candidates.Count);
            var partner = candidates[pickIdx];
            candidates.RemoveAt(pickIdx);

            SwapShooters(row0Shooter, partner);
        }
    }
    private void SwapShooters(Shooter a, Shooter b)
    {
        int aIdx = a.gridIdx;
        int bIdx = b.gridIdx;

        shooterMap[aIdx] = b;
        shooterMap[bIdx] = a;
        a.gridIdx = bIdx;
        b.gridIdx = aIdx;

        a.transform.localPosition = GridToLocalBottom(bIdx);
        b.transform.localPosition = GridToLocalBottom(aIdx);

        a.SetAnimState(ShooterAnimState.Blocked);

        b.SetAnimState(ShooterAnimState.Idle);
        b.RemoveProps(PropState.Blind);
    }

    public bool HasShooterBelowRow0()
    {
        foreach (var kv in shooterMap)
        {
            int idx = kv.Key;
            if (idx / gridBottomX > 0) return true;
        }
        return false;
    }
}