﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StarFieldGenerator : MonoBehaviour 
{

	public GameObject startModel;

	public float starCount = 1000;

	[Range(100,10000)]
	public float xRange = 100.0f;	
	[Range(100,10000)]
	public float yRange = 100.0f;	
	[Range(100,10000)]
	public float zRange = 100.0f;	

	List<GameObject> starsList = new List<GameObject>();

	public void GenerateStarField()
	{
		Random.seed = 1;

		Vector3 spawnPosition;
		float randX;
		float randY;
		float randZ;
		for(int i = 0; i < starCount; i ++)
		{
			randX = Random.Range(-xRange, xRange);
			randY = Random.Range(-yRange, yRange);
			randZ = Random.Range(-zRange, zRange);

			spawnPosition = new Vector3(randX, randY, randZ);

			starsList.Add( (GameObject) Instantiate(startModel, spawnPosition, Quaternion.identity) );
		}
	}

	public void DeleteStarField()
	{
		for(int i = 0; i < starsList.Count; i++)
		{
			DestroyImmediate(starsList[i]);
		}
		starsList.Clear();
	}
}
