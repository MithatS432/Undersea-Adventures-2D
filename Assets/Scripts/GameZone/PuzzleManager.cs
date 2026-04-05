using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchInfo
{
    public List<Tile> tiles;
    public bool isHorizontal;
}

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance;
    public bool IsProcessing { get; set; } = false;

    public GameObject[] tilePrefabs;
    public int gridWidth = 5;
    public int gridHeight = 5;
    public float spacing = 0.1f;
    public Vector2 startPosition = new Vector2(-2.42f, -3.46f);

    public Tile[,] grid;
    private Vector3 tileSize;
    [Range(0.05f, 1f)] public float animSpeed = 1f;

    private Vector2 lastSwapDirection;
    private Tile lastSwappedTile;

    private float idleTimer = 0f;
    private float hintDelay = 5f;
    private bool isShowingHint = false;
    private List<Tile> hintTiles = new List<Tile>();

    public GameObject horizontalSpecialPrefab;
    public GameObject verticalSpecialPrefab;
    public GameObject smallBombPrefab;
    public GameObject largeBombPrefab;
    public GameObject colorBombPrefab;

    public Material lightLineMaterial;

    private bool hammerMode = false;
    private System.Action onHammerUsed;
    public bool IsHammerMode => hammerMode;

    private const int GOLD_NORMAL_MATCH = 10;
    private const int GOLD_HORIZONTAL = 20;
    private const int GOLD_VERTICAL = 20;
    private const int GOLD_SMALL_BOMB = 30;
    private const int GOLD_LARGE_BOMB = 50;
    private const int GOLD_COLOR_BOMB_PER_TILE = 5;

    void Awake() { Instance = this; }

    void Start()
    {
        grid = new Tile[gridWidth, gridHeight];
        tileSize = GetTileSize(tilePrefabs[0]);
        CreateGrid();
        StartCoroutine(CheckInitialMatches());
    }
    void Update()
    {
        if (IsProcessing || isShowingHint)
        {
            idleTimer = 0f;
            return;
        }

        idleTimer += Time.deltaTime;

        if (idleTimer >= hintDelay)
        {
            idleTimer = 0f;
            ShowHint();
        }
    }
    void ShowHint()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Tile tile = grid[x, y];
                if (tile == null) continue;

                if (x < gridWidth - 1)
                {
                    Tile other = grid[x + 1, y];
                    if (CheckSwapCreatesMatch(tile, other))
                    {
                        StartCoroutine(PlayHint(tile, other));
                        return;
                    }
                }

                if (y < gridHeight - 1)
                {
                    Tile other = grid[x, y + 1];
                    if (CheckSwapCreatesMatch(tile, other))
                    {
                        StartCoroutine(PlayHint(tile, other));
                        return;
                    }
                }
            }
        }
    }

    IEnumerator PlayHint(Tile a, Tile b)
    {
        isShowingHint = true;
        hintTiles = new List<Tile> { a, b };

        Vector3 posA = a.transform.localPosition;
        Vector3 posB = b.transform.localPosition;

        // İkisi arasındaki mesafenin %30'u kadar yaklaş
        Vector3 midA = Vector3.Lerp(posA, posB, 0.3f);
        Vector3 midB = Vector3.Lerp(posB, posA, 0.3f);

        // 3 kez swap yapıyormuş gibi göster
        for (int i = 0; i < 3; i++)
        {
            if (a == null || b == null) break;

            Coroutine moveA = StartCoroutine(MoveTile(a, midA, 0.2f));
            Coroutine moveB = StartCoroutine(MoveTile(b, midB, 0.2f));
            yield return moveA;
            yield return moveB;

            yield return new WaitForSeconds(0.05f);

            Coroutine backA = StartCoroutine(MoveTile(a, posA, 0.2f));
            Coroutine backB = StartCoroutine(MoveTile(b, posB, 0.2f));
            yield return backA;
            yield return backB;

            yield return new WaitForSeconds(0.2f);
        }

        hintTiles.Clear();
        isShowingHint = false;
    }
    IEnumerator CheckInitialMatches()
    {
        IsProcessing = true;
        var matches = FindMatchesDetailed();
        if (matches.Count > 0)
            yield return StartCoroutine(ProcessMatchesDetailed(matches));
        IsProcessing = false;
    }

    void CreateGrid()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                SpawnTile(x, y);
    }

    void SpawnTile(int x, int y)
    {
        int randomIndex = Random.Range(0, tilePrefabs.Length);
        GameObject tileObj = Instantiate(tilePrefabs[randomIndex], transform);

        float posX = startPosition.x + x * (tileSize.x + spacing);
        float posY = startPosition.y + y * (tileSize.y + spacing);
        tileObj.transform.localPosition = new Vector3(posX, posY, 0);

        Tile tile = tileObj.GetComponent<Tile>();
        tile.x = x;
        tile.y = y;
        tile.type = randomIndex;
        tile.isSpecial = false;

        grid[x, y] = tile;
    }
    public void TrySwap(Tile tile, Vector2 dir)
    {
        idleTimer = 0f;
        isShowingHint = false;
        StopAllHints();

        if (IsProcessing) return;

        int targetX = tile.x + (int)dir.x;
        int targetY = tile.y + (int)dir.y;

        if (targetX < 0 || targetX >= gridWidth || targetY < 0 || targetY >= gridHeight)
            return;

        Tile other = grid[targetX, targetY];
        if (other == null) return;

        lastSwapDirection = dir;
        lastSwappedTile = tile;

        StartCoroutine(SwapRoutine(tile, other));
    }

    IEnumerator SwapRoutine(Tile a, Tile b)
    {
        if (a == null || b == null) yield break;

        IsProcessing = true;

        if (MissionManager.Instance != null)
            MissionManager.Instance.UseMove();

        Vector3 posA = a.transform.localPosition;
        Vector3 posB = b.transform.localPosition;

        int tempX = a.x, tempY = a.y;
        a.x = b.x; a.y = b.y;
        b.x = tempX; b.y = tempY;

        grid[a.x, a.y] = a;
        grid[b.x, b.y] = b;

        yield return StartCoroutine(MoveTile(a, posB));
        yield return StartCoroutine(MoveTile(b, posA));

        // Özel tile swap kontrolü — swap yapılan iki tile'dan biri özel mi?
        if (a.isSpecial && !b.isSpecial)
        {
            int coord = a.isHorizontal ? a.y : a.x;
            Vector3 pos = a.transform.position;
            grid[a.x, a.y] = null;
            Destroy(a.gameObject);

            if (a.isHorizontal)
                yield return StartCoroutine(ActivateHorizontalPower(coord, pos));
            else
                yield return StartCoroutine(ActivateVerticalPower(coord, pos));

            // Sonra normal match kontrol et
            var afterMatches = FindMatchesDetailed();
            if (afterMatches.Count > 0)
                yield return StartCoroutine(ProcessMatchesDetailed(afterMatches));
            else
            {
                if (!HasPossibleMoves())
                    yield return StartCoroutine(ReshuffleGrid());
                else
                    IsProcessing = false;
            }
            yield break;
        }
        else if (!a.isSpecial && b.isSpecial)
        {
            int coord = b.isHorizontal ? b.y : b.x;
            Vector3 pos = b.transform.position;

            Debug.Log($"Özel tile koordinatı — x:{b.x} y:{b.y} isHorizontal:{b.isHorizontal} coord:{coord}");

            grid[b.x, b.y] = null;
            Destroy(b.gameObject);

            if (b.isHorizontal)
                yield return StartCoroutine(ActivateHorizontalPower(coord, pos));
            else
                yield return StartCoroutine(ActivateVerticalPower(coord, pos));

            var afterMatches = FindMatchesDetailed();
            if (afterMatches.Count > 0)
                yield return StartCoroutine(ProcessMatchesDetailed(afterMatches));
            else
            {
                if (!HasPossibleMoves())
                    yield return StartCoroutine(ReshuffleGrid());
                else
                    IsProcessing = false;
            }
            yield break;

        }
        else if (a.isSmallBomb && !b.isSmallBomb)
        {
            int bx = a.x, by = a.y;
            Vector3 pos = a.transform.position;
            grid[a.x, a.y] = null;
            Destroy(a.gameObject);

            yield return StartCoroutine(ActivateSmallBomb(bx, by, pos));
            var afterMatches = FindMatchesDetailed();
            if (afterMatches.Count > 0)
                yield return StartCoroutine(ProcessMatchesDetailed(afterMatches));
            else
            {
                if (!HasPossibleMoves()) yield return StartCoroutine(ReshuffleGrid());
                else IsProcessing = false;
            }
            yield break;
        }
        else if (!a.isSmallBomb && b.isSmallBomb)
        {
            int bx = b.x, by = b.y;
            Vector3 pos = b.transform.position;
            grid[b.x, b.y] = null;
            Destroy(b.gameObject);

            yield return StartCoroutine(ActivateSmallBomb(bx, by, pos));
            var afterMatches = FindMatchesDetailed();
            if (afterMatches.Count > 0)
                yield return StartCoroutine(ProcessMatchesDetailed(afterMatches));
            else
            {
                if (!HasPossibleMoves()) yield return StartCoroutine(ReshuffleGrid());
                else IsProcessing = false;
            }
            yield break;
        }
        else if (a.isLargeBomb && !b.isLargeBomb)
        {
            int bx = a.x, by = a.y;
            Vector3 pos = a.transform.position;
            grid[a.x, a.y] = null;
            Destroy(a.gameObject);

            yield return StartCoroutine(ActivateLargeBomb(bx, by, pos));
            var afterMatches = FindMatchesDetailed();
            if (afterMatches.Count > 0)
                yield return StartCoroutine(ProcessMatchesDetailed(afterMatches));
            else
            {
                if (!HasPossibleMoves()) yield return StartCoroutine(ReshuffleGrid());
                else IsProcessing = false;
            }
            yield break;
        }
        else if (!a.isLargeBomb && b.isLargeBomb)
        {
            int bx = b.x, by = b.y;
            Vector3 pos = b.transform.position;
            grid[b.x, b.y] = null;
            Destroy(b.gameObject);

            yield return StartCoroutine(ActivateLargeBomb(bx, by, pos));
            var afterMatches = FindMatchesDetailed();
            if (afterMatches.Count > 0)
                yield return StartCoroutine(ProcessMatchesDetailed(afterMatches));
            else
            {
                if (!HasPossibleMoves()) yield return StartCoroutine(ReshuffleGrid());
                else IsProcessing = false;
            }
            yield break;
        }
        else if (a.isColorBomb && !b.isColorBomb)
        {
            int bx = a.x, by = a.y;
            int targetType = b.type;
            Vector3 pos = a.transform.position;
            grid[a.x, a.y] = null;
            Destroy(a.gameObject);

            yield return StartCoroutine(ActivateColorBomb(bx, by, pos, targetType));
            var afterMatches = FindMatchesDetailed();
            if (afterMatches.Count > 0)
                yield return StartCoroutine(ProcessMatchesDetailed(afterMatches));
            else
            {
                if (!HasPossibleMoves()) yield return StartCoroutine(ReshuffleGrid());
                else IsProcessing = false;
            }
            yield break;
        }
        else if (!a.isColorBomb && b.isColorBomb)
        {
            int bx = b.x, by = b.y;
            int targetType = a.type;
            Vector3 pos = b.transform.position;
            grid[b.x, b.y] = null;
            Destroy(b.gameObject);

            yield return StartCoroutine(ActivateColorBomb(bx, by, pos, targetType));
            var afterMatches = FindMatchesDetailed();
            if (afterMatches.Count > 0)
                yield return StartCoroutine(ProcessMatchesDetailed(afterMatches));
            else
            {
                if (!HasPossibleMoves()) yield return StartCoroutine(ReshuffleGrid());
                else IsProcessing = false;
            }
            yield break;
        }

        // Normal match kontrolü
        var matches = FindMatchesDetailed();
        if (matches.Count > 0)
        {
            yield return new WaitForSeconds(0.05f);
            yield return StartCoroutine(ProcessMatchesDetailed(matches));
        }
        else
        {
            // Geri al
            tempX = a.x; tempY = a.y;
            a.x = b.x; a.y = b.y;
            b.x = tempX; b.y = tempY;

            grid[a.x, a.y] = a;
            grid[b.x, b.y] = b;

            yield return StartCoroutine(MoveTile(a, posA));
            yield return StartCoroutine(MoveTile(b, posB));

            IsProcessing = false;
        }
    }
    void StopAllHints()
    {
        foreach (var t in hintTiles)
        {
            if (t != null)
                t.transform.localScale = Vector3.one;
        }
        hintTiles.Clear();
    }

    // ── Match bulma ──────────────────────────────────────────────

    List<MatchInfo> FindMatchesDetailed()
    {
        var result = new List<MatchInfo>();
        var processed = new HashSet<string>();

        // Yatay
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Tile cur = grid[x, y];
                if (cur == null || cur.isSpecial || cur.isSmallBomb || cur.isLargeBomb || cur.isColorBomb) continue;

                var row = new List<Tile> { cur };
                for (int i = x + 1; i < gridWidth; i++)
                {
                    Tile nxt = grid[i, y];
                    if (nxt == null || nxt.isSpecial || nxt.isSmallBomb || nxt.isLargeBomb || nxt.isColorBomb) break;
                    if (nxt.type == cur.type) row.Add(nxt);
                    else break;
                }

                if (row.Count >= 3)
                {
                    string key = $"H_{y}_{x}_{row.Count}";
                    if (!processed.Contains(key))
                    {
                        result.Add(new MatchInfo { tiles = row, isHorizontal = true });
                        processed.Add(key);
                    }
                    x += row.Count - 1;
                }
            }
        }

        // Dikey
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Tile cur = grid[x, y];
                if (cur == null || cur.isSpecial || cur.isSmallBomb || cur.isLargeBomb || cur.isColorBomb) continue;

                var col = new List<Tile> { cur };
                for (int i = y + 1; i < gridHeight; i++)
                {
                    Tile nxt = grid[x, i];
                    if (nxt == null || nxt.isSpecial || nxt.isSmallBomb || nxt.isLargeBomb || nxt.isColorBomb) break;
                    if (nxt.type == cur.type) col.Add(nxt);
                    else break;
                }

                if (col.Count >= 3)
                {
                    string key = $"V_{x}_{y}_{col.Count}";
                    if (!processed.Contains(key))
                    {
                        result.Add(new MatchInfo { tiles = col, isHorizontal = false });
                        processed.Add(key);
                    }
                    y += col.Count - 1;
                }
            }
        }

        // 2x2 kare
        for (int x = 0; x < gridWidth - 1; x++)
        {
            for (int y = 0; y < gridHeight - 1; y++)
            {
                Tile t1 = grid[x, y], t2 = grid[x + 1, y];
                Tile t3 = grid[x, y + 1], t4 = grid[x + 1, y + 1];

                if (t1 == null || t2 == null || t3 == null || t4 == null) continue;
                if (t1.isSpecial || t2.isSpecial || t3.isSpecial || t4.isSpecial) continue;
                if (t1.isSmallBomb || t2.isSmallBomb || t3.isSmallBomb || t4.isSmallBomb) continue;
                if (t1.isLargeBomb || t2.isLargeBomb || t3.isLargeBomb || t4.isLargeBomb) continue;
                if (t1.isColorBomb || t2.isColorBomb || t3.isColorBomb || t4.isColorBomb) continue;

                if (t1.type == t2.type && t1.type == t3.type && t1.type == t4.type)
                {
                    string key = $"S_{x}_{y}";
                    if (!processed.Contains(key))
                    {
                        result.Add(new MatchInfo
                        {
                            tiles = new List<Tile> { t1, t2, t3, t4 },
                            isHorizontal = false
                        });
                        processed.Add(key);
                    }
                }
            }
        }
        // L ve T şekli (5 tile) kontrolü
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Tile center = grid[x, y];
                if (center == null || center.isSpecial || center.isSmallBomb || center.isLargeBomb) continue;

                int t = center.type;

                // Yardımcı fonksiyon — tile var mı ve aynı type mı
                bool Has(int cx, int cy) =>
                    cx >= 0 && cx < gridWidth && cy >= 0 && cy < gridHeight &&
                    grid[cx, cy] != null && !grid[cx, cy].isSpecial &&
                    !grid[cx, cy].isSmallBomb && !grid[cx, cy].isLargeBomb &&
                    grid[cx, cy].type == t;

                Tile Get(int cx, int cy) => grid[cx, cy];

                List<Tile> shape = null;
                string shapeKey = null;

                // ── T şekilleri (5 tile) ──
                if (Has(x - 1, y) && Has(x + 1, y) && Has(x, y + 1) && Has(x, y + 2))
                { shape = new List<Tile> { center, Get(x - 1, y), Get(x + 1, y), Get(x, y + 1), Get(x, y + 2) }; shapeKey = $"T_{x}_{y}_U"; }

                else if (Has(x - 1, y) && Has(x + 1, y) && Has(x, y - 1) && Has(x, y - 2))
                { shape = new List<Tile> { center, Get(x - 1, y), Get(x + 1, y), Get(x, y - 1), Get(x, y - 2) }; shapeKey = $"T_{x}_{y}_D"; }

                else if (Has(x, y + 1) && Has(x, y - 1) && Has(x + 1, y) && Has(x + 2, y))
                { shape = new List<Tile> { center, Get(x, y + 1), Get(x, y - 1), Get(x + 1, y), Get(x + 2, y) }; shapeKey = $"T_{x}_{y}_R"; }

                else if (Has(x, y + 1) && Has(x, y - 1) && Has(x - 1, y) && Has(x - 2, y))
                { shape = new List<Tile> { center, Get(x, y + 1), Get(x, y - 1), Get(x - 1, y), Get(x - 2, y) }; shapeKey = $"T_{x}_{y}_L"; }

                // ── L şekilleri (5 tile) ──
                else if (Has(x - 1, y) && Has(x + 1, y) && Has(x + 1, y + 1) && Has(x + 1, y + 2))
                { shape = new List<Tile> { center, Get(x - 1, y), Get(x + 1, y), Get(x + 1, y + 1), Get(x + 1, y + 2) }; shapeKey = $"L_{x}_{y}_RU"; }

                else if (Has(x - 1, y) && Has(x + 1, y) && Has(x + 1, y - 1) && Has(x + 1, y - 2))
                { shape = new List<Tile> { center, Get(x - 1, y), Get(x + 1, y), Get(x + 1, y - 1), Get(x + 1, y - 2) }; shapeKey = $"L_{x}_{y}_RD"; }

                else if (Has(x - 1, y) && Has(x + 1, y) && Has(x - 1, y + 1) && Has(x - 1, y + 2))
                { shape = new List<Tile> { center, Get(x - 1, y), Get(x + 1, y), Get(x - 1, y + 1), Get(x - 1, y + 2) }; shapeKey = $"L_{x}_{y}_LU"; }

                else if (Has(x - 1, y) && Has(x + 1, y) && Has(x - 1, y - 1) && Has(x - 1, y - 2))
                { shape = new List<Tile> { center, Get(x - 1, y), Get(x + 1, y), Get(x - 1, y - 1), Get(x - 1, y - 2) }; shapeKey = $"L_{x}_{y}_LD"; }

                else if (Has(x, y - 1) && Has(x, y + 1) && Has(x + 1, y + 1) && Has(x + 2, y + 1))
                { shape = new List<Tile> { center, Get(x, y - 1), Get(x, y + 1), Get(x + 1, y + 1), Get(x + 2, y + 1) }; shapeKey = $"L_{x}_{y}_UR"; }

                else if (Has(x, y - 1) && Has(x, y + 1) && Has(x - 1, y + 1) && Has(x - 2, y + 1))
                { shape = new List<Tile> { center, Get(x, y - 1), Get(x, y + 1), Get(x - 1, y + 1), Get(x - 2, y + 1) }; shapeKey = $"L_{x}_{y}_UL"; }

                else if (Has(x, y - 1) && Has(x, y + 1) && Has(x + 1, y - 1) && Has(x + 2, y - 1))
                { shape = new List<Tile> { center, Get(x, y - 1), Get(x, y + 1), Get(x + 1, y - 1), Get(x + 2, y - 1) }; shapeKey = $"L_{x}_{y}_DR"; }

                else if (Has(x, y - 1) && Has(x, y + 1) && Has(x - 1, y - 1) && Has(x - 2, y - 1))
                { shape = new List<Tile> { center, Get(x, y - 1), Get(x, y + 1), Get(x - 1, y - 1), Get(x - 2, y - 1) }; shapeKey = $"L_{x}_{y}_DL"; }

                if (shape != null && shapeKey != null && !processed.Contains(shapeKey))
                {
                    result.Add(new MatchInfo { tiles = shape, isHorizontal = false });
                    processed.Add(shapeKey);
                }
            }
        }
        return result;
    }

    List<Tile> FindMatches()
    {
        var set = new HashSet<Tile>();
        foreach (var mi in FindMatchesDetailed())
            foreach (var t in mi.tiles)
                set.Add(t);
        return new List<Tile>(set);
    }

    // ── İşleme ───────────────────────────────────────────────────

    IEnumerator ProcessMatchesDetailed(List<MatchInfo> matches)
    {
        IsProcessing = true;

        var tilesToClear = new HashSet<Tile>();
        var specialTrigger = new List<(int x, int y, Vector3 pos, bool isSmallBomb, bool isLargeBomb, bool isColorBomb, bool isHorizontal)>();
        var specialCreate = new List<(int sx, int sy, bool isHorizontal, string specialType)>();

        foreach (var match in matches)
        {
            // L veya T şekli (5 tile) → color bomb
            if (match.tiles.Count == 5)
            {
                Tile anchor = GetBestAnchor(match.tiles);
                specialCreate.Add((anchor.x, anchor.y, match.isHorizontal, "colorbomb"));
                foreach (var t in match.tiles) tilesToClear.Add(t);
                continue;
            }

            // 2x2 kare → directional
            if (match.tiles.Count == 4 && IsSquareMatch(match.tiles))
            {
                bool isHoriz = Mathf.Abs(lastSwapDirection.x) > Mathf.Abs(lastSwapDirection.y);
                Tile anchor = GetBestAnchor(match.tiles);
                specialCreate.Add((anchor.x, anchor.y, isHoriz, "directional"));
                foreach (var t in match.tiles) tilesToClear.Add(t);
                continue;
            }

            // 4'lü yatay → small bomb
            if (match.tiles.Count == 4 && match.isHorizontal)
            {
                Tile anchor = GetBestAnchor(match.tiles);
                specialCreate.Add((anchor.x, anchor.y, match.isHorizontal, "smallbomb"));
                foreach (var t in match.tiles) tilesToClear.Add(t);
                continue;
            }

            // 4'lü dikey → large bomb
            if (match.tiles.Count == 4 && !match.isHorizontal)
            {
                Tile anchor = GetBestAnchor(match.tiles);
                specialCreate.Add((anchor.x, anchor.y, match.isHorizontal, "largebomb"));
                foreach (var t in match.tiles) tilesToClear.Add(t);
                continue;
            }

            // Normal 3'lü match
            foreach (var t in match.tiles)
            {
                if (t.isSpecial || t.isSmallBomb || t.isLargeBomb || t.isColorBomb)
                {
                    specialTrigger.Add((t.x, t.y, t.transform.position, t.isSmallBomb, t.isLargeBomb, t.isColorBomb, t.isHorizontal));
                    tilesToClear.Add(t);
                }
                else
                {
                    tilesToClear.Add(t);
                }
            }
        }

        if (tilesToClear.Count > 0)
            yield return StartCoroutine(ClearMatches(new List<Tile>(tilesToClear)));

        yield return new WaitForSeconds(0.05f * animSpeed);

        foreach (var sc in specialCreate)
        {
            float px = startPosition.x + sc.sx * (tileSize.x + spacing);
            float py = startPosition.y + sc.sy * (tileSize.y + spacing);

            GameObject prefab = sc.specialType == "smallbomb" ? smallBombPrefab
    : sc.specialType == "largebomb" ? largeBombPrefab
    : sc.specialType == "colorbomb" ? colorBombPrefab
    : sc.isHorizontal ? horizontalSpecialPrefab : verticalSpecialPrefab;

            if (prefab == null) continue;

            GameObject obj = Instantiate(prefab, transform);
            obj.transform.localPosition = new Vector3(px, py, 0);

            Tile newTile = obj.GetComponent<Tile>();
            newTile.x = sc.sx;
            newTile.y = sc.sy;
            newTile.type = -1;

            if (sc.specialType == "smallbomb")
            {
                newTile.isSmallBomb = true;
                newTile.isSpecial = false;
            }
            else if (sc.specialType == "largebomb")
            {
                newTile.isLargeBomb = true;
                newTile.isSpecial = false;
            }
            else if (sc.specialType == "colorbomb")
            {
                newTile.isColorBomb = true;
                newTile.isSpecial = false;
            }
            else
            {
                newTile.isSpecial = true;
                newTile.isHorizontal = sc.isHorizontal;
                newTile.isVertical = !sc.isHorizontal;
            }

            grid[sc.sx, sc.sy] = newTile;
        }

        yield return StartCoroutine(CollapseColumns());
        yield return new WaitForSeconds(0.1f * animSpeed);
        yield return StartCoroutine(RefillGrid());
        yield return new WaitForSeconds(0.1f * animSpeed);

        foreach (var st in specialTrigger)
        {
            if (st.isSmallBomb)
                yield return StartCoroutine(ActivateSmallBomb(st.x, st.y, st.pos));
            else if (st.isLargeBomb)
                yield return StartCoroutine(ActivateLargeBomb(st.x, st.y, st.pos));
            else if (st.isHorizontal)
                yield return StartCoroutine(ActivateHorizontalPower(st.y, st.pos));
            else if (st.isColorBomb)
                yield return StartCoroutine(ActivateColorBomb(st.x, st.y, st.pos));
            else
                yield return StartCoroutine(ActivateVerticalPower(st.x, st.pos));
        }

        var next = FindMatchesDetailed();
        if (next.Count > 0)
            yield return StartCoroutine(ProcessMatchesDetailed(next));
        else
        {
            if (!HasPossibleMoves())
                yield return StartCoroutine(ReshuffleGrid());
            else
                IsProcessing = false;
        }
    }
    Tile GetBestAnchor(List<Tile> tiles)
    {
        if (lastSwappedTile != null && tiles.Contains(lastSwappedTile))
            return lastSwappedTile;

        if (lastSwappedTile != null)
        {
            int tx = lastSwappedTile.x + (int)lastSwapDirection.x;
            int ty = lastSwappedTile.y + (int)lastSwapDirection.y;

            foreach (var t in tiles)
            {
                if (t.x == tx && t.y == ty)
                    return t;
            }
        }

        return tiles[0];
    }
    IEnumerator ClearMatches(List<Tile> matches)
    {
        foreach (var tile in matches)
        {
            if (tile == null || tile.gameObject == null) continue;

            if (tile.x >= 0 && tile.x < gridWidth &&
                tile.y >= 0 && tile.y < gridHeight &&
                grid[tile.x, tile.y] == tile)
            {
                grid[tile.x, tile.y] = null;
            }

            if (MissionManager.Instance != null)
                MissionManager.Instance.OnTileDestroyed(tile.type);

            if (SoundAndEffectManager.Instance != null)
                SoundAndEffectManager.Instance.AddToMatchQueue(tile.transform.position);

            if (GoldManager.Instance != null)
                GoldManager.Instance.SpawnGoldFromPosition(tile.transform.position, GOLD_NORMAL_MATCH);

            Destroy(tile.gameObject);
        }
        yield return null;
    }

    // ── Gravity ──────────────────────────────────────────────────

    IEnumerator CollapseColumns()
    {
        var routines = new List<Coroutine>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] != null) continue;

                for (int above = y + 1; above < gridHeight; above++)
                {
                    if (grid[x, above] == null) continue;

                    Tile tile = grid[x, above];
                    grid[x, y] = tile;
                    grid[x, above] = null;
                    tile.y = y;

                    Vector3 target = new Vector3(
                        startPosition.x + x * (tileSize.x + spacing),
                        startPosition.y + y * (tileSize.y + spacing), 0);

                    routines.Add(StartCoroutine(MoveTile(tile, target, 0.15f)));
                    break;
                }
            }
        }

        foreach (var r in routines) yield return r;
    }

    // ── Refill ───────────────────────────────────────────────────

    IEnumerator RefillGrid()
    {
        var routines = new List<Coroutine>();

        for (int x = 0; x < gridWidth; x++)
        {
            int missing = 0;
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] != null) continue;

                missing++;
                int idx = Random.Range(0, tilePrefabs.Length);
                GameObject obj = Instantiate(tilePrefabs[idx], transform);

                float tx = startPosition.x + x * (tileSize.x + spacing);
                float ty = startPosition.y + y * (tileSize.y + spacing);
                float spawnY = startPosition.y + (gridHeight + missing) * (tileSize.y + spacing);

                obj.transform.localPosition = new Vector3(tx, spawnY, 0);

                Tile tile = obj.GetComponent<Tile>();
                tile.x = x;
                tile.y = y;
                tile.type = idx;
                tile.isSpecial = false;
                grid[x, y] = tile;

                routines.Add(StartCoroutine(MoveTile(tile, new Vector3(tx, ty, 0), 0.3f)));
            }
        }

        foreach (var r in routines) yield return r;
    }

    // ── Special Tiles ────────────────────────────────────────────

    public IEnumerator ActivateHorizontalPower(int row, Vector3 effectPos)
    {
        if (SoundAndEffectManager.Instance != null)
            SoundAndEffectManager.Instance.PlayHorizontalPower(effectPos);

        int clearedCount = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            if (grid[x, row] == null) continue;
            Tile t = grid[x, row];
            grid[x, row] = null;

            if (GoldManager.Instance != null)
                GoldManager.Instance.SpawnGoldFromPosition(t.transform.position, GOLD_HORIZONTAL);

            if (MissionManager.Instance != null)
                MissionManager.Instance.OnTileDestroyed(t.type);

            if (t.gameObject != null) Destroy(t.gameObject);
            clearedCount++;
        }

        if (GoldManager.Instance != null)
            GoldManager.Instance.AddGold(GOLD_HORIZONTAL);

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(CollapseColumns());
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(RefillGrid());
        yield return new WaitForSeconds(0.1f);
    }

    public IEnumerator ActivateVerticalPower(int col, Vector3 effectPos)
    {
        if (SoundAndEffectManager.Instance != null)
            SoundAndEffectManager.Instance.PlayVerticalPower(effectPos);

        for (int y = 0; y < gridHeight; y++)
        {
            if (grid[col, y] == null) continue;
            Tile t = grid[col, y];
            grid[col, y] = null;
            if (GoldManager.Instance != null)
                GoldManager.Instance.SpawnGoldFromPosition(t.transform.position, GOLD_VERTICAL);

            if (MissionManager.Instance != null)
                MissionManager.Instance.OnTileDestroyed(t.type);

            if (t.gameObject != null) Destroy(t.gameObject);
        }

        if (GoldManager.Instance != null)
            GoldManager.Instance.AddGold(GOLD_VERTICAL);

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(CollapseColumns());
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(RefillGrid());
        yield return new WaitForSeconds(0.1f);
    }

    // ── Yardımcılar ──────────────────────────────────────────────

    bool HasPossibleMoves()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == null) continue;
                if (x < gridWidth - 1 && CheckSwapCreatesMatch(grid[x, y], grid[x + 1, y])) return true;
                if (y < gridHeight - 1 && CheckSwapCreatesMatch(grid[x, y], grid[x, y + 1])) return true;
            }
        }
        return false;
    }

    bool CheckSwapCreatesMatch(Tile a, Tile b)
    {
        if (a == null || b == null) return false;
        if (a.isSpecial || b.isSpecial) return true;
        if (a.isSmallBomb || b.isSmallBomb) return true;
        if (a.isLargeBomb || b.isLargeBomb) return true;
        if (a.isColorBomb || b.isColorBomb) return true;


        int ta = a.type, tb = b.type;
        a.type = tb; b.type = ta;
        bool has = FindMatchesDetailed().Count > 0;
        a.type = ta; b.type = tb;
        return has;
    }

    bool IsSquareMatch(List<Tile> tiles)
    {
        if (tiles.Count != 4) return false;
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var t in tiles)
        {
            if (t.x < minX) minX = t.x;
            if (t.x > maxX) maxX = t.x;
            if (t.y < minY) minY = t.y;
            if (t.y > maxY) maxY = t.y;
        }
        return maxX - minX == 1 && maxY - minY == 1;
    }

    Vector3 GetTileSize(GameObject tile)
    {
        return tile.GetComponent<SpriteRenderer>().bounds.size;
    }

    IEnumerator MoveTile(Tile tile, Vector3 target, float duration = 0.1f)
    {
        if (tile == null || tile.gameObject == null) yield break;
        Vector3 start = tile.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (tile == null || tile.gameObject == null) yield break;
            tile.transform.localPosition = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (tile != null && tile.gameObject != null)
            tile.transform.localPosition = target;
    }

    IEnumerator ReshuffleGrid()
    {
        var all = new List<Tile>();
        foreach (var t in grid) if (t != null) all.Add(t);

        var shrink = new List<Coroutine>();
        foreach (var t in all) shrink.Add(StartCoroutine(ScaleTile(t, 1f, 0.8f, 0.1f)));
        foreach (var r in shrink) yield return r;

        yield return new WaitForSeconds(0.05f);

        int attempts = 0;
        bool valid = false;
        while (!valid && attempts++ < 100)
        {
            for (int i = 0; i < all.Count; i++)
            {
                int r = Random.Range(0, all.Count);
                int tmp = all[i].type; all[i].type = all[r].type; all[r].type = tmp;
            }
            if (FindMatches().Count == 0 && HasPossibleMoves()) valid = true;
        }

        foreach (var t in all)
            t.GetComponent<SpriteRenderer>().sprite = tilePrefabs[t.type].GetComponent<SpriteRenderer>().sprite;

        var shake = new List<Coroutine>();
        foreach (var t in all) shake.Add(StartCoroutine(ShakeTile(t, 0.1f, 5f)));
        foreach (var r in shake) yield return r;

        var expand = new List<Coroutine>();
        foreach (var t in all) expand.Add(StartCoroutine(ScaleTile(t, 0.8f, 1f, 0.1f)));
        foreach (var r in expand) yield return r;

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                if (grid[x, y] != null) { grid[x, y].x = x; grid[x, y].y = y; }

        IsProcessing = false;
    }

    IEnumerator ScaleTile(Tile tile, float from, float to, float dur)
    {
        if (tile == null) yield break;
        float e = 0f;
        while (e < dur)
        {
            if (tile == null) yield break;
            tile.transform.localScale = Vector3.one * Mathf.Lerp(from, to, e / dur);
            e += Time.deltaTime;
            yield return null;
        }
        if (tile != null) tile.transform.localScale = Vector3.one * to;
    }

    IEnumerator ShakeTile(Tile tile, float intensity, float speed)
    {
        if (tile == null) yield break;
        Vector3 orig = tile.transform.localPosition;
        float e = 0f, dur = 0.2f;
        while (e < dur)
        {
            if (tile == null) yield break;
            tile.transform.localPosition = orig + new Vector3(
                Mathf.Sin(Time.time * speed) * intensity,
                Mathf.Cos(Time.time * speed * 1.5f) * intensity, 0);
            e += Time.deltaTime;
            yield return null;
        }
        if (tile != null) tile.transform.localPosition = orig;
    }
    public IEnumerator ActivateSmallBomb(int col, int row, Vector3 effectPos)
    {
        if (SoundAndEffectManager.Instance != null)
            SoundAndEffectManager.Instance.PlaySmallBomb(effectPos);

        for (int x = col - 1; x <= col + 1; x++)
            for (int y = row - 1; y <= row + 1; y++)
            {
                if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) continue;
                if (grid[x, y] == null) continue;
                Tile t = grid[x, y];
                grid[x, y] = null;

                if (GoldManager.Instance != null)
                    GoldManager.Instance.SpawnGoldFromPosition(t.transform.position, GOLD_SMALL_BOMB);

                if (MissionManager.Instance != null)
                    MissionManager.Instance.OnTileDestroyed(t.type);

                if (t.gameObject != null) Destroy(t.gameObject);
            }

        if (GoldManager.Instance != null)
            GoldManager.Instance.AddGold(GOLD_SMALL_BOMB);

        yield return new WaitForSeconds(0.1f * animSpeed);
        yield return StartCoroutine(CollapseColumns());
        yield return new WaitForSeconds(0.1f * animSpeed);
        yield return StartCoroutine(RefillGrid());
        yield return new WaitForSeconds(0.1f * animSpeed);
    }
    public IEnumerator ActivateLargeBomb(int col, int row, Vector3 effectPos)
    {
        if (SoundAndEffectManager.Instance != null)
            SoundAndEffectManager.Instance.PlayLargeBomb(effectPos);

        for (int x = col - 2; x <= col + 2; x++)
            for (int y = row - 2; y <= row + 2; y++)
            {
                if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) continue;
                if (grid[x, y] == null) continue;
                Tile t = grid[x, y];
                grid[x, y] = null;

                if (GoldManager.Instance != null)
                    GoldManager.Instance.SpawnGoldFromPosition(t.transform.position, GOLD_LARGE_BOMB);

                if (MissionManager.Instance != null)
                    MissionManager.Instance.OnTileDestroyed(t.type);

                if (t.gameObject != null) Destroy(t.gameObject);
            }

        if (GoldManager.Instance != null)
            GoldManager.Instance.AddGold(GOLD_LARGE_BOMB);

        yield return new WaitForSeconds(0.1f * animSpeed);
        yield return StartCoroutine(CollapseColumns());
        yield return new WaitForSeconds(0.1f * animSpeed);
        yield return StartCoroutine(RefillGrid());
        yield return new WaitForSeconds(0.1f * animSpeed);
    }
    public IEnumerator ActivateColorBomb(int col, int row, Vector3 effectPos, int targetType = -1)
    {
        if (SoundAndEffectManager.Instance != null)
            SoundAndEffectManager.Instance.PlayColorBomb(effectPos);

        if (targetType == -1)
        {
            Dictionary<int, int> typeCounts = new Dictionary<int, int>();
            foreach (var t in grid)
            {
                if (t == null || t.isSpecial || t.isSmallBomb || t.isLargeBomb || t.isColorBomb) continue;
                if (!typeCounts.ContainsKey(t.type)) typeCounts[t.type] = 0;
                typeCounts[t.type]++;
            }
            int maxCount = -1;
            foreach (var kv in typeCounts)
                if (kv.Value > maxCount) { maxCount = kv.Value; targetType = kv.Key; }
        }

        var targets = new List<Tile>();
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                if (grid[x, y] != null && grid[x, y].type == targetType)
                    targets.Add(grid[x, y]);

        // Sırayla her tile'a ışık gönder ve ulaşınca yok et
        foreach (var target in targets)
        {
            if (target == null || target.gameObject == null) continue;

            // Işık hedefe ulaşana kadar bekle
            yield return StartCoroutine(ShootLightToTarget(effectPos, target.transform.position));

            if (GoldManager.Instance != null)
                GoldManager.Instance.AddGold(targets.Count * GOLD_COLOR_BOMB_PER_TILE);

            // Ulaşınca yok et
            if (target != null && target.gameObject != null)
            {
                if (SoundAndEffectManager.Instance != null)
                    SoundAndEffectManager.Instance.AddToMatchQueue(target.transform.position);

                if (GoldManager.Instance != null)
                    GoldManager.Instance.SpawnGoldFromPosition(target.transform.position, GOLD_COLOR_BOMB_PER_TILE);

                if (MissionManager.Instance != null)
                    MissionManager.Instance.OnTileDestroyed(target.type);

                grid[target.x, target.y] = null;
                Destroy(target.gameObject);
            }

            yield return new WaitForSeconds(0.08f);
        }

        yield return new WaitForSeconds(0.1f * animSpeed);
        yield return StartCoroutine(CollapseColumns());
        yield return new WaitForSeconds(0.1f * animSpeed);
        yield return StartCoroutine(RefillGrid());
        yield return new WaitForSeconds(0.1f * animSpeed);
    }

    IEnumerator ShootLightToTarget(Vector3 from, Vector3 to)
    {
        GameObject lineObj = new GameObject("ColorBombLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lightLineMaterial != null ? lightLineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = 2;
        lr.sortingOrder = 10;
        lr.useWorldSpace = true;
        lr.SetPosition(0, from);
        lr.SetPosition(1, from);

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (lineObj == null) yield break;

            float t = elapsed / duration;
            float smooth = t * t * (3f - 2f * t); // ease in-out

            // Kalın ve görünür çizgi
            float thickness = 0.12f + Mathf.Sin(smooth * Mathf.PI) * 0.08f;
            lr.startWidth = thickness;
            lr.endWidth = thickness * 0.4f;

            // Parlak sarı → turuncu
            lr.startColor = new Color(1f, 0.95f, 0.1f, 1f);
            lr.endColor = new Color(1f, 0.5f, 0f, 0.5f);

            lr.SetPosition(0, from);
            lr.SetPosition(1, Vector3.Lerp(from, to, smooth));

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Hedefe tam ulaş
        lr.SetPosition(1, to);
        yield return new WaitForSeconds(0.05f);

        // Sönme
        float fadeDur = 0.12f;
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDur)
        {
            if (lineObj == null) yield break;
            float alpha = 1f - (fadeElapsed / fadeDur);
            lr.startColor = new Color(1f, 0.95f, 0.1f, alpha);
            lr.endColor = new Color(1f, 0.5f, 0f, alpha * 0.5f);
            fadeElapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(lineObj);
    }

    public void SetHammerMode(bool active, System.Action callback)
    {
        hammerMode = active;
        onHammerUsed = callback;
    }

    public void UseHammerOn(Tile tile)
    {
        if (!hammerMode) return;

        hammerMode = false;
        onHammerUsed?.Invoke();
        onHammerUsed = null;

        StartCoroutine(HammerRoutine(tile));
    }

    IEnumerator HammerRoutine(Tile tile)
    {
        IsProcessing = true;

        yield return StartCoroutine(FlashAndDestroy(tile));

        yield return new WaitForSeconds(0.1f * animSpeed);
        yield return StartCoroutine(CollapseColumns());
        yield return new WaitForSeconds(0.1f * animSpeed);
        yield return StartCoroutine(RefillGrid());
        yield return new WaitForSeconds(0.1f * animSpeed);

        var matches = FindMatchesDetailed();
        if (matches.Count > 0)
            yield return StartCoroutine(ProcessMatchesDetailed(matches));
        else
            IsProcessing = false;
    }

    IEnumerator FlashAndDestroy(Tile tile)
    {
        if (tile == null || tile.gameObject == null) yield break;

        if (SoundAndEffectManager.Instance != null)
            SoundAndEffectManager.Instance.PlayHammer(tile.transform.position);

        if (MissionManager.Instance != null)
            MissionManager.Instance.OnTileDestroyed(tile.type);


        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
        Vector3 originalScale = tile.transform.localScale;

        float dur = 0.12f;
        float e = 0f;
        while (e < dur)
        {
            if (tile == null) yield break;
            float t = e / dur;
            tile.transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.4f, t);
            e += Time.deltaTime;
            yield return null;
        }

        dur = 0.1f;
        e = 0f;
        Color originalColor = sr.color;
        while (e < dur)
        {
            if (tile == null) yield break;
            float t = e / dur;
            sr.color = Color.Lerp(originalColor, Color.white, t);
            e += Time.deltaTime;
            yield return null;
        }

        dur = 0.15f;
        e = 0f;
        while (e < dur)
        {
            if (tile == null) yield break;
            float t = e / dur;
            tile.transform.localScale = Vector3.Lerp(originalScale * 1.4f, Vector3.zero, t);
            sr.color = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0f), t);
            e += Time.deltaTime;
            yield return null;
        }

        grid[tile.x, tile.y] = null;
        Destroy(tile.gameObject);
    }

    public IEnumerator ActivateLightning()
    {
        IsProcessing = true;

        var allTiles = new List<Tile>();
        foreach (var t in grid)
            if (t != null && !t.isSpecial && !t.isSmallBomb && !t.isLargeBomb && !t.isColorBomb)
                allTiles.Add(t);

        int count = Mathf.Min(5, allTiles.Count);
        for (int i = 0; i < count; i++)
        {
            int rand = Random.Range(i, allTiles.Count);
            var tmp = allTiles[i]; allTiles[i] = allTiles[rand]; allTiles[rand] = tmp;
        }

        for (int i = 0; i < count; i++)
        {
            Tile t = allTiles[i];
            if (t == null || t.gameObject == null) continue;

            // Şimşek animasyonu
            yield return StartCoroutine(LightningStrike(t));

            if (MissionManager.Instance != null)
                MissionManager.Instance.OnTileDestroyed(t.type);


            grid[t.x, t.y] = null;
            Destroy(t.gameObject);

            yield return new WaitForSeconds(0.08f * animSpeed);
        }

        yield return new WaitForSeconds(0.1f * animSpeed);
        yield return StartCoroutine(CollapseColumns());
        yield return new WaitForSeconds(0.1f * animSpeed);
        yield return StartCoroutine(RefillGrid());
        yield return new WaitForSeconds(0.1f * animSpeed);

        var matches = FindMatchesDetailed();
        if (matches.Count > 0)
            yield return StartCoroutine(ProcessMatchesDetailed(matches));
        else
            IsProcessing = false;
    }

    IEnumerator LightningStrike(Tile tile)
    {
        if (tile == null || tile.gameObject == null) yield break;

        if (SoundAndEffectManager.Instance != null)
            SoundAndEffectManager.Instance.PlayLightning(tile.transform.position);

        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
        Color originalColor = sr.color;

        // Yukarıdan şimşek çizgisi indir
        Vector3 topPos = tile.transform.position + Vector3.up * 3f;
        yield return StartCoroutine(ShootLightningLine(topPos, tile.transform.position));

        // Tile'ı mavi-beyaz flash yap
        float dur = 0.06f;
        for (int flash = 0; flash < 3; flash++)
        {
            float e = 0f;
            while (e < dur)
            {
                if (tile == null) yield break;
                float t = e / dur;
                sr.color = flash % 2 == 0
                    ? Color.Lerp(originalColor, new Color(0.4f, 0.8f, 1f, 1f), t)
                    : Color.Lerp(new Color(0.4f, 0.8f, 1f, 1f), Color.white, t);
                e += Time.deltaTime;
                yield return null;
            }
        }

        if (tile != null)
            sr.color = originalColor;
    }

    IEnumerator ShootLightningLine(Vector3 from, Vector3 to)
    {
        GameObject lineObj = new GameObject("LightningLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lightLineMaterial != null ? lightLineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = 2;
        lr.sortingOrder = 10;
        lr.useWorldSpace = true;
        lr.startWidth = 0.06f;
        lr.endWidth = 0.02f;
        lr.startColor = new Color(0.5f, 0.9f, 1f, 1f);
        lr.endColor = new Color(1f, 1f, 1f, 0.8f);
        lr.SetPosition(0, from);
        lr.SetPosition(1, from);

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (lineObj == null) yield break;
            float t = elapsed / duration;
            lr.SetPosition(1, Vector3.Lerp(from, to, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        lr.SetPosition(1, to);
        yield return new WaitForSeconds(0.05f);
        Destroy(lineObj);
    }
    public IEnumerator ActivateMagicStar()
    {
        IsProcessing = true;

        if (SoundAndEffectManager.Instance != null)
            SoundAndEffectManager.Instance.PlayMagicStar();

        var allTiles = new List<Tile>();
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                if (grid[x, y] != null && !grid[x, y].isSpecial &&
                    !grid[x, y].isSmallBomb && !grid[x, y].isLargeBomb && !grid[x, y].isColorBomb)
                    allTiles.Add(grid[x, y]);

        // Renk geçişi — hepsi gökkuşağı rengine dön
        float rainbowDur = 0.4f;
        float e = 0f;
        var originalColors = new Dictionary<Tile, Color>();
        foreach (var t in allTiles)
            originalColors[t] = t.GetComponent<SpriteRenderer>().color;

        while (e < rainbowDur)
        {
            float progress = e / rainbowDur;
            foreach (var t in allTiles)
            {
                if (t == null) continue;
                // Her tile farklı bir gökkuşağı rengi alsın
                float hue = (progress + (t.x + t.y) * 0.1f) % 1f;
                t.GetComponent<SpriteRenderer>().color = Color.HSVToRGB(hue, 0.8f, 1f);
            }
            e += Time.deltaTime;
            yield return null;
        }

        // Karıştır — komşu swap'ları
        int swapCount = allTiles.Count * 2;
        for (int i = 0; i < swapCount; i++)
        {
            int randIdx = Random.Range(0, allTiles.Count);
            Tile tile = allTiles[randIdx];
            if (tile == null) continue;

            var neighbors = new List<Tile>();
            int tx = tile.x, ty = tile.y;
            if (tx > 0 && grid[tx - 1, ty] != null && !grid[tx - 1, ty].isSpecial) neighbors.Add(grid[tx - 1, ty]);
            if (tx < gridWidth - 1 && grid[tx + 1, ty] != null && !grid[tx + 1, ty].isSpecial) neighbors.Add(grid[tx + 1, ty]);
            if (ty > 0 && grid[tx, ty - 1] != null && !grid[tx, ty - 1].isSpecial) neighbors.Add(grid[tx, ty - 1]);
            if (ty < gridHeight - 1 && grid[tx, ty + 1] != null && !grid[tx, ty + 1].isSpecial) neighbors.Add(grid[tx, ty + 1]);

            if (neighbors.Count == 0) continue;


            Tile neighbor = neighbors[Random.Range(0, neighbors.Count)];
            if (tile.type < 0 || neighbor.type < 0) continue;

            int tmpType = tile.type;
            tile.type = neighbor.type;
            neighbor.type = tmpType;
        }

        // Sprite'ları güncelle ve normal renge dön
        float restoreDur = 0.3f;
        e = 0f;
        foreach (var t in allTiles)
        {
            if (t == null) continue;
            if (t.type < 0 || t.type >= tilePrefabs.Length) continue;
            t.GetComponent<SpriteRenderer>().sprite = tilePrefabs[t.type].GetComponent<SpriteRenderer>().sprite;
        }

        while (e < restoreDur)
        {
            float progress = e / restoreDur;
            foreach (var t in allTiles)
            {
                if (t == null) continue;
                float hue = ((1f - progress) + (t.x + t.y) * 0.1f) % 1f;
                Color targetColor = Color.white;
                t.GetComponent<SpriteRenderer>().color = Color.Lerp(
                    Color.HSVToRGB(hue, 0.8f, 1f), targetColor, progress);
            }
            e += Time.deltaTime;
            yield return null;
        }

        foreach (var t in allTiles)
            if (t != null)
                t.GetComponent<SpriteRenderer>().color = Color.white;

        yield return new WaitForSeconds(0.1f * animSpeed);

        var matches = FindMatchesDetailed();
        if (matches.Count > 0)
            yield return StartCoroutine(ProcessMatchesDetailed(matches));
        else
        {
            if (!HasPossibleMoves())
                yield return StartCoroutine(ReshuffleGrid());
            else
                IsProcessing = false;
        }
    }


    void DestroyTileWithEffects(Tile t, int goldAmount)
    {
        if (t == null || t.gameObject == null) return;

        if (MissionManager.Instance != null)
            MissionManager.Instance.OnTileDestroyed(t.type);

        if (SoundAndEffectManager.Instance != null)
            SoundAndEffectManager.Instance.AddToMatchQueue(t.transform.position);

        if (GoldManager.Instance != null)
            GoldManager.Instance.SpawnGoldFromPosition(t.transform.position, goldAmount);

        Destroy(t.gameObject);
    }
}