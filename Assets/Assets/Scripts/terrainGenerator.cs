using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class TerrainTile
{
	public Terrain tile;
	public float creationTime;

	public TerrainTile(Terrain t, float ct)
	{
		tile = t;
		creationTime = ct;
	}
}

public class terrainGenerator : MonoBehaviour {

	public bool debug_drawTrees = true;
	public bool debug_ContinueGenerating = false;
	public bool debug_renderWater = false;
	public bool debug_DrawGrass = false;

	public GameObject player;
	public Terrain defaultLand;
	public GameObject water;

	public int size = 256;
	public int height = 200;

	//0=ocean,1=beach,2=forstpine,3=shortgreengrass,4=longgreengrass,
	//5=brownwildfield,6=brownstonedirthill,7=drygrass,8=desertsand,9=browngreengrassblend
	public int oceanIndex = 0;
	public int beachIndex = 1;
	public int forestIndex = 3;
	public int plainsIndex = 4;
	public int cliffIndex = 5;
	public int mountainIndex = 6;
	public int desertIndex = 8;

	[Range(0.0f, 1.0f)]
	public float seaLevel = 0.1f;
	[Range(0.0f, 1.0f)]
	public float beachLevel = 0.2f;
	[Range(0.0f, 1.0f)]
	public float landLevel = 0.3f;
	[Range(0.0f, 1.0f)]
	public float mountainLevel = 0.6f;
	[Range(0.0f, 1.0f)]
	public float snowLevel = 0.8f;
	[Range(0.0f, 90.0f)]
	public float steep = 20.0f;
	[Range(1, 1024)]
	public int treeDensity = 250;
	//scale of perlin noise plain to terrain, this makes it 1:1 by default, and altered by frequency variable
	private float scale = 0.00390625f;

	//slider variables
	[Range(0.0f, .5f)]
	public float biomeSize = 0.02f;
	[Range(0.0f, 7.0f)]
	public float frequency = 2.0f;
	[Range(0.0f, 1.0f)]
	public float o1 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float o2 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float o3 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float o4 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float o5 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float o6 = 0.0f;
	[Range(0.0f, 10.0f)]
	public float power = 1.0f;

	[Range(0.0f, 1.0f)]
	public float m1 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float m2 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float m3 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float m4 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float m5 = 0.0f;
	[Range(0.0f, 1.0f)]
	public float m6 = 0.0f;

	public int cullRadiusX = 2;
	public int cullRadiusZ = 2;

	public float seedLand = 5000f;
	public float seedBiome = 3000f;
	Vector3 startPos;
	Hashtable chunkTable = new Hashtable();

	// Use this for initialization
	void Start ()
	{
		//Set time and space origins
		this.gameObject.transform.position = Vector3.zero;
		startPos = Vector3.zero;
		float updateTime = Time.realtimeSinceStartup;

		//create starint planes
		for (int x =-cullRadiusX; x<cullRadiusX; x++)
		{
			for (int z=-cullRadiusZ; z<cullRadiusZ; z++)
			{
				//get position of the new landscape
				Vector3 pos = new Vector3((x*size+startPos.x),0,(z*size+startPos.z));

				Terrain t = generateTerrain(pos);

				//log the terrain in a table
				addChunk(t,updateTime);
			}
		}
		setNeighbors();
	}

	//log chunk into our table, so we can retrieve them when get to its location rather than rebuilding it
	void addChunk(Terrain t, float time)
	{
		string chunkName = "chunk_"+((int)(t.transform.position.x/size))+"_"+((int)(t.transform.position.z/size));
		t.name = chunkName;
		TerrainTile tile = new TerrainTile(t, time);
		chunkTable.Add(chunkName, tile);
	}

	//Create terrain data, including heightmaps, splatmaps, biomes and features
	Terrain generateTerrain(Vector3 pos)
	{
		//make the terrain and its data
		Terrain t = (Terrain) Instantiate(defaultLand,pos,Quaternion.identity);
		Vector3 seaCenter = new Vector3(pos.x+(size/2),seaLevel*height,pos.z+(size/2));
		if (debug_renderWater)
		{
			GameObject sea = (GameObject) Instantiate(water,seaCenter,Quaternion.identity);
			sea.name = "water_"+pos.x+"_"+pos.z;
			float waterScale = ((float)size)/10f;
			sea.transform.localScale = new Vector3 (waterScale,0,waterScale);
		}
		t.terrainData.alphamapResolution = size+1;

		TerrainData newChunk = new TerrainData();
		newChunk.alphamapResolution = size;
		newChunk.heightmapResolution = size;
		newChunk.size = new Vector3(size,height,size);
		newChunk.SetHeights(0,0,generateHeights(t));
		t.terrainData = newChunk;

		//get the textures to be used
		t.terrainData.splatPrototypes = defaultLand.terrainData.splatPrototypes;

		//create ground textures
		float[,,] textures = createSplatmap(t);
		newChunk.SetAlphamaps(0,0,textures);

		//populate ground details
		newChunk.SetDetailResolution(size,16);
		newChunk.detailPrototypes = defaultLand.terrainData.detailPrototypes;
		newChunk.treePrototypes = defaultLand.terrainData.treePrototypes;
		newChunk.wavingGrassAmount = newChunk.wavingGrassAmount/2;
		t = populateGround(t);

		//final applying of things
		t.terrainData = newChunk;
		t.GetComponent<TerrainCollider>().terrainData = t.terrainData;
		t.Flush();
		return t;
	}

	//This is where all the customization takes place. Uses iterations of Perlin noise to generate a landscape
	//Makes a 2D array and adds float values to each cell that would map to a plane, making a 3D mountainous plane
	float[,] generateHeights(Terrain terr)
	{
		//make the 2D array. +1 on each size so there is no gap between this terrain plane and the ones to the north
		//and east
		int alteredSize = size+1;
		float[,] heights = new float[alteredSize,alteredSize];
		for (int z=0; z<alteredSize; z++)
		{
			for (int x=0; x<alteredSize; x++)
			{
				//first 1/3 = forest, plains
				//second 1/3 = desert
				//third 1/3 = mountainous

				//get default location heights
				float xCoord = (seedLand+(terr.transform.position.x+x))*scale;
				float zCoord = (seedLand+(terr.transform.position.z+z))*scale;

				//put detail into the heightmap
				float e = (
					(o1 * Mathf.PerlinNoise(1*xCoord*frequency,1*zCoord*frequency)) + 
					(o2 * Mathf.PerlinNoise(2*xCoord*frequency,2*zCoord*frequency)) + 
					(o3 * Mathf.PerlinNoise(4*xCoord*frequency,4*zCoord*frequency)) + 
					(o4 * Mathf.PerlinNoise(8*xCoord*frequency,8*zCoord*frequency)) + 
					(o5 * Mathf.PerlinNoise(16*xCoord*frequency,16*zCoord*frequency)) + 
					(o6 * Mathf.PerlinNoise(32*xCoord*frequency,32*zCoord*frequency)));

				e /= (o1+o2+o3+o4+o5+o6);

				float finalHeight = Mathf.Pow(e,power);
				float biomeOffset = getBiomeMap(xCoord,zCoord);
				finalHeight = finalHeight*(biomeOffset*2);

				heights[z,x] = finalHeight;
			}
		}
		return heights;
	}

	float[,,] createSplatmap(Terrain t)
	{
		float[,,] splatmapData = new float[size,size,t.terrainData.alphamapLayers];

		for (int z=0; z<size;z++)
		{
			for (int x=0; x<size; x++)
			{
				float nx = (float)x * 1.0f / (float)(size - 1);
				float nz = (float)z * 1.0f / (float)(size - 1);
				float xCoord = (seedBiome+(t.transform.position.x+(x)))*scale;
				float zCoord = (seedBiome+(t.transform.position.z+(z)))*scale;

				//moisture map, unused
				float m = (
					(m1 * Mathf.PerlinNoise(1*xCoord,1*zCoord)) + 
					(m2 * Mathf.PerlinNoise(2*xCoord,2*zCoord)) + 
					(m3 * Mathf.PerlinNoise(4*xCoord,4*zCoord)) + 
					(m4 * Mathf.PerlinNoise(8*xCoord,8*zCoord)) + 
					(m5 * Mathf.PerlinNoise(16*xCoord,16*zCoord)) + 
					(m6 * Mathf.PerlinNoise(32*xCoord,32*zCoord)));

				m /= (m1+m2+m3+m4+m5+m6);

				float terrainHeight = t.terrainData.GetHeight(z,x);
				float angle = t.terrainData.GetSteepness(nz, nx);
				int terrainType = getBiome(terrainHeight,m,angle);
				for (int j=0; j<t.terrainData.alphamapLayers; j++)
				{
					splatmapData[x,z,j] = 0;
				}
				splatmapData[x,z,terrainType] = 1;
			}
		}
		return splatmapData;
	}

	int getBiome(float elevation, float moisture, float angle)
	{
		//make el a fraction of the highest height
		float el = elevation/height;
		if (el < seaLevel)
		{
			return oceanIndex;
		}
		else if (el > seaLevel && el < landLevel)
		{
			return beachIndex;
		}
		else if (el > landLevel && el < mountainLevel)
		{
			if (angle > steep)
			{
				return plainsIndex;
			}
			else
			{
				return forestIndex;
			}
		}
		else if (el > mountainLevel && el < snowLevel)
		{
			if (angle > steep)
			{
				return cliffIndex;
			}
			else
			{
				return mountainIndex;
			}
		}
		else
		{
				return cliffIndex;
		}
	}

	//get the height offset according to the biome.
	float getBiomeMap(float x, float z)
	{
		float y = Mathf.PerlinNoise(biomeSize*x,biomeSize*z);
		return y;
	}

	Terrain populateGround(Terrain t)
	{
		float[,,] maps = t.terrainData.GetAlphamaps(0, 0, size, size);
		int unitsForest = 0;

		int[,] grassMap1 = new int[size,size];
		int[,] grassMap2 = new int[size,size];
		int[,] grassMap3 = new int[size,size];
		int[,] grassMap4 = new int[size,size];

		//apply grass
		for (int z = 0; z < size; z++)
		{
			for (int x = 0; x < size; x++)
			{
				if (maps[z,x,plainsIndex] != 0f)
				{
					grassMap1[z, x] = 1;
					grassMap2[z, x] = 0;
					grassMap3[z, x] = 1;
					grassMap4[z, x] = 1;
					unitsForest++;
				}
				else if (maps[z,x,forestIndex] != 0f)
				{
					grassMap1[z, x] = 1;
					grassMap2[z, x] = 1;
					grassMap3[z, x] = 1;
					grassMap4[z, x] = 1;
					unitsForest++;
				}
				else
				{
					grassMap1[z, x] = 0;
					grassMap2[z, x] = 0;
					grassMap3[z, x] = 0;
					grassMap4[z, x] = 0;
				}
			}

			if (debug_DrawGrass)
			{
				t.terrainData.SetDetailLayer(0, 0, 0, grassMap1);
				t.terrainData.SetDetailLayer(0, 0, 1, grassMap2);
				t.terrainData.SetDetailLayer(0, 0, 2, grassMap3);
				t.terrainData.SetDetailLayer(0, 0, 3, grassMap4);
			}
		}
		if (debug_drawTrees)
		{
			//apply trees
			bool[,] placement = new bool[size,size]; //true=tree there
			//float plainsTreeChance = 0.5f;
			//float beachTreeChance = 0.01f;
			//float mountainTreeChance = 0.03f;
			float mountainTreeChance = 1f;
			float beachTreeChance = 1f;
			float plainsTreeChance = 1f;

			int numTrees = 0;
			float realDensity = treeDensity;
			//float realDensity = ((float)unitsForest/((float)size*(float)size))*(float)treeDensity;
			//print("Input Density: "+treeDensity+" realDensity: "+realDensity+" unitsForest: "+unitsForest+" size"+size);
			while (numTrees < realDensity)
			{
				int x = Random.Range(0,size-1);
				int z = Random.Range(0,size-1);

				if (!placement[x,z])
				{
					float chance = Random.value;
					bool canPlace = (maps[z,x,forestIndex] != 0f) ||
						(maps[z,x,plainsIndex] != 0f && chance < plainsTreeChance) ||
						(maps[z,x,beachIndex] != 0f && chance < beachTreeChance) ||
						(maps[z,x,mountainIndex] != 0f && chance < mountainTreeChance);
					
					if (canPlace)
					{
						TreeInstance tree = createTree(x,z,t);
						placement[x,z] = true;
						numTrees++;
						t.AddTreeInstance(tree);
					}
				}
			}
		}
			
		return t;
	}

	TreeInstance createTree(int x, int z, Terrain t)
	{
		//init setup of tree
		TreeInstance tree = new TreeInstance();
		tree.color = Color.white;
		tree.lightmapColor = Color.white;

		//default init
		tree.prototypeIndex = 0;

		//forest
		if (t.terrainData.GetAlphamaps(0, 0, size, size)[z,x,forestIndex] != 0f)
		{
			int[] list = new int[3];
			list[0]=0;
			list[1]=1;
			list[2]=6;
			tree.prototypeIndex = list[Random.Range(0,list.Length-1)];
			tree.heightScale = (Random.value+0.5f)*2;
			tree.widthScale = tree.heightScale;
			//trees 0,1,6
		}
		//plains
		else if (t.terrainData.GetAlphamaps(0, 0, size, size)[z,x,plainsIndex] != 0f)
		{
			int[] list = new int[8];
			list[0]=4;
			list[1]=5;
			list[2]=6;
			list[3]=7;
			list[4]=8;
			list[5]=9;
			list[6]=10;
			list[7]=11;
			tree.prototypeIndex = list[Random.Range(0,list.Length-1)];
			tree.heightScale = (Random.value+1.0f);
			tree.widthScale = tree.heightScale;
			//trees 4-11
		}
		//mountains
		else if (t.terrainData.GetAlphamaps(0, 0, size, size)[z,x,mountainIndex] != 0f)
		{
			int[] list = new int[5];
			list[0]=2;
			list[1]=8;
			list[2]=9;
			list[3]=10;
			list[4]=11;
			tree.prototypeIndex = list[Random.Range(0,list.Length-1)];
			tree.heightScale = (Random.value+1.0f);
			tree.widthScale = tree.heightScale;
			//trees 2,8-11
		}
		//beach
		else if (t.terrainData.GetAlphamaps(0, 0, size, size)[z,x,beachIndex] != 0f)
		{
			int[] list = new int[5];
			list[0]=3;
			list[1]=8;
			list[2]=9;
			list[3]=10;
			list[4]=11;
			tree.prototypeIndex = list[Random.Range(0,list.Length-1)];
			tree.heightScale = (Random.value+1.0f)*4;
			tree.widthScale = tree.heightScale;
			//trees 3,8-11
		}
		else
		{
			int[] list = new int[2];
			list[0]=5;
			list[1]=11;
			tree.prototypeIndex = list[Random.Range(0,list.Length-1)];
			tree.heightScale = (Random.value+1.0f)*1.5f;
			tree.widthScale = tree.heightScale;
			//trees 5,11
		}
		if (tree.prototypeIndex >= 4)
		{
			tree.heightScale *= 2;
			tree.widthScale *= 2;
		}
		float nx = ((float)x/size);
		float nz = ((float)z/size);
		tree.position = new Vector3 (nx, t.terrainData.GetHeight(z,x), nz);

		return tree;
	}

	//Goes through all terrain planes in the scene and 'joins' them so the LOD will be consistant
	void setNeighbors()
	{
		//get all terrain planes in the scene
		Terrain[] allTerrain = FindObjectsOfType(typeof(Terrain)) as Terrain[];

		//for each one...
		foreach (Terrain terrain in allTerrain)
		{
			//get the position of the plane
			int thisX = (int)(terrain.transform.position.x/size);
			int thisZ = (int)(terrain.transform.position.z/size);

			//establish each possible neighbor
			Terrain leftLink = null;
			Terrain rightLink = null;
			Terrain upLink = null;
			Terrain downLink = null;

			//find each neighbor and set them to the appropriate variable
			if (GameObject.Find("chunk_"+(thisX-1)+"_"+(thisZ)))
			{
				//print("chunk_"+(thisX-1)+"_"+(thisZ)+" is left neighbor to "+thisX+" "+thisZ);
				leftLink = GameObject.Find("chunk_"+(thisX-1)+"_"+(thisZ)).GetComponent<Terrain>();
			}
			if (GameObject.Find("chunk_"+(thisX+1)+"_"+(thisZ)))
			{
				//print("chunk_"+(thisX+1)+"_"+(thisZ)+" is right neighbor to "+thisX+" "+thisZ);
				rightLink = GameObject.Find("chunk_"+(thisX+1)+"_"+(thisZ)).GetComponent<Terrain>();
			}
			if (GameObject.Find("chunk_"+(thisX)+"_"+(thisZ+1)))
			{
				//print("chunk_"+(thisX)+"_"+(thisZ+1)+" is top neighbor to "+thisX+" "+thisZ);
				upLink = GameObject.Find("chunk_"+(thisX)+"_"+(thisZ+1)).GetComponent<Terrain>();
			}
			if (GameObject.Find("chunk_"+(thisX)+"_"+(thisZ-1)))
			{
				//print("chunk_"+(thisX)+"_"+(thisZ-1)+" is bottom neighbor to "+thisX+" "+thisZ);
				downLink = GameObject.Find("chunk_"+(thisX)+"_"+(thisZ-1)).GetComponent<Terrain>();
			}

			//set the neighbors of this terrain plane. If one of the parameters is null, it represents no adjacent
			//terrain plane. This is only the case for planes on the edge, not near the player.
			terrain.SetNeighbors(leftLink,upLink,rightLink,downLink);
		}
	}

	// Update is called once per frame
	void Update ()
	{
		if (debug_ContinueGenerating)
		{
			generateChunks();
		}
	}

	void generateChunks()
	{
		int xMove = (int)(player.transform.position.x - startPos.x);
		int zMove = (int)(player.transform.position.z - startPos.z);

		if (Mathf.Abs(xMove) >= size || Mathf.Abs(zMove) >= size)
		{
			float updateTime = Time.realtimeSinceStartup;
			int playerX = (int)(Mathf.Floor(player.transform.position.x/size)*size);
			int playerZ = (int)(Mathf.Floor(player.transform.position.z/size)*size);

			for (int x = -cullRadiusX; x<cullRadiusX; x++)
			{
				for (int z = -cullRadiusZ; z<cullRadiusZ; z++)
				{
					Vector3 pos = new Vector3((x*size+playerX),0,(z*size+playerZ));

					string chunkName = "chunk_"+((int)(pos.x/size))+"_"+((int)(pos.z/size));

					if (!chunkTable.ContainsKey(chunkName))
					{
						print(chunkName+" not found in chunk table, generating new one.");
						Terrain t = generateTerrain(pos);
						addChunk(t,updateTime);
					}
					else
					{
						print(chunkName+" found, retrieving that one.");
						(chunkTable[chunkName] as TerrainTile).creationTime = updateTime;
					}
				}
			}

			Hashtable newTerrain = new Hashtable();
			foreach(TerrainTile t in chunkTable.Values)
			{
				if (t.creationTime != updateTime)
				{
					Destroy(t.tile);
				}
				else
				{
					newTerrain.Add(t.tile.name,t);
				}
			}

			chunkTable = newTerrain;
			startPos = player.transform.position;

			setNeighbors();
		}
	}
}