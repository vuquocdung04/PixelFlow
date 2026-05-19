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

        int shootersSpawned = 0, blindsApplied = 0, icesApplied = 0, tunnelsSpawned = 0;

        // ===== 1) COLORS → Shooter =====
        foreach (var kv in bottomData.colors)
        {
            ColorUtility.TryParseHtmlString(kv.Key, out Color color);

            foreach (int idx in kv.Value)
            {
                var shooter = Instantiate(shooterPrefab, bottomParent);
                shooter.name = $"Shooter_{idx}_{kv.Key}";
                shooter.transform.localPosition = GridToLocalBottom(idx);
                shooter.SetColor(color);
                shooter.colorHex = kv.Key;

                int row = idx / gridBottomX;
                shooter.SetAnimState(row == 0 ? Shooter.AnimState.Idle : Shooter.AnimState.Blocked);

                shooterMap[idx] = shooter;
                shootersSpawned++;
            }
        }

        // ===== 2) BLINDS =====
        foreach (int idx in bottomData.blinds)
        {
            if (!shooterMap.TryGetValue(idx, out var shooter)) continue;
            shooter.AddProps(PropState.Blind);
            blindsApplied++;
        }

        // ===== 3) ICES =====
        foreach (var ice in bottomData.ices)
        {
            if (!shooterMap.TryGetValue(ice.id, out var shooter)) continue;
            shooter.AddProps(PropState.Ice);
            shooter.SetIceCount(ice.count);
            icesApplied++;
        }

        // ===== 4) TUNNELS =====
        foreach (var t in bottomData.tunnels)
        {
            var colors = new List<Color>();
            foreach (var hex in t.colors)
            {
                ColorUtility.TryParseHtmlString(hex, out Color c);
                colors.Add(c);
            }

            var tunnel = Instantiate(tunnelPrefab, bottomParent);
            tunnel.transform.localPosition = GridToLocalBottom(t.tunnelID);
            tunnel.Setup(t.tunnelID, t.spawnAtID, GridToLocalBottom(t.spawnAtID), colors);

            tunnelMap[t.tunnelID] = tunnel;
            tunnelsSpawned++;
        }
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
            shooter.transform.DOLocalMove(newPos, 0.3f).SetEase(Ease.OutCubic);

            shooterMap.Remove(oldIdx);
            shooterMap[newIdx] = shooter;

            if (newIdx / gridBottomX == 0)
                shooter.SetAnimState(Shooter.AnimState.Idle);
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

    public Shooter GetShooter(int idx) => shooterMap.TryGetValue(idx, out var s) ? s : null;
    public Tunnel GetTunnel(int idx) => tunnelMap.TryGetValue(idx, out var t) ? t : null;
}