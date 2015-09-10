using UnityEngine;
using System.Collections;
using Stormancer;

public class ball_behaviour : MonoBehaviour 
{
	
	public long creation_time;
	public Client local;
	public int oscillation_time = 1000;

	// Use this for initialization
	void Start () {
		this.gameObject.GetComponent<SpriteRenderer>().color = Color.blue;
		Destroy (this.gameObject as UnityEngine.Object, 20);
	}

	void Update()
	{
		if (local != null)
		{
			if (((local.Clock - creation_time) / oscillation_time) % 2 == 0)
				this.gameObject.GetComponent<SpriteRenderer>().color = Color.blue;
			else			
				this.gameObject.GetComponent<SpriteRenderer>().color = Color.red;
		}
	}
}
