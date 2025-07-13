using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data structure mapping a Marching Squares configuration (0-15) to a prefab and rotation.
[System.Serializable]
public struct WallEntry
{
    public int config;        // marching‑squares code 0–15
    public GameObject prefab; // which wall prefab to use
    public Vector3 euler;     // rotation in degrees
}

/// <summary>
/// Generates walls using the Marching Squares algorithm on a tile map and starts a flood fill.
public class MarchingSquares : MonoBehaviour
{
    [SerializeField] private TileMapGenerator tileMapGenerator; // Refrence to the TilemapGenerator script
    [SerializeField] private FloodFillAlgorithm floodFill; // Reference to the FloodFill Algorithm script
    [SerializeField] private List<WallEntry> wallEntries = new List<WallEntry>(); // List of Wall entries
    private bool generationFinished;


    private Dictionary<int, (GameObject prefab, Quaternion rot)> _wallMap;



    private void Awake()
    {
        _wallMap = new Dictionary<int, (GameObject, Quaternion)>();
        foreach (var e in wallEntries)
        {
            _wallMap[e.config] = (e.prefab, Quaternion.Euler(e.euler));
        }
    }

    /// <summary>
    /// Coroutine that iterates over the tile map and instantiates wall prefabs
    /// based on Marching Squares configurations.
    /// Then starts the flood fill on the first detected floor tile.
    /// Big O Notation: O(W * H), where W and H are the grid dimensions.
    public IEnumerator MarchSquares(int[,] map)
    {
        generationFinished = false;
        int rows = map.GetLength(0) + 1;
        int cols = map.GetLength(1) + 1;

        for (int x = 0; x < rows - 1; x++)
        {
            for (int z = 0; z < cols - 1; z++)
            {
                int config = GetMarchingCode(map, x, z);

                if (config == 0)
                    continue;

                if (_wallMap.TryGetValue(config, out var entry))
                {
                    Vector3 position = new Vector3(x, 0, z);
                    Instantiate(entry.prefab, position, entry.rot);

                    yield return new WaitForFixedUpdate();
                }
            }
        }

        bool foundStart = false;
        int startX = -1;
        int startZ = -1;
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int z = 0; z < height && !foundStart; z++)
        {
            for (int x = 0; x < width && !foundStart; x++)
            {
                if (map[x, z] == 3)
                {
                    startX = x;
                    startZ = z;
                    foundStart = true;
                }
            }
        }

        if (foundStart)
        {
            Debug.Log($"FloodFill starting at first floor tile ({startX},{startZ})");

            // Start the flood fill
            floodFill.StartFloodFill(map, startX, startZ);
            generationFinished = true;
        }
        else
        {
            Debug.LogWarning("No floor tile found to start flood fill.");
        }


    }

    /// <summary>
    /// Computes the 4-bit Marching Squares code for a cell given tile indices.
    /// Each corner turns into a bit if it is a wall.
    private int GetMarchingCode(int[,] m, int x, int z)
    {
        int a = IsWall(m, x, z + 1) ? 8 : 0;
        int b = IsWall(m, x + 1, z + 1) ? 4 : 0;
        int c = IsWall(m, x + 1, z) ? 2 : 0;
        int d = IsWall(m, x, z) ? 1 : 0;
        return a | b | c | d;

    }

    /// <summary>
    /// Returns true if the specified grid cell is a wall (value == 1).
    /// Returns false if out of bounds or not a wall.
    private bool IsWall(int[,] m, int x, int z)
    {
        if (x < 0 || z < 0 || x >= m.GetLength(0) || z >= m.GetLength(1))
            return false;
        return m[x, z] == 1;
    }

    /// <summary>
    /// Draws Gizmos in the editor to visualize the tile grid and corner points.
    /// Only active before generation finishes.
    private void OnDrawGizmos()
    {
        if (!generationFinished)
        {
            if (tileMapGenerator == null || tileMapGenerator.getTileMap == null)
                return;

            int[,] map = tileMapGenerator.getTileMap;
            int w = map.GetLength(0);
            int h = map.GetLength(1);

            // 1) Draw tile centers in Green
            Gizmos.color = Color.green;
            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < h; z++)
                {
                    // tile (x,z) center is at (x+0.5, z+0.5)
                    Vector3 tileCenter = new Vector3(x + 0.5f, 0, z + 0.5f);
                    Gizmos.DrawWireCube(tileCenter, new Vector3(1, 0, 1));
                }
            }

            // 2) Draw marching‐square corners with wall value in Yellow
            Gizmos.color = Color.yellow;
            for (int x = 0; x < w - 1; x++)
            {
                for (int z = 0; z < h - 1; z++)
                {
                    Vector3 corner = new Vector3(x, 0, z);
                    int cfg = GetMarchingCode(map, x, z);

                    // Only draw if any of the 4 tiles are walls
                    bool hasWall =
                        IsWall(map, x, z) ||
                        IsWall(map, x + 1, z) ||
                        IsWall(map, x, z + 1) ||
                        IsWall(map, x + 1, z + 1);

                    if (_wallMap != null && _wallMap.ContainsKey(cfg) && hasWall)
                        Gizmos.DrawSphere(corner, 0.1f);
                }
            }
        }
    }
}