using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class VaultableTilemap : MonoBehaviour
{

    private BoxCollider2D bc;
    // Start is called before the first frame update
    private void cornerSetup()
    {
        Tilemap tilemap = gameObject.GetComponent<Tilemap>();
        for (int x = tilemap.cellBounds.min.x; x < tilemap.cellBounds.max.x; x++)
        {
            for (int y = tilemap.cellBounds.min.y; y < tilemap.cellBounds.max.y; y++)
            {
                int z = 0;

                if(tilemap.GetTile(new Vector3Int(x, y, z)) != null)
                {
                    if(tilemap.GetTile(new Vector3Int(x, y+1, z)) == null)
                    {
                        if (tilemap.GetTile(new Vector3Int(x - 1, y + 1, z)) == null
                            && tilemap.GetTile(new Vector3Int(x - 1, y, z)) == null)
                        {
                            GameObject corner = new GameObject("Corner");
                            corner.transform.SetParent(transform, false);
                            TileMapCorner corn = corner.AddComponent<TileMapCorner>();
                            corn.transform.position = tilemap.CellToLocal(new Vector3Int(x, y, z)) + new Vector3(0, 1, 0);
                            corn.setDirection(false);
                        }
                        if (tilemap.GetTile(new Vector3Int(x + 1, y + 1, z)) == null
                            && tilemap.GetTile(new Vector3Int(x + 1, y, z)) == null)
                        {
                            GameObject corner = new GameObject("Corner");
                            corner.transform.SetParent(transform, false);
                            TileMapCorner corn = corner.AddComponent<TileMapCorner>();
                            corn.transform.position = tilemap.CellToLocal(new Vector3Int(x, y, z)) + new Vector3(1, 1, 0);
                            corn.setDirection(true);
                        }
                    }
                }
            }

        }
    }
    void Start()
    {
        cornerSetup();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
