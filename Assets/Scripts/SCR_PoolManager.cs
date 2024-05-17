using System.Collections.Generic;
using UnityEngine;

public class TilePoolManager : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject tilePoolContainer;

    private Queue<GameObject> tilePool = new Queue<GameObject>();

    public GameObject GetTileFromPool(Vector3 position)
    {
        if (tilePool.Count > 0)
        {
            GameObject tile = tilePool.Dequeue();
            tile.SetActive(true);
            tile.transform.position = position;
            return tile;
        }
        else
        {
            GameObject newTile = Instantiate(tilePrefab, position, Quaternion.identity);
            newTile.transform.SetParent(tilePoolContainer.transform);
            return newTile;
        }
    }

    public void ReturnTileToPool(GameObject tile)
    {
        tile.SetActive(false);
        tilePool.Enqueue(tile);
    }
}
