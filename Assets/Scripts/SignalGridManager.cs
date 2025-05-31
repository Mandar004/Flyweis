using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.IO;

public class SignalGridManager : MonoBehaviour
{
    public static SignalGridManager Instance;

    [Header("Grid Setup")]
    public int gridSize = 5;
    public GameObject tilePrefab;
    public Transform gridParent;
    public GridLayoutGroup layoutGroup;

    [Header("UI")]
    public TMP_Text timerText;
    public TMP_Text movesText;
    public TMP_Text hintText;
    public Button peekButton;
    public Button resetButton;
    public GameObject winPanel;

    [HideInInspector]
    public List<Tile> tiles = new List<Tile>();

    private int moves;
    private float timer;
    private bool gameRunning = true;
    private bool peekUsed = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GenerateGrid();
        AssignToggleRules();

        peekButton.onClick.AddListener(() => PeekRandomTile());
        resetButton.onClick.AddListener(ResetPuzzle);
    }

    private void Update()
    {
        if (!gameRunning) return;

        timer += Time.deltaTime;
        timerText.text = $"Time: {timer:F1}s";

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void GenerateGrid()
    {
        tiles.Clear();
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        float tileSize = 100f;
        layoutGroup.constraintCount = gridSize;
        layoutGroup.cellSize = new Vector2(tileSize, tileSize);

        for (int i = 0; i < gridSize * gridSize; i++)
        {
            GameObject tileObj = Instantiate(tilePrefab, gridParent);
            Tile tile = tileObj.GetComponent<Tile>();
            tile.tileIndex = i;
            tile.SetState(Random.value > 0.5f);
            tiles.Add(tile);
        }
    }

    public void AssignToggleRules()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].toggleTargets = GetRandomPattern(i);
        }
    }

    List<int> GetRandomPattern(int center)
    {
        List<int> pattern = new List<int>();
        int x = center % gridSize;
        int y = center / gridSize;

        int[][] knightMoves = new int[][]
        {
            new int[] { 1, 2 },
            new int[] { 2, 1 },
            new int[] { -1, 2 },
            new int[] { -2, 1 },
            new int[] { 1, -2 },
            new int[] { 2, -1 },
            new int[] { -1, -2 },
            new int[] { -2, -1 }
        };

        int choice = Random.Range(0, 5);
        pattern.Add(center); // Always include self

        switch (choice)
        {
            case 0: // Knight
                foreach (var move in knightMoves)
                {
                    int nx = x + move[0];
                    int ny = y + move[1];
                    if (nx >= 0 && ny >= 0 && nx < gridSize && ny < gridSize)
                        pattern.Add(ny * gridSize + nx);
                }
                break;

            case 1: // Same row
                for (int i = 0; i < gridSize; i++)
                    pattern.Add(y * gridSize + i);
                break;

            case 2: // Diagonals
                for (int dx = -gridSize; dx <= gridSize; dx++)
                {
                    int nx = x + dx;
                    int ny = y + dx;
                    if (nx >= 0 && ny >= 0 && nx < gridSize && ny < gridSize)
                        pattern.Add(ny * gridSize + nx);

                    nx = x + dx;
                    ny = y - dx;
                    if (nx >= 0 && ny >= 0 && nx < gridSize && ny < gridSize)
                        pattern.Add(ny * gridSize + nx);
                }
                break;

            case 3: // Same column
                for (int i = 0; i < gridSize; i++)
                    pattern.Add(i * gridSize + x);
                break;

            case 4: // Only self (decoy)
                // Already added center
                break;
        }

        return pattern.Distinct().ToList();
    }

    public void ToggleGroup(List<int> indexes)
    {
        foreach (int i in indexes)
        {
            tiles[i].FlipState();
            StartCoroutine(tiles[i].FlashEffect());
        }

        moves++;
        movesText.text = $"Moves: {moves}";
        CheckWinCondition();
    }

    public void CheckWinCondition()
    {
        if (tiles.All(t => !t.isOn))
        {
            gameRunning = false;
            winPanel.SetActive(true);
        }
    }

    public void PeekRandomTile()
    {
        if (peekUsed) return;

        int random = Random.Range(0, tiles.Count);
        List<int> targets = tiles[random].toggleTargets;
        Debug.Log($"Peek: Tile {random} toggles [{string.Join(", ", targets)}]");
        hintText.text = $"🔍 Peek: Tile {random} toggles [{string.Join(", ", targets)}] Generate only one time";
        peekUsed = true;
    }

    public void ResetPuzzle()
    {
        timer = 0;
        moves = 0;
        movesText.text = $"Moves: {moves}";
        hintText.text ="" ;
        peekUsed = false;
        gameRunning = true;
        winPanel.SetActive(false);
        GenerateGrid();
        AssignToggleRules();
    }

    public void SavePuzzle(string fileName)
    {
        PuzzleSaveData data = new PuzzleSaveData
        {
            tileStates = tiles.Select(t => t.isOn).ToList(),
            togglePatterns = tiles.Select(t => t.toggleTargets).ToList()
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, fileName), json);
    }

    public void LoadPuzzle(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        PuzzleSaveData data = JsonUtility.FromJson<PuzzleSaveData>(json);

        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].SetState(data.tileStates[i]);
            tiles[i].toggleTargets = data.togglePatterns[i];
        }
    }
}
