using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private TileMapGenerator tileMapGenerator;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject[] wallPrefabs; // Based on marching square configuration
    public void GenerateFromMap(int[,] map)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width - 1; x++)
        {
            for (int z = 0; z < height - 1; z++)
            {
                int config = GetMarchingCode(map, x, z);
                Vector3 pos = new Vector3(x, 0, z);

                // Only place wall if the config is nonzero
                if (config > 0 && config < wallPrefabs.Length)
                {
                    Instantiate(wallPrefabs[config], pos, Quaternion.identity);
                }

                // Optionally spawn floor if current tile is floor or door
                if (map[x, z] == 2 || map[x, z] == 3)
                {
                    Instantiate(floorPrefab, new Vector3(x, 0, z), Quaternion.identity);
                }
            }
        }
    }
    // Binary marching squares code
    int GetMarchingCode(int[,] map, int x, int z)
    {
        int a = IsWall(map, x, z + 1) ? 8 : 0; // A (top-left)
        int b = IsWall(map, x + 1, z + 1) ? 4 : 0; // B (top-right)
        int c = IsWall(map, x + 1, z) ? 2 : 0; // C (bottom-right)
        int d = IsWall(map, x, z) ? 1 : 0; // D (bottom-left)

        return a | b | c | d; // combine bits
    }

    bool IsWall(int[,] map, int x, int z)
    {
        if (x < 0 || x >= map.GetLength(0) || z < 0 || z >= map.GetLength(1))
            return false;
        return map[x, z] == 1; // Only walls affect the marching config
    }
}
