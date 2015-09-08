using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using Stormancer;
using System.Threading.Tasks;
using Stormancer.Core;
using Stormancer.Plugins;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text;
using System.Collections.Concurrent;
using Stormancer.Diagnostics;
using UniRx;
public class GameEngine : MonoBehaviour {

	public connectionPanel					connectionPanel;
	public LeaderBoardPanel					leaderboard;
	public GameObject						gameOverPanel;
	public Button[]							disconnectBtn;
	public Button							continueBtn;
	public Image[]							lifeImgs;

	public GameObject						ballTemplate;

	private Scene							_scene;
	private Client							_local_client;
	private Dictionary<int, GameObject>	    _balls;

	private bool							_connecting = false;
	private bool							_isPlaying = false;
	private long							_lastUpdate = 0;

	private byte							life = 3;

	// Use this for initialization
	void Start () {

		// configuring UI
		connectionPanel.gameObject.SetActive (true);
		leaderboard.gameObject.SetActive (false);
		disconnectBtn [0].enabled = false;
		foreach (Image img in lifeImgs)
			img.enabled = false;

		// creating basic configs
		UniRx.MainThreadDispatcher.Initialize();
		_balls = new Dictionary<int, GameObject> ();
		var config = ClientConfiguration.ForAccount("", "");
		_local_client = new Client (config);

		// configuring UI buttons
		connectionPanel.connectBtn.onClick.AddListener (this.Connect);
		foreach (Button btn in disconnectBtn)
			btn.onClick.AddListener (this.Disconnect);
		continueBtn.onClick.AddListener (this.restartGame);
	}

	void Connect()
	{
		// hidding UI (just to be sure)
		connectionPanel.gameObject.SetActive (true);
		leaderboard.gameObject.SetActive (false);
		foreach (Button btn in disconnectBtn)
			btn.enabled = true;
		foreach (Image img in lifeImgs)
			img.enabled = false;

		if (_connecting == true)
			return;
		if (connectionPanel.username.text == "") {
			connectionPanel.error.text = "Please enter a username";
			return;
		}
		_connecting = true;
		connectionPanel.error.text = "Connecting";
		_local_client.GetPublicScene ("pop", connectionPanel.username.text).ContinueWith (task => 
		{
			_scene = task.Result;
			Debug.Log ("configuring routes");
			_scene.AddRoute ("create_ball", onCreateBalls);
			_scene.AddRoute ("destroy_ball", onDestroyBalls);
			Debug.Log ("connecting to remote scene");
			_scene.Connect().ContinueWith( Task => {CheckIfConnected();});
			UniRx.MainThreadDispatcher.Post (() => {});
		});
	}

	void Disconnect()
	{
		_scene.Disconnect ();
		Application.Quit ();
	}

	public void CheckIfConnected()
	{
		if (_scene.Connected) {
			connectionPanel.gameObject.SetActive (false);
			leaderboard.gameObject.SetActive (true);
			disconnectBtn [0].enabled = true;
			foreach (Image img in lifeImgs)
				img.enabled = true;
			leaderboard.localTxt.text = connectionPanel.username.text;

			_connecting = false;
			_isPlaying = true;
			_scene.Rpc<string, string>("update_leaderBoard", "");
		}
		else 
		{
			connectionPanel.error.text = "connection failed.";
			_connecting = false;
		}
	}

	public void onClick(Stream response)
	{
		var reader = new BinaryReader (response);
		char statut = reader.ReadChar ();
		if (statut == 0)
			Debug.Log ("missed");
		else if (statut == 1) 
			Debug.Log ("touched");
		else if (statut == 2)
		{
			Debug.Log ("touched wrong ball !!");
			life = reader.ReadByte();
			if (life == 0)
				GameOver();
			else
			{
				int i = 2;
				while (i > life)
					lifeImgs[i--].enabled = false;
			}
		}
	}

	public void GameOver()
	{
		_isPlaying = false;
		gameOverPanel.SetActive (true);
	}

	public void restartGame()
	{
		gameOverPanel.gameObject.SetActive (false);
		foreach (Image img in lifeImgs)
			img.enabled = true;
		_isPlaying = true;
		_scene.Rpc<string, string>("update_leaderBoard", "");
	}

	public void onCreateBalls(Packet<IScenePeer> packet)
	{
		var reader = new BinaryReader (packet.Stream);
		int id = reader.ReadInt32 ();
		float x = reader.ReadSingle();
		float y = reader.ReadSingle();
		float vx = reader.ReadSingle();
		float vy = reader.ReadSingle();

		GameObject newBall = Instantiate (ballTemplate);
		newBall.transform.position = new Vector3 (x, y, 0);
		newBall.GetComponent<Rigidbody2D> ().AddForce (new Vector2 (vx, vy));
		_balls.Add (id, newBall);
	}

	public void onDestroyBalls(Packet<IScenePeer> packet)
	{
		var reader = new BinaryReader (packet.Stream);
		int id = reader.ReadInt32 ();

		if (_balls.ContainsKey(id))
		{
			Destroy(_balls[id]);
		}
	}

	public void onUpdateLeaderBoard(LeaderBoardDtO update)
	{
		leaderboard.updateLeaderBoard (update);
	}

	void Update ()
	{
		if (_isPlaying == true)
	    {
			if (_lastUpdate + 1000 < _local_client.Clock)
			{
				_scene.Rpc<string, LeaderBoardDtO>("update_leaderBoard", "").Subscribe(DtO => {onUpdateLeaderBoard(DtO);});
			}
			if (Input.GetMouseButtonDown(0))
			{
				Debug.Log (Input.mousePosition);
			}
		}
	}
}
