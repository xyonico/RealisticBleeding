using HarmonyLib;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
	internal static class EntryPoint
	{
		private const string HarmonyID = "com.xyonico.realistic-bleeding";
		private const float BloodIndexUpdateCycleDuration = 12;
		private const int OuterRangeCount = 12;

		private static bool _hasLoaded;
		private static int _innerRangeIndex;
		private static float _bloodIndexUpdateCycleProgress;

		public static Configuration Configuration { get; private set; }

		internal static void OnLoaded(Configuration configuration)
		{
			Configuration = configuration;

			if (_hasLoaded) return;
			_hasLoaded = true;

			Debug.Log("Realistic Bleeding loaded!");

			var harmony = new Harmony(HarmonyID);
			harmony.PatchAll(typeof(EntryPoint).Assembly);
		}

		internal static void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.T))
			{
				var cam = Spectator.local.cam.transform;
				const float hitRange = 10f;

				var layerMask = LayerMask.GetMask(nameof(LayerName.Avatar), nameof(LayerName.Ragdoll), nameof(LayerName.NPC),
					nameof(LayerName.PlayerHandAndFoot));

				if (Physics.Raycast(cam.position, cam.forward, out var hit, hitRange, layerMask))
				{
					var rigidbody = hit.collider.attachedRigidbody;

					if (rigidbody == null) return;

					if (rigidbody.TryGetComponent(out RagdollPart part))
					{
						SpawnBloodDrop(hit.point, layerMask);
						var creature = part.ragdoll.creature;

						//NoseBleed.SpawnOn(creature, 1, 1);
						//MouthBleed.SpawnOn(creature, 1, 1);
					}
				}
			}
		}

		internal static void OnFixedUpdate()
		{
			// This isn't very readable currently. I'm trying to limit the amount of droplets that are updated per frame.
			// Instead of spreading it out evenly, which causes all of them to slow down, I update on a cycle.
			// That way, all the droplets get some time where they can update nearly every frame before slowing down and completely stopping.
			_bloodIndexUpdateCycleProgress += Time.deltaTime / BloodIndexUpdateCycleDuration;
			_bloodIndexUpdateCycleProgress %= 1;

			if (BloodDrop.ActiveBloodDrops.Count == 0) return;

			var updateCount = 0;

			var outerRangeCount = Mathf.Min(OuterRangeCount, BloodDrop.ActiveBloodDrops.Count);

			var outerRangeStart = Mathf.FloorToInt(BloodDrop.ActiveBloodDrops.Count * _bloodIndexUpdateCycleProgress);

			_innerRangeIndex %= outerRangeCount;
			
			var startIndex = _innerRangeIndex;

			do
			{
				var index = outerRangeStart + _innerRangeIndex;
				index %= outerRangeCount;
				
				var currentDrop = BloodDrop.ActiveBloodDrops[index];

				if (currentDrop.DoUpdate())
				{
					updateCount++;
				}

				_innerRangeIndex++;

				if (_innerRangeIndex >= outerRangeCount)
				{
					_innerRangeIndex = 0;
				}
			} while (updateCount < Configuration.MaxActiveBloodDrips && _innerRangeIndex != startIndex);
		}

		private static void SpawnBloodDrop(Vector3 position, int layerMask)
		{
			var bloodDropObject = new GameObject("Blood Drop");
			var bloodDrop = bloodDropObject.AddComponent<BloodDrop>();
			var decalDrawer = bloodDropObject.AddComponent<BloodDropDecalDrawer>();

			bloodDrop.SurfaceLayerMask = layerMask;

			bloodDrop.transform.position = position;
			bloodDrop.AttachToNearestCollider(0.2f);
		}
	}
}