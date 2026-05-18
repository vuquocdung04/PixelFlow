// Assets/Editor/PixelImageAnalyzer.cs
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class PixelImageAnalyzer : OdinEditorWindow
{
    [MenuItem("Tools/Pixel Image Analyzer")]
    private static void Open() => GetWindow<PixelImageAnalyzer>("Pixel Image Analyzer").Show();

    public enum PaintMode { Off, Paint, Pick }

    // ===================== Input =====================
    [Title("Input", bold: true)]
    [Required, PreviewField(100), LabelText("Texture")]
    [OnValueChanged("OnTextureChanged")]
    public Texture2D sourceTexture;

    [HorizontalGroup("size")]
    [LabelText("Logical Grid Size"), MinValue(1)]
    [InfoBox("Click 'Auto Detect' để tool tự đoán logical size từ texture (yêu cầu texture không compression).")]
    public Vector2Int logicalSize = new Vector2Int(32, 32);

    [HorizontalGroup("size", width: 120)]
    [Button("Auto Detect"), GUIColor(1f, 0.85f, 0.4f)]
    private void AutoDetect()
    {
        if (sourceTexture == null) { Warn("Cần texture."); return; }
        if (!EnsureReadable(sourceTexture)) return;
        var d = DetectLogicalSize(sourceTexture);
        logicalSize = d;
        Debug.Log($"<color=cyan>[Auto Detect]</color> {sourceTexture.name}: " +
                  $"texture {sourceTexture.width}×{sourceTexture.height} → logical <b>{d.x}×{d.y}</b> " +
                  $"(block = {sourceTexture.width / d.x}×{sourceTexture.height / d.y})");
        View(); // auto-view sau khi detect
    }

    [LabelText("Hiện grid lines")] public bool showGridLines = true;

    [Button("VIEW & SAMPLE", ButtonSizes.Gigantic), GUIColor(0.4f, 0.9f, 0.5f)]
    public void View()
    {
        if (sourceTexture == null) { Warn("Cần texture."); return; }
        if (!EnsureReadable(sourceTexture)) return;

        int M = logicalSize.x, N = logicalSize.y;
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
                int px = Mathf.Clamp((int)((x + 0.5f) / M * sourceTexture.width), 0, sourceTexture.width - 1);
                int py = Mathf.Clamp((int)((y + 0.5f) / N * sourceTexture.height), 0, sourceTexture.height - 1);
                sampledTex.SetPixel(x, y, sourceTexture.GetPixel(px, py));
            }
        }
        sampledTex.Apply();
        Debug.Log($"<color=cyan>[Viewer]</color> Sampled <b>{M}×{N}</b> from {sourceTexture.name}");
        Repaint();
    }

    // ===================== Color Quantize =====================
    [Title("Color Quantize", bold: true), ShowIf("@sampledTex != null")]
    [LabelText("Tolerance"), Range(0.01f, 1f)]
    [InfoBox("Càng cao càng gom mạnh. ~0.15–0.25 phù hợp để gộp các shade (đỏ nhạt, đỏ đậm,...) về 1 màu chung.\n" +
             "Lưu ý: pixel trong suốt (alpha = 0) sẽ được giữ nguyên, không bị gộp.")]
    public float colorTolerance = 0.2f;

    [ShowIf("@sampledTex != null"), LabelText("Bỏ qua pixel trong suốt")]
    public bool ignoreTransparent = true;

    [ShowIf("@sampledTex != null")]
    [Button("QUANTIZE COLORS", ButtonSizes.Large), GUIColor(1f, 0.6f, 0.9f)]
    public void QuantizeColors()
    {
        if (sampledTex == null) { Warn("Chưa có grid. Bấm VIEW & SAMPLE trước."); return; }

        var pixels = sampledTex.GetPixels();
        int n = pixels.Length;
        int beforeUnique = CountUnique(pixels);

        // ----- Pass 1: greedy clustering -----
        var sums = new List<Vector4>();
        var counts = new List<int>();
        var centroids = new List<Color>();
        var assignment = new int[n];

        float tolSq = colorTolerance * colorTolerance;

        for (int i = 0; i < n; i++)
        {
            Color p = pixels[i];

            // Pixel trong suốt được giữ làm cluster riêng (nếu ignoreTransparent)
            if (ignoreTransparent && p.a <= 0.001f)
            {
                assignment[i] = -1;
                continue;
            }

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
                assignment[i] = sums.Count - 1;
            }
            else
            {
                sums[best] += new Vector4(p.r, p.g, p.b, p.a);
                counts[best]++;
                centroids[best] = AvgFromSum(sums[best], counts[best]);
                assignment[i] = best;
            }
        }

        // ----- Pass 2: 1 vòng k-means refinement để kết quả ổn định -----
        for (int c = 0; c < sums.Count; c++) { sums[c] = Vector4.zero; counts[c] = 0; }
        for (int i = 0; i < n; i++)
        {
            if (assignment[i] == -1) continue;
            Color p = pixels[i];
            int best = 0;
            float bestDsq = float.MaxValue;
            for (int c = 0; c < centroids.Count; c++)
            {
                float dsq = ColorDistSq(p, centroids[c]);
                if (dsq < bestDsq) { bestDsq = dsq; best = c; }
            }
            assignment[i] = best;
            sums[best] += new Vector4(p.r, p.g, p.b, p.a);
            counts[best]++;
        }
        for (int c = 0; c < centroids.Count; c++)
            if (counts[c] > 0) centroids[c] = AvgFromSum(sums[c], counts[c]);

        // ----- Apply -----
        for (int i = 0; i < n; i++)
        {
            if (assignment[i] == -1) continue; // giữ pixel trong suốt
            pixels[i] = centroids[assignment[i]];
        }
        sampledTex.SetPixels(pixels);
        sampledTex.Apply();

        int afterUnique = CountUnique(pixels);
        Debug.Log($"<color=cyan>[Quantize]</color> tolerance = {colorTolerance:F2}  |  " +
                  $"unique colors: <b>{beforeUnique}</b> → <b>{afterUnique}</b>  " +
                  $"(clusters tạo ra: {centroids.Count})");
        Repaint();
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

    private static int CountUnique(Color[] arr)
    {
        var set = new HashSet<uint>();
        for (int i = 0; i < arr.Length; i++)
        {
            Color32 c = arr[i];
            uint k = ((uint)c.r << 24) | ((uint)c.g << 16) | ((uint)c.b << 8) | c.a;
            set.Add(k);
        }
        return set.Count;
    }

    // ===================== Paint Tool =====================
    [Title("Paint Tool", bold: true), ShowIf("@sampledTex != null")]
    [LabelText("Mode"), EnumToggleButtons]
    public PaintMode mode = PaintMode.Off;

    [ShowIf("@sampledTex != null && mode != PaintMode.Off")]
    [InfoBox("Paint: click hoặc drag để tô màu cell.\nPick: click vào cell để lấy màu của nó vào 'Paint Color'.", InfoMessageType.None)]
    [LabelText("Paint Color")]
    public Color paintColor = Color.white;

    [ShowIf("@sampledTex != null"), HorizontalGroup("actions")]
    [Button("Reset (re-sample)"), GUIColor(0.9f, 0.9f, 0.5f)]
    public void Reset() => View();

    [ShowIf("@sampledTex != null"), HorizontalGroup("actions")]
    [Button("SAVE AS PNG", ButtonSizes.Medium), GUIColor(0.5f, 0.8f, 1f)]
    public void SavePng()
    {
        if (sampledTex == null) { Warn("Chưa có grid."); return; }
        string defaultName = (sourceTexture != null ? sourceTexture.name : "grid") + "_corrected.png";
        string path = EditorUtility.SaveFilePanel("Save corrected PNG", "Assets", defaultName, "png");
        if (string.IsNullOrEmpty(path)) return;
        File.WriteAllBytes(path, sampledTex.EncodeToPNG());
        AssetDatabase.Refresh();
        Debug.Log($"<color=cyan>[Save]</color> {path}");
        EditorUtility.RevealInFinder(path);
    }

    // ===================== Internal =====================
    private Texture2D sampledTex;
    private int hoverX = -1, hoverY = -1;

    [OnInspectorGUI]
    private void DrawPreview()
    {
        if (sampledTex == null) return;

        GUILayout.Space(10);
        int W = sampledTex.width, H = sampledTex.height;
        var style = new GUIStyle(EditorStyles.boldLabel) { richText = true };
        string hoverInfo = (hoverX >= 0 && hoverY >= 0)
            ? $"   |   cell <b>({hoverX}, {hoverY})</b> = {ColorToHex(sampledTex.GetPixel(hoverX, hoverY))}"
            : "";
        GUILayout.Label($"<b>Grid Preview</b> — {W} × {H}{hoverInfo}", style);

        float maxSize = 600f;
        float available = EditorGUIUtility.currentViewWidth - 60f;
        float displayW = Mathf.Min(maxSize, available);
        float displayH = displayW * (float)H / W;

        Rect r = GUILayoutUtility.GetRect(displayW, displayH, GUILayout.ExpandWidth(false));
        EditorGUI.DrawRect(r, new Color(0.12f, 0.12f, 0.12f));
        GUI.DrawTexture(r, sampledTex, ScaleMode.ScaleToFit);

        float cellPx = displayW / W;

        // Grid lines
        if (showGridLines && cellPx >= 4f)
        {
            var lc = new Color(0, 0, 0, 0.35f);
            for (int x = 1; x < W; x++) EditorGUI.DrawRect(new Rect(r.x + x * cellPx, r.y, 1, displayH), lc);
            for (int y = 1; y < H; y++) EditorGUI.DrawRect(new Rect(r.x, r.y + y * cellPx, displayW, 1), lc);
        }

        // Mouse handling
        Event e = Event.current;
        if (r.Contains(e.mousePosition))
        {
            Vector2 local = e.mousePosition - new Vector2(r.x, r.y);
            int cellX = Mathf.Clamp((int)(local.x / cellPx), 0, W - 1);
            int cellYTop = Mathf.Clamp((int)(local.y / cellPx), 0, H - 1);
            int cellYTex = H - 1 - cellYTop; // texture là bottom-up, GUI là top-down
            hoverX = cellX; hoverY = cellYTex;

            // Highlight hover cell (chỉ khi đang active mode)
            if (mode != PaintMode.Off)
            {
                var hRect = new Rect(r.x + cellX * cellPx, r.y + cellYTop * cellPx, cellPx, cellPx);
                EditorGUI.DrawRect(hRect, new Color(1, 1, 1, 0.2f));
                // Outline trắng
                EditorGUI.DrawRect(new Rect(hRect.x, hRect.y, hRect.width, 1), Color.white);
                EditorGUI.DrawRect(new Rect(hRect.x, hRect.yMax - 1, hRect.width, 1), Color.white);
                EditorGUI.DrawRect(new Rect(hRect.x, hRect.y, 1, hRect.height), Color.white);
                EditorGUI.DrawRect(new Rect(hRect.xMax - 1, hRect.y, 1, hRect.height), Color.white);
            }

            // Paint
            if (mode == PaintMode.Paint && e.button == 0 &&
                (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
            {
                Color cur = sampledTex.GetPixel(cellX, cellYTex);
                if (!ColorsEqual(cur, paintColor))
                {
                    sampledTex.SetPixel(cellX, cellYTex, paintColor);
                    sampledTex.Apply();
                }
                e.Use();
                Repaint();
            }
            // Pick
            else if (mode == PaintMode.Pick && e.type == EventType.MouseDown && e.button == 0)
            {
                paintColor = sampledTex.GetPixel(cellX, cellYTex);
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseMove) Repaint();
        }
        else if (hoverX >= 0)
        {
            hoverX = -1; hoverY = -1; Repaint();
        }

        GUILayout.Space(10);
    }

    // ===================== Helpers =====================
    private void OnTextureChanged()
    {
        if (sampledTex != null) { DestroyImmediate(sampledTex); sampledTex = null; }
        hoverX = hoverY = -1;
    }

    private static Vector2Int DetectLogicalSize(Texture2D tex)
    {
        int w = tex.width, h = tex.height;
        var pixels = tex.GetPixels32();
        int blockW = w, blockH = h;

        // Quét tất cả hàng để tìm các vị trí đổi màu theo X
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
        // Quét tất cả cột theo Y
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

    private static bool ColorsEqual(Color a, Color b)
    {
        Color32 x = a, y = b;
        return x.r == y.r && x.g == y.g && x.b == y.b && x.a == y.a;
    }

    private bool EnsureReadable(Texture2D tex)
    {
        var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        if (imp == null || imp.isReadable) return true;
        if (!EditorUtility.DisplayDialog("Read/Write off",
            $"{tex.name} chưa bật Read/Write. Bật?", "Bật", "Hủy")) return false;
        imp.isReadable = true; imp.SaveAndReimport();
        return true;
    }

    private static string ColorToHex(Color c)
    {
        Color32 c32 = c;
        return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2}";
    }

    private static void Warn(string msg) => EditorUtility.DisplayDialog("⚠", msg, "OK");

    protected override void OnEnable()
    {
        base.OnEnable();
        wantsMouseMove = true; // để hover update mượt
    }
}