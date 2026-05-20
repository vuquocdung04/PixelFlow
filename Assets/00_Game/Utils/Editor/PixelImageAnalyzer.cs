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

    public enum BottomTool { None, Blind, Ice, Tunnel, Link, Projectile }

    // ===== Persisted fields (hidden from default inspector — drawn manually) =====
    [HideInInspector] public Texture2D sourceTexture;
    [HideInInspector] public bool bgReplace;
    [HideInInspector] public bool bgSkip;
    [HideInInspector] public Color bgColor = new Color(0.55f, 0.4f, 0.25f, 1f);
    [HideInInspector] public int gridX = 32;
    [HideInInspector] public int gridY = 32;
    [HideInInspector] public float similarity = 0.2f;
    [HideInInspector] public string outputFolder = "";
    [HideInInspector] public int indexLevel = 1;
    [HideInInspector] public int bottomX = 16;
    [HideInInspector] public int bottomY = 16;

    // ===== Runtime state =====
    private Texture2D sampledTex;
    private Vector2 tableScroll;
    private string tableMode = "";

    private class ColorRow { public Color color; public string hex; public List<int> indices; }
    private List<ColorRow> colorRows;

    private Texture2D bottomTex;
    private List<Color> bottomPalette;
    private List<int> paletteQuantities;
    private Color selectedColor = Color.white;

    // Bottom overlays
    private HashSet<int> blindCells;
    private Dictionary<int, int> iceCells;          // cellId -> count
    private List<TunnelData> tunnels;
    private Dictionary<int, int> projectileCounts;  // cellId -> count
    private Dictionary<int, int> linkGroups;        // cellId -> group #

    private class TunnelData
    {
        public int placeAtId;
        public int direction; // 0=up,1=right,2=down,3=left
        public Color[] colors;
        public int[] counts;
    }

    private BottomTool activeTool = BottomTool.None;
    private int iceCount = 1;
    private int tunnelSlotCount = 2;
    private List<Color> tunnelSlotColors;
    private int projectileCount = 1;
    private int linkGroup = 1;

    [HideInInspector] public List<int> tunnelSlotCounts;
    [HideInInspector] public List<string> logMessages = new List<string>();

    protected override void OnEnable()
    {
        base.OnEnable();
        outputFolder = EditorPrefs.GetString(PREF_OUTPUT_FOLDER, "");
    }

    private void EnsureOverlayCollections()
    {
        if (blindCells == null) blindCells = new HashSet<int>();
        if (iceCells == null) iceCells = new Dictionary<int, int>();
        if (tunnels == null) tunnels = new List<TunnelData>();
        if (projectileCounts == null) projectileCounts = new Dictionary<int, int>();
        if (linkGroups == null) linkGroups = new Dictionary<int, int>();
    }

    private void ResetOverlays()
    {
        blindCells = new HashSet<int>();
        iceCells = new Dictionary<int, int>();
        tunnels = new List<TunnelData>();
        projectileCounts = new Dictionary<int, int>();
        linkGroups = new Dictionary<int, int>();
    }

    // ============================================================
    // ============ MASTER DRAW (2 columns) =======================
    // ============================================================
    [OnInspectorGUI]
    private void DrawAll()
    {
        float totalW = EditorGUIUtility.currentViewWidth - 30f;
        float leftW = Mathf.Clamp(totalW * 0.42f, 360f, 620f);
        float rightW = totalW - leftW - 16f;
        if (rightW < 400f) rightW = 400f;

        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(leftW));
        DrawLeftColumn(leftW);
        GUILayout.EndVertical();

        GUILayout.Space(12);

        GUILayout.BeginVertical(GUILayout.Width(rightW));
        DrawRightColumn(rightW);
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    // ============================================================
    // ============ LEFT COLUMN (Top Analyzer) ====================
    // ============================================================
    private void DrawLeftColumn(float width)
    {
        var header = new GUIStyle(EditorStyles.boldLabel) { richText = true, fontSize = 12 };
        GUILayout.Label("<b>TOP ANALYZER</b>", header);
        DrawHorzLine();

        // Texture + control buttons
        GUILayout.BeginHorizontal();

        // Texture preview (drag/drop)
        GUILayout.BeginVertical(GUILayout.Width(110));
        EditorGUI.BeginChangeCheck();
        Texture2D newTex = (Texture2D)EditorGUILayout.ObjectField(sourceTexture, typeof(Texture2D), false,
            GUILayout.Width(100), GUILayout.Height(100));
        if (EditorGUI.EndChangeCheck()) { sourceTexture = newTex; OnTextureChanged(); }
        GUILayout.EndVertical();

        // Buttons
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        if (DrawColorBtn("LOG TABLE", new Color(0.9f, 0.7f, 1f))) LogTable();
        if (DrawColorBtn("LOG SIMPLE", new Color(1f, 0.65f, 0.85f))) LogSimple();
        GUILayout.EndHorizontal();

        // BG row
        GUILayout.BeginHorizontal();
        bgReplace = GUILayout.Toggle(bgReplace, " Replace", GUILayout.Width(80));
        bgSkip = GUILayout.Toggle(bgSkip, " Skip", GUILayout.Width(60));
        bgColor = EditorGUILayout.ColorField(bgColor, GUILayout.Width(70));
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // Size X, Y
        GUILayout.BeginHorizontal();
        GUILayout.Label("X", GUILayout.Width(15));
        gridX = Mathf.Max(1, EditorGUILayout.IntField(gridX, GUILayout.Width(50)));
        GUILayout.Space(12);
        GUILayout.Label("Y", GUILayout.Width(15));
        gridY = Mathf.Max(1, EditorGUILayout.IntField(gridY, GUILayout.Width(50)));
        GUILayout.EndHorizontal();

        // Similarity
        GUILayout.BeginHorizontal();
        GUILayout.Label("Similarity", GUILayout.Width(75));
        similarity = EditorGUILayout.Slider(similarity, 0.01f, 1f);
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // Folder
        GUILayout.BeginHorizontal();
        GUILayout.Label("Folder", GUILayout.Width(50));
        EditorGUI.BeginChangeCheck();
        outputFolder = EditorGUILayout.TextField(outputFolder);
        if (EditorGUI.EndChangeCheck()) SaveFolderPref();
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string start = !string.IsNullOrEmpty(outputFolder) ? outputFolder : Application.dataPath;
            string picked = EditorUtility.OpenFolderPanel("Chọn output folder", start, "");
            if (!string.IsNullOrEmpty(picked)) { outputFolder = picked; SaveFolderPref(); }
        }
        GUILayout.EndHorizontal();

        // IndexLevel + SAVE
        GUILayout.BeginHorizontal();
        GUILayout.Label("IndexLevel", GUILayout.Width(75));
        indexLevel = Mathf.Max(0, EditorGUILayout.IntField(indexLevel, GUILayout.Width(60)));
        GUILayout.Space(8);
        if (DrawColorBtn("SAVE JSON", new Color(1f, 0.8f, 0.5f), 140f)) SaveJson();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        DrawHorzLine();
        GUILayout.Space(4);

        // Grid preview
        DrawGridPreview();

        GUILayout.Space(8);

        // Color table
        DrawTable(width);
    }

    private void DrawGridPreview()
    {
        if (sampledTex == null)
        {
            EditorGUILayout.HelpBox("Drag texture vào ô trên + bấm Auto Detect / LOG TABLE / LOG SIMPLE.", MessageType.Info);
            return;
        }

        int W = sampledTex.width, H = sampledTex.height;
        var style = new GUIStyle(EditorStyles.boldLabel) { richText = true };
        string modeLabel = string.IsNullOrEmpty(tableMode) ? "" : $" [{tableMode}]";
        GUILayout.Label($"<b>Grid Preview</b> — {W} × {H}{modeLabel}", style);

        const float cellPx = 32f / 3f;
        float displayW = W * cellPx;
        float displayH = H * cellPx;

        Rect r = GUILayoutUtility.GetRect(displayW, displayH, GUILayout.Width(displayW), GUILayout.Height(displayH));
        EditorGUI.DrawRect(r, new Color(0.12f, 0.12f, 0.12f));
        GUI.DrawTexture(r, sampledTex, ScaleMode.ScaleToFit);

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
            EditorGUILayout.HelpBox("Chưa có data. Bấm LOG TABLE hoặc LOG SIMPLE.", MessageType.None);
            return;
        }

        string modeLabel = string.IsNullOrEmpty(tableMode) ? "" : $" [{tableMode}]";
        GUILayout.Label($"<b>Color Table</b> — {colorRows.Count} màu{modeLabel}", titleStyle);

        float colorW = 110f, countW = 50f;
        float posW = width - colorW - countW - 30f;
        if (posW < 80f) posW = 80f;

        var headerStyle = new GUIStyle(EditorStyles.toolbarButton) { alignment = TextAnchor.MiddleLeft };
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Color", headerStyle, GUILayout.Width(colorW));
        GUILayout.Label("Positions", headerStyle, GUILayout.Width(posW));
        GUILayout.Label("Count", headerStyle, GUILayout.Width(countW));
        GUILayout.EndHorizontal();

        tableScroll = GUILayout.BeginScrollView(tableScroll, GUILayout.MinHeight(200));
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
            DrawBorder(swatch, new Color(0, 0, 0, 0.4f), 1);
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

    // ============================================================
    // ============ RIGHT COLUMN (Bottom Editor) ==================
    // ============================================================
    private void DrawRightColumn(float width)
    {
        var header = new GUIStyle(EditorStyles.boldLabel) { richText = true, fontSize = 12 };
        GUILayout.Label("<b>BOTTOM EDITOR</b>", header);
        DrawHorzLine();

        // Bottom X / Y / Selected
        GUILayout.BeginHorizontal();
        GUILayout.Label("Bottom X", GUILayout.Width(60));
        bottomX = Mathf.Max(1, EditorGUILayout.IntField(bottomX, GUILayout.Width(50)));
        GUILayout.Space(12);
        GUILayout.Label("Y", GUILayout.Width(15));
        bottomY = Mathf.Max(1, EditorGUILayout.IntField(bottomY, GUILayout.Width(50)));
        GUILayout.Space(20);
        GUILayout.Label("Selected:", GUILayout.Width(60));
        Rect sel = GUILayoutUtility.GetRect(36, 20, GUILayout.Width(36), GUILayout.Height(20));
        EditorGUI.DrawRect(sel, selectedColor);
        DrawBorder(sel, Color.black, 1);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // VIEW / GET COLOR / AUTO COLOR
        GUILayout.BeginHorizontal();
        if (DrawColorBtn("VIEW", new Color(0.4f, 0.9f, 0.5f), 100f)) BottomViewBtn();
        if (DrawColorBtn("AUTO COLOR", new Color(1f, 0.65f, 0.85f), 110f)) AutoColorBtn();
        if (DrawColorBtn("AUTO PROJECTILE", new Color(1f, 0.85f, 0.4f), 130f)) AutoProjectile();
        if (DrawColorBtn("LOG", new Color(0.7f, 0.9f, 1f), 70f)) LogProjectileBalance();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        DrawLogPanel();
        // Tools
        if (bottomTex != null)
        {
            DrawHorzLine();
            DrawToolRow();
        }

        GUILayout.Space(6);

        // Palette
        if (bottomPalette != null && bottomPalette.Count > 0)
        {
            DrawPaletteBlock();
            GUILayout.Space(6);
        }

        // Bottom canvas
        if (bottomTex != null) DrawBottomGrid();
    }

    private void DrawToolRow()
    {
        EnsureOverlayCollections();

        const float BTN_W = 90f;
        const float BTN_H = 22f;
        const float NUM_W = 42f;
        const float SLOT_W = 26f;
        const float SLOT_H = 22f;

        // Row 1: Blind / Ice / Projectile / Link
        GUILayout.BeginHorizontal();

        DrawToolButton("BLIND", BottomTool.Blind, new Color(0.55f, 0.55f, 0.6f), BTN_W, BTN_H);
        GUILayout.Space(6);

        DrawToolButton("ICE", BottomTool.Ice, new Color(0.5f, 0.85f, 1f), BTN_W, BTN_H);
        iceCount = Mathf.Max(1, EditorGUILayout.IntField(iceCount, GUILayout.Width(NUM_W)));
        GUILayout.Space(6);

        DrawToolButton("PROJECTILE", BottomTool.Projectile, new Color(1f, 0.85f, 0.25f), BTN_W + 10, BTN_H);
        projectileCount = Mathf.Max(0, EditorGUILayout.IntField(projectileCount, GUILayout.Width(NUM_W)));
        GUILayout.Space(6);

        DrawToolButton("LINK", BottomTool.Link, new Color(0.75f, 0.5f, 1f), BTN_W, BTN_H);
        linkGroup = Mathf.Max(1, EditorGUILayout.IntField(linkGroup, GUILayout.Width(NUM_W)));

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // Row 2: Tunnel + slots
        GUILayout.BeginHorizontal();
        DrawToolButton("TUNNEL", BottomTool.Tunnel, new Color(1f, 0.7f, 0.3f), BTN_W, BTN_H);
        int newSlots = Mathf.Max(1, EditorGUILayout.IntField(tunnelSlotCount, GUILayout.Width(NUM_W)));
        if (newSlots != tunnelSlotCount) { tunnelSlotCount = newSlots; ResizeTunnelSlots(); }
        if (tunnelSlotColors == null || tunnelSlotColors.Count != tunnelSlotCount
            || tunnelSlotCounts == null || tunnelSlotCounts.Count != tunnelSlotCount)
            ResizeTunnelSlots();

        GUILayout.Space(4);
        for (int i = 0; i < tunnelSlotCount; i++)
        {
            Rect rr = GUILayoutUtility.GetRect(SLOT_W, SLOT_H, GUILayout.Width(SLOT_W), GUILayout.Height(SLOT_H));
            Color c = tunnelSlotColors[i];
            if (c.a < 0.001f)
            {
                EditorGUI.DrawRect(rr, new Color(0.3f, 0.3f, 0.3f));
                EditorGUI.DrawRect(new Rect(rr.x, rr.y, rr.width / 2, rr.height / 2), new Color(0.5f, 0.5f, 0.5f));
                EditorGUI.DrawRect(new Rect(rr.x + rr.width / 2, rr.y + rr.height / 2, rr.width / 2, rr.height / 2), new Color(0.5f, 0.5f, 0.5f));
            }
            else
            {
                EditorGUI.DrawRect(rr, c);
                int cnt = (tunnelSlotCounts != null && i < tunnelSlotCounts.Count) ? tunnelSlotCounts[i] : 0;
                if (cnt > 0)
                {
                    Rect plate = new Rect(rr.x + 2, rr.center.y - 6, rr.width - 4, 12);
                    EditorGUI.DrawRect(plate, new Color(0, 0, 0, 0.6f));
                    var ns = new GUIStyle(EditorStyles.boldLabel)
                    { alignment = TextAnchor.MiddleCenter, fontSize = 9, normal = { textColor = Color.white } };
                    GUI.Label(plate, cnt.ToString(), ns);
                }
            }
            DrawBorder(rr, new Color(0, 0, 0, 0.6f), 1);

            if (Event.current.type == EventType.MouseDown && rr.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0)
                {
                    if (activeTool == BottomTool.Projectile && c.a >= 0.001f)
                    {
                        if (tunnelSlotCounts[i] == projectileCount) tunnelSlotCounts[i] = 0;
                        else tunnelSlotCounts[i] = projectileCount;
                    }
                    else
                    {
                        tunnelSlotColors[i] = selectedColor;
                    }
                }
                else if (Event.current.button == 1)
                {
                    tunnelSlotColors[i] = new Color(0, 0, 0, 0);
                    tunnelSlotCounts[i] = 0;
                }
                Event.current.Use();
                Repaint();
            }
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void DrawToolButton(string label, BottomTool tool, Color activeTint, float w, float h)
    {
        Color old = GUI.backgroundColor;
        bool active = activeTool == tool;
        GUI.backgroundColor = active ? activeTint : Color.white; // default when inactive
        if (GUILayout.Button(label, GUILayout.Width(w), GUILayout.Height(h)))
        {
            activeTool = active ? BottomTool.None : tool;
            Repaint();
        }
        GUI.backgroundColor = old;
    }

    private bool DrawColorBtn(string label, Color tint, float width = 0f)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = tint;
        bool clicked = width > 0f
            ? GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(22))
            : GUILayout.Button(label, GUILayout.Height(22));
        GUI.backgroundColor = old;
        return clicked;
    }

    private void ResizeTunnelSlots()
    {
        var newColors = new List<Color>(tunnelSlotCount);
        var newCounts = new List<int>(tunnelSlotCount);
        for (int i = 0; i < tunnelSlotCount; i++)
        {
            newColors.Add(tunnelSlotColors != null && i < tunnelSlotColors.Count
                ? tunnelSlotColors[i] : new Color(0, 0, 0, 0));
            newCounts.Add(tunnelSlotCounts != null && i < tunnelSlotCounts.Count
                ? tunnelSlotCounts[i] : 0);
        }
        tunnelSlotColors = newColors;
        tunnelSlotCounts = newCounts;
    }

    private void DrawPaletteBlock()
    {
        if (paletteQuantities == null || paletteQuantities.Count != bottomPalette.Count)
        {
            paletteQuantities = new List<int>();
            for (int i = 0; i < bottomPalette.Count; i++) paletteQuantities.Add(1);
        }

        const int CHUNK = 10;
        const float CELL_W = 32f, CELL_GAP = 1f, SWATCH_H = 22f, QTY_H = 18f, ROW_GAP = 2f, LABEL_W = 65f;

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
            GUILayout.BeginVertical(GUILayout.Width(LABEL_W));
            string label = chunks > 1 ? $"Palette {chunk + 1}:" : "Palette:";
            GUILayout.Label(label, GUILayout.Height(SWATCH_H));
            GUILayout.Space(ROW_GAP);
            GUILayout.Label("Qty:", GUILayout.Height(QTY_H));
            GUILayout.EndVertical();

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
                DrawBorder(sR, border, bw);

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
        EnsureOverlayCollections();
        int W = bottomTex.width, H = bottomTex.height;

        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true };
        string toolLabel = activeTool == BottomTool.None ? "Paint" : activeTool.ToString();
        GUILayout.Label($"<b>Bottom Canvas</b> — {W} × {H}    <color=#aaa>(Tool: {toolLabel}  •  L: apply  •  R: clear cell)</color>", titleStyle);

        float cellPx = 32f;
        float displayW = W * cellPx;
        float displayH = H * cellPx;

        Rect r = GUILayoutUtility.GetRect(displayW, displayH, GUILayout.Width(displayW), GUILayout.Height(displayH));
        EditorGUI.DrawRect(r, new Color(0.12f, 0.12f, 0.12f));
        GUI.DrawTexture(r, bottomTex, ScaleMode.ScaleToFit);

        var cornerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9,
            normal = { textColor = Color.white }
        };
        var tunnelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            normal = { textColor = Color.white }
        };

        for (int row = 0; row < H; row++)
        {
            for (int col = 0; col < W; col++)
            {
                int cellId = row * W + col;
                Rect cellR = new Rect(r.x + col * cellPx, r.y + row * cellPx, cellPx, cellPx);

                var tunnel = FindTunnel(cellId);
                if (tunnel != null)
                {
                    EditorGUI.DrawRect(cellR, new Color(1f, 0.55f, 0.15f, 0.95f));
                    string arrow = DirectionToArrow(tunnel.direction);
                    GUI.Label(cellR, $"T{arrow}{tunnel.colors.Length}", tunnelStyle);
                    continue;
                }

                float halfW = cellR.width / 2f;
                float halfH = cellR.height / 2f;

                // TL — Ice (blue)
                if (iceCells.TryGetValue(cellId, out int iceN))
                {
                    Rect tl = new Rect(cellR.x, cellR.y, halfW, halfH);
                    EditorGUI.DrawRect(tl, new Color(0.25f, 0.7f, 0.95f, 0.85f));
                    GUI.Label(tl, $"I{iceN}", cornerStyle);
                }

                // TR — Projectile (yellow), only if > 0
                if (projectileCounts.TryGetValue(cellId, out int projN) && projN > 0)
                {
                    Rect tr = new Rect(cellR.x + halfW, cellR.y, halfW, halfH);
                    EditorGUI.DrawRect(tr, new Color(1f, 0.85f, 0.25f, 0.85f));
                    GUI.Label(tr, $"P{projN}", cornerStyle);
                }

                // BL — Blind (dark)
                if (blindCells.Contains(cellId))
                {
                    Rect bl = new Rect(cellR.x, cellR.y + halfH, halfW, halfH);
                    EditorGUI.DrawRect(bl, new Color(0.1f, 0.1f, 0.1f, 0.85f));
                    GUI.Label(bl, "B", cornerStyle);
                }

                // BR — Link (purple)
                if (linkGroups.TryGetValue(cellId, out int linkG))
                {
                    Rect br = new Rect(cellR.x + halfW, cellR.y + halfH, halfW, halfH);
                    EditorGUI.DrawRect(br, new Color(0.7f, 0.4f, 0.95f, 0.85f));
                    GUI.Label(br, $"L{linkG}", cornerStyle);
                }
            }
        }

        // Grid lines
        var lc = new Color(0, 0, 0, 0.4f);
        for (int x = 1; x < W; x++) EditorGUI.DrawRect(new Rect(r.x + x * cellPx, r.y, 1, displayH), lc);
        for (int y = 1; y < H; y++) EditorGUI.DrawRect(new Rect(r.x, r.y + y * cellPx, displayW, 1), lc);

        // Mouse interaction
        Event e = Event.current;
        if (r.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                Vector2 local = e.mousePosition - new Vector2(r.x, r.y);
                int col = Mathf.Clamp((int)(local.x / cellPx), 0, W - 1);
                int rowTop = Mathf.Clamp((int)(local.y / cellPx), 0, H - 1);
                int cellId = rowTop * W + col;
                int cellYTex = H - 1 - rowTop;

                if (e.button == 1)
                {
                    ClearCellCompletely(cellId, col, cellYTex);
                    e.Use();
                    Repaint();
                }
                else if (e.button == 0)
                {
                    bool onMouseDown = (e.type == EventType.MouseDown);
                    bool consumed = HandleLeftAction(cellId, col, cellYTex, onMouseDown);
                    if (consumed) { e.Use(); Repaint(); }
                }
            }
        }
    }

    private bool HandleLeftAction(int cellId, int col, int yTex, bool isMouseDown)
    {
        var tunnel = FindTunnel(cellId);

        switch (activeTool)
        {
            case BottomTool.None:
                if (tunnel != null) return false;
                Color cur = bottomTex.GetPixel(col, yTex);
                if (!ColorsEqual(cur, selectedColor))
                {
                    bottomTex.SetPixel(col, yTex, selectedColor);
                    bottomTex.Apply();
                }
                return true;

            case BottomTool.Blind:
                if (!isMouseDown) return false;
                if (tunnel != null) return false;
                if (blindCells.Contains(cellId)) blindCells.Remove(cellId);
                else blindCells.Add(cellId);
                return true;

            case BottomTool.Ice:
                if (!isMouseDown) return false;
                if (tunnel != null) return false;
                if (iceCells.TryGetValue(cellId, out int curIce) && curIce == iceCount)
                    iceCells.Remove(cellId);
                else
                    iceCells[cellId] = iceCount;
                return true;

            case BottomTool.Projectile:
                if (!isMouseDown) return false;
                if (tunnel != null) return false;
                // Must be on a colored cell
                if (bottomTex.GetPixel(col, yTex).a < 0.001f) return false;
                if (projectileCounts.TryGetValue(cellId, out int curProj) && curProj == projectileCount)
                    projectileCounts.Remove(cellId);
                else
                    projectileCounts[cellId] = projectileCount;
                return true;

            case BottomTool.Link:
                if (!isMouseDown) return false;
                if (tunnel != null) return false;
                if (linkGroups.TryGetValue(cellId, out int curLink) && curLink == linkGroup)
                    linkGroups.Remove(cellId);
                else
                    linkGroups[cellId] = linkGroup;
                return true;

            case BottomTool.Tunnel:
                if (!isMouseDown) return false;
                if (tunnel != null)
                {
                    tunnel.direction = (tunnel.direction + 1) % 4;
                    return true;
                }
                // Place new tunnel — clear all other props
                bottomTex.SetPixel(col, yTex, new Color(0, 0, 0, 0));
                bottomTex.Apply();
                blindCells.Remove(cellId);
                iceCells.Remove(cellId);
                projectileCounts.Remove(cellId);
                linkGroups.Remove(cellId);
                tunnels.Add(new TunnelData
                {
                    placeAtId = cellId,
                    direction = 0,
                    colors = tunnelSlotColors != null ? tunnelSlotColors.ToArray() : new Color[0],
                    counts = tunnelSlotCounts != null ? tunnelSlotCounts.ToArray() : new int[0]
                });
                if (tunnelSlotColors != null)
                    for (int i = 0; i < tunnelSlotColors.Count; i++) tunnelSlotColors[i] = new Color(0, 0, 0, 0);
                if (tunnelSlotCounts != null)
                    for (int i = 0; i < tunnelSlotCounts.Count; i++) tunnelSlotCounts[i] = 0;
                return true;
        }
        return false;
    }

    private void ClearCellCompletely(int cellId, int col, int yTex)
    {
        bottomTex.SetPixel(col, yTex, new Color(0, 0, 0, 0));
        bottomTex.Apply();
        if (blindCells != null) blindCells.Remove(cellId);
        if (iceCells != null) iceCells.Remove(cellId);
        if (projectileCounts != null) projectileCounts.Remove(cellId);
        if (linkGroups != null) linkGroups.Remove(cellId);
        if (tunnels != null)
            for (int i = tunnels.Count - 1; i >= 0; i--)
                if (tunnels[i].placeAtId == cellId) tunnels.RemoveAt(i);
    }

    private TunnelData FindTunnel(int cellId)
    {
        if (tunnels == null) return null;
        for (int i = 0; i < tunnels.Count; i++)
            if (tunnels[i].placeAtId == cellId) return tunnels[i];
        return null;
    }

    private static string DirectionToArrow(int dir)
    {
        switch (dir)
        {
            case 0: return "↑";
            case 1: return "→";
            case 2: return "↓";
            case 3: return "←";
            default: return "?";
        }
    }

    // ============================================================
    // ============ ACTIONS =======================================
    // ===========================================================

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
                if (!dict.TryGetValue(hex, out var rr))
                {
                    rr = new ColorRow { color = c, hex = hex, indices = new List<int>() };
                    dict[hex] = rr;
                }
                rr.indices.Add(idx);
            }
        }

        colorRows = new List<ColorRow>(dict.Values);
        colorRows.Sort((a, b) => b.indices.Count.CompareTo(a.indices.Count));
        tableMode = "full";
        AutoGetColor();
        Debug.Log($"<color=cyan>[Log Table]</color> grid {W}×{H}  |  <b>{colorRows.Count}</b> màu unique");
        Repaint();
    }

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
                if (assignment[readingIdx] == -1) pixels[arrIdx] = new Color(0, 0, 0, 0);
                else pixels[arrIdx] = centroids[assignment[readingIdx]];
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
        AutoGetColor();
        Debug.Log($"<color=cyan>[Log Simple]</color> tolerance={similarity:F2}  |  clusters: <b>{colorRows.Count}</b>");
        Repaint();
    }

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
                if (fitX) { tx = x - offX; if (tx < 0 || tx >= texW) outside = true; }
                else tx = Mathf.Clamp((int)((x + 0.5f) / M * texW), 0, texW - 1);
                if (fitY) { ty = y - offY; if (ty < 0 || ty >= texH) outside = true; }
                else ty = Mathf.Clamp((int)((y + 0.5f) / N * texH), 0, texH - 1);

                Color c = outside ? new Color(0, 0, 0, 0) : sourceTexture.GetPixel(tx, ty);
                if (bgReplace && c.a < BG_ALPHA_THRESHOLD) { Color cc = bgColor; cc.a = 1f; c = cc; }
                sampledTex.SetPixel(x, y, c);
            }
        }
        sampledTex.Apply();
    }

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
        ResetOverlays();
        Repaint();
    }
    private void AutoGetColor()
    {
        if (colorRows == null || colorRows.Count == 0) return;

        bottomPalette = new List<Color>();
        paletteQuantities = new List<int>();
        foreach (var rr in colorRows)
        {
            bottomPalette.Add(rr.color);
            paletteQuantities.Add(1);
        }
        if (bottomPalette.Count > 0) selectedColor = bottomPalette[0];

        // invalidate bottom canvas + overlays vì palette đổi
        if (bottomTex != null) { DestroyImmediate(bottomTex); bottomTex = null; }
        ResetOverlays();
    }
    public void AutoColorBtn()
    {
        if (bottomPalette == null || bottomPalette.Count == 0) { Warn("Bấm GET COLOR trước."); return; }
        if (paletteQuantities == null || paletteQuantities.Count != bottomPalette.Count) { Warn("Palette không đồng bộ."); return; }

        int totalCells = 0;
        for (int i = 0; i < paletteQuantities.Count; i++) totalCells += Mathf.Max(0, paletteQuantities[i]);
        if (totalCells == 0) { Warn("Tổng quantity = 0."); return; }

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
        ResetOverlays();
        Debug.Log($"<color=cyan>[Auto Color]</color> X={bottomX}, total qty={totalCells} → Y={newY} (filled {filled})");
        Repaint();
    }
    private void DrawLogPanel()
    {
        if (logMessages == null || logMessages.Count == 0) return;
        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true };
        GUILayout.Label($"<b>Log</b> — {logMessages.Count} dòng", titleStyle);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        foreach (var msg in logMessages) GUILayout.Label(msg);
        EditorGUILayout.EndVertical();
    }

    private int NearestPaletteIndex(Color c)
    {
        if (colorRows == null || colorRows.Count == 0) return -1;
        int best = 0;
        float bestDsq = float.MaxValue;
        for (int i = 0; i < colorRows.Count; i++)
        {
            float dsq = ColorDistSq(c, colorRows[i].color);
            if (dsq < bestDsq) { bestDsq = dsq; best = i; }
        }
        return best;
    }

    private void AutoProjectile()
    {
        if (colorRows == null || colorRows.Count == 0) { Warn("Chưa có color table."); return; }
        if (bottomTex == null) { Warn("Chưa có bottom canvas."); return; }
        EnsureOverlayCollections();

        int W = bottomTex.width, H = bottomTex.height;
        var tunnelCells = new HashSet<int>();
        foreach (var t in tunnels) tunnelCells.Add(t.placeAtId);

        var canvasByIdx = new Dictionary<int, List<int>>();
        for (int row = 0; row < H; row++)
        {
            int texY = H - 1 - row;
            for (int x = 0; x < W; x++)
            {
                int cid = row * W + x;
                if (tunnelCells.Contains(cid)) continue;
                Color c = bottomTex.GetPixel(x, texY);
                if (c.a < 0.001f) continue;
                int idx = NearestPaletteIndex(c);
                if (idx < 0) continue;
                if (!canvasByIdx.TryGetValue(idx, out var list)) { list = new List<int>(); canvasByIdx[idx] = list; }
                list.Add(cid);
            }
        }

        var slotsByIdx = new Dictionary<int, List<(int ti, int si)>>();
        for (int ti = 0; ti < tunnels.Count; ti++)
        {
            var t = tunnels[ti];
            for (int si = 0; si < t.colors.Length; si++)
            {
                if (t.colors[si].a < 0.001f) continue;
                int idx = NearestPaletteIndex(t.colors[si]);
                if (idx < 0) continue;
                if (!slotsByIdx.TryGetValue(idx, out var list))
                {
                    list = new List<(int ti, int si)>();   // <-- thêm tên
                    slotsByIdx[idx] = list;
                }
                list.Add((ti, si));
            }
        }

        for (int ci = 0; ci < colorRows.Count; ci++)
        {
            int totalCount = colorRows[ci].indices.Count;
            var canvasCells = canvasByIdx.TryGetValue(ci, out var cc) ? cc : new List<int>();
            var slotLocs = slotsByIdx.TryGetValue(ci, out var sl) ? sl : new List<(int ti, int si)>();  // <-- thêm tên

            int totalLocs = canvasCells.Count + slotLocs.Count;
            if (totalLocs == 0) continue;

            int per = totalCount / totalLocs;
            int rem = totalCount - per * totalLocs;
            int idx = 0;

            for (int i = 0; i < canvasCells.Count; i++)
            {
                bool last = (idx == totalLocs - 1);
                projectileCounts[canvasCells[i]] = per + (last ? rem : 0);
                idx++;
            }
            for (int i = 0; i < slotLocs.Count; i++)
            {
                bool last = (idx == totalLocs - 1);
                var t = tunnels[slotLocs[i].ti];
                if (t.counts == null || t.counts.Length != t.colors.Length)
                    t.counts = new int[t.colors.Length];
                t.counts[slotLocs[i].si] = per + (last ? rem : 0);
                idx++;
            }
        }
        Repaint();
    }

    private void LogProjectileBalance()
    {
        logMessages.Clear();
        if (colorRows == null || colorRows.Count == 0) { Warn("Chưa có color table."); return; }
        if (bottomTex == null) { Warn("Chưa có bottom canvas."); return; }
        EnsureOverlayCollections();

        int W = bottomTex.width, H = bottomTex.height;
        var tunnelCells = new HashSet<int>();
        foreach (var t in tunnels) tunnelCells.Add(t.placeAtId);

        var bottomTotals = new Dictionary<string, int>();
        for (int row = 0; row < H; row++)
        {
            int texY = H - 1 - row;
            for (int x = 0; x < W; x++)
            {
                int cid = row * W + x;
                if (tunnelCells.Contains(cid)) continue;
                Color c = bottomTex.GetPixel(x, texY);
                if (c.a < 0.001f) continue;
                int idx = NearestPaletteIndex(c);
                if (idx < 0) continue;
                string hex = colorRows[idx].hex;
                int cnt = projectileCounts.TryGetValue(cid, out int v) ? v : 0;
                if (!bottomTotals.ContainsKey(hex)) bottomTotals[hex] = 0;
                bottomTotals[hex] += cnt;
            }
        }
        foreach (var t in tunnels)
        {
            for (int si = 0; si < t.colors.Length; si++)
            {
                if (t.colors[si].a < 0.001f) continue;
                int idx = NearestPaletteIndex(t.colors[si]);
                if (idx < 0) continue;
                string hex = colorRows[idx].hex;
                int cnt = (t.counts != null && si < t.counts.Length) ? t.counts[si] : 0;
                if (!bottomTotals.ContainsKey(hex)) bottomTotals[hex] = 0;
                bottomTotals[hex] += cnt;
            }
        }

        var topLookup = new Dictionary<string, int>();
        foreach (var rr in colorRows) topLookup[rr.hex] = rr.indices.Count;

        var all = new HashSet<string>();
        foreach (var h in topLookup.Keys) all.Add(h);
        foreach (var h in bottomTotals.Keys) all.Add(h);

        foreach (var hex in all)
        {
            int top = topLookup.TryGetValue(hex, out int tc) ? tc : 0;
            int bot = bottomTotals.TryGetValue(hex, out int bc) ? bc : 0;
            int diff = bot - top;
            if (diff < 0) logMessages.Add($"{hex}: thiếu {-diff}");
            else if (diff > 0) logMessages.Add($"{hex}: thừa {diff}");
        }
        if (logMessages.Count == 0) logMessages.Add("✓ Tất cả màu đã khớp.");
        Repaint();
    }


    // ============================================================
    // ============ SAVE JSON =====================================
    // ============================================================
    public void SaveJson()
    {
        if (colorRows == null || colorRows.Count == 0) { Warn("Chưa có data. Bấm LOG TABLE/SIMPLE trước."); return; }
        if (sampledTex == null) { Warn("Chưa có grid."); return; }

        string folder = EnsureOutputFolder();
        if (string.IsNullOrEmpty(folder)) return;

        int W = sampledTex.width, H = sampledTex.height;

        var sb = new StringBuilder();
        sb.Append("{\n");

        // ----- TOP block (unchanged) -----
        sb.Append("  \"top\": {\n");
        sb.Append($"    \"gridX\": {W},\n");
        sb.Append($"    \"gridY\": {H},\n");
        sb.Append("    \"colors\": {\n");
        for (int i = 0; i < colorRows.Count; i++)
        {
            var rr = colorRows[i];
            sb.Append($"      \"{rr.hex}\": [");
            for (int k = 0; k < rr.indices.Count; k++)
            {
                if (k > 0) sb.Append(",");
                sb.Append(rr.indices[k]);
            }
            sb.Append("]");
            if (i < colorRows.Count - 1) sb.Append(",");
            sb.Append("\n");
        }
        sb.Append("    }\n");
        sb.Append("  }");

        // ----- BOTTOM block -----
        if (bottomTex != null)
        {
            sb.Append(",\n");
            sb.Append("  \"bottom\": {\n");
            WriteBottomBlock(sb);
            sb.Append("  }\n");
        }
        else
        {
            sb.Append("\n");
        }

        sb.Append("}\n");

        string fileName = $"level_{indexLevel}.json";
        string path = Path.Combine(folder, fileName);
        File.WriteAllText(path, sb.ToString());
        RefreshAssetsIfInProject(path);
        Debug.Log($"<color=cyan>[Save JSON]</color> {path}");
    }

    private void WriteBottomBlock(StringBuilder sb)
    {
        EnsureOverlayCollections();
        int W = bottomTex.width, H = bottomTex.height;
        sb.Append($"    \"gridX\": {W},\n");
        sb.Append($"    \"gridY\": {H},\n");

        // tunnel cells - exclude from colors
        var tunnelCells = new HashSet<int>();
        foreach (var t in tunnels) tunnelCells.Add(t.placeAtId);

        // ----- colors: {hex: {cellId: count}} -----
        var colorGroups = new Dictionary<string, List<int>>();
        for (int row = 0; row < H; row++)
        {
            int texY = H - 1 - row;
            for (int x = 0; x < W; x++)
            {
                int cellId = row * W + x;
                if (tunnelCells.Contains(cellId)) continue;
                Color c = bottomTex.GetPixel(x, texY);
                if (c.a < 0.001f) continue;
                string hex = ColorToHexRGB(c);
                if (!colorGroups.TryGetValue(hex, out var list))
                {
                    list = new List<int>();
                    colorGroups[hex] = list;
                }
                list.Add(cellId);
            }
        }

        sb.Append("    \"colors\": {");
        if (colorGroups.Count > 0)
        {
            sb.Append("\n");
            int ci = 0, clast = colorGroups.Count - 1;
            foreach (var kv in colorGroups)
            {
                sb.Append($"      \"{kv.Key}\": {{");
                for (int k = 0; k < kv.Value.Count; k++)
                {
                    int cellId = kv.Value[k];
                    int count = 0;
                    projectileCounts.TryGetValue(cellId, out count);
                    if (k > 0) sb.Append(", ");
                    sb.Append($"\"{cellId}\": {count}");
                }
                sb.Append("}");
                if (ci < clast) sb.Append(",");
                sb.Append("\n");
                ci++;
            }
            sb.Append("    ");
        }
        sb.Append("},\n");

        // ----- blinds: [array] -----
        sb.Append("    \"blinds\": [");
        if (blindCells.Count > 0)
        {
            var list = new List<int>(blindCells); list.Sort();
            for (int i = 0; i < list.Count; i++) { if (i > 0) sb.Append(","); sb.Append(list[i]); }
        }
        sb.Append("],\n");

        // ----- ices: {cellId: count} -----
        sb.Append("    \"ices\": {");
        if (iceCells.Count > 0)
        {
            var ids = new List<int>(iceCells.Keys); ids.Sort();
            for (int i = 0; i < ids.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append($"\"{ids[i]}\": {iceCells[ids[i]]}");
            }
        }
        sb.Append("},\n");

        // ----- tunnels: {cellId: {spawnAtID, colors}} -----
        sb.Append("    \"tunnels\": {");
        if (tunnels.Count > 0)
        {
            sb.Append("\n");
            var sorted = new List<TunnelData>(tunnels);
            sorted.Sort((a, b) => a.placeAtId.CompareTo(b.placeAtId));
            for (int i = 0; i < sorted.Count; i++)
            {
                var t = sorted[i];
                int spawnId = ComputeSpawnAtId(t.placeAtId, t.direction, W, H);
                sb.Append($"      \"{t.placeAtId}\": {{ \"spawnAtID\": {spawnId}, \"colors\": [");
                for (int k = 0; k < t.colors.Length; k++)
                {
                    if (k > 0) sb.Append(", ");
                    int cnt = (t.counts != null && k < t.counts.Length) ? t.counts[k] : 0;
                    sb.Append($"{{\"hex\":\"{ColorToHexRGB(t.colors[k])}\",\"count\":{cnt}}}");
                }
                sb.Append("] }");
                if (i < sorted.Count - 1) sb.Append(",");
                sb.Append("\n");
            }
            sb.Append("    ");
        }
        sb.Append("},\n");

        // ----- links: [[id1, id2, ...], ...] -----
        sb.Append("    \"links\": [");
        if (linkGroups.Count > 0)
        {
            var groupMap = new Dictionary<int, List<int>>();
            foreach (var kv in linkGroups)
            {
                if (!groupMap.TryGetValue(kv.Value, out var list))
                {
                    list = new List<int>();
                    groupMap[kv.Value] = list;
                }
                list.Add(kv.Key);
            }
            var groupIds = new List<int>(groupMap.Keys); groupIds.Sort();
            for (int gi = 0; gi < groupIds.Count; gi++)
            {
                var cells = groupMap[groupIds[gi]]; cells.Sort();
                sb.Append("[");
                for (int k = 0; k < cells.Count; k++)
                {
                    if (k > 0) sb.Append(",");
                    sb.Append(cells[k]);
                }
                sb.Append("]");
                if (gi < groupIds.Count - 1) sb.Append(", ");
            }
        }
        sb.Append("]\n");
    }

    private static int ComputeSpawnAtId(int placeAtId, int direction, int W, int H)
    {
        int row = placeAtId / W, col = placeAtId % W;
        int dr = 0, dc = 0;
        switch (direction) { case 0: dr = -1; break; case 1: dc = 1; break; case 2: dr = 1; break; case 3: dc = -1; break; }
        int nr = row + dr, nc = col + dc;
        if (nr < 0 || nr >= H || nc < 0 || nc >= W) return -1;
        return nr * W + nc;
    }

    // ============================================================
    // ============ HELPERS =======================================
    // ============================================================
    private void SaveFolderPref() => EditorPrefs.SetString(PREF_OUTPUT_FOLDER, outputFolder ?? "");

    private string EnsureOutputFolder()
    {
        if (!string.IsNullOrEmpty(outputFolder) && Directory.Exists(outputFolder)) return outputFolder;
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

    private void OnTextureChanged()
    {
        if (sampledTex != null) { DestroyImmediate(sampledTex); sampledTex = null; }
        colorRows = null;
        tableMode = "";

        if (sourceTexture != null && EnsureReadable(sourceTexture))
        {
            var d = DetectLogicalSize(sourceTexture);
            gridX = d.x; gridY = d.y;
        }
    }

    private static void DrawHorzLine()
    {
        Rect r = EditorGUILayout.GetControlRect(false, 2f);
        EditorGUI.DrawRect(r, new Color(0.45f, 0.45f, 0.45f, 0.6f));
    }

    private static void DrawBorder(Rect r, Color c, int w)
    {
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, w), c);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - w, r.width, w), c);
        EditorGUI.DrawRect(new Rect(r.x, r.y, w, r.height), c);
        EditorGUI.DrawRect(new Rect(r.xMax - w, r.y, w, r.height), c);
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
                    blockW = GCD(blockW, x); prev = c;
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
                    blockH = GCD(blockH, y); prev = c;
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
}