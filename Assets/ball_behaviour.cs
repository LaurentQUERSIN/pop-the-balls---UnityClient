using UnityEngine;
using System.Collections;
using Stormancer;

public class ball_behaviour : MonoBehaviour 
{
	
	public long creation_time;
	public Client local;
	public int oscillation_time = 1000;
	public bool isDead = false;
	
	// Use this for initialization
	void Start () {
		this.gameObject.GetComponent<SpriteRenderer>().color = Color.blue;
	}

	void Update()
	{
		if (local != null)
		{
			if (((local.Clock - creation_time) / (oscillation_time - 500) % 2 == 0))
				this.gameObject.GetComponent<SpriteRenderer>().color = Color.Lerp(this.gameObject.GetComponent<SpriteRenderer>().color, Color.blue, 2f * Time.deltaTime);
			else			
				this.gameObject.GetComponent<SpriteRenderer>().color = Color.Lerp(this.gameObject.GetComponent<SpriteRenderer>().color, Color.red, 2f * Time.deltaTime);
		}
		if (local.Clock - creation_time > 32000)
			isDead = true;
		if (isDead == true)
		{
			this.gameObject.transform.localScale = Vector3.Lerp(this.gameObject.transform.localScale, Vector3.zero, 3f * Time.deltaTime);
			if (this.gameObject.transform.localScale.x < 0.01f)
				Destroy(this.gameObject as UnityEngine.Object);
		}
	}
}
