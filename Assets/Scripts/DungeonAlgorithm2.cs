using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonAlgorithm2 : MonoBehaviour
{
    public Vector3Int startingSize;
    [SerializeField] private int minSizeLimit;
    [SerializeField] private int maxSizeLimit;
    [SerializeField] private bool useSeed;
    [SerializeField] private int seed;
    [SerializeField] private int doorWidth = 2;
    [SerializeField, Range(0f, 1f)] private float doorProbability = 0.5f;

    private System.Random rng;
    private int nextRoomId = 0;

    private class RoomNode
    {
        public int id;
        public Transform transform;
        public Vector3Int size;
        public List<int> neighbors = new List<int>();

        public RoomNode(int id, Transform t, Vector3Int s)
        {
            this.id = id;
            transform = t;
            size = s;
        }
    }

    private List<RoomNode> rooms = new List<RoomNode>();
    private List<GameObject> walls = new List<GameObject>();
    // Store branching connections for Gizmos
    private List<(int a, int b)> connections = new List<(int a, int b)>();
    private List<Vector3Int> wallSizes = new List<Vector3Int>();

    private void Start()
    {
        if (!useSeed)
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        rng = new System.Random(seed);

        // Create initial room
        GameObject go = new GameObject("Room_" + nextRoomId);
        go.transform.position = new Vector3(startingSize.x / 2f, 0, startingSize.z / 2f);
        var root = new RoomNode(nextRoomId++, go.transform, new Vector3Int(startingSize.x, 0, startingSize.z));
        rooms.Add(root);

        StartCoroutine(RoomGeneration());
    }

    private IEnumerator RoomGeneration()
    {
        var unfinished = new List<RoomNode>(rooms);
        rooms.Clear();

        while (unfinished.Count > 0)
        {
            var current = unfinished[unfinished.Count - 1];
            unfinished.RemoveAt(unfinished.Count - 1);

            int axis = rng.Next(0, 2);
            int limit = rng.Next(minSizeLimit, maxSizeLimit);
            Vector3 pos = current.transform.position;
            Vector3Int sz = current.size;

            bool splitX = (axis == 0 || sz.z < limit) && sz.x > limit;
            bool splitZ = (axis == 1 || sz.x < limit) && sz.z > limit;

            if (splitX)
            {
                int cut = rng.Next(3, sz.x - 3);
                float xMin = pos.x - sz.x / 2f;
                float xMax = pos.x + sz.x / 2f;

                // Room A
                GameObject goA = new GameObject("Room_" + nextRoomId);
                goA.transform.position = new Vector3(xMin + cut / 2f, pos.y, pos.z);
                var nodeA = new RoomNode(nextRoomId++, goA.transform, new Vector3Int(cut, 0, sz.z));

                // Room B
                GameObject goB = new GameObject("Room_" + nextRoomId);
                goB.transform.position = new Vector3(xMax - (sz.x - cut) / 2f, pos.y, pos.z);
                var nodeB = new RoomNode(nextRoomId++, goB.transform, new Vector3Int(sz.x - cut, 0, sz.z));

                nodeA.neighbors.Add(nodeB.id);
                nodeB.neighbors.Add(nodeA.id);

                unfinished.Add(nodeA);
                unfinished.Add(nodeB);
            }
            else if (splitZ)
            {
                int cut = rng.Next(3, sz.z - 3);
                float zMin = pos.z - sz.z / 2f;
                float zMax = pos.z + sz.z / 2f;

                // Room A
                GameObject goA = new GameObject("Room_" + nextRoomId);
                goA.transform.position = new Vector3(pos.x, pos.y, zMin + cut / 2f);
                var nodeA = new RoomNode(nextRoomId++, goA.transform, new Vector3Int(sz.x, 0, cut));

                // Room B
                GameObject goB = new GameObject("Room_" + nextRoomId);
                goB.transform.position = new Vector3(pos.x, pos.y, zMax - (sz.z - cut) / 2f);
                var nodeB = new RoomNode(nextRoomId++, goB.transform, new Vector3Int(sz.x, 0, sz.z - cut));

                nodeA.neighbors.Add(nodeB.id);
                nodeB.neighbors.Add(nodeA.id);

                unfinished.Add(nodeA);
                unfinished.Add(nodeB);
            }
            else
            {
                rooms.Add(current);
            }

            yield return new WaitForFixedUpdate();
        }

        StartCoroutine(WallGeneration());
    }

    private IEnumerator WallGeneration()
    {
        float halfY = startingSize.y / 2f;

        for (int i = 0; i < rooms.Count; i++)
        {
            var node = rooms[i];
            Vector3 pos = node.transform.position;
            Vector3Int sz = node.size;

            Vector3[] positions = new Vector3[4]
            {
                pos + new Vector3(0, halfY, -sz.z/2f + 0.5f),
                pos + new Vector3(0, halfY,  sz.z/2f + 0.5f),
                pos + new Vector3(-sz.x/2f + 0.5f, halfY, 0),
                pos + new Vector3( sz.x/2f + 0.5f, halfY, 0)
            };

            Vector3Int[] sizes = new Vector3Int[4]
            {
                new Vector3Int(sz.x, startingSize.y, 1),
                new Vector3Int(sz.x, startingSize.y, 1),
                new Vector3Int(1, startingSize.y, sz.z),
                new Vector3Int(1, startingSize.y, sz.z)
            };

            bool[] isX = new bool[4] { true, true, false, false };

            for (int k = 0; k < 4; k++)
            {
                TryAddWall(positions[k], sizes[k], $"wall_{node.id}_{k}", isX[k]);
                yield return new WaitForFixedUpdate();
            }
        }

        StartCoroutine(ConnectAndPrune());
        yield return null;
    }

    private void TryAddWall(Vector3 pos, Vector3Int size, string name, bool isXAxis)
    {
        const float eps = 0.01f;
        bool onEdge = false;
        foreach (var node in rooms)
        {
            var rPos = node.transform.position;
            var rSz = node.size;
            if (isXAxis)
            {
                // Horizontal wall must sit at z = roomZ +/- (roomDepth/2 + 0.5)
                float z0 = rPos.z - rSz.z / 2f + 0.5f;
                float z1 = rPos.z + rSz.z / 2f + 0.5f;
                if ((Mathf.Abs(pos.z - z0) < eps || Mathf.Abs(pos.z - z1) < eps)
                    && pos.x > rPos.x - rSz.x / 2f - eps
                    && pos.x < rPos.x + rSz.x / 2f + eps)
                {
                    onEdge = true; break;
                }
            }
            else
            {
                // Vertical wall must sit at x = roomX +/- (roomWidth/2 + 0.5)
                float x0 = rPos.x - rSz.x / 2f + 0.5f;
                float x1 = rPos.x + rSz.x / 2f + 0.5f;
                if ((Mathf.Abs(pos.x - x0) < eps || Mathf.Abs(pos.x - x1) < eps)
                    && pos.z > rPos.z - rSz.z / 2f - eps
                    && pos.z < rPos.z + rSz.z / 2f + eps)
                {
                    onEdge = true; break;
                }
            }
        }
        if (!onEdge)
            return;  // this wall isn’t actually on any room boundary

        Vector2 p2 = new Vector2(pos.x, pos.z);
        Vector2 s2 = new Vector2(size.x, size.z);

        for (int j = 0; j < walls.Count; j++)
        {
            Vector3 wp = walls[j].transform.position;
            Vector2 wp2 = new Vector2(wp.x, wp.z);
            Vector2 ws2 = new Vector2(wallSizes[j].x, wallSizes[j].z);

            if (isXAxis)
            {
                if (Mathf.Abs(p2.y - wp2.y) < 0.01f)
                {
                    float minA = p2.x - s2.x / 2f, maxA = p2.x + s2.x / 2f;
                    float minB = wp2.x - ws2.x / 2f, maxB = wp2.x + ws2.x / 2f;
                    if (maxA > minB && minA < maxB)
                        return;
                }
            }
            else
            {
                if (Mathf.Abs(p2.x - wp2.x) < 0.01f)
                {
                    float minA = p2.y - s2.y / 2f, maxA = p2.y + s2.y / 2f;
                    float minB = wp2.y - ws2.y / 2f, maxB = wp2.y + ws2.y / 2f;
                    if (maxA > minB && minA < maxB)
                        return;
                }
            }
        }

        GameObject w = new GameObject(name);
        w.transform.position = new Vector3(
            pos.x,
            startingSize.y * 0.5f,
            pos.z
        );
        walls.Add(w);
        wallSizes.Add(size);
    }

    // Returns, for each room ID, a list of (neighborID, isXAxis)
    Dictionary<int, List<(int neighborId, bool isXAxis)>> ComputeFinalAdjacency()
    {
        var adj = new Dictionary<int, List<(int, bool)>>();
        foreach (var A in rooms)
            adj[A.id] = new List<(int, bool)>();

        for (int i = 0; i < rooms.Count; i++)
        {
            var A = rooms[i];
            var Amin = A.transform.position - new Vector3(A.size.x / 2f, 0, A.size.z / 2f);
            var Amax = A.transform.position + new Vector3(A.size.x / 2f, 0, A.size.z / 2f);

            for (int j = i + 1; j < rooms.Count; j++)
            {
                var B = rooms[j];
                var Bmin = B.transform.position - new Vector3(B.size.x / 2f, 0, B.size.z / 2f);
                var Bmax = B.transform.position + new Vector3(B.size.x / 2f, 0, B.size.z / 2f);

                // Horizontal adjacency?
                bool touchX = Mathf.Approximately(Amax.x, Bmin.x) || Mathf.Approximately(Bmax.x, Amin.x);
                bool overlapZ = Amax.z > Bmin.z && Bmax.z > Amin.z;
                if (touchX && overlapZ)
                {
                    adj[A.id].Add((B.id, true));
                    adj[B.id].Add((A.id, true));
                    continue;
                }

                // Vertical adjacency?
                bool touchZ = Mathf.Approximately(Amax.z, Bmin.z) || Mathf.Approximately(Bmax.z, Amin.z);
                bool overlapX = Amax.x > Bmin.x && Bmax.x > Amin.x;
                if (touchZ && overlapX)
                {
                    adj[A.id].Add((B.id, false));
                    adj[B.id].Add((A.id, false));
                }
            }
        }

        return adj;
    }



    private IEnumerator ConnectAndPrune()
    {
        // Rebuild adjacency on *final* rooms
        var adjacency = ComputeFinalAdjacency();
        int startId = adjacency.Keys.ElementAtOrDefault(rng.Next(adjacency.Count));
        var visited = new HashSet<int> { startId };
        var queue = new Queue<int>();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();

            // pick up to 2 neighbors at random
            var choices = adjacency[cur]
                .Where(pair => !visited.Contains(pair.neighborId))
                .OrderBy(_ => rng.Next())
                .Take(rng.Next(1,3))
                .ToList();

            foreach (var (nxt, isXAxis) in choices)
            {
                visited.Add(nxt);
                queue.Enqueue(nxt);
                connections.Add((cur, nxt));

                // carve hole with the known orientation
                SplitWallForDoor(cur, nxt, isXAxis);
                yield return new WaitForFixedUpdate();
            }
        }

        Debug.Log("[ConnectAndPrune] BFS complete, pruning...");
        // Prune unreachable
        var toRemove = rooms.Where(r => !visited.Contains(r.id)).ToList();
        foreach (var node in toRemove)
        {
            rooms.Remove(node);
            Destroy(node.transform.gameObject);
            Debug.Log($"[Prune] Removed room {node.id}");
        }
    }


    void SplitWallForDoor(int idA, int idB, bool isXAxisWall)
    {
        var A = rooms.Find(r => r.id == idA);
        var B = rooms.Find(r => r.id == idB);
        Vector3 center = (A.transform.position + B.transform.position) / 2f;

        // Find the wall at that spot
        for (int i = 0; i < walls.Count; i++)
        {
            if (!new Bounds(walls[i].transform.position, wallSizes[i]).Contains(center))
                continue;

            // remove original
            Destroy(walls[i]);
            walls.RemoveAt(i);
            wallSizes.RemoveAt(i);

            // Now split into two segments along the correct axis:
            float fullLen = isXAxisWall ? wallSizes[i].x : wallSizes[i].z;
            float halfGap = doorWidth / 2f;
            float halfSeg = (fullLen - doorWidth) / 2f;
            float h = startingSize.y;

            Vector3 size1 = isXAxisWall
                ? new Vector3(halfSeg, h, 1)
                : new Vector3(1, h, halfSeg);

            Vector3 offset = isXAxisWall
                ? new Vector3(-halfGap - halfSeg / 2f, 0, 0)
                : new Vector3(0, 0, -halfGap - halfSeg / 2f);

            // Left/Bottom piece
            var wA = new GameObject($"doorL_{idA}_{idB}");
            wA.transform.position = new Vector3(center.x + offset.x, startingSize.y/2f, center.z + offset.z);
            walls.Add(wA);
            wallSizes.Add(Vector3Int.RoundToInt(size1));

            // Right/Top piece
            var wB = new GameObject($"doorR_{idA}_{idB}");
            wB.transform.position = new Vector3(center.x + offset.x, startingSize.y / 2f, center.z + offset.z);
            walls.Add(wB);
            wallSizes.Add(Vector3Int.RoundToInt(size1));

            break;
        }
    }


    private void OnDrawGizmos()
    {
        // Draw rooms
        Gizmos.color = Color.green;
        foreach (var node in rooms)
        {
            if (node.transform == null) continue;
            Gizmos.DrawWireCube(node.transform.position, node.size);
        }

        // Draw walls
        Gizmos.color = Color.magenta;
        for (int i = 0; i < walls.Count; i++)
        {
            if (walls[i] == null) continue;
            Gizmos.DrawWireCube(walls[i].transform.position, wallSizes[i]);
        }

        // Draw branching connections
        Gizmos.color = Color.cyan;
        foreach (var pair in connections)
        {
            int a = pair.a;
            int b = pair.b;
            var nodeA = rooms.Find(n => n.id == a);
            var nodeB = rooms.Find(n => n.id == b);
            if (nodeA == null || nodeB == null) continue;
            if (nodeA.transform == null || nodeB.transform == null) continue;
            Gizmos.DrawLine(nodeA.transform.position, nodeB.transform.position);
        }
    
        Gizmos.color = Color.green;
        foreach (var node in rooms)
            Gizmos.DrawWireCube(node.transform.position, node.size);

        Gizmos.color = Color.magenta;
        for (int i = 0; i<walls.Count; i++)
            Gizmos.DrawWireCube(walls[i].transform.position, wallSizes[i]);
    }
}

public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list, System.Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
