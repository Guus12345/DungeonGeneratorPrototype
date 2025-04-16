using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonAlgorithm2 : MonoBehaviour
{
    public Vector3Int startingSize;
    private List<Transform> roomTransforms = new List<Transform>();
    private List<Transform> roomTransformsFinished = new List<Transform>();
    private List<Vector3Int> roomSizes = new List<Vector3Int>();
    private List<Vector3Int> roomSizesFinished = new List<Vector3Int>();
    private List<GameObject> rooms = new List<GameObject>();
    private List<Vector3Int> wallSize = new List<Vector3Int>();
    private List<GameObject> walls = new List<GameObject>();
    private System.Random rng;

    [SerializeField] private int minLimit;
    [SerializeField] private int maxLimit;
    [SerializeField] private bool useSeed;
    [SerializeField] private int seed;
    void Start()
    {
        if (!useSeed)
        {
            seed = Random.Range(-99999999,99999999);
        }
        Debug.Log("using seed: " + seed);
        rng = new System.Random(seed * (int)(Mathf.PI * 1000 + Mathf.PI));
        roomSizes.Add(new Vector3Int(startingSize.x, 0, startingSize.z));
        roomTransforms.Add(new GameObject("StartingRoom").transform);
        roomTransforms[0].gameObject.transform.position = new Vector3(startingSize.x / 2, 0, startingSize.z / 2);
        rooms.Add(roomTransforms[0].gameObject);
        StartCoroutine(RoomGeneration());
    }

    IEnumerator RoomGeneration()
    {
        while (roomTransforms.Count > 0)
        {
            int CurrentRoom = roomTransforms.Count - 1;

            int Axis = rng.Next(0,2);

            int Limit = rng.Next(minLimit,maxLimit);

            yield return new WaitForFixedUpdate();

            if((Axis == 0 || roomSizes[CurrentRoom].z < Limit) && roomSizes[CurrentRoom].x > Limit) // X axis
            {
                int split = rng.Next(3, roomSizes[CurrentRoom].x - 3);
                float xMin = roomTransforms[CurrentRoom].position.x - (float)roomSizes[CurrentRoom].x / 2f;
                float xMax = roomTransforms[CurrentRoom].position.x + (float)roomSizes[CurrentRoom].x / 2f;

                yield return new WaitForFixedUpdate();
                
                roomTransforms.Add(new GameObject("Room" + rooms.Count + "A").transform);
                roomTransforms.Last().position = new Vector3(xMin + (float)split / 2, roomTransforms[CurrentRoom].position.y, roomTransforms[CurrentRoom].position.z);
                roomSizes.Add(new Vector3Int(split, roomSizes[CurrentRoom].y, roomSizes[CurrentRoom].z));
                rooms.Add(roomTransforms.Last().gameObject);

                yield return new WaitForFixedUpdate();

                roomTransforms.Add(new GameObject("Room" + rooms.Count + "B").transform);
                roomTransforms.Last().position = new Vector3(xMax - ((roomSizes[CurrentRoom].x - split) / 2f), roomTransforms[CurrentRoom].position.y, roomTransforms[CurrentRoom].position.z);
                roomSizes.Add(new Vector3Int(roomSizes[CurrentRoom].x - split, roomSizes[CurrentRoom].y, roomSizes[CurrentRoom].z));
                rooms.Add(roomTransforms.Last().gameObject);

                yield return new WaitForFixedUpdate();

                rooms.RemoveAt(CurrentRoom);
                Destroy(roomTransforms[CurrentRoom].gameObject);
                roomTransforms.RemoveAt(CurrentRoom);
                roomSizes.RemoveAt(CurrentRoom);

            } else if((Axis == 1 || roomSizes[CurrentRoom].x < Limit)&& roomSizes[CurrentRoom].z > Limit) // Z Axis
            {
                int split = rng.Next(3, roomSizes[CurrentRoom].z - 3);
                float zMin = roomTransforms[CurrentRoom].position.z - (float)roomSizes[CurrentRoom].z / 2f;
                float zMax = roomTransforms[CurrentRoom].position.z + (float)roomSizes[CurrentRoom].z / 2f;

                yield return new WaitForFixedUpdate();

                roomTransforms.Add(new GameObject("Room" + rooms.Count + "A").transform);
                roomTransforms.Last().position = new Vector3(roomTransforms[CurrentRoom].position.x , roomTransforms[CurrentRoom].position.y, zMin + (float)split / 2);
                roomSizes.Add(new Vector3Int(roomSizes[CurrentRoom].x, roomSizes[CurrentRoom].y, split));
                rooms.Add(roomTransforms.Last().gameObject);

                yield return new WaitForFixedUpdate();

                roomTransforms.Add(new GameObject("Room" + rooms.Count + "B").transform);
                roomTransforms.Last().position = new Vector3(roomTransforms[CurrentRoom].position.x , roomTransforms[CurrentRoom].position.y, zMax - ((roomSizes[CurrentRoom].z - split) / 2f));
                roomSizes.Add(new Vector3Int(roomSizes[CurrentRoom].x, roomSizes[CurrentRoom].y, roomSizes[CurrentRoom].z - split));
                rooms.Add(roomTransforms.Last().gameObject);

                yield return new WaitForFixedUpdate();

                rooms.RemoveAt(CurrentRoom);
                Destroy(roomTransforms[CurrentRoom].gameObject);
                roomTransforms.RemoveAt(CurrentRoom);
                roomSizes.RemoveAt(CurrentRoom);
            } else
            {
                roomTransformsFinished.Add(roomTransforms[CurrentRoom]);
                roomTransforms.RemoveAt(CurrentRoom);
                roomSizesFinished.Add(roomSizes[CurrentRoom]);
                roomSizes.RemoveAt(CurrentRoom);
            }

            if(roomTransforms.Count == 0)
            {
                StartCoroutine(WallGeneration());
            }


        }
    }

    IEnumerator WallGeneration()
    {
        float tolerance = 0.01f;

        for(int i = 0; i < rooms.Count; i++)
        {
            Vector3 wall1Pos = new Vector3(roomTransformsFinished[i].position.x, (float)startingSize.y / 2, roomTransformsFinished[i].position.z - (float)roomSizesFinished[i].z / 2 + 0.5f);
            Vector3 wall2Pos = new Vector3(roomTransformsFinished[i].position.x, (float)startingSize.y / 2, roomTransformsFinished[i].position.z + (float)roomSizesFinished[i].z / 2 + 0.5f);
            Vector3 wall3Pos = new Vector3(roomTransformsFinished[i].position.x - (float)roomSizesFinished[i].x / 2 + 0.5f, (float)startingSize.y / 2, roomTransformsFinished[i].position.z);
            Vector3 wall4Pos = new Vector3(roomTransformsFinished[i].position.x + (float)roomSizesFinished[i].x / 2 + 0.5f, (float)startingSize.y / 2, roomTransformsFinished[i].position.z);

            yield return new WaitForFixedUpdate();

            if (!walls.Any(wall => Vector3.Distance(wall.transform.position, wall1Pos) < tolerance))
            {
            walls.Add(new GameObject("wall" + i + "A"));
            walls.Last().transform.position = wall1Pos;
            wallSize.Add(new Vector3Int(roomSizesFinished[i].x, startingSize.y, 1));
            }

            yield return new WaitForFixedUpdate();

            if (!walls.Any(wall => Vector3.Distance(wall.transform.position, wall2Pos) < tolerance))
            {
            walls.Add(new GameObject("wall" + i + "B"));
            walls.Last().transform.position = wall2Pos;
            wallSize.Add(new Vector3Int(roomSizesFinished[i].x, startingSize.y, 1));
            }

            yield return new WaitForFixedUpdate();

            if (!walls.Any(wall => Vector3.Distance(wall.transform.position, wall3Pos) < tolerance))
            {
            walls.Add(new GameObject("wall" + i + "C"));
            walls.Last().transform.position = wall3Pos;
            wallSize.Add(new Vector3Int(1, startingSize.y, roomSizesFinished[i].z));
            }

            yield return new WaitForFixedUpdate();

            if (!walls.Any(wall => Vector3.Distance(wall.transform.position, wall4Pos) < tolerance))
            {
            walls.Add(new GameObject("wall" + i + "D"));
            walls.Last().transform.position = wall4Pos;
            wallSize.Add(new Vector3Int(1, startingSize.y, roomSizesFinished[i].z));
            }

            yield return new WaitForFixedUpdate();
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw unfinished rooms in blue:
        Gizmos.color = Color.blue;
        foreach (Transform room in roomTransforms)
        {
            int index = roomTransforms.IndexOf(room);
            if(index < roomSizes.Count)
                Gizmos.DrawWireCube(room.position, roomSizes[index]);
        }
    
        // Draw finished rooms in green:
        Gizmos.color = Color.green;
        for (int i = 0; i < roomTransformsFinished.Count; i++)
        {
            if (i < roomSizesFinished.Count)
                Gizmos.DrawWireCube(roomTransformsFinished[i].position, roomSizesFinished[i]);
        }

        // Draw finished Walls in magenta:
        Gizmos.color = Color.magenta;
        int count = Mathf.Min(walls.Count, wallSize.Count);
        for (int i = 0; i < count; i++)
        {
            if (i < walls.Count)
                Gizmos.DrawWireCube(walls[i].transform.position, wallSize[i]);
        }
    }

}

