using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public class DungeonAlgorithm2 : MonoBehaviour
{
    public Vector3Int startingSize;
    [SerializeField] private int minSizeLimit;
    [SerializeField] private int maxSizeLimit;
    [SerializeField] private bool useSeed;
    [SerializeField] private int seed;
    [SerializeField] private int doorWidth = 2;
    [SerializeField, Range(0f, 1f)] private float branchChance = 0.8f;
    [SerializeField] private UnityEvent onDungeonBuilt;

    public class RoomNode
    {
        public int id;
        public Transform transform;
        public Vector3Int size;
        public RoomNode(int id, Transform t, Vector3Int s)
        {
            this.id = id;
            transform = t;
            size = s;
        }
    }

    private System.Random rng;
    private List<RoomNode> unfinishedRooms = new List<RoomNode>();
    private List<RoomNode> rooms = new List<RoomNode>();
    private int nextRoomId = 0;
    private List<GameObject> doors = new List<GameObject>();
    private List<Vector3Int> doorSizes = new List<Vector3Int>();
    private List<(int a, int b)> doorConnections = new List<(int a, int b)>();
    private Dictionary<(int a, int b), GameObject> doorMap = new Dictionary<(int a, int b), GameObject>();
    private List<(int a, int b)> connections = new List<(int a, int b)>();

    public List<RoomNode> GetRooms() => rooms;
    public List<GameObject> GetDoors() => doors;
    public List<Vector3Int> GetDoorSizes() => doorSizes;

    private void Start()
    {

        if (!useSeed)
            seed = Random.Range(int.MinValue, int.MaxValue);
        rng = new System.Random(seed);

        // Create initial room
        GameObject go = new GameObject("Room_" + nextRoomId);
        go.transform.position = new Vector3(startingSize.x / 2f, 0, startingSize.z / 2f);
        rooms.Add(new RoomNode(nextRoomId++, go.transform, startingSize));

        StartCoroutine(RoomGeneration());
    }

    private IEnumerator RoomGeneration()
    {
        unfinishedRooms = new List<RoomNode>(rooms);
        rooms.Clear();

        while (unfinishedRooms.Count > 0)
        {
            var current = unfinishedRooms.Last();
            unfinishedRooms.RemoveAt(unfinishedRooms.Count - 1);
            var pos = current.transform.position;
            var sz = current.size;
            int axis = rng.Next(0, 2);
            int limit = rng.Next(minSizeLimit, maxSizeLimit);

            bool splitX = (axis == 0 || sz.z < limit) && sz.x > limit;
            bool splitZ = (axis == 1 || sz.x < limit) && sz.z > limit;

            if (splitX)
            {
                int cut = rng.Next(3, sz.x - 3);
                float xMin = pos.x - sz.x / 2f;
                float xMax = pos.x + sz.x / 2f;
                var goA = new GameObject("Room_" + nextRoomId);
                goA.transform.position = new Vector3(xMin + cut / 2f + 0.5f, pos.y, pos.z);
                var nodeA = new RoomNode(nextRoomId++, goA.transform, new Vector3Int(cut + 1, sz.y, sz.z));
                var goB = new GameObject("Room_" + nextRoomId);
                goB.transform.position = new Vector3(xMax - (sz.x - cut) / 2f, pos.y, pos.z);
                var nodeB = new RoomNode(nextRoomId++, goB.transform, new Vector3Int(sz.x - cut, sz.y, sz.z));
                unfinishedRooms.Add(nodeA);
                unfinishedRooms.Add(nodeB);
            }
            else if (splitZ)
            {
                int cut = rng.Next(3, sz.z - 3);
                float zMin = pos.z - sz.z / 2f;
                float zMax = pos.z + sz.z / 2f;
                var goA = new GameObject("Room_" + nextRoomId);
                goA.transform.position = new Vector3(pos.x, pos.y, zMin + cut / 2f + 0.5f);
                var nodeA = new RoomNode(nextRoomId++, goA.transform, new Vector3Int(sz.x, sz.y, cut + 1));
                var goB = new GameObject("Room_" + nextRoomId);
                goB.transform.position = new Vector3(pos.x, pos.y, zMax - (sz.z - cut) / 2f);
                var nodeB = new RoomNode(nextRoomId++, goB.transform, new Vector3Int(sz.x, sz.y, sz.z - cut));
                unfinishedRooms.Add(nodeA);
                unfinishedRooms.Add(nodeB);
            }
            else
            {
                rooms.Add(current);
            }

            yield return new WaitForFixedUpdate();
        }

        unfinishedRooms.Clear();
        StartCoroutine(DoorGeneration());
    }

    private IEnumerator DoorGeneration()
    {
        float halfY = startingSize.y * 0.5f;

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                var A = rooms[i];
                var B = rooms[j];

                // Compute XZ overlap
                Vector3 minA = A.transform.position - new Vector3(A.size.x / 2f, 0, A.size.z / 2f);
                Vector3 maxA = A.transform.position + new Vector3(A.size.x / 2f, 0, A.size.z / 2f);
                Vector3 minB = B.transform.position - new Vector3(B.size.x / 2f, 0, B.size.z / 2f);
                Vector3 maxB = B.transform.position + new Vector3(B.size.x / 2f, 0, B.size.z / 2f);

                float ix1 = Mathf.Max(minA.x, minB.x);
                float iz1 = Mathf.Max(minA.z, minB.z);
                float ix2 = Mathf.Min(maxA.x, maxB.x);
                float iz2 = Mathf.Min(maxA.z, maxB.z);

                if (ix2 <= ix1 || iz2 <= iz1)
                    continue;

                Vector3 center = new Vector3((ix1 + ix2) * 0.5f, halfY, (iz1 + iz2) * 0.5f);

                // Must have exactly two rooms at center and ±1m along wall axis:
                if (CountCoverAt(center) != 2)
                    continue;

                // after computing:
                float overlapX = ix2 - ix1;
                float overlapZ = iz2 - iz1;

                // choose wall‐axis direction correctly:
                bool useXWall = overlapX < overlapZ;
                // if the overlap in X is smaller, wall runs along Z, so step in Z:
                Vector3 sideDir = useXWall ? Vector3.forward : Vector3.right;

                // now test at ±half‐door width
                float offset = doorWidth * 0.5f + 0.01f;
                if (CountCoverAt(center + sideDir * offset) != 2 ||
                    CountCoverAt(center - sideDir * offset) != 2)
                    continue;

                // Spawn door
                Debug.Log("hi");
                Vector3 scale = useXWall
                    ? new Vector3(overlapX, startingSize.y, doorWidth)
                    : new Vector3(doorWidth, startingSize.y, overlapZ);

                if (useXWall)
                {
                    center.z = Mathf.Round(center.z);
                }
                else
                {
                    center.x = Mathf.Round(center.x);
                }

                GameObject door = new GameObject();
                door.name = $"door_{A.id}_{B.id}";
                door.transform.position = center;
                door.transform.localScale = scale;

                doors.Add(door);
                doorSizes.Add(new Vector3Int(
                    Mathf.RoundToInt(scale.x),
                    startingSize.y,
                    Mathf.RoundToInt(scale.z)
                ));
                doorConnections.Add((A.id, B.id));
                doorMap[(A.id, B.id)] = door;
            }

            yield return new WaitForFixedUpdate();

        }

        StartCoroutine(ConnectAndPrune());
    }

    // Helper: count how many rooms cover a given XZ point
    private int CountCoverAt(Vector3 point)
    {
        int count = 0;
        foreach (var R in rooms)
        {
            Vector3 rMin = R.transform.position - new Vector3(R.size.x / 2f, 0, R.size.z / 2f);
            Vector3 rMax = R.transform.position + new Vector3(R.size.x / 2f, 0, R.size.z / 2f);
            if (point.x >= rMin.x && point.x <= rMax.x
             && point.z >= rMin.z && point.z <= rMax.z)
            {
                count++;
                if (count > 2)
                    return count;
            }
        }
        return count;
    }



    private IEnumerator ConnectAndPrune()
    {
        // Assign door node IDs
        int baseRoomCount = rooms.Count > 0 ? rooms.Max(r => r.id) + 1 : nextRoomId;
        var doorNodeId = new Dictionary<(int, int), int>();
        foreach (var pair in doorConnections) doorNodeId[pair] = baseRoomCount++;

        // Build room-to-door mapping
        var roomToDoors = new Dictionary<int, List<(int, int)>>();
        foreach (var pair in doorConnections)
        {
            if (!roomToDoors.ContainsKey(pair.Item1)) roomToDoors[pair.Item1] = new List<(int, int)>();
            if (!roomToDoors.ContainsKey(pair.Item2)) roomToDoors[pair.Item2] = new List<(int, int)>();
            roomToDoors[pair.Item1].Add(pair);
            roomToDoors[pair.Item2].Add(pair);
        }

        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        int start = rooms[rng.Next(rooms.Count)].id;
        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (!roomToDoors.ContainsKey(current)) continue;

            var neighbors = roomToDoors[current];
            neighbors.Shuffle(rng);

            bool guaranteedConnected = false;
            foreach (var pair in neighbors)
            {
                int neighbor = pair.Item1 == current ? pair.Item2 : pair.Item1;
                if (visited.Contains(neighbor)) continue;

                if (!guaranteedConnected || rng.NextDouble() < branchChance)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    connections.Add(pair);
                    guaranteedConnected = true;
                }
            }

            yield return new WaitForFixedUpdate();
        }

        // Prune unused rooms
        var toRemove = rooms.Where(r => !visited.Contains(r.id)).ToList();
        foreach (var r in toRemove)
        {
            rooms.Remove(r);
            Destroy(r.transform.gameObject);
        }

        // Prune unused doors
        for (int i = doors.Count - 1; i >= 0; i--)
        {
            var pair = doorConnections[i];
            if (!connections.Contains(pair) && !connections.Contains((pair.Item2, pair.Item1)))
            {
                Destroy(doors[i]);
                doors.RemoveAt(i);
                doorSizes.RemoveAt(i);
                doorConnections.RemoveAt(i);
                doorMap.Remove(pair);
            }
        }

        onDungeonBuilt.Invoke();
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        foreach (var n in unfinishedRooms)
            if (n?.transform != null) Gizmos.DrawWireCube(n.transform.position, new Vector3(n.size.x, 0, n.size.z));

        Gizmos.color = Color.green;
            foreach (var n in rooms)
                if (n?.transform != null) Gizmos.DrawWireCube(n.transform.position, new Vector3(n.size.x, 0, n.size.z));

        Gizmos.color = Color.magenta;
            foreach (var n in rooms)
                if (n?.transform != null) Gizmos.DrawWireCube(new Vector3(n.transform.position.x, n.transform.position.y + (float)n.size.y / 2, n.transform.position.z), n.size);

        Gizmos.color = Color.red;
            for (int i = 0; i < doors.Count; i++)
                if (doors[i] != null) Gizmos.DrawWireCube(doors[i].transform.position, doorSizes[i]);

        Gizmos.color = Color.cyan;
            foreach (var (a, b) in connections) { var A = rooms.Find(r => r.id == a); var B = rooms.Find(r => r.id == b);
                if (A != null && B != null) Gizmos.DrawLine(A.transform.position, B.transform.position); }
    }
}

public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list, System.Random rng)
    {
        int n = list.Count; while (n > 1)
        {
            n--; int k = rng.Next(n + 1); var tmp = list[k]; list[k] = list[n]; list[n] = tmp;
        }
    }
}
