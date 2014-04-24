using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MeshLinesGenerator : MonoBehaviour 
{
	public GameObject meshLinePrefab;
	GameObject[] meshLinesPoolArray;
	Mesh[] meshLinesMeshComponentArray;
	PVA[] meshLinesPVAComponentArray;
	public int meshLinesPoolSize = 100;

	public float spawnCooldown = 1.0f;
	float spawnCooldownCounter = 0.0f;

	[Range (0,100)]
	public float meshSpeed;

	public Material meshMaterial;

	int dataRepCount = 1;

	AudioDirectorScript audioDirector;

	public int verticesFrequencyDepthCount = 200;
	public float verticesSpread = 1.0f;
	Mesh calculationsMiniMesh;
	Vector3[] miniVertsArray;
	Vector3[] vertsArrayLast2;

	Vector3[] verticesArray;
	int[] indicesArray;
	Vector3 tempVector;

	float xScale = 1.0f;
	float zScale = 1.0f;

	// collumns stuff
	GameObject[] meshCollumnsArray;
	Mesh[] meshCollumnsMeshComponentArray;
	public int collumnDepth = 200;
	Vector3[] tempCollumnVerticesArray;
	Vector3[] tempCollumnNormalsArray;

	public Vector3[][] collumnsArrayVerticesArray;
	Vector3[][] collumnsArrayNormalsArray;

	[Range(1, 10)]
	public int collumnStitchIndex = 1;
	public Vector3 stitchAnchorOffset = new Vector3(0, 0, 0);

	// predeclared temp variables (trying to avoid GC)
	GameObject tempMeshLineGO;
	int freshMeshLineIndex;
	Mesh tempMesh;
	Vector3 currentLinesForward;

	public Color meshColorViewer;

	// Use this for initialization
	void Start () 
	{
		audioDirector = (AudioDirectorScript) GameObject.FindWithTag("AudioDirector").GetComponent("AudioDirectorScript");
		GenerateCalculationsMiniMesh();

		// mesh lines (i.e. rows) setup
		vertsArrayLast2 = new Vector3[2 * verticesFrequencyDepthCount];

		meshLinesPoolArray = new GameObject[meshLinesPoolSize];
		meshLinesMeshComponentArray = new Mesh[meshLinesPoolSize];
		meshLinesPVAComponentArray = new PVA[meshLinesPoolSize];
		for(int i = 0; i < meshLinesPoolSize; i++)
		{
			meshLinesPoolArray[i] = (GameObject)Instantiate(meshLinePrefab, transform.position, Quaternion.identity);
			meshLinesPoolArray[i].renderer.sharedMaterial = meshMaterial;
			meshLinesPoolArray[i].SetActive(false);

			meshLinesMeshComponentArray[i] = meshLinesPoolArray[i].GetComponent<MeshFilter>().mesh;
			meshLinesPVAComponentArray[i] = meshLinesPoolArray[i].GetComponent<PVA>();
		}
		


		// do basic setup for all meshe lines / rows
		verticesArray = new Vector3[verticesFrequencyDepthCount];
		for(int i = 0; i < verticesArray.Length; i++)
		{
			verticesArray[i] = new Vector3(i * verticesSpread , 0, 0);
		}

		
		List<int> indicesList = new List<int>();
		List<Vector2> uvsLinesList = new List<Vector2>();
		List<Vector4> tangentLinesList = new List<Vector4>();
		for(int i =0; i < verticesArray.Length - 1; i++)
		{
			indicesList.Add(i);
			indicesList.Add(i +1);
			uvsLinesList.Add(new Vector2(0, 0));
			tangentLinesList.Add(new Vector4(0, 0, 0, 0));
		}
		// add final uv
		uvsLinesList.Add(new Vector2(0,0));
		tangentLinesList.Add(new Vector4(0, 0, 0, 0));
		indicesArray = indicesList.ToArray();

		for(int i = 0; i < meshLinesPoolSize; i++)
		{
			meshLinesMeshComponentArray[i].Clear();
			meshLinesMeshComponentArray[i].vertices = verticesArray;
			meshLinesMeshComponentArray[i].uv = uvsLinesList.ToArray();
			meshLinesMeshComponentArray[i].tangents = tangentLinesList.ToArray();
			meshLinesMeshComponentArray[i].SetIndices(indicesArray, MeshTopology.Lines, 0);
		}

		// mesh collumns setup
		// basic object setup
		meshCollumnsArray = new GameObject[verticesFrequencyDepthCount];
		for(int i = 0; i < meshCollumnsArray.Length; i++)
		{
			meshCollumnsArray[i] = new GameObject("MeshRow_" + i);
			meshCollumnsArray[i].AddComponent<MeshFilter>();
			meshCollumnsArray[i].AddComponent<MeshRenderer>();
			meshCollumnsArray[i].renderer.sharedMaterial = meshMaterial;
			meshCollumnsArray[i].renderer.receiveShadows = false;
			meshCollumnsArray[i].renderer.castShadows = false;
		}
		
		// vertices and indices setup
		tempCollumnVerticesArray = new Vector3[collumnDepth];
		tempCollumnNormalsArray = new Vector3[collumnDepth];

		
		// Generate indices
		List<int> rowIndicesList = new List<int>();
		List<Vector2> uvsCollumnsList = new List<Vector2>();
		List<Vector4> tangentsCollumnsList = new List<Vector4>();
		for(int i = 0; i< tempCollumnVerticesArray.Length -1; i++)
		{
			rowIndicesList.Add(i);
			rowIndicesList.Add(i+1);
			uvsCollumnsList.Add(new Vector2(0, 0));
			tangentsCollumnsList.Add( new Vector4(0, 0, 0, 0));
		}
		// add final uv
		uvsCollumnsList.Add(new Vector2(0, 0));
		tangentsCollumnsList.Add(new Vector4(0, 0, 0, 0));

		// setup mesh component
		meshCollumnsMeshComponentArray = new Mesh[meshCollumnsArray.Length];

		// these 2D arrays will be used to locally store and manage vertices and normals, minimizing how often mesh.verties,etc gets called (which causes GC spike)
		collumnsArrayVerticesArray = new Vector3[meshCollumnsMeshComponentArray.Length][];
		collumnsArrayNormalsArray = new Vector3[meshCollumnsMeshComponentArray.Length][];

		Vector3[] emptyNormals = new Vector3[tempCollumnVerticesArray.Length];
		for(int i = 0; i < meshCollumnsMeshComponentArray.Length; i++)
		{
			meshCollumnsMeshComponentArray[i] = meshCollumnsArray[i].GetComponent<MeshFilter>().mesh;
			meshCollumnsMeshComponentArray[i].Clear();
			meshCollumnsMeshComponentArray[i].vertices = tempCollumnVerticesArray;
			meshCollumnsMeshComponentArray[i].SetIndices(rowIndicesList.ToArray(), MeshTopology.Lines,0);
			meshCollumnsMeshComponentArray[i].normals = emptyNormals;
			meshCollumnsMeshComponentArray[i].uv = uvsCollumnsList.ToArray();
			meshCollumnsMeshComponentArray[i].tangents = tangentsCollumnsList.ToArray();

			collumnsArrayVerticesArray[i] = new Vector3[collumnDepth];
			collumnsArrayNormalsArray[i] =  new Vector3[collumnDepth];
			for(int j = 0; j < collumnDepth; j++)
			{
				collumnsArrayVerticesArray[i][j] = new Vector3(0, 0, 0);
				collumnsArrayNormalsArray[i][j] = new Vector3(0, 0, 0);	
			}
			
		}



		tempVector = new Vector3(0, 0, 0);
	}
	
	// Update is called once per frame
	void Update () 
	{

		Profiler.BeginSample("UpdateCollumnVerticesPosition");
		UpdateCollumnVerticesPosition();
		Profiler.EndSample();
	
		spawnCooldownCounter += Time.deltaTime;
		if(spawnCooldownCounter > spawnCooldown)
		{
			spawnCooldownCounter -= spawnCooldown;

			Profiler.BeginSample("GenerateLineMesh");
			GenerateLineMesh();
			Profiler.EndSample();


			Profiler.BeginSample("StitchNewRowIntoCollumns");
			StitchNewRowIntoCollumns();
			Profiler.EndSample();

		}

		


		meshMaterial.color = audioDirector.calculatedRGB;

		meshColorViewer = meshMaterial.color;

		/*
		if (Time.frameCount % 30 == 0)
		{
		   System.GC.Collect();
		}
		*/

	}

	int GetFreeMeshLineIndex()
	{
		for(int i = 0; i < meshLinesPoolArray.Length; i++)
		{
			if(meshLinesPoolArray[i].activeSelf == false)
				return i;
		}
		// if nothing found
			return -1;
	}

	void GenerateLineMesh()
	{
		freshMeshLineIndex = GetFreeMeshLineIndex();
		
		if(freshMeshLineIndex == -1)
			return;
		else
		{
			tempMeshLineGO = meshLinesPoolArray[freshMeshLineIndex];
			tempMeshLineGO.SetActive(true);
		
			tempMeshLineGO.transform.localScale = 0.03f * audioDirector.averageAmplitude * new Vector3(1, 1, 1);

			tempMeshLineGO.transform.position = transform.position;
			float xOffset = - 0.5f * tempMeshLineGO.transform.localScale.x * verticesFrequencyDepthCount * verticesSpread;
			tempVector = tempMeshLineGO.transform.position;
			tempVector.x += xOffset;
			tempMeshLineGO.transform.position = tempVector;
			currentLinesForward = tempMeshLineGO.transform.forward;
		}

		tempMesh = meshLinesMeshComponentArray[freshMeshLineIndex];

		// SET HEIGHT

		for(int i = 1; i<verticesFrequencyDepthCount; i++)
		{
			tempVector = verticesArray[i];
			tempVector.y = 8.0f * audioDirector.pseudoLogArrayBuffer[i/(dataRepCount+1)]; //* verticesAudioHeightScale * yScale; // normal version
			//tempVector.y = ( tempHeight * verticesAudioHeightScale + verticesArray[i + verticesFrequencyDepthCount].y)/2.0f ; // time axis smoothing version
			verticesArray[i] = tempVector;
		}

		// reset the audio data buffer
		for(int i = 0; i < audioDirector.pseudoLogArray.Length; i++)
			audioDirector.pseudoLogArrayBuffer[i] = 0;

			
		// calculate normals

		// push down normals
		for(int i = 0; i < verticesFrequencyDepthCount; i ++)
		{
			vertsArrayLast2[i + verticesFrequencyDepthCount] = vertsArrayLast2[i];
		}
		// insert new data
		for(int i = 0; i < verticesFrequencyDepthCount; i++)
		{
			vertsArrayLast2[i] = verticesArray[i];
		}
		
		calculationsMiniMesh.vertices = vertsArrayLast2;
		calculationsMiniMesh.RecalculateNormals();
		
		// apply data to mesh
		tempMesh.vertices = verticesArray;
		
		// looks like copying values from one array to another causes GC to go wilde spikes >_<
		// Take() is much better than manual copy though
		tempMesh.normals = calculationsMiniMesh.normals.Take(verticesFrequencyDepthCount).ToArray();

		meshLinesPVAComponentArray[freshMeshLineIndex].ResetPVA();
		meshLinesPVAComponentArray[freshMeshLineIndex].velocity = meshSpeed *transform.forward;
	}

	void StitchNewRowIntoCollumns()
	{

		// first push down the vertices by 1 index for all collumns

		for(int h = 0; h < meshCollumnsArray.Length; h++)
		{
			// shift values down
			//tempCollumnVerticesArray = collumnsArrayVerticesArray[h]; //meshCollumnsMeshComponentArray[h].vertices;
			//tempCollumnNormalsArray = collumnsArrayNormalsArray[h]; //meshCollumnsMeshComponentArray[h].normals;
			for(int i = collumnDepth -1 ; i > collumnStitchIndex ; i--)
			{
				//tempCollumnVerticesArray[i] = tempCollumnVerticesArray[i-1];
				//tempCollumnNormalsArray[i] = tempCollumnNormalsArray[i-1];

				collumnsArrayVerticesArray[h][i] = collumnsArrayVerticesArray[h][i-1];
				collumnsArrayNormalsArray[h][i] = collumnsArrayNormalsArray[h][i-1];
			}
			
			// add the new row value to all corresponding collumn start vertex
			//tempCollumnVerticesArray[collumnStitchIndex] = tempMeshLineGO.transform.TransformPoint(verticesArray[h]) ;//+ meshLinesPoolArray[freshMeshLineIndex].transform.position ;
			//tempCollumnNormalsArray[collumnStitchIndex] = meshLinesMeshComponentArray[freshMeshLineIndex].normals[h];

			collumnsArrayVerticesArray[h][collumnStitchIndex] = tempMeshLineGO.transform.TransformPoint(verticesArray[h]) ;
			collumnsArrayNormalsArray[h][collumnStitchIndex] = meshLinesMeshComponentArray[freshMeshLineIndex].normals[h];


			//collumnsArrayVerticesArray[h] = tempCollumnVerticesArray;
			//collumnsArrayNormalsArray[h] = tempCollumnNormalsArray;

			meshCollumnsMeshComponentArray[h].vertices = collumnsArrayVerticesArray[h]; //tempCollumnVerticesArray;
			meshCollumnsMeshComponentArray[h].normals = collumnsArrayNormalsArray[h]; // tempCollumnNormalsArray;
		}


	}

	void UpdateCollumnVerticesPosition()
	{
		Vector3 tempPosition;
		for(int h = 0; h < meshCollumnsArray.Length; h++)
		{	
			tempCollumnVerticesArray = collumnsArrayVerticesArray[h];// meshCollumnsMeshComponentArray[h].vertices;
			for(int i = collumnStitchIndex ; i < collumnDepth ; i++)
			{
				tempPosition = /*collumnsArrayVerticesArray[h][i]; //*/tempCollumnVerticesArray[i];
				// not unified physics now, could cause trouble later
				tempPosition += meshSpeed * transform.forward * Time.deltaTime;
				tempCollumnVerticesArray[i] /*collumnsArrayVerticesArray[h][i]*/ = tempPosition;
			}
			tempCollumnVerticesArray[collumnStitchIndex-1] /*collumnsArrayVerticesArray[h][collumnStitchIndex-1]*/ = transform.position + stitchAnchorOffset;
			collumnsArrayVerticesArray[h] = tempCollumnVerticesArray;
			meshCollumnsMeshComponentArray[h].vertices = /*collumnsArrayVerticesArray[h];//*/tempCollumnVerticesArray;
		}

	}


	void GenerateCalculationsMiniMesh()
	{

		GameObject calculationsMiniMeshGameObject = new GameObject("CalculationsMiniMesh");
		calculationsMiniMeshGameObject.AddComponent("MeshFilter");
		//calculationsMiniMeshGameObject.AddComponent("MeshRenderer");
		calculationsMiniMesh = calculationsMiniMeshGameObject.GetComponent<MeshFilter>().mesh;

        List<int> trisList = new List<int>();
        List<Vector3> vertsList = new List<Vector3>();

        List<Vector2> tempUVList = new List<Vector2>();

        // initial line
        for(int j = 0; j < verticesFrequencyDepthCount; j++)
    	{
    		vertsList.Add( new Vector3(0,0,j * zScale) );
    		tempUVList.Add( new Vector2(0,0) );
    	}

    	// populate the rest of the vertices, triangles
    	// use verticesFrequencyDepthCount to shift between frewuency collumns

    	// generate only one extra row for normals calculations
        for(int i = 1; i < 2; i++)
        {
        	for(int j = 0; j < verticesFrequencyDepthCount; j += 2)
        	{
        		// bottom left triangle
        		vertsList.Add( new Vector3(i * xScale,0, j * zScale) );
        		int currentListIndex = vertsList.Count -1;

	        	trisList.Add(currentListIndex);
	        	trisList.Add(currentListIndex - verticesFrequencyDepthCount);
	        	trisList.Add(currentListIndex - verticesFrequencyDepthCount + 1);

	        	// fill triangles in between this and previous triangle below
	        	if( j > 0) // is not at the edge
	        	{
	        		// bottom left triangle
	        		trisList.Add(currentListIndex -1);
		        	trisList.Add(currentListIndex - verticesFrequencyDepthCount -1);
		        	trisList.Add(currentListIndex - verticesFrequencyDepthCount);

		        	// top right triangle
		        	trisList.Add(currentListIndex);
		        	trisList.Add(currentListIndex - 1);
		        	trisList.Add(currentListIndex - verticesFrequencyDepthCount);
	        	}	

	        	// top right triangle
	        	vertsList.Add( new Vector3( i*xScale,0, (j + 1)*zScale ) );
	        	currentListIndex++;
	        	
	        	trisList.Add(currentListIndex);
	        	trisList.Add(currentListIndex - 1);
	        	trisList.Add(currentListIndex - verticesFrequencyDepthCount);
  	
	        	tempUVList.Add( new Vector2(0,0) );
	        	tempUVList.Add( new Vector2(0,0) );
        	}
        }

		calculationsMiniMesh.Clear();
		calculationsMiniMesh.MarkDynamic();
		calculationsMiniMesh.vertices = vertsList.ToArray();
		calculationsMiniMesh.uv = tempUVList.ToArray();
		calculationsMiniMesh.triangles = trisList.ToArray();
		calculationsMiniMesh.RecalculateNormals();

		miniVertsArray = calculationsMiniMesh.normals;
	}




}
