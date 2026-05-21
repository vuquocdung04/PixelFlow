using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class Tunnel : MonoBehaviour
{
    [Header("Data")]
    public int tunnelID;
    public int spawnAtID;
    public List<TunnelColor> spawnColors = new List<TunnelColor>();

    [Tooltip("Local position (so với parent) của ô spawnAtID, set bởi LevelController.")]
    public Vector3 spawnAtLocalPos;

    [Header("Spawn")]
    public Transform pointSpawn;
    public Shooter shooterPrefab;
    public float moveDuration = 0.4f;
    public Ease moveEase = Ease.OutQuad;

    [Header("UI")]
    public TMP_Text countTxt;

    public bool HasNext => spawnColors.Count > 0;
    public int RemainingCount => spawnColors.Count;

    public void Setup(int tunnelID, int spawnAtID, Vector3 spawnAtLocalPos, List<TunnelColor> colors)
    {
        this.tunnelID = tunnelID;
        this.spawnAtID = spawnAtID;
        this.spawnAtLocalPos = spawnAtLocalPos;
        this.spawnColors = colors != null ? new List<TunnelColor>(colors) : new List<TunnelColor>();

        gameObject.name = $"Tunnel_{tunnelID}_to_{spawnAtID}";
        UpdateCountText();
    }

    public Shooter SpawnNext()
    {
        if (spawnColors.Count == 0) return null;
        if (shooterPrefab == null) { Debug.LogWarning($"[{name}] thiếu shooterPrefab"); return null; }
        if (pointSpawn == null) { Debug.LogWarning($"[{name}] thiếu pointSpawn"); return null; }

        TunnelColor data = spawnColors[0];
        spawnColors.RemoveAt(0);

        ColorUtility.TryParseHtmlString(data.hex, out Color color);

        var parent = transform.parent;
        var shooter = Instantiate(shooterPrefab, pointSpawn.position, Quaternion.identity, parent);
        shooter.CachePropHandlers();
        shooter.SetColor(color);
        shooter.colorHex = data.hex;
        shooter.SetProjectileCount(data.count);

        shooter.transform
            .DOLocalMove(spawnAtLocalPos, moveDuration)
            .SetEase(moveEase)
            .SetLink(shooter.gameObject);

        UpdateCountText();
        return shooter;
    }

    private void UpdateCountText()
    {
        if (countTxt != null) countTxt.text = spawnColors.Count.ToString();
    }
}