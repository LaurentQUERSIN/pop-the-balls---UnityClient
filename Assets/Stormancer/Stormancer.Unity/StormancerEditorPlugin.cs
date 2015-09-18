using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Stormancer;
using Stormancer.Core;
using Stormancer.Plugins;

namespace Stormancer.EditorPlugin
{
	public class StormancerEditorPlugin
	{

		private Client _client;
		private string _id = Guid.NewGuid().ToString();

		public void Build(PluginBuildContext ctx)
		{

			ctx.ClientCreated += client =>
			{

				var innerLoggerFactory = client.GetComponentFactory<ILogger>();
				client.RegisterComponent<ILogger>(()=> new InterceptorLogger(this,innerLoggerFactory()));
				_client = client;
				StormancerEditorDataCollector.Instance.clients.TryAdd(_id, new StormancerClientViewModel(client));
			};

			ctx.ClientDestroyed += client =>
			{
				StormancerClientViewModel temp;
				StormancerEditorDataCollector.Instance.clients.TryRemove(_id, out temp);
			};

			ctx.SceneCreated +=  scene =>
			{
				StormancerClientViewModel temp;
				StormancerEditorDataCollector.Instance.clients[_id].scenes.TryAdd(scene.Id, new StormancerSceneViewModel(scene));
			};

			ctx.SceneConnected += scene =>
			{
				StormancerEditorDataCollector.Instance.clients[_id].scenes[scene.Id].connected = true;
			};

			ctx.SceneDisconnected += scene =>
			{
				StormancerEditorDataCollector.Instance.clients[_id].scenes[scene.Id].connected = false;
			};

			ctx.RouteCreated += (scene, route) =>
			{
				StormancerEditorDataCollector.Instance.clients[_id].scenes[scene.Id].routes.Enqueue(route);
			};
		}

		private class InterceptorLogger : ILogger
		{
			private readonly StormancerEditorPlugin _plugin;
			private readonly ILogger _innerLogger;
			public InterceptorLogger(StormancerEditorPlugin plugin,ILogger innerLogger)
			{
				_innerLogger = innerLogger;
				_plugin = plugin;
			}


			#region ILogger implementation
			public void Trace (string message, params object[] p)
			{
				StormancerEditorDataCollector.Instance.clients[_id].log.Enqueue("trace", string.Format(message, p).ToString());
				_innerLogger.Trace(message,p);
			}
			public void Debug (string message, params object[] p)
			{
				StormancerEditorDataCollector.Instance.clients[_id].log.Enqueue("Debug", string.Format(message, p).ToString());
				throw new NotImplementedException ();
			}
			public void Error (Exception ex)
			{
				StormancerEditorDataCollector.Instance.clients[_id].log.Enqueue("Error", ex.Message);
				throw new NotImplementedException ();
			}
			public void Error (string format, params object[] p)
			{
				StormancerEditorDataCollector.Instance.clients[_id].log.Enqueue("Error", string.Format(message, p).ToString());
				throw new NotImplementedException ();
			}
			public void Info (string format, params object[] p)
			{
				StormancerEditorDataCollector.Instance.clients[_id].log.Enqueue("Info", string.Format(message, p).ToString());
				throw new NotImplementedException ();
			}
			#endregion
		}

	}


}