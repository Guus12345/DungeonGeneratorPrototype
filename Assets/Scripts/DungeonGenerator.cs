using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private TileMapGenerator tMapGen;
    private string tileMap;
    void Start()
    {
        tileMap = tMapGen.finishedTileMap;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
