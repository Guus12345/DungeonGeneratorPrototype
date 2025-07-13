using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;


/// <summary>
/// Generates a 2D tile map representation of a procedural dungeon
/// using data from DungeonAlgorithm2 and passes it to MarchingSquares.
public class TileMapGenerator : MonoBehaviour
{
    [SerializeField] private DungeonAlgorithm2 dungeonAlgorithm; // refernce for the dungeonAlgorithm
    [SerializeField] private MarchingSquares marchingSquares; // reference for the marching squares algorithm
    private int[,] tileMap; // tilemap for looking up what goes where
    private int dungeonWidth, dungeonHeight; // dimensions of the dungeon
    public int[,] getTileMap; // public version of the tilemap

    /// <summary>
    /// Initializes the tile map size based on the dungeon starting bounds.
    void Start()
    {
        Vector3Int bounds = dungeonAlgorithm.startingSize;
        dungeonWidth = bounds.x + 1;
        dungeonHeight = bounds.z + 1;

        tileMap = new int[dungeonWidth, dungeonHeight];
    }

    /// <summary>
    /// Generates the tile map array by rasterizing all rooms and doors.
    public void GenerateTileMap()
    {
        // Clear map
        for (int x = 0; x < dungeonWidth; x++)
            for (int z = 0; z < dungeonHeight; z++)
                tileMap[x, z] = 0;

        // === Draw Rooms ===
        foreach (var room in dungeonAlgorithm.GetRooms())
        {
            Vector3 center = room.transform.position + new Vector3(0.5f, 0, 0.5f);
            Vector3Int size = room.size;

            float halfX = size.x * 0.5f;
            float halfZ = size.z * 0.5f;

            int x0 = Mathf.CeilToInt(center.x - halfX);
            int x1 = Mathf.CeilToInt(center.x + halfX) - 1;
            int z0 = Mathf.CeilToInt(center.z - halfZ);
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
            var size = sizes[i];
            Vector3 pos = door.transform.position + new Vector3(0.5f, 0, 0.5f);

            bool alongX = size.x > size.z;
            if (alongX)
            {
                int z = Mathf.RoundToInt(pos.z);
                int x0 = Mathf.RoundToInt(pos.x - size.x / 2f + 0.5f);
                int x1 = Mathf.RoundToInt(pos.x + size.x / 2f - 0.5f);
                for (int x = x0; x <= x1; x++)
                    if (InBounds(x, z))
                        tileMap[x, z] = 2;
            }
            else
            {
                int x = Mathf.RoundToInt(pos.x);
                int z0 = Mathf.RoundToInt(pos.z - size.z / 2f + 0.5f);
                int z1 = Mathf.RoundToInt(pos.z + size.z / 2f - 0.5f);
                for (int z = z0; z <= z1; z++)
                    if (InBounds(x, z))
                        tileMap[x, z] = 2;
            }
        }

        Debug.Log(TileMapToString());
        getTileMap = tileMap;
        Debug.Log(getTileMap);
        StartCoroutine(marchingSquares.MarchSquares(getTileMap));
    }

    /// <summary>
    /// Checks if a tile coordinate is inside the tile map bounds.
    private bool InBounds(int x, int z) =>
        x >= 0 && x < dungeonWidth && z >= 0 && z < dungeonHeight;


    /// <summary>
    /// Converts the tile map into a readable string for debugging.
    /// Each row is reversed vertically to show the map top-down.
    private string TileMapToString()
    {
        var sb = new StringBuilder();
        for (int z = dungeonHeight - 1; z >= 0; z--)
        {
            for (int x = 0; x < dungeonWidth; x++)
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
