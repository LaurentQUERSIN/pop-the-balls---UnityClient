using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Stormancer;
using Stormancer.Core;

namespace Stormancer.EditorPlugin
{
	public class StormancerClientViewModel
	{
		public Client client;
		public ConcurrentDictionary<string, StormancerSceneViewModel> scenes = new ConcurrentDictionary<string, StormancerSceneViewModel>();
		public ConcurrentQueue<StormancerEditorLog> log = new ConcurrentQueue<StormancerEditorLog>();

		public StormancerClientViewModel(Client clt)
		{
			client = clt;
		}
	}

	public class StormancerSceneViewModel
	{
		public Scene scene;
		public bool connected = false;
		public ConcurrentQueue<string> routes = new ConcurrentQueue<string>();

		public StormancerSceneViewModel(Scene scn)
		{
			scene = scn;
		}
	}

	public struct StormancerEditorLog
	{
		public string context;
		public string message;
	}

	public class StormancerEditorDataCollector
	{
		private StormancerEditorDataCollector _instance;
		public StormancerEditorDataCollector Instance
		{
			get
			{
				if (_instance == null)
					_instance = new StormancerEditorDataCollector();
				return _instance;
			}
		}

		public ConcurrentDictionary<string, StormancerClientViewModel> clients = new ConcurrentDictionary<string, StormancerClientViewModel>();

	}
}