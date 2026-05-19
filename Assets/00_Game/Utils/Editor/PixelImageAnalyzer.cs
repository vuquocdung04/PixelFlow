// Assets/Editor/PixelImageAnalyzer.cs
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class PixelImageAnalyzer : OdinEditorWindow
{
    [MenuItem("Tools/Pixel Image Analyzer")]
    private static void Open() => GetWindow<PixelImageAnalyzer>("Pixel Image Analyzer").Show();

    private const string PREF_OUTPUT_FOLDER = "PixelImageAnalyzer.OutputFolder";
    private const float BG_ALPHA_THRESHOLD = 0.5f;

    // ===================== Row 1: texture | (Auto / [LogTable | LogSimple] / [Replace, Skip, Color]) =====================
    [HorizontalGroup("Top", Width = 110)]
    [PreviewField(100, Alignment = ObjectFieldAlignment.Left), HideLabel]
    [OnValueChanged("OnTextureChanged")]
    public Texture2D sourceTexture;

    [VerticalGroup("Top/Right")]
    [Button("Auto Detect", ButtonSizes.Medium), GUIColor(1f, 0.85f, 0.4f)]
    private void AutoDetect()
    {
        if (sourceTexture == null) { Warn("Cần texture."); return; }
        if (!EnsureReadable(sourceTexture)) return;
        var d = DetectLogicalSize(sourceTexture);
        gridX = d.x; gridY = d.y;
        SampleTexture();
        Debug.Log($"<color=cyan>[Auto Detect]</color> {sourceTexture.name}: " +
                  $"texture {sourceTexture.width}×{sourceTexture.height} → logical <b>{d.x}×{d.y}</b>");
        Repaint();
    }

    [HorizontalGroup("Top/Right/Logs")]
    [Button("LOG TABLE", ButtonSizes.Medium), GUIColor(0.9f, 0.7f, 1f)]
    public void LogTable()
    {
        if (sourceTexture == null) { Warn("Cần texture."); return; }
        if (!EnsureReadable(sourceTexture)) return;
        SampleTexture();

        int W = sampledTex.width, H = sampledTex.height;
        var dict = new Dictionary<string, ColorRow>();

        for (int row = 0; row < H; row++)
        {
            int texY = H - 1 - row;
            for (int x = 0; x < W; x++)
            {
                int idx = row * W + x;
                Color c = sampledTex.GetPixel(x, texY);

                if (bgSkip && c.a < BG_ALPHA_THRESHOLD) continue;

                string hex = ColorToHexRGB(c);
                if (!dict.TryGetValue(hex, out var r))
                {
                    r = new ColorRow { color = c, hex = hex, indices = new List<int>() };
                    dict[hex] = r;
                }
                r.indices.Add(idx);
            }
        }

        colorRows = new List<ColorRow>(dict.Values);
        colorRows.Sort((a, b) => b.indices.Count.CompareTo(a.indices.Count));
        tableMode = "full";

        Debug.Log($"<color=cyan>[Log Table]</color> grid {W}×{H}  |  <b>{colorRows.Count}</b> màu unique");
        Repaint();
    }

    [HorizontalGroup("Top/Right/Logs")]
    [Button("LOG SIMPLE", ButtonSizes.Medium), GUIColor(1f, 0.65f, 0.85f)]
    public void LogSimple()
    {
        if (sourceTexture == null) { Warn("Cần texture."); return; }
        if (!EnsureReadable(sourceTexture)) return;
        SampleTexture();

        int W = sampledTex.width, H = sampledTex.height;
        int n = W * H;
        var pixels = sampledTex.GetPixels();

        var assignment = new int[n];
        var sums = new List<Vector4>();
        var counts = new List<int>();
        var centroids = new List<Color>();

        float tolSq = similarity * similarity;

        for (int row = 0; row < H; row++)
        {
            int texY = H - 1 - row;
            for (int x = 0; x < W; x++)
            {
                int readingIdx = row * W + x;
                Color p = pixels[texY * W + x];

                if (bgSkip && p.a < BG_ALPHA_THRESHOLD) { assignment[readingIdx] = -1; continue; }

                int best = -1;
                float bestDsq = float.MaxValue;
                for (int c = 0; c < centroids.Count; c++)
                {
                    float dsq = ColorDistSq(p, centroids[c]);
                    if (dsq < bestDsq) { bestDsq = dsq; best = c; }
                }

                if (best == -1 || bestDsq > tolSq)
                {
                    sums.Add(new Vector4(p.r, p.g, p.b, p.a));
                    counts.Add(1);
                    centroids.Add(p);
                    assignment[readingIdx] = sums.Count - 1;
                }
                else
                {
                    sums[best] += new Vector4(p.r, p.g, p.b, p.a);
                    counts[best]++;
                    centroids[best] = AvgFromSum(sums[best], counts[best]);
                    assignment[readingIdx] = best;
                }
            }
        }

        for (int c = 0; c < sums.Count; c++) { sums[c] = Vector4.zero; counts[c] = 0; }
        for (int row = 0; row < H; row++)
        {
            int texY = H - 1 - row;
            for (int x = 0; x < W; x++)
            {
                int readingIdx = row * W + x;
                if (assignment[readingIdx] == -1) continue;

                Color p = pixels[texY * W + x];
                int best = 0;
                float bestDsq = float.MaxValue;
                for (int c = 0; c < centroids.Count; c++)
                {
                    float dsq = ColorDistSq(p, centroids[c]);
                    if (dsq < bestDsq) { bestDsq = dsq; best = c; }
                }
                assignment[readingIdx] = best;
                sums[best] += new Vector4(p.r, p.g, p.b, p.a);
                counts[best]++;
            }
        }
        for (int c = 0; c < centroids.Count; c++)
            if (counts[c] > 0) centroids[c] = AvgFromSum(sums[c], counts[c]);

        var clusterIndices = new List<int>[centroids.Count];
        for (int i = 0; i < clusterIndices.Length; i++) clusterIndices[i] = new List<int>();
        for (int i = 0; i < n; i++)
        {
            if (assignment[i] == -1) continue;
            clusterIndices[assignment[i]].Add(i);
        }

        for (int row = 0; row < H; row++)
        {
            int texY = H - 1 - row;
            for (int x = 0; x < W; x++)
            {
                int readingIdx = row * W + x;
                int arrIdx = texY * W + x;
                if (assignment[readingIdx] == -1)
                    pixels[arrIdx] = new Color(0, 0, 0, 0);
                else
                    pixels[arrIdx] = centroids[assignment[readingIdx]];
            }
        }
        sampledTex.SetPixels(pixels);
        sampledTex.Apply();

        colorRows = new List<ColorRow>();
        for (int c = 0; c < centroids.Count; c++)
        {
            if (clusterIndices[c].Count == 0) continue;
            colorRows.Add(new ColorRow
            {
                color = centroids[c],
                hex = ColorToHexRGB(centroids[c]),
                indices = clusterIndices[c]
            });
        }
        colorRows.Sort((a, b) => b.indices.Count.CompareTo(a.indices.Count));
        tableMode = $"simple (tol={similarity:F2})";

        Debug.Log($"<color=cyan>[Log Simple]</color> tolerance={similarity:F2}  |  " +
                  $"clusters: <b>{colorRows.Count}</b>");
        Repaint();
    }

    [HorizontalGroup("Top/Right/Bg")]
    [ToggleLeft, LabelText("Replace")]
    public bool bgReplace;

    [HorizontalGroup("Top/Right/Bg")]
    [ToggleLeft, LabelText("Skip")]
    public bool bgSkip;

    [HorizontalGroup("Top/Right/Bg", Width = 80)]
    [HideLabel]
    public Color bgColor = new Color(0.55f, 0.4f, 0.25f, 1f);

    private void SampleTexture()
    {
        int M = gridX, N = gridY;
        int texW = sourceTexture.width;
        int texH = sourceTexture.height;
        bool fitX = texW <= M, fitY = texH <= N;
        int offX = fitX ? (M - texW) / 2 : 0;
        int offY = fitY ? (N - texH) / 2 : 0;

        if (sampledTex != null) DestroyImmediate(sampledTex);
        sampledTex = new Texture2D(M, N, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < M; x++)
            {
                bool outside = false;
                int tx, ty;
                if (fitX)
                {
                    tx = x - offX;
                    if (tx < 0 || tx >= texW) outside = true;
                }
                else tx = Mathf.Clamp((int)((x + 0.5f) / M * texW), 0, texW - 1);
                if (fitY)
                {
                    ty = y - offY;
                    if (ty < 0 || ty >= texH) outside = true;
                }
                else ty = Mathf.Clamp((int)((y + 0.5f) / N * texH), 0, texH - 1);

                Color c = outside ? new Color(0, 0, 0, 0) : sourceTexture.GetPixel(tx, ty);

                if (bgReplace && c.a < BG_ALPHA_THRESHOLD)
                {
                    Color cc = bgColor; cc.a = 1f; c = cc;
                }

                sampledTex.SetPixel(x, y, c);
            }
        }
        sampledTex.Apply();
    }

    // ===================== Row 2: X | Y =====================
    [HorizontalGroup("Size")]
    [LabelText("X"), LabelWidth(15), MinValue(1)]
    public int gridX = 32;

    [HorizontalGroup("Size")]
    [LabelText("Y"), LabelWidth(15), MinValue(1)]
    public int gridY = 32;

    [LabelText("Similarity (for LOG SIMPLE)"), Range(0.01f, 1f)]
    public float similarity = 0.2f;

    [HorizontalGroup("Save")]
    [FolderPath(AbsolutePath = true), OnValueChanged("SaveFolderPref")]
    [LabelText("Folder"), LabelWidth(50)]
    public string outputFolder = "";

    [HorizontalGroup("Save", Width = 140)]
    [LabelText("IndexLevel"), LabelWidth(75), MinValue(0)]
    public int indexLevel = 1;

    [HorizontalGroup("Save", Width = 130)]
    [Button("SAVE JSON", ButtonSizes.Medium), GUIColor(1f, 0.8f, 0.5f)]
    public void SaveJson()
    {
        if (colorRows == null || colorRows.Count == 0) { Warn("Chưa có data. Bấm LOG TABLE/SIMPLE trước."); return; }
        if (sampledTex == null) { Warn("Chưa có grid."); return; }

        string folder = EnsureOutputFolder();
        if (string.IsNullOrEmpty(folder)) return;

        int W = sampledTex.width, H = sampledTex.height;

        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append($"  \"gridX\": {W},\n");
        sb.Append($"  \"gridY\": {H},\n");
        sb.Append("  \"colors\": {\n");
        for (int i = 0; i < colorRows.Count; i++)
        {
            var r = colorRows[i];
            sb.Append($"    \"{r.hex}\": [");
            for (int k = 0; k < r.indices.Count; k++)
            {
                if (k > 0) sb.Append(",");
                sb.Append(r.indices[k]);
            }
            sb.Append("]");
            if (i < colorRows.Count - 1) sb.Append(",");
            sb.Append("\n");
        }
        sb.Append("  }\n");
        sb.Append("}\n");

        string fileName = $"level_{indexLevel}.json";
        string path = Path.Combine(folder, fileName);
        File.WriteAllText(path, sb.ToString());
        RefreshAssetsIfInProject(path);
        Debug.Log($"<color=cyan>[Save JSON]</color> {path}  |  {colorRows.Count} màu, grid {W}×{H}");
    }

    // ===================== State =====================
    private Texture2D sampledTex;
    private Vector2 tableScroll;
    private string tableMode = "";

    private class ColorRow
    {
        public Color color;
        public string hex;
        public List<int> indices;
    }
    private List<ColorRow> colorRows;

    // ===================== Preview (grid | table) =====================
    [OnInspectorGUI]
    private void DrawPreview()
    {
        if (sampledTex == null) return;

        GUILayout.Space(10);

        float totalAvail = EditorGUIUtility.currentViewWidth - 30f;
        float gridW = Mathf.Clamp(totalAvail * 0.5f, 250f, 500f);
        float tableW = Mathf.Max(200f, totalAvail - gridW - 12f);

        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(gridW));
        DrawGrid(gridW);
        GUILayout.EndVertical();

        GUILayout.Space(8);

        GUILayout.BeginVertical(GUILayout.Width(tableW));
        DrawTable(tableW);
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.Space(10);
    }

    private void DrawGrid(float displayW)
    {
        int W = sampledTex.width, H = sampledTex.height;
        var style = new GUIStyle(EditorStyles.boldLabel) { richText = true };
        string modeLabel = string.IsNullOrEmpty(tableMode) ? "" : $" [{tableMode}]";
        GUILayout.Label($"<b>Grid Preview</b> — {W} × {H}{modeLabel}", style);

        float displayH = displayW * (float)H / W;
        Rect r = GUILayoutUtility.GetRect(displayW, displayH, GUILayout.Width(displayW), GUILayout.Height(displayH));
        EditorGUI.DrawRect(r, new Color(0.12f, 0.12f, 0.12f));
        GUI.DrawTexture(r, sampledTex, ScaleMode.ScaleToFit);

        float cellPx = displayW / W;
        if (cellPx >= 4f)
        {
            var lc = new Color(0, 0, 0, 0.35f);
            for (int x = 1; x < W; x++) EditorGUI.DrawRect(new Rect(r.x + x * cellPx, r.y, 1, displayH), lc);
            for (int y = 1; y < H; y++) EditorGUI.DrawRect(new Rect(r.x, r.y + y * cellPx, displayW, 1), lc);
        }
    }

    private void DrawTable(float width)
    {
        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true };

        if (colorRows == null || colorRows.Count == 0)
        {
            GUILayout.Label("<b>Color Table</b>", titleStyle);
            GUILayout.Space(4);
            EditorGUILayout.HelpBox("Chưa có data.\nBấm LOG TABLE hoặc LOG SIMPLE.", MessageType.None);
            return;
        }

        string modeLabel = string.IsNullOrEmpty(tableMode) ? "" : $" [{tableMode}]";
        GUILayout.Label($"<b>Color Table</b> — {colorRows.Count} màu{modeLabel}", titleStyle);

        float colorW = 110f;
        float countW = 50f;
        float posW = width - colorW - countW - 24f;
        if (posW < 80f) posW = 80f;

        var header = new GUIStyle(EditorStyles.toolbarButton) { alignment = TextAnchor.MiddleLeft };
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Color", header, GUILayout.Width(colorW));
        GUILayout.Label("Positions", header, GUILayout.Width(posW));
        GUILayout.Label("Count", header, GUILayout.Width(countW));
        GUILayout.EndHorizontal();

        tableScroll = GUILayout.BeginScrollView(tableScroll, GUILayout.Width(width), GUILayout.MinHeight(200));

        var wrapStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
        var monoStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };

        for (int i = 0; i < colorRows.Count; i++)
        {
            var rowData = colorRows[i];
            Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22));
            if (i % 2 == 0) EditorGUI.DrawRect(rowRect, new Color(1, 1, 1, 0.03f));

            GUILayout.BeginHorizontal(GUILayout.Width(colorW));
            GUILayout.Space(2);
            Rect swatch = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
            EditorGUI.DrawRect(swatch, rowData.color);
            EditorGUI.DrawRect(new Rect(swatch.x, swatch.y, swatch.width, 1), new Color(0, 0, 0, 0.4f));
            EditorGUI.DrawRect(new Rect(swatch.x, swatch.yMax - 1, swatch.width, 1), new Color(0, 0, 0, 0.4f));
            EditorGUI.DrawRect(new Rect(swatch.x, swatch.y, 1, swatch.height), new Color(0, 0, 0, 0.4f));
            EditorGUI.DrawRect(new Rect(swatch.xMax - 1, swatch.y, 1, swatch.height), new Color(0, 0, 0, 0.4f));
            GUILayout.Space(4);
            GUILayout.Label(rowData.hex, monoStyle, GUILayout.Width(colorW - 28));
            GUILayout.EndHorizontal();

            string posStr = string.Join(",", rowData.indices);
            GUILayout.Label(posStr, wrapStyle, GUILayout.Width(posW));
            GUILayout.Label(rowData.indices.Count.ToString(), GUILayout.Width(countW));
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    // =======================================================================
    // ============================= BOTTOM ==================================
    // =======================================================================

    [OnInspectorGUI, PropertyOrder(100)]
    private void DrawSeparator()
    {
        if (sampledTex == null) return;
        GUILayout.Space(10);
        Rect r = EditorGUILayout.GetControlRect(false, 2f);
        EditorGUI.DrawRect(r, new Color(0.45f, 0.45f, 0.45f, 0.8f));
        GUILayout.Space(6);
    }

    [HideInInspector] public int bottomX = 16;
    [HideInInspector] public int bottomY = 16;

    [OnInspectorGUI, PropertyOrder(110)]
    private void DrawBottomTopRow()
    {
        if (sampledTex == null) return;

        GUILayout.BeginHorizontal();

        GUILayout.Label("Bottom X", GUILayout.Width(60));
        bottomX = Mathf.Max(1, EditorGUILayout.IntField(bottomX, GUILayout.Width(50)));

        GUILayout.Space(15);
        GUILayout.Label("Y", GUILayout.Width(15));
        bottomY = Mathf.Max(1, EditorGUILayout.IntField(bottomY, GUILayout.Width(50)));

        GUILayout.Space(25);
        GUILayout.Label("Selected:", GUILayout.Width(60));
        Rect sel = GUILayoutUtility.GetRect(36, 20, GUILayout.Width(36), GUILayout.Height(20));
        EditorGUI.DrawRect(sel, selectedColor);
        EditorGUI.DrawRect(new Rect(sel.x, sel.y, sel.width, 1), Color.black);
        EditorGUI.DrawRect(new Rect(sel.x, sel.yMax - 1, sel.width, 1), Color.black);
        EditorGUI.DrawRect(new Rect(sel.x, sel.y, 1, sel.height), Color.black);
        EditorGUI.DrawRect(new Rect(sel.xMax - 1, sel.y, 1, sel.height), Color.black);

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    [HorizontalGroup("BottomBtns"), PropertyOrder(120)]
    [Button("VIEW", ButtonSizes.Medium), GUIColor(0.4f, 0.9f, 0.5f)]
    public void BottomViewBtn()
    {
        if (bottomTex != null) DestroyImmediate(bottomTex);
        bottomTex = new Texture2D(bottomX, bottomY, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var clear = new Color(0, 0, 0, 0);
        var pixels = new Color[bottomX * bottomY];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        bottomTex.SetPixels(pixels);
        bottomTex.Apply();
        Repaint();
    }

    [HorizontalGroup("BottomBtns"), PropertyOrder(120)]
    [Button("GET COLOR", ButtonSizes.Medium), GUIColor(0.9f, 0.7f, 1f)]
    [EnableIf("@colorRows != null && colorRows.Count > 0")]
    public void GetColorBtn()
    {
        if (colorRows == null || colorRows.Count == 0)
        {
            Warn("Color table trống. Bấm LOG TABLE hoặc LOG SIMPLE trước.");
            return;
        }
        bottomPalette = new List<Color>();
        paletteQuantities = new List<int>();
        foreach (var r in colorRows)
        {
            bottomPalette.Add(r.color);
            paletteQuantities.Add(1); // default = 1
        }
        if (bottomPalette.Count > 0) selectedColor = bottomPalette[0];
        Repaint();
    }

    [HorizontalGroup("BottomBtns"), PropertyOrder(120)]
    [Button("AUTO COLOR", ButtonSizes.Medium), GUIColor(1f, 0.65f, 0.85f)]
    [EnableIf("@bottomPalette != null && bottomPalette.Count > 0")]
    public void AutoColorBtn()
    {
        if (bottomPalette == null || bottomPalette.Count == 0) { Warn("Bấm GET COLOR trước."); return; }
        if (paletteQuantities == null || paletteQuantities.Count != bottomPalette.Count) { Warn("Palette không đồng bộ. Bấm GET COLOR lại."); return; }

        // Tổng quantity từ palette (user nhập)
        int totalCells = 0;
        for (int i = 0; i < paletteQuantities.Count; i++) totalCells += Mathf.Max(0, paletteQuantities[i]);
        if (totalCells == 0) { Warn("Tổng quantity = 0."); return; }

        // Y tự tính: ceil(total / X). X giữ nguyên theo user nhập.
        int newY = Mathf.CeilToInt((float)totalCells / bottomX);
        bottomY = newY;

        if (bottomTex != null) DestroyImmediate(bottomTex);
        bottomTex = new Texture2D(bottomX, newY, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[bottomX * newY];
        var clear = new Color(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        // Fill top-down theo thứ tự palette × quantity
        int filled = 0;
        for (int i = 0; i < bottomPalette.Count; i++)
        {
            int qty = Mathf.Max(0, paletteQuantities[i]);
            Color c = bottomPalette[i];
            for (int k = 0; k < qty; k++)
            {
                int row = filled / bottomX;
                int col = filled % bottomX;
                int texY = newY - 1 - row;
                pixels[texY * bottomX + col] = c;
                filled++;
            }
        }

        bottomTex.SetPixels(pixels);
        bottomTex.Apply();

        Debug.Log($"<color=cyan>[Auto Color]</color> X={bottomX}, total qty={totalCells} → Y={newY} (filled {filled})");
        Repaint();
    }

    private Texture2D bottomTex;
    private List<Color> bottomPalette;
    private List<int> paletteQuantities;
    private Color selectedColor = Color.white;

    [OnInspectorGUI, PropertyOrder(130)]
    private void DrawBottomSection()
    {
        if (bottomTex == null && (bottomPalette == null || bottomPalette.Count == 0)) return;

        GUILayout.Space(6);

        GUILayout.BeginHorizontal();

        // ============ LEFT: palettes ============
        if (bottomPalette != null && bottomPalette.Count > 0)
        {
            GUILayout.BeginVertical();
            DrawPaletteBlock();
            GUILayout.EndVertical();
        }

        GUILayout.Space(20);

        // ============ RIGHT: canvas ============
        if (bottomTex != null)
        {
            GUILayout.BeginVertical();
            DrawBottomGrid();
            GUILayout.EndVertical();
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
    }

    private void DrawPaletteBlock()
    {
        if (paletteQuantities == null || paletteQuantities.Count != bottomPalette.Count)
        {
            paletteQuantities = new List<int>();
            for (int i = 0; i < bottomPalette.Count; i++) paletteQuantities.Add(1);
        }

        const int CHUNK = 10;
        const float CELL_W = 32f;
        const float CELL_GAP = 1f;
        const float SWATCH_H = 22f;
        const float QTY_H = 18f;
        const float ROW_GAP = 2f;
        const float LABEL_W = 65f;

        int total = bottomPalette.Count;
        int chunks = Mathf.CeilToInt((float)total / CHUNK);

        for (int chunk = 0; chunk < chunks; chunk++)
        {
            int startIdx = chunk * CHUNK;
            int endIdx = Mathf.Min(startIdx + CHUNK, total);
            int chunkSize = endIdx - startIdx;

            float blockW = chunkSize * CELL_W + (chunkSize - 1) * CELL_GAP;
            float blockH = SWATCH_H + ROW_GAP + QTY_H;

            GUILayout.BeginHorizontal();

            // Labels
            GUILayout.BeginVertical(GUILayout.Width(LABEL_W));
            string label = chunks > 1 ? $"Palette {chunk + 1}:" : "Palette:";
            GUILayout.Label(label, GUILayout.Height(SWATCH_H));
            GUILayout.Space(ROW_GAP);
            GUILayout.Label("Qty:", GUILayout.Height(QTY_H));
            GUILayout.EndVertical();

            // Block
            Rect block = GUILayoutUtility.GetRect(blockW, blockH, GUILayout.Width(blockW), GUILayout.Height(blockH));

            for (int j = 0; j < chunkSize; j++)
            {
                int i = startIdx + j;
                float x = block.x + j * (CELL_W + CELL_GAP);
                Rect sR = new Rect(x, block.y, CELL_W, SWATCH_H);
                Rect qR = new Rect(x, block.y + SWATCH_H + ROW_GAP, CELL_W, QTY_H);

                Color c = bottomPalette[i];
                EditorGUI.DrawRect(sR, c);

                bool isSelected = ColorsEqual(c, selectedColor);
                Color border = isSelected ? Color.yellow : new Color(0, 0, 0, 0.5f);
                int bw = isSelected ? 2 : 1;
                EditorGUI.DrawRect(new Rect(sR.x, sR.y, sR.width, bw), border);
                EditorGUI.DrawRect(new Rect(sR.x, sR.yMax - bw, sR.width, bw), border);
                EditorGUI.DrawRect(new Rect(sR.x, sR.y, bw, sR.height), border);
                EditorGUI.DrawRect(new Rect(sR.xMax - bw, sR.y, bw, sR.height), border);

                if (Event.current.type == EventType.MouseDown && sR.Contains(Event.current.mousePosition))
                {
                    selectedColor = c;
                    Event.current.Use();
                    Repaint();
                }

                int nv = EditorGUI.IntField(qR, paletteQuantities[i]);
                if (nv < 0) nv = 0;
                paletteQuantities[i] = nv;
            }

            GUILayout.EndHorizontal();

            if (chunk < chunks - 1) GUILayout.Space(4);
        }
    }

    private void DrawBottomGrid()
    {
        int W = bottomTex.width, H = bottomTex.height;
        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true };
        GUILayout.Label($"<b>Bottom Canvas</b> — {W} × {H}    <color=#aaa>(L-click: paint  •  R-click: erase)</color>", titleStyle);

        // Cell size cố định ~ 32px. Canvas nhỏ vì game thường X = 4–5.
        // Nếu Y rất lớn dẫn tới height > viewport, window tự scroll.
        float cellPx = 32f;
        float displayW = W * cellPx;
        float displayH = H * cellPx;

        Rect r = GUILayoutUtility.GetRect(displayW, displayH, GUILayout.Width(displayW), GUILayout.Height(displayH));
        EditorGUI.DrawRect(r, new Color(0.12f, 0.12f, 0.12f));
        GUI.DrawTexture(r, bottomTex, ScaleMode.ScaleToFit);

        if (cellPx >= 4f)
        {
            var lc = new Color(0, 0, 0, 0.35f);
            for (int x = 1; x < W; x++) EditorGUI.DrawRect(new Rect(r.x + x * cellPx, r.y, 1, displayH), lc);
            for (int y = 1; y < H; y++) EditorGUI.DrawRect(new Rect(r.x, r.y + y * cellPx, displayW, 1), lc);
        }

        // Mouse paint
        Event e = Event.current;
        if (r.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                Vector2 local = e.mousePosition - new Vector2(r.x, r.y);
                int cellX = Mathf.Clamp((int)(local.x / cellPx), 0, W - 1);
                int cellYTop = Mathf.Clamp((int)(local.y / cellPx), 0, H - 1);
                int cellYTex = H - 1 - cellYTop;

                if (e.button == 0) // left: paint
                {
                    bottomTex.SetPixel(cellX, cellYTex, selectedColor);
                    bottomTex.Apply();
                    e.Use();
                    Repaint();
                }
                else if (e.button == 1) // right: erase
                {
                    bottomTex.SetPixel(cellX, cellYTex, new Color(0, 0, 0, 0));
                    bottomTex.Apply();
                    e.Use();
                    Repaint();
                }
            }
        }
    }

    // ===================== Folder pref / save helpers =====================
    private void SaveFolderPref()
    {
        EditorPrefs.SetString(PREF_OUTPUT_FOLDER, outputFolder ?? "");
    }

    private string EnsureOutputFolder()
    {
        if (!string.IsNullOrEmpty(outputFolder) && Directory.Exists(outputFolder))
            return outputFolder;

        string start = !string.IsNullOrEmpty(outputFolder) ? outputFolder : Application.dataPath;
        string picked = EditorUtility.OpenFolderPanel("Chọn output folder", start, "");
        if (string.IsNullOrEmpty(picked)) return null;
        outputFolder = picked;
        SaveFolderPref();
        return picked;
    }

    private static void RefreshAssetsIfInProject(string path)
    {
        string norm = path.Replace('\\', '/');
        string proj = Application.dataPath.Replace('\\', '/');
        if (norm.StartsWith(proj)) AssetDatabase.Refresh();
    }

    // ===================== Helpers =====================
    private void OnTextureChanged()
    {
        if (sampledTex != null) { DestroyImmediate(sampledTex); sampledTex = null; }
        colorRows = null;
        tableMode = "";
    }

    private static bool ColorsEqual(Color a, Color b)
    {
        Color32 x = a, y = b;
        return x.r == y.r && x.g == y.g && x.b == y.b && x.a == y.a;
    }

    private static Color AvgFromSum(Vector4 s, int c)
    {
        float inv = 1f / c;
        return new Color(s.x * inv, s.y * inv, s.z * inv, s.w * inv);
    }

    private static float ColorDistSq(Color a, Color b)
    {
        float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b, da = a.a - b.a;
        return dr * dr + dg * dg + db * db + da * da;
    }

    private static Vector2Int DetectLogicalSize(Texture2D tex)
    {
        int w = tex.width, h = tex.height;
        var pixels = tex.GetPixels32();
        int blockW = w, blockH = h;

        for (int y = 0; y < h && blockW > 1; y++)
        {
            Color32 prev = pixels[y * w];
            for (int x = 1; x < w; x++)
            {
                Color32 c = pixels[y * w + x];
                if (c.r != prev.r || c.g != prev.g || c.b != prev.b || c.a != prev.a)
                {
                    blockW = GCD(blockW, x);
                    prev = c;
                    if (blockW == 1) break;
                }
            }
        }
        for (int x = 0; x < w && blockH > 1; x++)
        {
            Color32 prev = pixels[x];
            for (int y = 1; y < h; y++)
            {
                Color32 c = pixels[y * w + x];
                if (c.r != prev.r || c.g != prev.g || c.b != prev.b || c.a != prev.a)
                {
                    blockH = GCD(blockH, y);
                    prev = c;
                    if (blockH == 1) break;
                }
            }
        }
        return new Vector2Int(w / blockW, h / blockH);
    }

    private static int GCD(int a, int b) { while (b != 0) { int t = b; b = a % b; a = t; } return a; }

    private bool EnsureReadable(Texture2D tex)
    {
        var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        if (imp == null || imp.isReadable) return true;
        if (!EditorUtility.DisplayDialog("Read/Write off",
            $"{tex.name} chưa bật Read/Write. Bật?", "Bật", "Hủy")) return false;
        imp.isReadable = true; imp.SaveAndReimport();
        return true;
    }

    private static string ColorToHexRGB(Color c)
    {
        Color32 c32 = c;
        return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}";
    }

    private static void Warn(string msg) => EditorUtility.DisplayDialog("⚠", msg, "OK");

    protected override void OnEnable()
    {
        base.OnEnable();
        outputFolder = EditorPrefs.GetString(PREF_OUTPUT_FOLDER, "");
    }
}