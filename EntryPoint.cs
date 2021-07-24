using HarmonyLib;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
	internal static class EntryPoint
	{
		private const string HarmonyID = "com.xyonico.realistic-bleeding";
		private static bool _hasLoaded;

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
					}
				}
			}
		}

		private static void SpawnBloodDrop(Vector3 position, int layerMask)
		{
			var bloodDropObject = new GameObject("Blood Drop");
			var bloodDrop = bloodDropObject.AddComponent<BloodDrop>();
			var decalDrawer = bloodDropObject.AddComponent<BloodDropDecalDrawer>();

			bloodDrop.LayerMask = layerMask;

			bloodDrop.transform.position = position;
			bloodDrop.AttachToNearestCollider(0.2f);
		}
	}
}