using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class islandGenerator : MonoBehaviour {

	[Header("Debug Settings")]
	public bool debug_drawOnlyTerrain = true;
	public bool debug_drawTrees = true;
	public bool debug_renderWater = false;
	public bool debug_DrawGrass = false;
	public bool useRandomSeed = true;

	[Header("Game Objects")]
	public Terrain defaultLand;
	public GameObject water;

	[Header("General Settings")]
	public int size = 1024;
	public int height = 100;

	[Header("Paint Texture Indices")]
	//0=ocean,1=beach,2=forstpine,3=shortgreengrass,4=longgreengrass,
	//5=brownwildfield,6=brownstonedirthill,7=drygrass,8=desertsand,9=browngreengrassblend
	public int oceanIndex = 0;
	public int beachIndex = 1;
	public int forestIndex = 3;
	public int plainsIndex = 4;
	public int cliffIndex = 5;
	public int mountainIndex = 6;
	public int desertIndex = 8;

	[Header("Texture Height Levels")]
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

	[Header("Terrain Settings")]
	//slider variables
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
	public float a = 0.10f;
	[Range(0.0f, 2.0f)]
	public float b = 1.0f;
	[Range(0.0f, 10.0f)]
	public float c = 1.3f;

	public float seedLand = 5000f;
	Vector3 startPos;

	// Use this for initialization
	void Start ()
	{
		seedLand = (int)(Random.value*100000f);
		//Set time and space origins
		this.gameObject.transform.position = Vector3.zero;
		//make the terrain and its data
		Terrain t = (Terrain) Instantiate(defaultLand,Vector3.zero,Quaternion.identity);
		Vector3 seaCenter = new Vector3(size/2,seaLevel*height,size/2);
		if (debug_renderWater)
		{
			GameObject sea = (GameObject) Instantiate(water,seaCenter,Quaternion.identity);
			sea.name = "water";
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

		if (!debug_drawOnlyTerrain)
		{
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
		}

		//final applying of things
		t.terrainData = newChunk;
		t.GetComponent<TerrainCollider>().terrainData = t.terrainData;
		t.Flush();
	}

	//This is where all the customization takes place. Uses iterations of Perlin noise to generate a landscape
	//Makes a 2D array and adds float values to each cell that would map to a plane, making a 3D mountainous plane
	float[,] generateHeights(Terrain terr)
	{
		float[,] heights = new float[size,size];
		for (int z=0; z<size; z++)
		{
			for (int x=0; x<size; x++)
			{
				//get default location heights
				float xCoord = (seedLand+(terr.transform.position.x+x))*scale;
				float zCoord = (seedLand+(terr.transform.position.z+z))*scale;

				float nx = ((float)x * 1.0f / (float)(size - 1))-0.5f;
				float nz = ((float)z * 1.0f / (float)(size - 1))-0.5f;
				float d = 2*Mathf.Sqrt((nx*nx)+(nz*nz));

				//put detail into the heightmap
				float e = (
					(o1 * Mathf.PerlinNoise(1*xCoord*frequency,1*zCoord*frequency)) + 
					(o2 * Mathf.PerlinNoise(2*xCoord*frequency,2*zCoord*frequency)) + 
					(o3 * Mathf.PerlinNoise(4*xCoord*frequency,4*zCoord*frequency)) + 
					(o4 * Mathf.PerlinNoise(8*xCoord*frequency,8*zCoord*frequency)) + 
					(o5 * Mathf.PerlinNoise(16*xCoord*frequency,16*zCoord*frequency)) + 
					(o6 * Mathf.PerlinNoise(32*xCoord*frequency,32*zCoord*frequency)));

				e /= (o1+o2+o3+o4+o5+o6);
				e = (e+a)*(1-(b*Mathf.Pow(d,c)));
				//e = Mathf.Pow(e,power);

				heights[z,x] = e;
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

				float terrainHeight = t.terrainData.GetHeight(z,x);
				float angle = t.terrainData.GetSteepness(nz, nx);
				int terrainType = getBiome(terrainHeight,angle);
				for (int j=0; j<t.terrainData.alphamapLayers; j++)
				{
					splatmapData[x,z,j] = 0;
				}
				splatmapData[x,z,terrainType] = 1;
			}
		}
		return splatmapData;
	}

	int getBiome(float elevation, float angle)
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
					grassMap1[z, x] = 3;
					grassMap2[z, x] = 0;
					grassMap3[z, x] = 3;
					grassMap4[z, x] = 2;
					unitsForest++;
				}
				else if (maps[z,x,forestIndex] != 0f)
				{
					grassMap1[z, x] = 2;
					grassMap2[z, x] = 4;
					grassMap3[z, x] = 2;
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
			float mountainTreeChance = 1f;
			float beachTreeChance = .25f;
			float plainsTreeChance = 1f;

			int numTrees = 0;
			float realDensity = ((float)unitsForest/((float)size*(float)size))*(float)treeDensity;
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
			tree.heightScale = (Random.value+0.5f)*1.2f;
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
			tree.heightScale = (Random.value+0.5f);
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
			tree.heightScale = (Random.value+0.5f);
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
			tree.heightScale = (Random.value+0.2f)*2;
			tree.widthScale = tree.heightScale;
			//trees 3,8-11
		}
		else
		{
			int[] list = new int[2];
			list[0]=5;
			list[1]=11;
			tree.prototypeIndex = list[Random.Range(0,list.Length-1)];
			tree.heightScale = (Random.value+0.3f)*1.5f;
			tree.widthScale = tree.heightScale;
			//trees 5,11
		}
		if (tree.prototypeIndex >= 4)
		{
			tree.heightScale *= 1;
			tree.widthScale *= 1;
		}
		float nx = ((float)x/size);
		float nz = ((float)z/size);
		tree.position = new Vector3 (nx, t.terrainData.GetHeight(z,x), nz);

		return tree;
	}

	// Update is called once per frame
	void Update ()
	{

	}
}