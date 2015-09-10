using UnityEngine;
using System.Collections;

public class ball_behaviour : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Destroy (this.gameObject as UnityEngine.Object, 20);
	}

}
