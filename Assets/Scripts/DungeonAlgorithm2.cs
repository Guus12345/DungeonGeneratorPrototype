using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Main component that generates a procedural dungeon by recursively splitting space into rooms,
/// detecting overlapping areas for doors, and connecting rooms with a spanning tree.
/// Overall expected time complexity: O(N^2) for door generation and connection pruning,
/// where N is the number of rooms.
public class DungeonAlgorithm2 : MonoBehaviour
{
    public Vector3Int startingSize;
    [SerializeField] private int maxRoomSizeLimit = 12; // Maximum Room size limit
    [SerializeField] private bool useSeed; // bool for using seed or not
    [SerializeField] private int seed;
    [SerializeField] private int doorWidth = 2; // width of the door
    [SerializeField, Range(0f, 1f)] private float branchChance = 0.2f; // chance the connect and prune algorithm branches off into two
    [SerializeField] private UnityEvent onDungeonBuilt; // event for signalling next script activation
    

    public class RoomNode // node for storing room information
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

    private System.Random random; // random for predictable randomness for seed
    private List<RoomNode> unfinishedRooms = new List<RoomNode>(); // unfinished rooms for showing in debug
    private List<RoomNode> rooms = new List<RoomNode>(); // list of rooms with their respective properties
    private int nextRoomId = 0;
    private List<GameObject> doors = new List<GameObject>(); // list of all doors for showing in debug
    private List<Vector3Int> doorSizes = new List<Vector3Int>(); // list of all door sizes for showing in debug
    private List<(int a, int b)> doorConnections = new List<(int a, int b)>(); // all door connections for the connect and prune algorithm
    private Dictionary<(int a, int b), GameObject> doorMap = new Dictionary<(int a, int b), GameObject>(); // dictionary for quickly looking up all rooms with their connected doors
    private List<(int a, int b)> connections = new List<(int a, int b)>(); // list of connections that remain after pruning
    private bool generationFinished;

    public List<RoomNode> GetRooms() => rooms;
    public List<GameObject> GetDoors() => doors;
    public List<Vector3Int> GetDoorSizes() => doorSizes;
    public Vector2Int randomRoom;

    private void Start()
    {

        if (!useSeed)
            seed = Random.Range(int.MinValue, int.MaxValue);
        random = new System.Random(seed);

        // Create initial room
        GameObject room = new GameObject("Room_" + nextRoomId);
        room.transform.position = new Vector3(startingSize.x / 2f, 0, startingSize.z / 2f);
        rooms.Add(new RoomNode(nextRoomId++, room.transform, startingSize));
        generationFinished = false;

        StartCoroutine(RoomGeneration());
    }

    /// <summary>
    /// Coroutine that recursively splits rooms into smaller rooms along X or Z.
    /// Big O Notation: O(log N) splits, where N is proportional to startingSize.
    private IEnumerator RoomGeneration()
    {
        unfinishedRooms = new List<RoomNode>(rooms);
        rooms.Clear();

        while (unfinishedRooms.Count > 0)
        {
            var current = unfinishedRooms.Last();
            unfinishedRooms.RemoveAt(unfinishedRooms.Count - 1);
            var pos = current.transform.position;
            var size = current.size;


            if (size.x > maxRoomSizeLimit)
            {
                int cut = random.Next(3, size.x - 3);
                float xMin = pos.x - size.x / 2f;
                float xMax = pos.x + size.x / 2f;
                var roomA = new GameObject("Room_" + nextRoomId);
                roomA.transform.position = new Vector3(xMin + cut / 2f + 0.5f, pos.y, pos.z);
                var nodeA = new RoomNode(nextRoomId++, roomA.transform, new Vector3Int(cut + 1, size.y, size.z));
                var roomB = new GameObject("Room_" + nextRoomId);
                roomB.transform.position = new Vector3(xMax - (size.x - cut) / 2f, pos.y, pos.z);
                var nodeB = new RoomNode(nextRoomId++, roomB.transform, new Vector3Int(size.x - cut, size.y, size.z));
                unfinishedRooms.Add(nodeA);
                unfinishedRooms.Add(nodeB);
            }
            else if (size.z > maxRoomSizeLimit)
            {
                int cut = random.Next(3, size.z - 3);
                float zMin = pos.z - size.z / 2f;
                float zMax = pos.z + size.z / 2f;
                var roomA = new GameObject("Room_" + nextRoomId);
                roomA.transform.position = new Vector3(pos.x, pos.y, zMin + cut / 2f + 0.5f);
                var nodeA = new RoomNode(nextRoomId++, roomA.transform, new Vector3Int(size.x, size.y, cut + 1));
                var roomB = new GameObject("Room_" + nextRoomId);
                roomB.transform.position = new Vector3(pos.x, pos.y, zMax - (size.z - cut) / 2f);
                var nodeB = new RoomNode(nextRoomId++, roomB.transform, new Vector3Int(size.x, size.y, size.z - cut));
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

    


    /// <summary>
    /// Generates doors between overlapping room pairs.
    /// Tries offset placement near overlap edge if space >= 2.5× door width; 
    /// falls back to centered placement if offset fails.
    /// Skips door if both checks fail.
    /// Big O notation: O(N^2) pairs × O(N) per CountCoverAt.
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

                float roomOverlapX1 = Mathf.Max(minA.x, minB.x);
                float roomOverlapZ1 = Mathf.Max(minA.z, minB.z);
                float roomOverlapX2 = Mathf.Min(maxA.x, maxB.x);
                float roomOverlapZ2 = Mathf.Min(maxA.z, maxB.z);

                if (roomOverlapX2 <= roomOverlapX1 || roomOverlapZ2 <= roomOverlapZ1)
                    continue;

                // Base midpoint
                float baseX = (roomOverlapX1 + roomOverlapX2) * 0.5f;
                float baseZ = (roomOverlapZ1 + roomOverlapZ2) * 0.5f;

                // Compute overlap ranges
                float overlapX = roomOverlapX2 - roomOverlapX1;
                float overlapZ = roomOverlapZ2 - roomOverlapZ1;

                // Determine wall orientation
                bool useXWall = overlapX < overlapZ;
                Vector3 sideDirection = useXWall ? Vector3.forward : Vector3.right;

                bool doorPlaced = false;

                // Compute perpendicular size to check eligibility for offset
                float perpendicularSize = useXWall ? overlapZ : overlapX;
                float minRequiredSize = doorWidth * 2.5f;

                if (perpendicularSize >= minRequiredSize)
                {
                    // Try offset position near edge
                    float offsetDistance = (perpendicularSize / 2f) - (doorWidth / 2f) - 0.5f;
                    float offsetSign = (Random.value < 0.5f) ? 1f : -1f;

                    Vector3 offsetCenter = new Vector3(baseX, halfY, baseZ);
                    offsetCenter += sideDirection * offsetDistance * offsetSign;

                    if (
                        CountCoverAt(offsetCenter) == 2 &&
                        CountCoverAt(offsetCenter + sideDirection * (doorWidth * 0.5f + 0.01f)) == 2 &&
                        CountCoverAt(offsetCenter - sideDirection * (doorWidth * 0.5f + 0.01f)) == 2
                    )
                    {
                        // Valid offset placement, spawn door here
                        Vector3 scale = useXWall
                            ? new Vector3(overlapX, startingSize.y, doorWidth)
                            : new Vector3(doorWidth, startingSize.y, overlapZ);

                        if (useXWall)
                            offsetCenter.z = Mathf.Round(offsetCenter.z);
                        else
                            offsetCenter.x = Mathf.Round(offsetCenter.x);

                        GameObject door = new GameObject($"door_{A.id}_{B.id}");
                        door.transform.position = offsetCenter;
                        door.transform.localScale = scale;

                        doors.Add(door);
                        doorSizes.Add(new Vector3Int(
                            Mathf.RoundToInt(scale.x),
                            startingSize.y,
                            Mathf.RoundToInt(scale.z)
                        ));
                        doorConnections.Add((A.id, B.id));
                        doorMap[(A.id, B.id)] = door;

                        doorPlaced = true;
                    }
                }

                if (doorPlaced)
                    continue;

                // Fallback to original center logic (unchanged)
                Vector3 center = new Vector3(baseX, halfY, baseZ);

                if (CountCoverAt(center) != 2)
                    continue;

                float offset = doorWidth * 0.5f + 0.01f;

                if (CountCoverAt(center + sideDirection * offset) != 2 ||
                    CountCoverAt(center - sideDirection * offset) != 2)
                    continue;

                Vector3 fallbackScale = useXWall
                    ? new Vector3(overlapX, startingSize.y, doorWidth)
                    : new Vector3(doorWidth, startingSize.y, overlapZ);

                if (useXWall)
                    center.z = Mathf.Round(center.z);
                else
                    center.x = Mathf.Round(center.x);

                GameObject fallbackDoor = new GameObject($"door_{A.id}_{B.id}");
                fallbackDoor.transform.position = center;
                fallbackDoor.transform.localScale = fallbackScale;

                doors.Add(fallbackDoor);
                doorSizes.Add(new Vector3Int(
                    Mathf.RoundToInt(fallbackScale.x),
                    startingSize.y,
                    Mathf.RoundToInt(fallbackScale.z)
                ));
                doorConnections.Add((A.id, B.id));
                doorMap[(A.id, B.id)] = fallbackDoor;
            }

            yield return new WaitForFixedUpdate();
        }

        StartCoroutine(PruneRooms());
    }


    /// <summary>
    /// Counts how many rooms cover a given world position.
    /// Time complexity: O(N).
    private int CountCoverAt(Vector3 centerPoint)
    {
        int count = 0;
        foreach (var R in rooms)
        {
            Vector3 rMin = R.transform.position - new Vector3(R.size.x / 2f, 0, R.size.z / 2f);
            Vector3 rMax = R.transform.position + new Vector3(R.size.x / 2f, 0, R.size.z / 2f);
            if (centerPoint.x >= rMin.x && centerPoint.x <= rMax.x
             && centerPoint.z >= rMin.z && centerPoint.z <= rMax.z)
            {
                count++;
                if (count > 2)
                    return count;
            }
        }
        return count;
    }

    private IEnumerator PruneRooms()
    {
        // Compute area for each room
        var roomAreas = rooms
            .Select(r => new { Room = r, Area = r.size.x * r.size.z })
            .OrderBy(x => x.Area)
            .ToList();

        // Determine how many to remove (10% of total, rounded down)
        int removeCount = Mathf.FloorToInt(rooms.Count * 0.10f);

        var toRemove = roomAreas.Take(removeCount).Select(x => x.Room).ToList();

        // Remove rooms and destroy their GameObjects
        foreach (var r in toRemove)
        {
            rooms.Remove(r);
            Destroy(r.transform.gameObject);
        }   

        // Prune doors attached to removed rooms
        for (int i = doors.Count - 1; i >= 0; i--)
        {
            var roomPair = doorConnections[i];
            if (toRemove.Any(r => r.id == roomPair.a || r.id == roomPair.b))
            {
                Destroy(doors[i]);
                doors.RemoveAt(i);
                doorSizes.RemoveAt(i);
                doorConnections.RemoveAt(i);
                doorMap.Remove(roomPair);
            }
        }

        yield return StartCoroutine(ConnectAndCheck());
    }




    /// <summary>
    /// Coroutine that performs breadth-first traversal from one room,
    /// connecting reachable rooms and pruning isolated ones.
    /// Big O Notation: O(N + E), where N is room count and E is door count.
    private IEnumerator ConnectAndCheck()
    {
        // Build room-to-door mapping
        var roomToDoors = new Dictionary<int, List<(int, int)>>();
        foreach (var roomPair in doorConnections)
        {
            if (!roomToDoors.ContainsKey(roomPair.Item1))
                roomToDoors[roomPair.Item1] = new List<(int, int)>();
            if (!roomToDoors.ContainsKey(roomPair.Item2))
                roomToDoors[roomPair.Item2] = new List<(int, int)>();
            roomToDoors[roomPair.Item1].Add(roomPair);
            roomToDoors[roomPair.Item2].Add(roomPair);
        }

        var visited = new HashSet<int>();
        bool isFirstRoom = true;
        int roomCounts = 0;

        while (visited.Count < rooms.Count)
        {
            roomCounts++;

            // Pick any unvisited room as start
            int startRoomId = rooms.First(r => !visited.Contains(r.id)).id;

            var queue = new Queue<int>();
            queue.Enqueue(startRoomId);
            visited.Add(startRoomId);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();

                if (!roomToDoors.ContainsKey(current))
                    continue;

                var neighbors = roomToDoors[current];
                neighbors.Shuffle(random);

                bool guaranteedConnected = false;
                foreach (var roomPair in neighbors)
                {
                    int neighbor = roomPair.Item1 == current ? roomPair.Item2 : roomPair.Item1;
                    if (visited.Contains(neighbor))
                        continue;

                    if (!guaranteedConnected || random.NextDouble() < branchChance)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                        connections.Add(roomPair);
                        guaranteedConnected = true;
                    }
                }

                yield return new WaitForFixedUpdate();
            }

            // If not first room, try to find first room
            if (!isFirstRoom)
            {
                // Find any door between this room and previous room
                var bridgingDoor = doorConnections.FirstOrDefault(pair =>
                    (visited.Contains(pair.a) && !visited.Contains(pair.b)) ||
                    (!visited.Contains(pair.a) && visited.Contains(pair.b)));

                // If found, add it to connections
                if (bridgingDoor != default)
                {
                    connections.Add(bridgingDoor);
                    visited.Add(bridgingDoor.a);
                    visited.Add(bridgingDoor.b);

                    Debug.Log($"Bridging disconnected component with door between Room {bridgingDoor.a} and Room {bridgingDoor.b}.");
                }
                else
                {
                    Debug.LogWarning("Could not find a bridging door between disconnected components.");
                }
            }

            isFirstRoom = false;
        }

        if (roomCounts == 1)
        {
            Debug.Log("Dungeon connectivity: All rooms connected");
        }
        else
        {
            Debug.Log($"Dungeon connectivity: {roomCounts} disconnected, merged.");
        }

        // Prune unused doors
        for (int i = doors.Count - 1; i >= 0; i--)
        {
            var roomPair = doorConnections[i];
            if (!connections.Contains(roomPair) && !connections.Contains((roomPair.Item2, roomPair.Item1)))
            {
                Destroy(doors[i]);
                doors.RemoveAt(i);
                doorSizes.RemoveAt(i);
                doorConnections.RemoveAt(i);
                doorMap.Remove(roomPair);
            }
        }

        // Signal generation complete
        onDungeonBuilt.Invoke();
        generationFinished = true;
        var randIndex = random.Next(rooms.Count);
        randomRoom = new Vector2Int((int)rooms[randIndex].transform.position.x, (int)rooms[randIndex].transform.position.z);
    }



    /// <summary>
    /// Draws gizmos in the editor to visualize rooms and doors during generation.
    private void OnDrawGizmos()
    {
        if (!generationFinished)
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
            foreach (var (a, b) in connections)
            {
                var A = rooms.Find(r => r.id == a); var B = rooms.Find(r => r.id == b);
                if (A != null && B != null) Gizmos.DrawLine(A.transform.position, B.transform.position);
            }
        }
    }
}

public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list, System.Random random)
    {
        int n = list.Count; while (n > 1)
        {
            n--; int k = random.Next(n + 1); var tmp = list[k]; list[k] = list[n]; list[n] = tmp;
        }
    }
}
