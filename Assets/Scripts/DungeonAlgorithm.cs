using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class DungeonAlgorithm : MonoBehaviour
{
    public Vector3Int startingSize;
    private List<Transform> roomTransforms = new List<Transform>();
    private List<Vector3Int> roomSizes = new List<Vector3Int>();
    public int roomDensity;
    [SerializeField] private bool useSeed;
    [SerializeField] private int seed;
    private int roomAmount;
    void Start()
    {
        if (!useSeed)
        {
            seed = Random.Range(-99999999,99999999);
        }
        Debug.Log("using seed: " + seed);
        roomSizes.Add(startingSize);
        roomTransforms.Add(new GameObject("StartingRoom").transform);
        roomTransforms[0].gameObject.transform.position = new Vector3Int(startingSize.x / 2, 1, startingSize.z / 2);
        roomAmount = (int)((startingSize.x / 100) * (startingSize.z / 100) * roomDensity);
        StartCoroutine(RoomGeneration());
    }

    IEnumerator RoomGeneration()
    {
        for (int i = 0; i < roomAmount; i++)
        {
            if(i % 2 == 0)
            {
                Random.InitState(seed + roomSizes.Count * (int)(Mathf.PI * 1000 + Mathf.PI));

                int biggestIndex = 0;
                float biggestSize = roomSizes[0].x; // Assume first room is the biggest

                for (int x = 1; x < roomSizes.Count; x++)
                {
                    if (((roomSizes[x].x + roomSizes[x].z) / 2) > biggestSize)
                    {
                        biggestSize = roomSizes[x].x;
                        biggestIndex = x;
                    }
                }

                int randomRoom = biggestIndex;

                Vector3 roomPosFloat = roomTransforms[randomRoom].position;

                Vector3Int roomPos = new Vector3Int(
                    Mathf.FloorToInt(roomPosFloat.x),
                    Mathf.FloorToInt(roomPosFloat.y),
                    Mathf.FloorToInt(roomPosFloat.z)
                );

                Vector3Int roomSize = roomSizes[randomRoom];

                int xMin = (int)(roomPos.x - Mathf.FloorToInt(roomSize.x / 2));
                int xMax = (int)(roomPos.x + Mathf.CeilToInt(roomSize.x / 2));

                int xSplitMin = (int)(roomPos.x - Mathf.FloorToInt(roomSize.x / 4));
                int xSplitMax = (int)(roomPos.x + Mathf.CeilToInt(roomSize.x / 4));

                int split = Random.Range(xSplitMin, xSplitMax);

                yield return new WaitForFixedUpdate();

                int leftWidth = split - xMin;
                Vector3Int leftPos = new Vector3Int(Mathf.FloorToInt((xMin + split) / 2), roomPos.y, roomPos.z);
                Vector3Int leftSize = new Vector3Int(leftWidth, roomSize.y, roomSize.z);

                yield return new WaitForFixedUpdate();

                int rightWidth = xMax - split;
                Vector3Int rightPos = new Vector3Int(Mathf.CeilToInt((split + xMax) / 2), roomPos.y, roomPos.z);
                Vector3Int rightSize = new Vector3Int(rightWidth, roomSize.y, roomSize.z);

                yield return new WaitForFixedUpdate();

                Destroy(roomTransforms[biggestIndex].gameObject);
                roomSizes.RemoveAt(randomRoom);
                roomTransforms.RemoveAt(randomRoom);

                yield return new WaitForFixedUpdate();

                roomSizes.Add(leftSize);
                roomTransforms.Add(new GameObject("Room" + roomTransforms.Count + "A").transform);
                roomTransforms.Last().position = leftPos;

                yield return new WaitForFixedUpdate();

                roomSizes.Add(rightSize);
                roomTransforms.Add(new GameObject("Room" + roomTransforms.Count + "B").transform);
                roomTransforms.Last().position = rightPos;

            
            } else if(i % 2 == 1)
            {
                Random.InitState(seed + roomSizes.Count * (int)(Mathf.PI * 1000 + Mathf.PI));

                int biggestIndex = 0;
                float biggestSize = roomSizes[0].z; // Assume first room is the biggest

                for (int x = 1; x < roomSizes.Count; x++)
                {
                    if (((roomSizes[x].x + roomSizes[x].z) / 2) > biggestSize)
                    {
                        biggestSize = roomSizes[x].z;
                        biggestIndex = x;
                    }
                }

                int randomRoom = biggestIndex;

                Vector3 roomPosFloat = roomTransforms[randomRoom].position;

                Vector3Int roomPos = new Vector3Int(
                    Mathf.FloorToInt(roomPosFloat.x),
                    Mathf.FloorToInt(roomPosFloat.y),
                    Mathf.FloorToInt(roomPosFloat.z)
                );

                Vector3Int roomSize = roomSizes[randomRoom];

                int zMin = (int)(roomPos.z - Mathf.FloorToInt(roomSize.z / 2));
                int zMax = (int)(roomPos.z + Mathf.CeilToInt(roomSize.z / 2));

                int zSplitMin = (int)(roomPos.z - Mathf.FloorToInt(roomSize.z / 4));
                int zSplitMax = (int)(roomPos.z + Mathf.CeilToInt(roomSize.z / 4));

                int split = Random.Range(zSplitMin, zSplitMax);

                yield return new WaitForFixedUpdate();

                int frontDepth = split - zMin;
                Vector3Int frontPos = new Vector3Int(roomPos.x, roomPos.y, Mathf.FloorToInt(zMin + split) / 2);
                Vector3Int frontSize = new Vector3Int(roomSize.x, roomSize.y, frontDepth);

                yield return new WaitForFixedUpdate();

                int backDepth = zMax - split;
                Vector3Int backPos = new Vector3Int(roomPos.x, roomPos.y, Mathf.CeilToInt(split + zMax) / 2);
                Vector3Int backSize = new Vector3Int(roomSize.x, roomSize.y, backDepth);

                yield return new WaitForFixedUpdate();

                Destroy(roomTransforms[biggestIndex].gameObject);
                roomSizes.RemoveAt(randomRoom);
                roomTransforms.RemoveAt(randomRoom);

                yield return new WaitForFixedUpdate();

                roomSizes.Add(frontSize);
                roomTransforms.Add(new GameObject("Room" + roomTransforms.Count + "A").transform);
                roomTransforms.Last().position = frontPos;

                yield return new WaitForFixedUpdate();

                roomSizes.Add(backSize);
                roomTransforms.Add(new GameObject("Room" + roomTransforms.Count + "B").transform);
                roomTransforms.Last().position = backPos;
            }

            //if (i >= roomAmount)
            //{
            //    StartCoroutine(DoorGeneration());
            //}
        }  
    }

    //IEnumerator DoorGeneration()
    //{
    //    Debug.Log("hi");
    //}
    void OnDrawGizmos()
    {
        for(int i = 0; i < roomTransforms.Count; i++)
        {
            if (roomTransforms.Count < roomAmount)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(roomTransforms[i].position, roomSizes[i]);
            } else 
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(roomTransforms[i].position, roomSizes[i]);
            }
        }
    }
}
