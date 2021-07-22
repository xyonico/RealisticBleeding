using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
	public class Bleeder : MonoBehaviour
	{
		private static bool _hasAssignedLayerMask;
		private static int _layerMask;

		private const float FrequencyRangeMin = 0.4f;
		private const float FrequencyRangeMax = 2.5f;
		private const float DurationRangeMin = 2;
		private const float DurationRangeMax = 5;
		private const float RandomSpawnOffsetMax = 0.015f;

		public float FrequencyMultiplier { get; set; } = 1;
		public float DurationMultiplier { get; set; } = 1;
		public float SizeMultiplier { get; set; } = 1;

		private float _timer;
		private float _nextDropTime;
		private float _durationRemaining;

		private void Awake()
		{
			_durationRemaining = Random.Range(DurationRangeMin, DurationRangeMax) * DurationMultiplier;
			
			if (!_hasAssignedLayerMask)
			{
				_layerMask = LayerMask.GetMask(nameof(LayerName.Avatar), nameof(LayerName.Ragdoll), nameof(LayerName.NPC),
					nameof(LayerName.PlayerHandAndFoot));
				
				_hasAssignedLayerMask = true;
			}
		}

		private void Update()
		{
			var deltaTime = Time.deltaTime;

			_timer += deltaTime;

			if (_timer > _nextDropTime)
			{
				_timer -= _nextDropTime;

				_nextDropTime = Random.Range(FrequencyRangeMin, FrequencyRangeMax) * FrequencyMultiplier;
				
				SpawnDroplet();
			}

			_durationRemaining -= deltaTime;

			if (_durationRemaining <= 0)
			{
				Destroy(gameObject);
			}
		}

		private void SpawnDroplet()
		{
			Debug.Log("Spawning droplet");
			var bloodDropObject = new GameObject("Blood Drop");
			var bloodDrop = bloodDropObject.AddComponent<BloodDrop>();
			var decalDrawer = bloodDropObject.AddComponent<BloodDropDecalDrawer>();

			bloodDrop.LayerMask = _layerMask;

			var spawnPos = transform.position;

			var randomOffset = Random.insideUnitSphere * RandomSpawnOffsetMax;

			bloodDrop.transform.position = spawnPos + randomOffset;
			bloodDrop.AttachToNearestCollider(0.2f);
		}
	}
}