using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Camera Movement Manager
 * 
 * ADWS to move left, right, up and down, respectively
 * Q to move upward
 * Z to move downward
 * Hold Left Shit to speed up
 * 
 * This script is good for spectator-mode-like cameras - for simply moving around a scene
 */
public class cameraMovement : MonoBehaviour
{
	public float speedH = 2.0f;
	public float speedV = 2.0f;
	public float speedP = 5.0f;

	private float yaw = 0.0f;
	private float pitch = 0.0f;

	void Update ()
	{
		//get the set speed by the editor 
		float speedMod = speedP;
		if (Input.GetKey(KeyCode.LeftShift))
		{
			//double the speed
			speedMod = speedMod*2;
		}

		//neat math to get rotations based on mouse movement
		yaw += speedH * Input.GetAxis("Mouse X");
		pitch -= speedV * Input.GetAxis("Mouse Y");
		transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

		//move right
		if(Input.GetKey(KeyCode.D))
		{
			transform.Translate(new Vector3(speedMod * Time.deltaTime,0,0));
		}
		//move left
		if(Input.GetKey(KeyCode.A))
		{
			transform.Translate(new Vector3(-speedMod * Time.deltaTime,0,0));
		}
		//move forward
		if(Input.GetKey(KeyCode.W))
		{
			transform.Translate(new Vector3(0,0,speedMod * Time.deltaTime));
		}
		//move backward
		if(Input.GetKey(KeyCode.S))
		{
			transform.Translate(new Vector3(0,0,-speedMod * Time.deltaTime));
		}
		//move upward
		if(Input.GetKey(KeyCode.Q))
		{
			transform.Translate(new Vector3(0,speedMod * Time.deltaTime,0));
		}
		//move downward
		if(Input.GetKey(KeyCode.Z))
		{
			transform.Translate(new Vector3(0,-speedMod * Time.deltaTime,0));
		}
	}
}