using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneratorV2 : MonoBehaviour {

	/*
	[Header("Game Objects")]
	public Terrain defaultLand;
	public GameObject water;
	*/

	[Header("General Settings")]
	public int size = 1024;
	public int height = 100;
	public float seedLand = 5000f;
	public float seedBiome = 2000f;
	public bool useRandomSeed = true;
	public bool useRandomBiomeSeed = true;
	public bool resetTerrainParams = false;
	public bool paintTerrain = true;

	[Header("Island Settings")]
	public bool makeIsland = false;
	public bool euclideanDistance = true;
	public bool multiply = true;
	[Range(0.0f, 1.0f)]
	public float a = 0.10f;
	[Range(0.0f, 2.0f)]
	public float b = 1.0f;
	[Range(0.0f, 10.0f)]
	public float c = 1.3f;

	[Header("Terrain Settings")]
	public bool createLargeMountains = false;
	public bool createLargeOceans = false;
	public bool compressOceanMountains = true;
	[Range(0.00000f, 0.00500f)]
	public float biomeSize = .0009765625f;
	[Range(0.00f, 20.00f)]
	public float compressionAmount = 1f;
	[Range(0.0f, 1.0f)]
	public float oceanThreshhold = .5f;
	[Range(0.0f, 1.0f)]
	public float mountainThreshhold = .5f;
	[Range(0.00f, 20.00f)]
	public float inflationAmount = 1f;
	[Range(-2.00f, 1.00f)]
	public float mountainFlatteningAmount = .5f;
	[Range(0.0000f, 0.0500f)]
	public float frequency = .0009765625f;
	[Range(0.00f, 1.00f)]
	public float o1 = .5f;
	[Range(0.00f, 1.00f)]
	public float o2 = .5f;
	[Range(0.00f, 1.00f)]
	public float o3 = .5f;
	[Range(0.00f, 1.00f)]
	public float o4 = .5f;
	[Range(0.00f, 1.00f)]
	public float o5 = .5f;
	[Range(0.00f, 1.00f)]
	public float o6 = .5f;
	[Range(0.00f, 10.00f)]
	public float power = 1f;


	[Header("Texture Settings")]
	public bool useDefaultTextureHeights = true;
	public float steepness = 45f;
	public int overlap = 5;
	public int seaLevel = 10;
	public int forestLevel = 20;
	public int grassLevel = 30;
	public int rockLevel = 50;
	public int snowLevel = 90;

	[Header("Prototype Texture Indicies")]
	public int nullIndex = 4;
	public int waterIndex = 5;
	public int sandIndex = 0;
	public int grassIndex = 1;
	public int forestIndex = 6;
	public int rockIndex = 2;
	public int snowIndex = 3;

	// Use this for initialization
	public void Start()
	{
		//if the user wants a random seed every time
		if (useRandomSeed) seedLand = (int)(Random.value*100000f);
		if (useRandomBiomeSeed) seedBiome = (int)(Random.value*100000f);
		if (useDefaultTextureHeights)
		{
			seaLevel = Mathf.RoundToInt(height/5.3684f);
			grassLevel = Mathf.RoundToInt(height/5.1000f);
			rockLevel = Mathf.RoundToInt(height/3.500f);
			snowLevel = Mathf.RoundToInt(height/2.5000f);
		};
		Terrain thisTerrain = Terrain.activeTerrain;
		//Terrain t = (Terrain) Instantiate(defaultLand,Vector3.zero,Quaternion.identity);
		if (resetTerrainParams)
		{
			thisTerrain.terrainData.alphamapResolution = size;
			thisTerrain.terrainData.heightmapResolution = size;
			thisTerrain.terrainData.SetDetailResolution(size,16);
			thisTerrain.terrainData.size = new Vector3(size,height,size);
			float[,,] reset = new float[size,size,thisTerrain.terrainData.alphamapLayers];
			for (int z=0; z<size;z++)
			{
				for (int x=0; x<size; x++)
				{
					for (int s=0; s<thisTerrain.terrainData.alphamapLayers; s++)
					{
						reset[x,z,s] = 0;
					}
					//reset[x,z,0] = 1;
				}
			}
			thisTerrain.terrainData.SetAlphamaps(0,0,reset);
		}
		thisTerrain.terrainData.SetHeights(0,0,generateHeights(thisTerrain));
		float[,,] splatmap = generateSplatmap(thisTerrain);
		if (paintTerrain)
		{
			thisTerrain.terrainData.SetAlphamaps(0,0,splatmap);
		}
		thisTerrain.GetComponent<TerrainCollider>().terrainData = thisTerrain.terrainData;
		thisTerrain.Flush();

	}
		
	float[,] generateHeights(Terrain t)
	{
		float maxHeight = 0f;
		float[,] heights = new float[size,size];
		for (int z=0; z<size; z++)
		{
			for (int x=0; x<size; x++)
			{
				float xCoord = (float)(seedLand+x);
				float zCoord = (float)(seedLand+z);

				float xCoordBiome = (float)(seedBiome+x);
				float zCoordBiome = (float)(seedBiome+z);

				//put detail into the heightmap
				float e = (
					(o1 * Mathf.PerlinNoise(1*xCoord*frequency,1*zCoord*frequency)) + 
					(o2 * Mathf.PerlinNoise(2*xCoord*frequency,2*zCoord*frequency)) + 
					(o3 * Mathf.PerlinNoise(4*xCoord*frequency,4*zCoord*frequency)) + 
					(o4 * Mathf.PerlinNoise(8*xCoord*frequency,8*zCoord*frequency)) + 
					(o5 * Mathf.PerlinNoise(16*xCoord*frequency,16*zCoord*frequency)) + 
					(o6 * Mathf.PerlinNoise(32*xCoord*frequency,32*zCoord*frequency)));

				e /= (o1+o2+o3+o4+o5+o6);

				if (!makeIsland)
				{
					e = Mathf.Pow(e,power);
				}

				//create mountains
				if (createLargeMountains)
				{
					float mountainMultiplier = Mathf.PerlinNoise(xCoordBiome*biomeSize,zCoordBiome*biomeSize);
					if ((mountainMultiplier < mountainThreshhold))
					{
						//lower terrain
						e += mountainThreshhold-mountainMultiplier;

						//inflate mountains
						e = (e*(1f+mountainThreshhold-mountainMultiplier))*
							Mathf.Pow((1f+(mountainThreshhold-mountainMultiplier)),
								inflationAmount+(mountainThreshhold-mountainMultiplier));
						if (e >= 1f) e = 1f - (e - 1f);
					}
				}

				//add height so we can add seas later
				//Elevation above sea-level
				float seaLevelNormalized = (float)forestLevel/(float)height;
				float easl = e;
				e+=seaLevelNormalized;

				//create oceans
				if (createLargeOceans)
				{
					float oceanMultiplier = Mathf.PerlinNoise(xCoordBiome*biomeSize,zCoordBiome*biomeSize);
					if ((oceanMultiplier > oceanThreshhold))
					{
						//lower terrain
						e -= oceanMultiplier-oceanThreshhold;

						//if there are mountains in the ocean, compress them
						//uses math and stuff
						if (compressOceanMountains)
						{
							e = (e/(1f+oceanMultiplier-oceanThreshhold))*
								Mathf.Pow((1f-(oceanMultiplier-oceanThreshhold)),
										  compressionAmount+(oceanMultiplier-oceanThreshhold));
						}
					}
				}

				//flatten really high mountains
				float snowHeight = (float)rockLevel/(float)height;
				//float snowHeight = (((float)snowLevel+(float)rockLevel)/2)/(float)height;
				if (e > snowHeight)
				{
					e = (e/(1+e-snowHeight))*Mathf.Pow((1-(e-snowHeight)),mountainFlatteningAmount+(e-snowHeight));
				}

				//generate rivers
				//not yet

				if (makeIsland)
				{
					float d = 0f;
					float nx = ((float)x * 1.0f / (float)(size - 1))-0.5f;
					float nz = ((float)z * 1.0f / (float)(size - 1))-0.5f;
					if (euclideanDistance) d = 2*Mathf.Sqrt((nx*nx)+(nz*nz));
					else d = 2*Mathf.Max(Mathf.Abs(nx),Mathf.Abs(nz));

					if (multiply) e = Mathf.Clamp01( (e+a)*(1-(b*Mathf.Pow(d,c))) );
					else e = Mathf.Clamp01( (e+a) - (b*Mathf.Pow(d,c)) );
				}
				heights[z,x] = e;
				if (e > maxHeight)
				{
					maxHeight = e;
				}
			}
		}
		print("Max Height: "+maxHeight*height);
		return heights;
	}
		
	float[,,] generateSplatmap(Terrain t)
	{
		float[,,] splatmapData = new float[size,size,t.terrainData.alphamapLayers];

		for (int z=0; z<size;z++)
		{
			for (int x=0; x<size; x++)
			{
				float height = t.terrainData.GetHeight(z,x);
				float nx = (float)x * 1.0f / (float)(size - 1);
				float nz = (float)z * 1.0f / (float)(size - 1);

				float slope = t.terrainData.GetSteepness(nz,nx);
				int indexToApply = getLandType(height,slope);
				for (int i=0; i<t.terrainData.alphamapLayers; i++)
				{
					splatmapData[x,z,i] = 0;
				}
				splatmapData[x,z,indexToApply] = 1;
			}
		}
		return splatmapData;
	}

	int getLandType(float elevation, float slope)
	{
		//make el a fraction of the highest height
		float el = elevation;
		if (el < seaLevel-1) {return waterIndex;}
		else if(el > seaLevel-1 && el < forestLevel) {return sandIndex;}
		else if (el > forestLevel && el < grassLevel) {return forestIndex;}
		else if (el > grassLevel && el < rockLevel && slope < steepness) {return grassIndex;}
		else if (el > rockLevel && el < snowLevel || slope > steepness) {return rockIndex;}
		else if (el > snowLevel) {return snowIndex;}
		else return nullIndex;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
