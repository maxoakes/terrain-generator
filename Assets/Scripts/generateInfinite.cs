using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Tile
{
	public GameObject theTile;
	public float creationTime;

	public Tile(GameObject t, float ct)
	{
		theTile = t;
		creationTime = ct;
	}
}

public class generateInfinite : MonoBehaviour {

	public GameObject plane;
	public GameObject player;

	int planeSize = 10;
	public int halfTilesX = 10;
	public int halfTilesZ = 10;

	Vector3 startPos;

	Hashtable tiles = new Hashtable();

	// Use this for initialization
	void Start () {
		this.gameObject.transform.position = Vector3.zero;
		startPos = Vector3.zero;

		float updateTime = Time.realtimeSinceStartup;

		for (int x =-halfTilesX; x<halfTilesX; x++)
		{
			for (int z=-halfTilesZ; z<halfTilesZ; z++)
			{
				Vector3 pos = new Vector3((x*planeSize+startPos.x),0,(z*planeSize+startPos.z));
				GameObject t = (GameObject) Instantiate(plane,pos,Quaternion.identity);

				string tilename = "tile_"+((int)(pos.x))+"_"+((int)(pos.z));
				t.name = tilename;
				Tile tile = new Tile(t, updateTime);
				tiles.Add(tilename, tile);
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		int xMove = (int)(player.transform.position.x - startPos.x);
		int zMove = (int)(player.transform.position.z - startPos.z);

		if (Mathf.Abs(xMove) >= planeSize || Mathf.Abs(zMove) >= planeSize)
		{
			float updateTime = Time.realtimeSinceStartup;

			int playerX = (int)(Mathf.Floor(player.transform.position.x/planeSize)*planeSize);
			int playerZ = (int)(Mathf.Floor(player.transform.position.z/planeSize)*planeSize);

			for (int x = -halfTilesX; x<halfTilesX; x++)
			{
				for (int z = -halfTilesZ; z<halfTilesZ; z++)
				{
					Vector3 pos = new Vector3((x*planeSize+playerX),0,(z*planeSize+playerZ));

					string tilename = "tile_"+((int)(pos.x))+"_"+((int)(pos.z));

					if (!tiles.ContainsKey(tilename))
					{
						GameObject t = (GameObject) Instantiate(plane, pos, Quaternion.identity);
						t.name = tilename;
						Tile tile = new Tile(t, updateTime);
						tiles.Add(tilename, tile);
					}
					else
					{
						(tiles[tilename] as Tile).creationTime = updateTime;
					}
				}
			}

			Hashtable newTerrain = new Hashtable();
			foreach(Tile t in tiles.Values)
			{
				if (t.creationTime != updateTime)
				{
					Destroy(t.theTile);
				}
				else
				{
					newTerrain.Add(t.theTile.name,t);
				}
			}

			tiles = newTerrain;
			startPos = player.transform.position;
		}
	}
}
