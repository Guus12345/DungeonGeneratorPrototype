using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DungeonAlgorithm : MonoBehaviour
{
    public Vector3 startingSize;
    private List<Transform> roomTransforms = new List<Transform>();
    private List<Vector3> roomSizes = new List<Vector3>();
    public int roomAmount;
    void Start()
    {
        roomSizes.Add(startingSize);
        roomTransforms.Add(transform);
        StartCoroutine(RoomGeneration());
    }

    IEnumerator RoomGeneration()
    {
        for (int i = 0; i < roomAmount; i++)
        {
            if(i % 2 == 0)
            {
                Debug.Log("Step 1: Start");
                int randomRoom = 0;
                int split = (int)Random.Range(1, roomSizes[randomRoom].x -1);

                yield return new WaitForSeconds(.1f);

                roomSizes.Add(new Vector3(roomSizes[randomRoom].x - split, roomSizes[randomRoom].y, roomSizes[randomRoom].z));

                yield return new WaitForSeconds(.1f);

                GameObject room1 = new GameObject("Room1");
                room1.transform.position = new Vector3((roomSizes[randomRoom].x - split) / 2, roomTransforms[randomRoom].position.y, roomTransforms[randomRoom].position.z);
                roomTransforms.Add(room1.transform);

                yield return new WaitForSeconds(.1f);

                roomSizes.Add(new Vector3(split, roomSizes[randomRoom].y, roomSizes[randomRoom].z));

                yield return new WaitForSeconds(.1f);

                GameObject room2 = new GameObject("Room2");
                room2.transform.position = new Vector3(roomSizes[randomRoom].x - (split / 2), roomTransforms[randomRoom].position.y, roomTransforms[randomRoom].position.z);
                roomTransforms.Add(room2.transform);

                yield return new WaitForSeconds(.1f); // Wait for 2 seconds

                roomSizes.RemoveAt(randomRoom);
                roomTransforms.RemoveAt(randomRoom);
            } else if(i % 2 == 1)
            {
                Debug.Log("Step 1: Start");
                int randomRoom = 0;
                int split = (int)Random.Range(1, roomSizes[randomRoom].z -1);

                yield return new WaitForSeconds(.1f);

                roomSizes.Add(new Vector3(roomSizes[randomRoom].x, roomSizes[randomRoom].y, roomSizes[randomRoom].z - split));

                yield return new WaitForSeconds(.1f);

                GameObject room1 = new GameObject("Room1");
                room1.transform.position = new Vector3(roomTransforms[randomRoom].position.x, roomTransforms[randomRoom].position.y, (roomSizes[randomRoom].z - split) / 2);
                roomTransforms.Add(room1.transform);

                yield return new WaitForSeconds(.1f);

                roomSizes.Add(new Vector3(roomSizes[randomRoom].x, roomSizes[randomRoom].y, split));

                yield return new WaitForSeconds(.1f);

                GameObject room2 = new GameObject("Room2");
                room2.transform.position = new Vector3(roomTransforms[randomRoom].position.x, roomTransforms[randomRoom].position.y, roomSizes[randomRoom].z - (split / 2));
                roomTransforms.Add(room2.transform);

                yield return new WaitForSeconds(.1f); // Wait for 2 seconds

                roomSizes.RemoveAt(randomRoom);
                roomTransforms.RemoveAt(randomRoom);
            }

            Debug.Log("Step 2: After 2 seconds");
        }  
    }
    void OnDrawGizmos()
    {
        for(int i = 0; i < roomTransforms.Count; i++)
        {
            DebugExtension.DrawLocalCube(roomTransforms[i], roomSizes[i], Color.blue);
        }
    }
}
