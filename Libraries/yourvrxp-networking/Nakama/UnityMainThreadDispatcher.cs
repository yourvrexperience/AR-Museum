using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace yourvrexperience.Networking
{
	public class UnityMainThreadDispatcher : MonoBehaviour {

		private static readonly Queue<Action> _executionQueue = new Queue<Action>();

		public void Update() {
			lock(_executionQueue) {
				while (_executionQueue.Count > 0) {
					_executionQueue.Dequeue().Invoke();
				}
			}
		}

		public void Clear()
		{
			_executionQueue.Clear();
		}

		public void Enqueue(IEnumerator action) {
			lock (_executionQueue) {
				_executionQueue.Enqueue (() => {
					StartCoroutine (action);
				});
			}
		}

		public void Enqueue(Action action)
		{
			Enqueue(ActionWrapper(action));
		}
		
		public Task EnqueueAsync(Action action)
		{
			var tcs = new TaskCompletionSource<bool>();

			void WrappedAction() {
				try 
				{
					action();
					tcs.TrySetResult(true);
				} catch (Exception ex) 
				{
					tcs.TrySetException(ex);
				}
			}

			Enqueue(ActionWrapper(WrappedAction));
			return tcs.Task;
		}

		
		IEnumerator ActionWrapper(Action a)
		{
			a();
			yield return null;
		}


		private static UnityMainThreadDispatcher _instance = null;

		public static bool Exists() {
			return _instance != null;
		}

		public static UnityMainThreadDispatcher Instance() {
			if (!Exists ()) {
				GameObject container = new GameObject();
				DontDestroyOnLoad(container);
				container.name = "UnityMainThreadDispatcher";
				_instance = container.AddComponent(typeof(UnityMainThreadDispatcher)) as UnityMainThreadDispatcher;
			}
			return _instance;
		}


		void Awake() {
			if (_instance == null) {
				_instance = this;
				DontDestroyOnLoad(this.gameObject);
			}
		}

		void OnDestroy() {
				_instance = null;
		}
	}
}