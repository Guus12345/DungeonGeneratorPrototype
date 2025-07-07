using System.Text;
using UnityEngine;

public class TileMapGenerator : MonoBehaviour
{
    [SerializeField] private DungeonAlgorithm2 dungeonAlgorithm;
    [SerializeField] private DungeonGenerator dungeonGenerator;
    private int[,] tileMap;
    private int width, height;
    public int[,] getTileMap;

    void Start()
    {
        Vector3Int bounds = dungeonAlgorithm.startingSize;
        width = bounds.x + 1;
        height = bounds.z + 1;

        tileMap = new int[width, height];
    }

    public void GenerateTileMap()
    {
        // Clear map
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                tileMap[x, z] = 0;

        // === Draw Rooms ===
        foreach (var room in dungeonAlgorithm.GetRooms())
        {
            Vector3 center = room.transform.position + new Vector3(0.5f, 0, 0.5f);
            Vector3Int size = room.size;

            float halfX = size.x * 0.5f;
            float halfZ = size.z * 0.5f;

            int x0 = Mathf.FloorToInt(center.x - halfX);
            int x1 = Mathf.CeilToInt(center.x + halfX) - 1;
            int z0 = Mathf.FloorToInt(center.z - halfZ);
            int z1 = Mathf.CeilToInt(center.z + halfZ) - 1;

            // Fill interior (floor)
            for (int x = x0 + 1; x < x1; x++)
                for (int z = z0 + 1; z < z1; z++)
                    if (InBounds(x, z))
                        tileMap[x, z] = 3;

            // Walls: top/bottom (Z axis)
            for (int x = x0; x <= x1; x++)
            {
                if (InBounds(x, z0)) tileMap[x, z0] = 1;
                if (InBounds(x, z1)) tileMap[x, z1] = 1;
            }

            // Walls: left/right (X axis)
            for (int z = z0; z <= z1; z++)
            {
                if (InBounds(x0, z)) tileMap[x0, z] = 1;
                if (InBounds(x1, z)) tileMap[x1, z] = 1;
            }
        }

        // === Draw Doors ===
        var doors = dungeonAlgorithm.GetDoors();
        var sizes = dungeonAlgorithm.GetDoorSizes();
        for (int i = 0; i < doors.Count; i++)
        {
            var door = doors[i];
            var sz = sizes[i];
            Vector3 pos = door.transform.position + new Vector3(0.5f, 0, 0.5f);

            bool alongX = sz.x > sz.z;
            if (alongX)
            {
                int z = Mathf.RoundToInt(pos.z);
                int x0 = Mathf.RoundToInt(pos.x - sz.x / 2f + 0.5f);
                int x1 = Mathf.RoundToInt(pos.x + sz.x / 2f - 0.5f);
                for (int x = x0; x <= x1; x++)
                    if (InBounds(x, z))
                        tileMap[x, z] = 2;
            }
            else
            {
                int x = Mathf.RoundToInt(pos.x);
                int z0 = Mathf.RoundToInt(pos.z - sz.z / 2f + 0.5f);
                int z1 = Mathf.RoundToInt(pos.z + sz.z / 2f - 0.5f);
                for (int z = z0; z <= z1; z++)
                    if (InBounds(x, z))
                        tileMap[x, z] = 2;
            }
        }

        Debug.Log(TileMapToString());
        getTileMap = tileMap;
        dungeonGenerator.GenerateFromMap(getTileMap);
    }

    private bool InBounds(int x, int z) =>
        x >= 0 && x < width && z >= 0 && z < height;

    private string TileMapToString()
    {
        var sb = new StringBuilder();
        for (int z = height - 1; z >= 0; z--)
        {
            for (int x = 0; x < width; x++)
            {
                char c = tileMap[x, z] switch
                {
                    0 => ' ',  // empty
                    1 => '#',  // wall or overlap
                    2 => '.',  // door
                    3 => '.',  // floor
                    _ => '?'
                };
                sb.Append(c);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
