using System.Collections;
using ThunderRoad;
using UnityEngine;
using Action = System.Action;

namespace RealisticBleeding
{
	public class LevelModule : ThunderRoad.LevelModule
	{
		private LevelModule()
		{
			EntryPoint.OnLoaded();
		}

		public override IEnumerator OnLoadCoroutine(Level level)
		{
			var hook = level.gameObject.AddComponent<FixedUpdateHook>();
			hook.FixedUpdateEvent += EntryPoint.OnFixedUpdate;
			
			yield break;
		}

		public override void Update(Level level)
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