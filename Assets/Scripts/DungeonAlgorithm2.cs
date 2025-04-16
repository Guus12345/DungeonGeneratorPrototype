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
        roomSizes.Add(startingSize);
        roomTransforms.Add(new GameObject("StartingRoom").transform);
        roomTransforms[0].gameObject.transform.position = new Vector3(startingSize.x / 2, (float)startingSize.y / 2, startingSize.z / 2);
        rooms.Add(roomTransforms[0].gameObject);
        StartCoroutine(RoomGeneration());
    }

    IEnumerator RoomGeneration()
    {
        while (roomTransforms.Count > 0)
        {
            int CurrentRoom = roomTransforms.Count - 1;

            int Axis = rng.Next(0,1);

            int Limit = rng.Next(minLimit,maxLimit);

            yield return new WaitForFixedUpdate();

            if((Axis == 0 || roomSizes[CurrentRoom].z < Limit) && roomSizes[CurrentRoom].x > Limit) // X axis
            {
                int split = Random.Range(3, roomSizes[CurrentRoom].x - 3);
                float xMin = roomTransforms[CurrentRoom].position.x - (float)roomSizes[CurrentRoom].x / 2f;
                float xMax = roomTransforms[CurrentRoom].position.x + (float)roomSizes[CurrentRoom].x / 2f;

                yield return new WaitForFixedUpdate();
                
                roomTransforms.Add(new GameObject("Room" + roomTransforms.Count + "A").transform);
                roomTransforms.Last().position = new Vector3(xMin + (float)split / 2, roomTransforms[CurrentRoom].position.y, roomTransforms[CurrentRoom].position.z);
                roomSizes.Add(new Vector3Int(split, roomSizes[CurrentRoom].y, roomSizes[CurrentRoom].z));
                rooms.Add(roomTransforms.Last().gameObject);

                yield return new WaitForFixedUpdate();

                roomTransforms.Add(new GameObject("Room" + roomTransforms.Count + "B").transform);
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
                int split = Random.Range(3, roomSizes[CurrentRoom].z - 3);
                float zMin = roomTransforms[CurrentRoom].position.z - (float)roomSizes[CurrentRoom].z / 2f;
                float zMax = roomTransforms[CurrentRoom].position.z + (float)roomSizes[CurrentRoom].z / 2f;

                yield return new WaitForFixedUpdate();

                roomTransforms.Add(new GameObject("Room" + roomTransforms.Count + "A").transform);
                roomTransforms.Last().position = new Vector3(roomTransforms[CurrentRoom].position.x , roomTransforms[CurrentRoom].position.y, zMin + (float)split / 2);
                roomSizes.Add(new Vector3Int(roomSizes[CurrentRoom].x, roomSizes[CurrentRoom].y, split));
                rooms.Add(roomTransforms.Last().gameObject);

                yield return new WaitForFixedUpdate();

                roomTransforms.Add(new GameObject("Room" + roomTransforms.Count + "B").transform);
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


        }
    }
    

    // Update is called once per frame
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
    }

}

