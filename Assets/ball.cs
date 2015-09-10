using UnityEngine;
using System.Collections;
using Stormancer;

public class Ball : MonoBehaviour {

	public float x = 0;
	public float y = 0;
	public float vx = 0;
	public float vy = 0;
	public int id = 0;

	public long creation_time = 0;
	public int oscillation_time = 0;
	
	public Ball(float nx, float ny, float nvx, float nvy, int nid, long time, int ot)
	{
		x = nx;
		y = ny;
		vx = nvx;
		vy = nvy;
		id = nid;
		creation_time = time;
		oscillation_time = ot;
	}
}