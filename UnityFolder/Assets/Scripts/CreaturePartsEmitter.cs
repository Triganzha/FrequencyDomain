﻿using UnityEngine;
using System.Collections;

public class CreaturePartsEmitter : MonoBehaviour 
{
	public GameObject partPrefab;

	public float emisionRadius = 0.0f;

	AudioDirectorScript audioDirector;

	// Use this for initialization
	void Start () 
	{
		audioDirector = (AudioDirectorScript) GameObject.FindWithTag("AudioDirector").GetComponent("AudioDirectorScript");
	}
	
	// Update is called once per frame
	void Update () 
	{
		
		for(int i = 0 ; i < audioDirector.decadesAveragesArray.Length; i += 1)
		{
			float decadeAverage = audioDirector.decadesAveragesArray[i];
			if( decadeAverage > 10.0f)
			{
				GameObject newPart = (GameObject)Instantiate(partPrefab, GetEmissionPosition(i), Quaternion.identity);
				newPart.GetComponent<PVA>().velocity = 20.0f * decadeAverage * GetEmissionDirection(i);			
			}
		}
	

	}

	Vector3 GetEmissionDirection(int decade)
	{
		Vector3 directionVector = new Vector3(0, 0, 0);

		float angle = (float)decade/10.0f * Mathf.PI;
		directionVector.x = Mathf.Cos(angle);
		directionVector.z = 1.0f;
		directionVector.Normalize();

		directionVector = Vector3.Cross( transform.up, directionVector);

		directionVector.y = 0.5f * Mathf.Sin(angle);
	
		return directionVector;
	}

	Vector3 GetEmissionPosition(int decade)
	{
		Vector3 emissionPos = new Vector3(0, 0, 0);

		float angle = (float)decade/10.0f * Mathf.PI;
		emissionPos.x = emisionRadius * Mathf.Cos(angle);
		emissionPos.y = emisionRadius * Mathf.Sin(angle);

		// apply rotation of the emitter itself
		emissionPos = transform.rotation * emissionPos;
		// apply emitter position offset
		emissionPos += transform.position;

		return emissionPos;

	}

}
