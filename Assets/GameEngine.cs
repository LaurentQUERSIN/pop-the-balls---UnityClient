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

	public ConnectionPanel					connectionPanel;
	public LeaderBoardPanel					leaderboard;
	public GameObject						gameOverPanel;
	public Button[]							disconnectBtn;
	public Button							continueBtn;
	public Image[]							lifeImgs;

	public GameObject						ballTemplate;
	public Camera							mainCamera;

	private Scene							_scene;
	private Client							_local_client;
	private Dictionary<int, GameObject>	    _balls;
	private List<ball>						_ballsToCreate;
	private List<int>						_ballsToDestroy;
	private LeaderBoardDtO					_lbDtO = null;

	private bool 							_connectionFailed = false;
	private bool							_connecting = false;
	private bool							_isPlaying = false;
	private long							_lastUpdate = 0;

	private byte							life = 3;

	// Use this for initialization
	void Start () {

		// configuring UI
		connectionPanel.gameObject.SetActive (true);
		leaderboard.gameObject.SetActive (false);
		gameOverPanel.SetActive (false);
		disconnectBtn [0].gameObject.SetActive(false);
		foreach (Image img in lifeImgs)
			img.enabled = false;

		// creating basic configs
		UniRx.MainThreadDispatcher.Initialize();
		_balls = new Dictionary<int, GameObject> ();
		_ballsToCreate = new List<ball>();
		_ballsToDestroy = new List<int>();
		var config = ClientConfiguration.ForAccount("7794da14-4d7d-b5b5-a717-47df09ca8492", "poptheballs");
		_local_client = new Client (config);

		// configuring UI buttons
		connectionPanel.connectBtn.onClick.AddListener (this.Connect);
		foreach (Button btn in disconnectBtn)
			btn.onClick.AddListener (this.Disconnect);
		continueBtn.onClick.AddListener (this.restartGame);
		Debug.Log ("init finished");
	}

	void Connect()
	{
		Debug.Log ("start connection procedure");
		// hidding UI (just to be sure)
		connectionPanel.gameObject.SetActive (true);
		leaderboard.gameObject.SetActive (false);
		gameOverPanel.SetActive (false);
		disconnectBtn [0].gameObject.SetActive(false);
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
		Debug.Log ("try to connect");
		_local_client.GetPublicScene ("main", connectionPanel.username.text).ContinueWith (task => 
		{
			if (task.IsFaulted)
			{
				Debug.Log ("connextion failed ");
				_connectionFailed = true;
			}
			else
			{
				_scene = task.Result;
				Debug.Log ("configuring routes");
				_scene.AddRoute ("create_ball", onCreateBalls);
				_scene.AddRoute ("destroy_ball", onDestroyBalls);
				Debug.Log ("connecting to remote scene");
				_scene.Connect().ContinueWith( Task => {});
				UniRx.MainThreadDispatcher.Post (() => {});
			}
		});
	}

	void Disconnect()
	{
		_scene.Disconnect ();
		Application.Quit ();
	}

	public void CheckIfConnected()
	{
		Debug.Log ("check if connected");
		if (_scene != null && _scene.Connected)
		{
			Debug.Log("connection succeful");
			connectionPanel.gameObject.SetActive (false);
			leaderboard.gameObject.SetActive (true);
			disconnectBtn [0].gameObject.SetActive (true);
			foreach (Image img in lifeImgs)
				img.enabled = true;
			leaderboard.localTxt.text = connectionPanel.username.text;

			_connecting = false;
			_isPlaying = true;
			_scene.Rpc<string, string>("update_leaderBoard", "");
		}
		else if (_connectionFailed == true)
		{
			connectionPanel.error.text = "Connection failed";
			_connecting = false;
			_connectionFailed = false;
			_scene = null;
		}
	}

	public void onClick(Packet<IScenePeer> response)
	{
		Debug.Log ("response to the click received !");
		var reader = new BinaryReader (response.Stream);
		int statut = reader.ReadInt32 ();
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
		try
		{
			var reader = new BinaryReader (packet.Stream);
			int id = reader.ReadInt32 ();
			float x = reader.ReadSingle();
			float y = reader.ReadSingle();
			float vx = reader.ReadSingle();
			float vy = reader.ReadSingle();

			_ballsToCreate.Add(new ball(x, y, vx, vy, id));
		}
		catch (Exception e)
		{
			Debug.Log ("error while reading ball data");
		}
	}

	public void onDestroyBalls(Packet<IScenePeer> packet)
	{
		var reader = new BinaryReader (packet.Stream);
		int id = reader.ReadInt32 ();

		_ballsToDestroy.Add(id);
	}

	public void onUpdateLeaderBoard(LeaderBoardDtO update)
	{
		_lbDtO = update;
		//leaderboard.updateLeaderBoard (update);
	}

	void Update ()
	{   
		if (_isPlaying == true) 
		{
			if (_lastUpdate + 1000 < _local_client.Clock)
			{
				_lastUpdate = _local_client.Clock;

				if (_lbDtO != null)
					leaderboard.updateLeaderBoard(_lbDtO);
				_scene.Rpc<string, LeaderBoardDtO> ("update_leaderBoard", "").Subscribe (DtO => {
					onUpdateLeaderBoard (DtO);});
			}
			if (Input.GetMouseButtonDown (0))
			{
				Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;
				Physics.Raycast(ray, out hit);
				_scene.Rpc("click", s => {
					BinaryWriter writer = new BinaryWriter(s, Encoding.UTF8);
					writer.Write(hit.point.x);
					writer.Write(hit.point.y);
					writer.Write(_local_client.Clock);
				}
				).Subscribe( rp => {
					onClick(rp);});
			}
			while (_ballsToCreate.Count > 0)
			{
				Debug.Log ("intastiating ball");
				ball temp = _ballsToCreate.First();
				if (!_balls.ContainsKey(temp.id))
				{
					GameObject newBall = Instantiate (ballTemplate, new Vector3 (temp.x, temp.y, 0), Quaternion.identity) as GameObject;
					newBall.GetComponent<Rigidbody2D> ().AddForce (new Vector2 (temp.vx, temp.vy));
					_balls.Add (temp.id, newBall);
				}
				_ballsToCreate.Remove(_ballsToCreate.First());
			}
			while (_ballsToDestroy.Count > 0)
			{
				Debug.Log ("destroying ball");
				int temp = _ballsToDestroy.First();
				if (_balls.ContainsKey(temp))
				{
					Destroy (_balls[temp] as UnityEngine.Object);
					_balls.Remove(temp);	
				}
				_ballsToDestroy.Remove(temp);
			}
		}
		else if (_connecting == true && _lastUpdate + 100 < _local_client.Clock)
		{
			CheckIfConnected ();
			_lastUpdate = _local_client.Clock;
		}
	}
}
