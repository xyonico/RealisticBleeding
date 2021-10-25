using System;
using System.Collections;
using UnityEngine;
using Action = System.Action;

namespace RealisticBleeding
{
	public class LevelModule : ThunderRoad.LevelModule
	{
		private LevelModule()
		{
			try
			{
				EntryPoint.OnLoaded();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		public override IEnumerator OnLoadCoroutine()
		{
			var hook = level.gameObject.AddComponent<FixedUpdateHook>();
			hook.FixedUpdateEvent += EntryPoint.OnFixedUpdate;
			
			yield break;
		}

		public override void Update()
		{
			EntryPoint.OnUpdate();
		}

		public class FixedUpdateHook : MonoBehaviour
		{
			public event Action FixedUpdateEvent;
			
			private void FixedUpdate()
			{
				FixedUpdateEvent?.Invoke();
			}
		}
	}
}