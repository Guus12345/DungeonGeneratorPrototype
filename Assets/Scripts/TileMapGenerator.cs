using System.Text;
using UnityEngine;

public class TileMapGenerator : MonoBehaviour
{
    [SerializeField] private DungeonAlgorithm2 dungeonAlgorithm;
    private int[,] tileMap;
    private int width, height;

    void Start()
    {
        // 1) fetch bounds from your dungeon
        Vector3Int bounds = dungeonAlgorithm.startingSize;
        width = bounds.x;
        height = bounds.z;

        // 2) allocate & fill
        tileMap = new int[width, height];
        

        // 3) optional: print it
        Debug.Log(TileMapToString());
    }

    public void GenerateTileMap()
    {
        // clear to 0
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                tileMap[x, z] = 0;

        // --- mark rooms as 1 ---
        foreach (var room in dungeonAlgorithm.GetRooms())
        {
            Vector3 pos = room.transform.position;
            Vector3Int sz = room.size;

            // compute integer cell extents
            int x0 = Mathf.CeilToInt(pos.x - sz.x * 0.5f);
            int x1 = Mathf.FloorToInt(pos.x + sz.x * 0.5f);
            int z0 = Mathf.CeilToInt(pos.z - sz.z * 0.5f);
            int z1 = Mathf.FloorToInt(pos.z + sz.z * 0.5f);

            // fill those cells
            for (int x = x0; x < x1; x++)
                for (int z = z0; z < z1; z++)
                    if (InBounds(x, z))
                        tileMap[x, z] = 1;
        }

        // --- mark doors as 2 ---
        var doors = dungeonAlgorithm.GetDoors();
        var doorSizes = dungeonAlgorithm.GetDoorSizes();
        for (int i = 0; i < doors.Count; i++)
        {
            var door = doors[i];
            var sz = doorSizes[i];

            Vector3 pos = door.transform.position;
            bool runsAlongX = sz.x > sz.z;

            if (runsAlongX)
            {
                // door width along X
                int z = Mathf.RoundToInt(pos.z);
                int x0 = Mathf.CeilToInt(pos.x - sz.x * 0.5f);
                int x1 = Mathf.FloorToInt(pos.x + sz.x * 0.5f);
                for (int x = x0; x <= x1; x++)
                    if (InBounds(x, z))
                        tileMap[x, z] = 2;
            }
            else
            {
                // door width along Z
                int x = Mathf.RoundToInt(pos.x);
                int z0 = Mathf.CeilToInt(pos.z - sz.z * 0.5f);
                int z1 = Mathf.FloorToInt(pos.z + sz.z * 0.5f);
                for (int z = z0; z <= z1; z++)
                    if (InBounds(x, z))
                        tileMap[x, z] = 2;
            }
        }
        Debug.Log(TileMapToString());
    }

    // helper: check array bounds
    private bool InBounds(int x, int z) =>
        x >= 0 && x < width && z >= 0 && z < height;

    // turn the int[,] into a clickable string with 0,1,2
    private string TileMapToString()
    {
        var sb = new StringBuilder();
        // flip so Y=0 is at bottom
        for (int z = height - 1; z >= 0; z--)
        {
            for (int x = 0; x < width; x++)
            {
                char c = tileMap[x, z] switch
                {
                    0 => '.',
                    1 => '#',  // room
                    2 => 'D',  // door
                    _ => '?'
                };
                sb.Append(c);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
