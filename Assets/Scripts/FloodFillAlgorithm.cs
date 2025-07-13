using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;


/// <summary>
/// Performs a breadth-first (BFS) flood fill over a tile map,
/// spawning prefabs on walkable tiles and building a NavMesh at the end.
public class FloodFillAlgorithm : MonoBehaviour
{
    [Header("Prefab to spawn")]
    [SerializeField] private GameObject fillPrefab; // Floor prefab for filling

    [Header("NavMesh Surface")]
    [SerializeField] private NavMeshSurface navMeshSurface; // Navmesh surface for generating navmesh for player

    /// <summary>
    /// Starts the flood fill coroutine from the grid coordinates.
    public void StartFloodFill(int[,] grid, int startX, int startY)
    {
        StartCoroutine(FloodFill(grid, startX, startY));
    }

    /// <summary>
    /// Coroutine performing a 4-way flood fill.
    /// Prefabs are spawned at each traversed floor or door cell.
    /// At the end, NavMeshSurface.BuildNavMesh() is called.
    /// Big O Notation: O(N), where N is the number of floor+door cells connected to the start.
    private IEnumerator FloodFill(int[,] grid, int x, int y)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        // Validate start position
        if (x < 0 || y < 0 || x >= width || y >= height)
        {
            Debug.LogWarning($"FloodFill: start index ({x},{y}) out of bounds.");
            yield break;
        }

        // Validate starting tile
        if (grid[x, y] != 2 && grid[x, y] != 3)
        {
            Debug.LogWarning($"FloodFill: start tile {grid[x, y]} is not floor or door.");
            yield break;
        }

        // BFS structures
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        Vector2Int start = new Vector2Int(x, y);
        queue.Enqueue(start);
        visited.Add(start);

        // BFS loop
        while (queue.Count > 0)
        {
            Vector2Int currentFloor = queue.Dequeue();

            // Spawn prefab shifted by -0.5
            if (fillPrefab != null)
            {
                Vector3 spawnPos = new Vector3(currentFloor.x - .5f, 0, currentFloor.y - .5f);
                Instantiate(fillPrefab, spawnPos, Quaternion.identity);
            }

            // 4-way neighbors
            Vector2Int[] neighbors =
            {
                new Vector2Int(currentFloor.x - 1, currentFloor.y),
                new Vector2Int(currentFloor.x + 1, currentFloor.y),
                new Vector2Int(currentFloor.x, currentFloor.y - 1),
                new Vector2Int(currentFloor.x, currentFloor.y + 1)
            };

            foreach (var n in neighbors)
            {
                if (n.x < 0 || n.y < 0 || n.x >= width || n.y >= height)
                    continue;

                if (visited.Contains(n))
                    continue;

                if (grid[n.x, n.y] != 2 && grid[n.x, n.y] != 3)
                    continue;

                visited.Add(n);
                queue.Enqueue(n);
            }

            yield return new WaitForFixedUpdate();
        }

        Debug.Log("Flood fill complete.");

        // Build the navmesh now
        if (navMeshSurface != null)
        {
            Debug.Log("Building NavMesh...");
            navMeshSurface.BuildNavMesh();
            Debug.Log("NavMesh built.");
        }
        else
        {
            Debug.LogWarning("NavMeshSurface reference is missing!");
        }
    }
}
