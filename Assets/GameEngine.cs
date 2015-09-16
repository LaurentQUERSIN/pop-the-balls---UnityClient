using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;
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

	private string							version = "a0.3.2";

	private Scene							_scene;
	private Client							_local_client;
	private Dictionary<int, GameObject>		_balls;
	private List<Ball>						_ballsToCreate;
	private List<int>						_ballsToDestroy;
	private LeaderBoardDtO					_lbDtO = null;

	private bool							_connecting = false;
	private bool							_connected = false;
	private bool							_isPlaying = false;
	private long							_lastUpdate = 0;

	private long							_testRPC = 0;

	private byte							life = 3;

	// Use this for initialization
	void Start () {

		// configuring UI
		connectionPanel.gameObject.SetActive (true);
		connectionPanel.connectBtn.enabled = false;
		leaderboard.gameObject.SetActive (false);
		gameOverPanel.SetActive (false);
		disconnectBtn [0].gameObject.SetActive(false);
		foreach (Image img in lifeImgs)
			img.enabled = false;
		connectionPanel.error.text = "Please enter a username";

		// creating basic configs
		UniRx.MainThreadDispatcher.Initialize();
		_balls = new Dictionary<int, GameObject> ();
		_ballsToCreate = new List<Ball>();
		_ballsToDestroy = new List<int>();
		var config = ClientConfiguration.ForAccount("7794da14-4d7d-b5b5-a717-47df09ca8492", "poptheballs");
		_local_client = new Client (config);

		// configuring UI buttons
		connectionPanel.QuitBtn.onClick.AddListener(this.Quit);
			foreach (Button btn in disconnectBtn)
			btn.onClick.AddListener (this.ReturnToMenu);
		continueBtn.onClick.AddListener (this.restartGame);
		connectionPanel.connectBtn.onClick.AddListener (this.clickOnPlay);
		connectionPanel.EnableBloombtn.onClick.AddListener(this.onEnableBloom);
		Debug.Log ("init finished");
	}

	void Connect()
	{
		if (_connected == false && _connecting == false)
		{
			Debug.Log ("start connection procedure");
			ConnectionDtO cdto = new ConnectionDtO(connectionPanel.username.text, version);
			connectionPanel.error.text = "Connecting";
			_connecting = true;
			_local_client.GetPublicScene ("main", cdto).ContinueWith (task => 
			                                                          {
				if (task.IsFaulted)
				{
					Debug.Log ("connection failed : " + task.Exception.Message);
					_connecting = false;
				}
				else
				{
					_scene = task.Result;
					Debug.Log ("configuring routes");
					_scene.AddRoute ("create_ball", onCreateBalls);
					_scene.AddRoute ("destroy_ball", onDestroyBalls);
					Debug.Log ("connecting to remote scene");
					_scene.Connect().ContinueWith( t => 
					    {
						Debug.Log("check if connection failed");
						if (_scene.Connected)
						{
							_connected = true;
							_connecting = false;
							Debug.Log("connection succeful");
							UniRx.MainThreadDispatcher.Post (() =>{
								connectionPanel.error.text = "Please enter a username";
								connectionPanel.connectBtn.enabled = true;
							});
						}
						else
						{
							Debug.Log ("connection failed: " + t.Exception.InnerException.InnerException.Message);
							_connecting = false;
							UniRx.MainThreadDispatcher.Post (() =>{
								connectionPanel.error.text = t.Exception.InnerException.InnerException.Message;
							});
						}
					});
				}
			});
		}
	}

	void ReturnToMenu()
	{
		_isPlaying = false;
		connectionPanel.gameObject.SetActive (true);
		leaderboard.gameObject.SetActive (false);
		gameOverPanel.SetActive (false);
		disconnectBtn [0].gameObject.SetActive(false);
		foreach (Image img in lifeImgs)
			img.enabled = false;
		_scene.Send("exit", "");
	}

	void Quit()
	{
		Application.Quit ();
	}
	
	public void clickOnPlay()
	{
		if (_scene != null && _connected == true)
		{
			ConnectionDtO ctdo = new ConnectionDtO(connectionPanel.username.text, version);
			_scene.Rpc<ConnectionDtO, int>("play", ctdo).Subscribe(resp => {onPlay(resp);});
		}
	}

	public void onPlay(int status)
	{
		Debug.Log ("connection request reponse received : status = " + status.ToString());
		if (status == 0)
		{
			UniRx.MainThreadDispatcher.Post (() => {
				connectionPanel.error.text = "";
				connectionPanel.gameObject.SetActive (false);
				leaderboard.gameObject.SetActive (true);
				disconnectBtn [0].gameObject.SetActive (true);
				foreach (Image img in lifeImgs)
					img.enabled = true;
				leaderboard.localTxt.text = connectionPanel.username.text;
				_isPlaying = true;
			});
		}
	}

	public void onClick(Packet<IScenePeer> response)
	{
		var reader = new BinaryReader (response.Stream);
		int status = reader.ReadInt32 ();
		bool _changedLife = false;

		Debug.Log ("time between RPC send and response = " + (_testRPC - _local_client.Clock).ToString() );

		if (status == 0)
			Debug.Log ("missed");
		else if (status == 1) 
			Debug.Log ("touched");
		else if (status == 2)
		{
			life = reader.ReadByte();
			_changedLife = true;
			if (life == 0)
				GameOver();
		}
		else if (status == 3)
		{
			life = reader.ReadByte();
			_changedLife = true;
		}
		if (_changedLife == true)
		{
			UniRx.MainThreadDispatcher.Post (() => {
				int i = 0;
				while (i < life)
					lifeImgs[i++].enabled = true;
				while (i < 3)
					lifeImgs[i++].enabled = false;
			});
		}
		_lastUpdate = _local_client.Clock;
		_scene.Rpc<string, LeaderBoardDtO> ("update_leaderBoard", "").Subscribe (DtO => {
			onUpdateLeaderBoard (DtO);});
	}

	public void GameOver()
	{
		UniRx.MainThreadDispatcher.Post (() => {
			if (_isPlaying == true)
			{
				_isPlaying = false;
				gameOverPanel.gameObject.SetActive(true);
			}
		});
	}

	public void restartGame()
	{
		gameOverPanel.gameObject.SetActive (false);
		foreach (Image img in lifeImgs)
			img.enabled = true;
		_isPlaying = true;
		_scene.Rpc<string, string>("update_leaderBoard", "");
	}

	public void onEnableBloom()
	{
		Bloom bloom = mainCamera.GetComponent<Bloom>();
		if (bloom.enabled == false)
			bloom.enabled = true;
		else
			bloom.enabled = false;
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
			long time = reader.ReadInt64();
			int ot = reader.ReadInt32();

			_ballsToCreate.Add(new Ball(x, y, vx, vy, id, time, ot));
		}
		catch (Exception e)
		{
			Debug.Log ("error while reading ball data");
		}
	}

//	public void onCreateBalls(Packet<IScenePeer> packet)
//	{
//		try
//		{
//			BallDtO data = packet.ReadObject<BallDtO>();
//			_ballsToCreate.Add(new Ball(data));
//		}
//		catch (Exception e)
//		{
//			Debug.Log ("error while reading ball data");
//		}
//	}

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

	public void updateLeaderBoard()
	{
		if (_lbDtO != null)
		{
			leaderboard.updateLeaderBoard(_lbDtO);
			_lbDtO = null;
		}
		if (_lastUpdate + 1000 < _local_client.Clock && _scene != null && _scene.Connected)
		{
			_lastUpdate = _local_client.Clock;
			_scene.Rpc<string, LeaderBoardDtO> ("update_leaderBoard", "").Subscribe (DtO => {
				onUpdateLeaderBoard (DtO);});
		}
	}

	void Update ()
	{   
		Connect();
		updateLeaderBoard();
		if (_isPlaying == true) 
		{
			if (Input.GetMouseButtonDown (0) || (Input.touchSupported && Input.GetTouch(0).phase == TouchPhase.Began))
			{
				Ray ray;
				if (Input.touchSupported && Input.GetTouch(0).phase == TouchPhase.Began)
					ray = mainCamera.ScreenPointToRay(Input.GetTouch(0).position);
				else
					ray = mainCamera.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;
				Physics.Raycast(ray, out hit);
				_testRPC = _local_client.Clock;
				_scene.Rpc("click", s => {
					BinaryWriter writer = new BinaryWriter(s, Encoding.UTF8);
					writer.Write(hit.point.x);
					writer.Write(hit.point.y);
					writer.Write(_local_client.Clock);
				}
				).Subscribe( rp => {
					onClick(rp);});
			}
		}
		while (_ballsToCreate.Count > 0)
		{
			Ball temp = _ballsToCreate.First();
			if (!_balls.ContainsKey(temp.id))
			{
				GameObject newBallGO = Instantiate (ballTemplate, new Vector3 (temp.x, temp.y, 0), Quaternion.identity) as GameObject;
				newBallGO.GetComponent<Rigidbody2D> ().AddForce (new Vector2 (temp.vx, temp.vy));
				newBallGO.AddComponent<ball_behaviour>();
				newBallGO.GetComponent<ball_behaviour>().creation_time = temp.creation_time;
				newBallGO.GetComponent<ball_behaviour>().oscillation_time = temp.oscillation_time;
				newBallGO.GetComponent<ball_behaviour>().local = _local_client;
				_balls.Add (temp.id, newBallGO);
			}
			_ballsToCreate.Remove(_ballsToCreate.First());
		}
		while (_ballsToDestroy.Count > 0)
		{
			int temp = _ballsToDestroy.First();
			if (_balls.ContainsKey(temp))
			{
				if (_balls[temp] != null)
					_balls[temp].GetComponent<ball_behaviour>().isDead = true;
				_balls.Remove(temp);
			}
			_ballsToDestroy.Remove(temp);
		}
	}

	void OnApplicationQuit()
	{
		if (_scene != null)
		{
			_scene.Disconnect();
		}
	}
}
