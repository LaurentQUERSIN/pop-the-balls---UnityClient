using UnityEngine;
using System.Collections;

public class ball : MonoBehaviour {

	public float x = 0;
	public float y = 0;
	public float vx = 0;
	public float vy = 0;
	public int id = 0;

	public ball(float nx, float ny, float nvx, float nvy, int nid)
	{
		x = nx;
		y = ny;
		vx = nvx;
		vy = nvy;
		id = nid;
	}
}
