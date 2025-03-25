using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class DungeonAlgorithm : MonoBehaviour
{
    public Vector3 startingSize;
    private List<Transform> roomTransforms = new List<Transform>();
    private List<Vector3> roomSizes = new List<Vector3>();
    public int roomDensity;
    private int roomAmount;
    void Start()
    {
        roomSizes.Add(startingSize);
        roomTransforms.Add(transform);
        roomAmount = (int)((startingSize.x / 100) * (startingSize.z / 100) * roomDensity);
        StartCoroutine(RoomGeneration());
    }

    IEnumerator RoomGeneration()
    {
        for (int i = 0; i < roomAmount; i++)
        {
            if(i % 2 == 0)
            {

                int biggestIndex = 0;
                float biggestSize = roomSizes[0].x; // Assume first room is the biggest

                for (int x = 1; x < roomSizes.Count; x++)
                {
                    if (roomSizes[x].x > biggestSize)
                    {
                        biggestSize = roomSizes[x].x;
                        biggestIndex = x;
                    }
                }

                int randomRoom = biggestIndex;

                Vector3 roomPos = roomTransforms[randomRoom].position;
                Vector3 roomSize = roomSizes[randomRoom];

                float xMin = roomPos.x - (roomSize.x / 2);
                float xMax = roomPos.x + (roomSize.x / 2);

                int split = (int)Random.Range(xMin + 1, xMax - 1);

                GameObject oldRoom = roomTransforms[randomRoom].gameObject;

                yield return new WaitForSeconds(.1f);

                float leftWidth = split - xMin;
                Vector3 leftPos = new Vector3((xMin + split) / 2, roomPos.y, roomPos.z);
                Vector3 leftSize = new Vector3(leftWidth, roomSize.y, roomSize.z);

                yield return new WaitForSeconds(.1f);

                float rightWidth = xMax - split;
                Vector3 rightPos = new Vector3((split + xMax) / 2, roomPos.y, roomPos.z);
                Vector3 rightSize = new Vector3(rightWidth, roomSize.y, roomSize.z);

                yield return new WaitForSeconds(.1f);

                roomSizes.RemoveAt(randomRoom);
                roomTransforms.RemoveAt(randomRoom);

                yield return new WaitForSeconds(.1f);

                roomSizes.Add(leftSize);
                roomTransforms.Add(new GameObject("Left Room").transform);
                roomTransforms.Last().position = leftPos;

                yield return new WaitForSeconds(.1f);

                roomSizes.Add(rightSize);
                roomTransforms.Add(new GameObject("Right Room").transform);
                roomTransforms.Last().position = rightPos;


            
            } else if(i % 2 == 1)
            {
                int biggestIndex = 0;
                float biggestSize = roomSizes[0].x; // Assume first room is the biggest

                for (int x = 1; x < roomSizes.Count; x++)
                {
                    if (roomSizes[x].z > biggestSize)
                    {
                        biggestSize = roomSizes[x].z;
                        biggestIndex = x;
                    }
                }

                int randomRoom = biggestIndex;

                Vector3 roomPos = roomTransforms[randomRoom].position;
                Vector3 roomSize = roomSizes[randomRoom];

                float zMin = roomPos.z - (roomSize.z / 2);
                float zMax = roomPos.z + (roomSize.z / 2);

                int split = (int)Random.Range(zMin + 1, zMax - 1);

                GameObject oldRoom = roomTransforms[randomRoom].gameObject;

                yield return new WaitForSeconds(.1f);

                float frontDepth = split - zMin;
                Vector3 frontPos = new Vector3(roomPos.x, roomPos.y, (zMin + split) / 2);
                Vector3 frontSize = new Vector3(roomSize.x, roomSize.y, frontDepth);

                yield return new WaitForSeconds(.1f);

                float backDepth = zMax - split;
                Vector3 backPos = new Vector3(roomPos.x, roomPos.y, (split + zMax) / 2);
                Vector3 backSize = new Vector3(roomSize.x, roomSize.y, backDepth);

                yield return new WaitForSeconds(.1f);

                roomSizes.RemoveAt(randomRoom);
                roomTransforms.RemoveAt(randomRoom);

                yield return new WaitForSeconds(.1f);

                roomSizes.Add(frontSize);
                roomTransforms.Add(new GameObject("Left Room").transform);
                roomTransforms.Last().position = frontPos;

                yield return new WaitForSeconds(.1f);

                roomSizes.Add(backSize);
                roomTransforms.Add(new GameObject("Right Room").transform);
                roomTransforms.Last().position = backPos;
            }
        }  
    }
    void OnDrawGizmos()
    {
        for(int i = 0; i < roomTransforms.Count; i++)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(roomTransforms[i].position, roomSizes[i]);
        }
    }
}
